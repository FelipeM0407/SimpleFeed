using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;
using SimpleFeed.Domain.Enums;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class BillingSummaryRepository : IBillingSummaryRepository
    {
        private readonly string _connectionString;

        public BillingSummaryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task<int> LogClientActionAsync(
                    NpgsqlConnection connection,
                    NpgsqlTransaction transaction,
                    int clientId,
                    int formId,
                    ClientActionType actionType,
                    object details)
        {
            try
            {
                var insertLogQuery = @"
            INSERT INTO client_action_logs (client_id, action_id, form_id, timestamp, details)
            VALUES (@ClientId, @ActionId, @FormId, NOW(), @Details);";

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(details, jsonOptions);

                using (var logCmd = new NpgsqlCommand(insertLogQuery, connection, transaction))
                {
                    logCmd.Parameters.AddWithValue("@ClientId", clientId);
                    logCmd.Parameters.AddWithValue("@ActionId", (int)actionType); // enum → ID direto
                    logCmd.Parameters.AddWithValue("@FormId", formId);
                    logCmd.Parameters.AddWithValue("@Details", json);
                    return await logCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao registrar a ação do cliente.", ex);
            }
        }
        public async Task<BillingSummaryDto> GetBillingSummaryAsync(int clientId, DateTime referenceMonth)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            WITH
            forms_cobranca AS (
            SELECT f.id, f.client_id
            FROM forms f
            LEFT JOIN form_settings fs ON fs.form_id = f.id
            WHERE (
                f.is_active = TRUE
                OR (f.is_active = FALSE AND f.updated_at >= DATE_TRUNC('month', @referenceMonth))
            )
            AND f.created_at < DATE_TRUNC('month', @referenceMonth + INTERVAL '1 month')
            AND (
                fs.inativation_date IS NULL
                OR fs.inativation_date::date != DATE_TRUNC('month', @referenceMonth)::date
            )
            ),

            respostas_ate_mes AS (
            SELECT client_id, COUNT(*) AS total_responses
            FROM feedbacks
            WHERE submitted_at < DATE_TRUNC('month', @referenceMonth + INTERVAL '1 month')
            GROUP BY client_id
            ),

            ia_reports_mes AS (
            SELECT client_id, COUNT(*) AS total_ai_reports
            FROM ai_reports
            WHERE created_at >= DATE_TRUNC('month', @referenceMonth)
              AND created_at < DATE_TRUNC('month', @referenceMonth + INTERVAL '1 month')
            GROUP BY client_id
            )

            SELECT
            c.""Id"" AS client_id,
            p.id AS plan_id,
            DATE_TRUNC('month', @referenceMonth) AS reference_month,

            COUNT(DISTINCT fc.id) AS total_forms_mes,
            COALESCE(p.max_forms, null) AS forms_dentro_plano,
            GREATEST(COUNT(DISTINCT fc.id) - COALESCE(p.max_forms, 0), 0) AS forms_excedentes,

            COALESCE(rm.total_responses, 0) AS total_respostas_armazenadas,
            COALESCE(p.max_responses, null) AS respostas_dentro_plano,
            GREATEST(COALESCE(rm.total_responses, 0) - COALESCE(p.max_responses, 0), 0) AS respostas_excedentes,

            COALESCE(ia.total_ai_reports, 0) AS total_ai_reports,
                COALESCE((p.ai_reports_per_form * p.max_forms), null) AS ai_reports_limite,
            GREATEST(
                COALESCE(ia.total_ai_reports, 0) - (COUNT(DISTINCT fc.id) * COALESCE(p.ai_reports_per_form, 0)),
                0
            ) AS extra_ai_reports,

            GREATEST(COUNT(DISTINCT fc.id) - COALESCE(p.max_forms, 0), 0) * COALESCE(pr_form.price, 0) AS form_excess_charge,
            GREATEST(COALESCE(rm.total_responses, 0) - COALESCE(p.max_responses, 0), 0) /
                COALESCE(NULLIF(pr_resp.unit_size, 0), 100) * COALESCE(pr_resp.price, 0) AS response_excess_charge,
            GREATEST(
                COALESCE(ia.total_ai_reports, 0) - (COUNT(DISTINCT fc.id) * COALESCE(p.ai_reports_per_form, 0)),
                0
            ) * COALESCE(pr_ai.price, 0) AS ai_report_excess_charge,

            CASE
                WHEN p.plan_type = 'usage_based' THEN (
                GREATEST(COUNT(DISTINCT fc.id) - COALESCE(p.max_forms, 0), 0) * COALESCE(pr_form.price, 0) +
                GREATEST(COALESCE(rm.total_responses, 0) - COALESCE(p.max_responses, 0), 0) /
                    COALESCE(NULLIF(pr_resp.unit_size, 0), 100) * COALESCE(pr_resp.price, 0) +
                GREATEST(
                    COALESCE(ia.total_ai_reports, 0) - (COUNT(DISTINCT fc.id) * COALESCE(p.ai_reports_per_form, 0)),
                    0
                ) * COALESCE(pr_ai.price, 0)
                )
                ELSE (
                p.base_price +
                GREATEST(COUNT(DISTINCT fc.id) - COALESCE(p.max_forms, 0), 0) * COALESCE(pr_form.price, 0) +
                GREATEST(COALESCE(rm.total_responses, 0) - COALESCE(p.max_responses, 0), 0) /
                    COALESCE(NULLIF(pr_resp.unit_size, 0), 100) * COALESCE(pr_resp.price, 0) +
                GREATEST(
                    COALESCE(ia.total_ai_reports, 0) - (COUNT(DISTINCT fc.id) * COALESCE(p.ai_reports_per_form, 0)),
                    0
                ) * COALESCE(pr_ai.price, 0)
                )
            END AS valor_fatura_ate_agora,
            p.base_price as valor_base_fatura,
            p.name as nome_plano

            FROM clients c
            JOIN plans p ON p.id = c.""PlanId""
            LEFT JOIN pricing_rules pr_form ON pr_form.plan_id = p.id AND pr_form.item = 'form'
            LEFT JOIN pricing_rules pr_resp ON pr_resp.plan_id = p.id AND pr_resp.item = 'response_pack'
            LEFT JOIN pricing_rules pr_ai ON pr_ai.plan_id = p.id AND pr_ai.item = 'ai_report'
            LEFT JOIN forms_cobranca fc ON fc.client_id = c.""Id""
            LEFT JOIN respostas_ate_mes rm ON rm.client_id = c.""Id""
            LEFT JOIN ia_reports_mes ia ON ia.client_id = c.""Id""

            WHERE c.""Id"" = @clientId
            GROUP BY
            c.""Id"", p.id, p.plan_type, p.base_price, p.max_forms, p.max_responses, p.ai_reports_per_form,
            rm.total_responses, ia.total_ai_reports,
            pr_form.price, pr_resp.price, pr_resp.unit_size, pr_ai.price;
        ";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@clientId", clientId);
                command.Parameters.AddWithValue("@referenceMonth", referenceMonth);

                using var reader = await command.ExecuteReaderAsync();
                BillingSummaryDto? dto = null;

                if (await reader.ReadAsync())
                {
                    dto = new BillingSummaryDto
                    {
                        ClientId = reader.GetInt32(reader.GetOrdinal("client_id")),
                        PlanId = reader.GetInt32(reader.GetOrdinal("plan_id")),
                        ReferenceMonth = reader.GetDateTime(reader.GetOrdinal("reference_month")),
                        TotalFormsMes = reader.GetInt32(reader.GetOrdinal("total_forms_mes")),
                        FormsDentroPlano = reader.IsDBNull(reader.GetOrdinal("forms_dentro_plano")) ? 0 : reader.GetInt32(reader.GetOrdinal("forms_dentro_plano")),
                        FormsExcedentes = reader.GetInt32(reader.GetOrdinal("forms_excedentes")),
                        TotalRespostasArmazenadas = reader.GetInt32(reader.GetOrdinal("total_respostas_armazenadas")),
                        RespostasDentroPlano = reader.IsDBNull(reader.GetOrdinal("respostas_dentro_plano")) ? 0 : reader.GetInt32(reader.GetOrdinal("respostas_dentro_plano")),
                        RespostasExcedentes = reader.GetInt32(reader.GetOrdinal("respostas_excedentes")),
                        TotalAiReports = reader.GetInt32(reader.GetOrdinal("total_ai_reports")),
                        AiReportsLimite = reader.IsDBNull(reader.GetOrdinal("ai_reports_limite")) ? 0 : reader.GetInt32(reader.GetOrdinal("ai_reports_limite")),
                        ExtraAiReports = reader.GetInt32(reader.GetOrdinal("extra_ai_reports")),
                        FormExcessCharge = reader.GetDecimal(reader.GetOrdinal("form_excess_charge")),
                        ResponseExcessCharge = reader.GetDecimal(reader.GetOrdinal("response_excess_charge")),
                        AiReportExcessCharge = reader.GetDecimal(reader.GetOrdinal("ai_report_excess_charge")),
                        ValorFaturaAteAgora = reader.GetDecimal(reader.GetOrdinal("valor_fatura_ate_agora")),
                        ValorBaseFatura = reader.GetDecimal(reader.GetOrdinal("valor_base_fatura")),
                        NomePlano = reader.GetString(reader.GetOrdinal("nome_plano"))
                    };
                }

                return dto!;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao calcular o resumo da fatura.", ex);
            }
        }

        public async Task<BillingSummaryDto?> GetBillingSummaryFromStoredAsync(int clientId, DateTime referenceMonth)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
            SELECT *
            FROM billing_summary
            WHERE client_id = @clientId
              AND reference_month = DATE_TRUNC('month', @referenceMonth)
            LIMIT 1;
        ";

                await using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@clientId", clientId);
                command.Parameters.AddWithValue("@referenceMonth", referenceMonth);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new BillingSummaryDto
                    {
                        ClientId = reader.GetInt32(reader.GetOrdinal("client_id")),
                        PlanId = reader.GetInt32(reader.GetOrdinal("plan_id")),
                        NomePlano = reader.GetString(reader.GetOrdinal("plan_name")),
                        ReferenceMonth = reader.GetDateTime(reader.GetOrdinal("reference_month")),
                        TotalFormsMes = reader.GetInt32(reader.GetOrdinal("total_forms")),
                        FormsDentroPlano = reader.GetInt32(reader.GetOrdinal("included_forms")),
                        FormsExcedentes = reader.GetInt32(reader.GetOrdinal("extra_forms")),
                        TotalRespostasArmazenadas = reader.GetInt32(reader.GetOrdinal("total_responses")),
                        RespostasDentroPlano = reader.GetInt32(reader.GetOrdinal("included_responses")),
                        RespostasExcedentes = reader.GetInt32(reader.GetOrdinal("extra_responses")),
                        TotalAiReports = reader.GetInt32(reader.GetOrdinal("total_ai_reports")),
                        AiReportsLimite = reader.GetInt32(reader.GetOrdinal("included_ai_reports")),
                        ExtraAiReports = reader.GetInt32(reader.GetOrdinal("extra_ai_reports")),
                        FormExcessCharge = reader.GetDecimal(reader.GetOrdinal("amount_forms")),
                        ResponseExcessCharge = reader.GetDecimal(reader.GetOrdinal("amount_responses")),
                        AiReportExcessCharge = reader.GetDecimal(reader.GetOrdinal("amount_ai_reports")),
                        ValorBaseFatura = reader.GetDecimal(reader.GetOrdinal("base_price")),
                        ValorFaturaAteAgora = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar histórico de faturas.", ex);
            }
        }

        public async Task<bool> MigratePlanAsync(int clientId, int newPlanId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
            await connection.OpenAsync();
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                // Buscar plano atual
                var getOldPlanCmd = new NpgsqlCommand(@"
                    SELECT p.id, p.name
                    FROM clients c
                    JOIN plans p ON p.id = c.""PlanId""
                    WHERE c.""Id"" = @ClientId
                ", connection, transaction);

                getOldPlanCmd.Parameters.AddWithValue("@ClientId", clientId);

                string? oldPlanName = null;
                int? oldPlanId = null;
                using (var reader = await getOldPlanCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                    oldPlanId = reader.GetInt32(0);
                    oldPlanName = reader.GetString(1);
                    }
                }

                if (oldPlanId == null || oldPlanId == newPlanId)
                {
                    await transaction.RollbackAsync();
                    return false; // Nenhuma mudança ou erro
                }

                // Buscar nome do novo plano
                var getNewPlanCmd = new NpgsqlCommand(@"SELECT name FROM plans WHERE id = @NewPlanId", connection, transaction);
                getNewPlanCmd.Parameters.AddWithValue("@NewPlanId", newPlanId);
                var newPlanName = (string?)await getNewPlanCmd.ExecuteScalarAsync();

                if (string.IsNullOrEmpty(newPlanName))
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Atualizar o plano
                var updateCmd = new NpgsqlCommand(@"
                    UPDATE clients SET ""PlanId"" = @NewPlanId, ""UpdatedAt"" = NOW() WHERE ""Id"" = @ClientId
                ", connection, transaction);

                updateCmd.Parameters.AddWithValue("@NewPlanId", newPlanId);
                updateCmd.Parameters.AddWithValue("@ClientId", clientId);
                var affected = await updateCmd.ExecuteNonQueryAsync();

                if (affected == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Inserir log de auditoria usando método de log
                var logDetails = new
                {
                    previous_plan = oldPlanName,
                    new_plan = newPlanName,
                    migrated_at = DateTime.UtcNow
                };
                await LogClientActionAsync(connection, transaction, clientId, 0, ClientActionType.MigratePlan, logDetails);

                await transaction.CommitAsync();
                return true;
                }
                catch
                {
                await transaction.RollbackAsync();
                throw new Exception("Erro ao migrar o plano do cliente.");
                }
            }
            }
        }


    }

}