using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class FeedbackFormRepository : IFeedbackFormRepository
    {
        private readonly string _connectionString;

        public FeedbackFormRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> CheckAccessAsync(string formId)
        {
            var query = @"
            SELECT COUNT(*) 
            FROM feedbacks 
            WHERE form_id = @FormId AND created_at::date = CURRENT_DATE";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
            await connection.OpenAsync();
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@FormId", formId);

                var count = (int)await command.ExecuteScalarAsync();
                return count == 0; // Retorna true se o acesso é permitido (não há resposta anterior)
            }
            }
        }

        public async Task<FormDetailDto> GetFormAsync(string formId, string uniqueId)
        {
            var query = @"
                SELECT f.id, f.name, f.is_active, f.created_at, f.updated_at
                FROM forms f
                WHERE f.id = @FormId";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FormId", formId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new FormDetailDto
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Name = reader["name"]?.ToString(),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            };
                        }
                    }
                }
            }

            return null; // Retorna null se o formulário não for encontrado
        }

        public async Task SaveFeedbackAsync(string formId, FeedbackInputDto feedback)
        {
            var query = @"
                INSERT INTO feedbacks (form_id, answers, submitted_at, unique_id, ip_address)
                VALUES (@FormId, @Answers, NOW(), @UniqueId, @IpAddress)";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FormId", formId);
                    command.Parameters.AddWithValue("@Answers", feedback.Answers);
                    command.Parameters.AddWithValue("@UniqueId", feedback.UniqueId);
                    command.Parameters.AddWithValue("@IpAddress", feedback.IpAddress);

                    await command.ExecuteNonQueryAsync(); // Executa a inserção no banco de dados
                }
            }
        }
    }
}