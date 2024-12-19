using System;
using System.Collections.Generic;
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
            var feedbacks = new List<FeedbackDetailDto>();

            var query = @"
                SELECT f.id, f.submitted_at, f.answers, f.is_new
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
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("submitted_at")),
                                Answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(reader.GetOrdinal("answers"))), // Ajustado para object
                                IsNew = reader.GetBoolean(reader.GetOrdinal("is_new"))
                            });
                        }
                    }
                }
            }

            return feedbacks;
        }

        public async Task MarkFeedbacksAsReadAsync(int formId)
        {
            var query = @"
                UPDATE feedbacks
                SET is_new = FALSE
                WHERE form_id = @FormId AND is_new = TRUE";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FormId", formId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}