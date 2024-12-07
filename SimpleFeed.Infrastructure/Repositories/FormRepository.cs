using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class FormRepository : IFormRepository
    {
        private readonly string _connectionString;

        public FormRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<FormDashboardDto>> GetActiveFormsWithResponsesAsync(int clientId)
        {
            var forms = new List<FormDashboardDto>();

            var query = @"
                SELECT f.id AS Id, 
                    f.name AS FormName, 
                    COUNT(fe.Id) AS ResponseCount,
                    f.updated_at AS LastUpdated
                FROM forms f
                LEFT JOIN feedbacks fe ON fe.form_id = f.Id
                WHERE f.client_id = @ClientId AND f.is_active = TRUE
                GROUP BY f.Id, f.name, f.updated_at;";


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
                            forms.Add(new FormDashboardDto
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                FormName = reader["FormName"].ToString(),
                                ResponseCount = reader.GetInt32(reader.GetOrdinal("ResponseCount")),
                                LastUpdated = reader.GetDateTime(reader.GetOrdinal("LastUpdated"))
                            });
                        }
                    }
                }
            }

            return forms;
        }

        public async Task<int> DuplicateFormAsync(int formId)
        {
            int newFormId;
            var query = @"
                WITH form_copy AS (
                    INSERT INTO forms (client_id, template_id, name, custom_questions, is_active)
                    SELECT client_id, template_id, name || ' (Copy)', custom_questions, is_active
                    FROM forms
                    WHERE id = @FormId
                    RETURNING id
                )
                SELECT id FROM form_copy;";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FormId", formId);
                    newFormId = (int)await command.ExecuteScalarAsync();
                }
            }

            return newFormId;
        }

        public async Task RenameFormAsync(int formId, string newName)
        {
            var query = "UPDATE forms SET name = @NewName, updated_at = NOW() WHERE id = @FormId";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FormId", formId);
                    command.Parameters.AddWithValue("@NewName", newName);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteFormWithFeedbacksAsync(int formId)
        {
            var query = @"
                DELETE FROM feedbacks WHERE form_id = @FormId;
                DELETE FROM forms WHERE id = @FormId;";

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
