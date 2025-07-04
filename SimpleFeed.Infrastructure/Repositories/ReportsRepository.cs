using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;
using SimpleFeed.Domain.Entities;
using SimpleFeed.Domain.Enums;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class ReportsRepository : IReportsRepository
    {
        private readonly string _connectionString;

        public ReportsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> LogClientActionAsync(int clientId, int formId, ClientActionType actionType, object details, RelatorioDTO report, string rangeDataRequested)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                int? reportId = null;

                // 1. Inserir na tabela ai_reports e retornar o id
                if (actionType == ClientActionType.AiAnalysis)
                {
                    const string insertReport = @"
                        INSERT INTO ai_reports (client_id, form_id, created_at, report, range_data_solicited)
                        VALUES (@ClientId, @FormId, NOW(), @Report, @RangeDataSolicited)
                        RETURNING id;
                    ";

                    await using var reportCmd = new NpgsqlCommand(insertReport, connection, transaction);
                    reportCmd.Parameters.AddWithValue("@ClientId", clientId);
                    reportCmd.Parameters.AddWithValue("@FormId", formId);
                    reportCmd.Parameters.AddWithValue("@Report", JsonSerializer.Serialize(report));
                    reportCmd.Parameters.AddWithValue("@RangeDataSolicited", rangeDataRequested);

                    var result = await reportCmd.ExecuteScalarAsync();
                    reportId = result != null ? Convert.ToInt32(result) : (int?)null;
                }

                // 2. Inserir na tabela client_action_logs
                const string insertLog = @"
                    INSERT INTO client_action_logs (client_id, action_id, form_id, timestamp, details)
                    VALUES (@ClientId, @ActionId, @FormId, NOW(), @Details);
                ";

                await using var logCmd = new NpgsqlCommand(insertLog, connection, transaction);
                logCmd.Parameters.AddWithValue("@ClientId", clientId);
                logCmd.Parameters.AddWithValue("@ActionId", (int)actionType);
                logCmd.Parameters.AddWithValue("@FormId", formId);
                logCmd.Parameters.AddWithValue("@Details", JsonSerializer.Serialize(details));

                await logCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                return reportId ?? 0; // Retorna o ID do relatório ou 0 se não houver
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Erro ao registrar ação do cliente: " + ex.Message);
            }
        }


        public async Task<(int ClientId, string FormName, IEnumerable<FeedbackDetailDto> Feedbacks, IEnumerable<FormFieldDto> Fields)>
        GetReportDataAsync(int formId, DateTime? dataInicio, DateTime? dataFim, int clientId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Buscar o nome do formulário
            string formName = string.Empty;
            var formNameCmd = new NpgsqlCommand("SELECT name FROM forms WHERE id = @FormId", connection);
            formNameCmd.Parameters.AddWithValue("@FormId", formId);
            using (var reader = await formNameCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    formName = reader.GetString(0);
                }
            }

            var feedbacks = new List<FeedbackDetailDto>();
            string feedbackQuery = @"
                SELECT answers::TEXT, submitted_at 
                FROM feedbacks 
                WHERE form_id = @FormId AND client_id = @ClientId";

            if (dataInicio.HasValue && dataFim.HasValue)
            {
                feedbackQuery += " AND submitted_at BETWEEN @Start AND @End";
            }

            var feedbackCmd = new NpgsqlCommand(feedbackQuery, connection);
            feedbackCmd.Parameters.AddWithValue("@FormId", formId);
            feedbackCmd.Parameters.AddWithValue("@ClientId", clientId);
            if (dataInicio.HasValue && dataFim.HasValue)
            {
                feedbackCmd.Parameters.AddWithValue("@Start", dataInicio.Value);
                feedbackCmd.Parameters.AddWithValue("@End", dataFim.Value);
            }

            using (var reader = await feedbackCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    feedbacks.Add(new FeedbackDetailDto
                    {
                        Answers = reader.GetString(0),
                        Submitted_At = reader.GetDateTime(1)
                    });
                }
            }

            var fields = new List<FormFieldDto>();
            var fieldCmd = new NpgsqlCommand("SELECT id, name, type FROM form_fields WHERE form_id = @FormId", connection);
            fieldCmd.Parameters.AddWithValue("@FormId", formId);
            using (var reader = await fieldCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    fields.Add(new FormFieldDto
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Type = reader.GetString(2)
                    });
                }
            }

            return (clientId, formName, feedbacks, fields);
        }

        public async Task<List<ReportsIAs>> GetReportsIaAsync(int formId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var reports = new List<ReportsIAs>();

                string query = @"
                    SELECT ar.id, ar.client_id, ar.form_id, ar.created_at, ar.range_data_solicited
                    FROM ai_reports ar
                    WHERE ar.form_id = @FormId";

                if (startDate.HasValue && endDate.HasValue)
                {
                    // Ajusta o endDate para o final do dia, se a hora for 00:00:00
                    if (endDate.Value.TimeOfDay == TimeSpan.Zero)
                    {
                        endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    }
                    query += " AND ar.created_at BETWEEN @StartDate AND @EndDate";
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@FormId", formId);

                if (startDate.HasValue && endDate.HasValue)
                {
                    command.Parameters.AddWithValue("@StartDate", startDate.Value);
                    command.Parameters.AddWithValue("@EndDate", endDate.Value);
                }

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    reports.Add(new ReportsIAs
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        ClientId = reader.GetInt32(reader.GetOrdinal("client_id")),
                        FormId = reader.GetInt32(reader.GetOrdinal("form_id")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                        RangeDataSolicited = reader["range_data_solicited"]?.ToString()
                    });
                }

                await reader.CloseAsync();

                return reports;
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao recuperar os relatórios IA.", ex);
            }
        }

        public async Task<DetailReportIA> GetReportByIdAsync(int reportId)
        {
            try
            {
                var query = @"
                SELECT id, form_id, created_at, range_data_solicited, report
                FROM ai_reports
                WHERE id = @ReportId";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ReportId", reportId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new DetailReportIA
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        FormId = reader.GetInt32(reader.GetOrdinal("form_id")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                        RangeDataSolicited = reader["range_data_solicited"]?.ToString() ?? string.Empty,
                        Report = reader["report"]?.ToString() ?? string.Empty
                    };
                }

                return null; // Retorna null se não encontrar o relatório
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao recuperar o relatório.", ex);
            }
        }


        public async Task<IAReportCreationStatusDto> GetServicesAvailableByPlanAsync(string clientGuid)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                SELECT
                p.id AS plan_id,
                p.name AS plan_name,
                COALESCE((p.ai_reports_per_form * p.max_forms), 0) AS ai_reports_per_form,
                COALESCE(SUM(CASE WHEN ar.created_at >= DATE_TRUNC('month', CURRENT_DATE) THEN 1 ELSE 0 END), 0) AS total_ai_reports_month
                FROM clients c
                INNER JOIN plans p ON p.id = c.""PlanId""
                LEFT JOIN ai_reports ar ON ar.client_id = c.""Id""
                AND ar.created_at >= DATE_TRUNC('month', CURRENT_DATE)
                AND ar.created_at < DATE_TRUNC('month', CURRENT_DATE + INTERVAL '1 month')
                WHERE c.""UserId"" = @clientGuid
                GROUP BY p.id, p.name, p.ai_reports_per_form;
            ";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@clientGuid", clientGuid);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var planoId = reader.GetInt32(reader.GetOrdinal("plan_id"));
                    var planoNome = reader.GetString(reader.GetOrdinal("plan_name"));
                    var limiteRelatorios = reader.GetInt32(reader.GetOrdinal("ai_reports_per_form"));
                    var totalRelatoriosMes = reader.GetInt32(reader.GetOrdinal("total_ai_reports_month"));

                    // Se o plano for Free, não pode exceder
                    bool podeExceder = planoId == 1 ? false : true;
                    bool cobrancaExtra = limiteRelatorios > 0 && totalRelatoriosMes >= limiteRelatorios;

                    return new IAReportCreationStatusDto
                    {
                        PlanoId = planoId,
                        PlanoNome = planoNome,
                        LimiteRelatoriosIAMes = limiteRelatorios,
                        TotalRelatoriosIAMes = totalRelatoriosMes,
                        PodeExcederRelatorios = podeExceder,
                        CriacaoGeraraCobranca = cobrancaExtra
                    };
                }

                throw new Exception("Cliente não encontrado ou sem plano.");
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar status de criação de relatório IA.", ex);
            }
        }
    }

}