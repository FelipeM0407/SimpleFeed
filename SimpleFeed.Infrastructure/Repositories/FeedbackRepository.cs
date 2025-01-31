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
                throw new Exception("An error occurred while retrieving feedbacks by form ID.", ex);
            }
        }

        public async Task MarkFeedbacksAsReadAsync(int formId)
        {
            try
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
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("An error occurred while marking feedbacks as read.", ex);
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
                throw new Exception("An error occurred while filtering feedbacks.", ex);
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
                throw new Exception("An error occurred while deleting feedbacks.", ex);
            }
        }
    }
}