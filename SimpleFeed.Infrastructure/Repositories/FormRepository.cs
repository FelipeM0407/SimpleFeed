using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;
using System.Collections.Generic;
using System.Text.Json;
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

        public async Task<int> CreateFormAsync(CreateFormDto formDto, string customQuestionsJson)
        {
            var query = @"
        INSERT INTO forms (client_id, name, custom_questions, is_active, created_at, updated_at)
        VALUES (@ClientId, @Name, @CustomQuestions::jsonb, @IsActive, NOW(), NOW())
        RETURNING id;";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", formDto.ClientId);
                    command.Parameters.AddWithValue("@Name", formDto.Name);
                    command.Parameters.AddWithValue("@CustomQuestions", JsonSerializer.Serialize(formDto.Fields)); // Serializa o JSON
                    command.Parameters.AddWithValue("@IsActive", formDto.IsActive);

                    var formId = (int)await command.ExecuteScalarAsync();
                    return formId;
                }
            }
        }

        public async Task<FormTemplateDto> GetTemplateByIdAsync(int templateId)
        {
            var query = @"
        SELECT id, name, description, fields
        FROM form_templates
        WHERE id = @TemplateId";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TemplateId", templateId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new FormTemplateDto
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Description = reader.GetString(reader.GetOrdinal("description")),
                                Fields = reader.GetString(reader.GetOrdinal("fields"))
                            };
                        }
                    }
                }
            }

            return null;
        }

        public async Task<string> GetClientPlanAsync(int clientId)
        {
            var query = @"
                SELECT p.name
                FROM clients c
                JOIN plans p ON c.""PlanId"" = p.id
                WHERE c.""Id"" = @ClientId";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", clientId);
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString() ?? string.Empty;
                }
            }
        }


    }
}
