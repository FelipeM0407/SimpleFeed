using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly string _connectionString;

        public FeedbackRepository(string connectionString)
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

        public async Task<IEnumerable<FeedbackDetailDto>> GetFeedbacksByFormAsync(int formId)
        {
            try
            {
                var feedbacks = new List<FeedbackDetailDto>();

                var query = @"
                    SELECT f.id, f.answers, f.submitted_at, f.is_new
                    FROM feedbacks f
                    WHERE f.form_id = @FormId";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FormId", formId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                feedbacks.Add(new FeedbackDetailDto
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    Answers = reader["answers"]?.ToString() ?? "[]",
                                    Submitted_At = reader.GetDateTime(reader.GetOrdinal("submitted_at")),
                                    IsNew = reader.GetBoolean(reader.GetOrdinal("is_new"))
                                });
                            }
                        }
                    }
                }

                return feedbacks;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao recuperar os feedbacks pelo ID do formulário.", ex);
            }
        }

        public async Task MarkFeedbacksAsReadAsync(int[] feedbacksId)
        {
            try
            {
                var query = @"
                    UPDATE feedbacks
                    SET is_new = FALSE
                    WHERE id = ANY(@FeedbackIds)";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FeedbackIds", feedbacksId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao marcar os feedbacks como lidos.", ex);
            }
        }

        public async Task<IEnumerable<FeedbackDetailDto>> FilterFeedbacksAsync(int formId, DateTime? submitted_Start, DateTime? submitted_End)
        {
            try
            {
                var feedbacks = new List<FeedbackDetailDto>();

                var query = @"
                    SELECT f.id, f.answers, f.submitted_at, f.is_new
                    FROM feedbacks f
                    WHERE f.form_id = @FormId";

                if (submitted_Start.HasValue && submitted_End.HasValue)
                {
                    query += " AND DATE(f.submitted_at) BETWEEN DATE(@SubmittedStart) AND DATE(@SubmittedEnd)";
                }

                query += " ORDER BY f.submitted_at DESC";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FormId", formId);

                        if (submitted_Start.HasValue && submitted_End.HasValue)
                        {
                            command.Parameters.AddWithValue("@SubmittedStart", submitted_Start);
                            command.Parameters.AddWithValue("@SubmittedEnd", submitted_End);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                feedbacks.Add(new FeedbackDetailDto
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    Answers = reader["answers"]?.ToString() ?? "[]",
                                    Submitted_At = reader.GetDateTime(reader.GetOrdinal("submitted_at")),
                                    IsNew = reader.GetBoolean(reader.GetOrdinal("is_new"))
                                });
                            }
                        }
                    }
                }

                return feedbacks;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao filtrar os feedbacks.", ex);
            }
        }

        public async Task DeleteFeedbacksAsync(int[] feedbackIds)
        {
            try
            {
            var getFormQuery = "SELECT form_id FROM feedbacks WHERE id = ANY(@FeedbackIds) LIMIT 1";
            var getFormNameQuery = "SELECT name FROM forms WHERE id = @FormId";
            var getClientQuery = "SELECT client_id FROM forms WHERE id = @FormId";
            var deleteQuery = "DELETE FROM feedbacks WHERE id = ANY(@FeedbackIds)";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                try
                {
                    int formId;
                    using (var getFormCmd = new NpgsqlCommand(getFormQuery, connection, transaction))
                    {
                    getFormCmd.Parameters.AddWithValue("@FeedbackIds", feedbackIds);
                    formId = (int)(await getFormCmd.ExecuteScalarAsync() ?? throw new Exception("Formulário não encontrado."));
                    }

                    string formName;
                    using (var getFormNameCmd = new NpgsqlCommand(getFormNameQuery, connection, transaction))
                    {
                    getFormNameCmd.Parameters.AddWithValue("@FormId", formId);
                    formName = (string)(await getFormNameCmd.ExecuteScalarAsync() ?? throw new Exception("Nome do formulário não encontrado."));
                    }

                    int clientId;
                    using (var getClientCmd = new NpgsqlCommand(getClientQuery, connection, transaction))
                    {
                    getClientCmd.Parameters.AddWithValue("@FormId", formId);
                    clientId = (int)(await getClientCmd.ExecuteScalarAsync() ?? throw new Exception("Cliente não encontrado."));
                    }

                    using (var deleteCmd = new NpgsqlCommand(deleteQuery, connection, transaction))
                    {
                    deleteCmd.Parameters.AddWithValue("@FeedbackIds", feedbackIds);
                    await deleteCmd.ExecuteNonQueryAsync();
                    }

                    await LogClientActionAsync(connection, transaction, clientId, formId, ClientActionType.ExcludeFeedback, new
                    {
                    form_id = formId,
                    form_name = formName,
                    deleted_count = feedbackIds.Length,
                    reason = "Exclusão solicitada manualmente"
                    });

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception("Erro ao deletar feedbacks com log.", ex);
                }
                }
            }
            }
            catch (Exception ex)
            {
            throw new Exception("Ocorreu um erro ao deletar os feedbacks.", ex);
            }
        }


        public async Task<int> GetNewFeedbacksCountAsync(int clientId)
        {
            try
            {
                var query = "SELECT COUNT(*) FROM feedbacks WHERE client_id = @ClientId AND is_new = TRUE";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        return Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao contar os feedbacks novos.", ex);
            }
        }

        public async Task<int> GetAllFeedbacksCountAsync(int clientId)
        {
            try
            {
                var query = "SELECT COUNT(*) FROM feedbacks WHERE client_id = @ClientId";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        return Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao contar todos os feedbacks.", ex);
            }
        }

        public async Task<int> GetTodayFeedbacksCountAsync(int clientId)
        {
            try
            {
                var query = "SELECT COUNT(*) FROM feedbacks WHERE client_id = @ClientId AND DATE(submitted_at) = CURRENT_DATE";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        return Convert.ToInt32(await command.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao contar os feedbacks de hoje.", ex);
            }
        }

        public async Task<List<FeedbacksChartDto>> GetFeedbacksCountLast30DaysByClientAsync(int clientId)
        {
            var feedbacksCount = new List<FeedbacksChartDto>();

            var query = @"
        WITH dates AS (
            SELECT generate_series(
            CURRENT_DATE - INTERVAL '29 days',
            CURRENT_DATE,
            INTERVAL '1 day'
            )::DATE AS day
        )
        SELECT 
            d.day,
            COUNT(f.id) AS count
        FROM dates d
        LEFT JOIN feedbacks f ON DATE(f.submitted_at) = d.day AND f.client_id = @ClientId
        GROUP BY d.day
        ORDER BY d.day;
        ";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", clientId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var date = reader.GetDateTime(reader.GetOrdinal("day"));
                            var count = reader.GetInt32(reader.GetOrdinal("count"));

                            feedbacksCount.Add(new FeedbacksChartDto
                            {
                                Label = date.ToString("dd/MMM", new CultureInfo("pt-BR")).Replace(date.ToString("MMM", new CultureInfo("pt-BR")), CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("MMM", new CultureInfo("pt-BR")))),
                                Value = count
                            });
                        }
                    }
                }
            }

            return feedbacksCount;
        }



    }
}