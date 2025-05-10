using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly string _connectionString;

        public FeedbackRepository(string connectionString)
        {
            _connectionString = connectionString;
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
                var query = "DELETE FROM feedbacks WHERE id = ANY(@FeedbackIds)";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FeedbackIds", feedbackIds);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao deletar os feedbacks.", ex);
            }
        }

        public async Task<int> GetNewFeedbacksCountAsync(int formId)
        {
            try
            {
                var query = "SELECT COUNT(*) FROM feedbacks WHERE form_id = @FormId AND is_new = TRUE";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FormId", formId);
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

        public async Task<int> GetAllFeedbacksCountAsync(int formId)
        {
            try
            {
                var query = "SELECT COUNT(*) FROM feedbacks WHERE form_id = @FormId";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FormId", formId);
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

        public async Task<int> GetTodayFeedbacksCountAsync(int formId)
        {
            try
            {
                var query = "SELECT COUNT(*) FROM feedbacks WHERE form_id = @FormId AND DATE(submitted_at) = CURRENT_DATE";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FormId", formId);
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

        //um metodo para trazer a somatoria de feedbacks recebidos ao logo de 30 dias atraz, o retorno deve ser um dicionario com a data e a quantidade de feedbacks recebidos
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