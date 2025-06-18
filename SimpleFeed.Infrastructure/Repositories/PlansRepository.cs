using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;
using SimpleFeed.Domain.Entities.Enum;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class PlansRepository : IPlansRepository
    {
        private readonly string _connectionString;

        public PlansRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<PlansDto>> GetAllPlansAsync()
        {
            var plans = new Dictionary<int, PlansDto>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                SELECT 
                p.id, p.name, p.max_forms, p.max_responses, p.base_price, 
                p.ai_reports_per_form, p.ai_reports_discount, p.unlimited_responses, p.plan_type,
                pr.item, pr.unit_size, pr.price, pr.discounted_price
                FROM plans p
                LEFT JOIN pricing_rules pr ON pr.plan_id = p.id
                ORDER BY p.id, pr.item;
            ";

                using var command = new NpgsqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32(0);

                    if (!plans.ContainsKey(id))
                    {
                        plans[id] = new PlansDto
                        {
                            Id = id,
                            Name = reader.GetString(1),
                            MaxForms = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                            MaxResponses = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                            BasePrice = reader.GetDecimal(4),
                            AiReportsPerForm = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                            AiReportsDiscount = reader.GetBoolean(6),
                            UnlimitedResponses = reader.GetBoolean(7),
                            PlanType = reader.GetString(8),
                            Pricing = new List<PricingRuleDto>()
                        };
                    }

                    if (!reader.IsDBNull(9))
                    {
                        plans[id].Pricing.Add(new PricingRuleDto
                        {
                            Item = reader.GetString(9),
                            UnitSize = reader.GetInt32(10),
                            Price = reader.GetDecimal(11),
                            DiscountedPrice = reader.IsDBNull(12) ? null : reader.GetDecimal(12)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle as needed
                throw new Exception("Error fetching plans from the database.", ex);
            }

            return new List<PlansDto>(plans.Values);
        }

        public async Task<FormCreationStatusDto> GetServicesAvailableByPlanAsync(string clientGuid)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT
                p.id AS plan_id,
                p.name AS plan_name,
                COALESCE(p.max_forms, 0) AS max_forms,
                COUNT(DISTINCT f.id) AS active_forms
            FROM clients c
            INNER JOIN plans p ON p.id = c.""PlanId""
            LEFT JOIN forms f ON f.client_id = c.""Id""
                AND (f.is_active = TRUE OR (f.is_active = FALSE AND f.updated_at >= DATE_TRUNC('month', CURRENT_DATE)))
                AND f.created_at < DATE_TRUNC('month', CURRENT_DATE + INTERVAL '1 month')
            WHERE c.""UserId"" = @clientGuid
            GROUP BY p.id, p.name, p.max_forms;
        ";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@clientGuid", clientGuid);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var total = reader.GetInt32(reader.GetOrdinal("active_forms"));
                    var limite = reader.GetInt32(reader.GetOrdinal("max_forms"));
                    var planoNome = reader.GetString(reader.GetOrdinal("plan_name"));
                    var planoId = reader.GetInt32(reader.GetOrdinal("plan_id"));

                    // Se o plano for Free, não pode criar formulário
                    bool podeExceder = planoId == (int)Plans.Free ? false : true;
                    bool cobrancaExtra = limite > 0 && total >= limite;

                    return new FormCreationStatusDto
                    {
                        PlanoId = planoId,
                        PlanoNome = planoNome,
                        LimiteFormularios = limite,
                        TotalFormulariosAtivosMes = total,
                        PodeExcederFormulario = podeExceder,
                        CriacaoGeraraCobranca = cobrancaExtra
                    };
                }

                throw new Exception("Cliente não encontrado ou sem plano.");
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar status de criação de formulário.", ex);
            }
        }

        public async Task<FormCreationStatusDto> GetFormReactivationStatus(int formId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            WITH form_info AS (
                SELECT 
                    f.id,
                    f.client_id,
                    f.created_at,
                    f.updated_at
                FROM forms f
                WHERE f.id = @formId AND f.is_active = FALSE
                LIMIT 1
            ), plan_info AS (
                SELECT 
                    c.""PlanId"" AS plan_id,
                    p.name AS plan_name,
                    COALESCE(p.max_forms, 0) AS max_forms,
                    c.""Id"" AS client_id
                FROM clients c
                INNER JOIN plans p ON p.id = c.""PlanId""
                INNER JOIN form_info fi ON fi.client_id = c.""Id""
            ), forms_ativos_mes_atual AS (
                SELECT COUNT(*) AS total
                FROM forms f
                INNER JOIN form_info fi ON fi.client_id = f.client_id
                WHERE 
                    (f.is_active = TRUE OR (f.is_active = FALSE AND f.updated_at >= DATE_TRUNC('month', CURRENT_DATE)))
                    AND f.created_at < DATE_TRUNC('month', CURRENT_DATE + INTERVAL '1 month')
            )
            SELECT 
                pi.plan_id,
                pi.plan_name,
                pi.max_forms,
                fam.total AS active_forms,
                (
                    SELECT 
                        CASE 
                            WHEN fi.created_at >= DATE_TRUNC('month', CURRENT_DATE) 
                                 OR fi.updated_at >= DATE_TRUNC('month', CURRENT_DATE)
                            THEN TRUE ELSE FALSE
                        END
                    FROM form_info fi
                ) AS foi_cobrado_mes_atual
            FROM plan_info pi, forms_ativos_mes_atual fam;
        ";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@formId", formId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var planoId = reader.GetInt32(reader.GetOrdinal("plan_id"));
                    var planoNome = reader.GetString(reader.GetOrdinal("plan_name"));
                    var limite = reader.GetInt32(reader.GetOrdinal("max_forms"));
                    var total = reader.GetInt32(reader.GetOrdinal("active_forms"));
                    var foiCobradoMesAtual = reader.GetBoolean(reader.GetOrdinal("foi_cobrado_mes_atual"));

                    bool podeExceder = planoId != (int)Plans.Free;
                    bool cobrancaExtra = !foiCobradoMesAtual && limite > 0 && (total + 1) > limite;

                    return new FormCreationStatusDto
                    {
                        PlanoId = planoId,
                        PlanoNome = planoNome,
                        LimiteFormularios = limite,
                        TotalFormulariosAtivosMes = total,
                        PodeExcederFormulario = podeExceder,
                        CriacaoGeraraCobranca = cobrancaExtra
                    };
                }

                throw new Exception("Formulário não encontrado ou já está ativo.");
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao verificar status de reativação do formulário.", ex);
            }
        }

    }
}