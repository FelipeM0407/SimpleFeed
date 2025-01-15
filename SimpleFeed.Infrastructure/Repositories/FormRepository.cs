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
                    f.name AS Name, 
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
                                Name = reader["Name"].ToString(),
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

        public async Task<int> CreateFormAsync(CreateFormDto formDto)
        {
            var formQuery = @"
                INSERT INTO forms (client_id, name, is_active, template_id, created_at)
                VALUES (@Client_Id, @Name, @Is_Active, @Template_id, NOW())
                RETURNING id;";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        int formId;
                        using (var formCommand = new NpgsqlCommand(formQuery, connection, transaction))
                        {
                            formCommand.Parameters.AddWithValue("@Client_Id", formDto.Client_Id);
                            formCommand.Parameters.AddWithValue("@Name", formDto.Name);
                            formCommand.Parameters.AddWithValue("@Is_Active", formDto.Is_Active);
                            formCommand.Parameters.AddWithValue("@Template_id", formDto.Template_Id == 0 ? DBNull.Value : formDto.Template_Id);

                            formId = (int)await formCommand.ExecuteScalarAsync();
                        }

                        var fieldsQuery = @"
                            INSERT INTO form_fields (form_id, name, label, type, required, ordenation, options, field_type_id)
                            VALUES (@FormId, @Name, @Label, @Type, @Required, @Ordenation, @Options, @Field_Type_Id);";

                        foreach (var field in formDto.Fields)
                        {
                            using (var fieldCommand = new NpgsqlCommand(fieldsQuery, connection, transaction))
                            {
                                fieldCommand.Parameters.AddWithValue("@FormId", formId);
                                fieldCommand.Parameters.AddWithValue("@Name", field.Name);
                                fieldCommand.Parameters.AddWithValue("@Label", field.Label);
                                fieldCommand.Parameters.AddWithValue("@Type", field.Type);
                                fieldCommand.Parameters.AddWithValue("@Required", field.Required);
                                fieldCommand.Parameters.AddWithValue("@Ordenation", field.Ordenation);
                                fieldCommand.Parameters.AddWithValue("@Options", string.IsNullOrWhiteSpace(field.Options) ?
                                    DBNull.Value : JsonDocument.Parse(field.Options)).NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
                                fieldCommand.Parameters.AddWithValue("@Field_Type_Id", field.Field_Type_Id);

                                await fieldCommand.ExecuteNonQueryAsync();
                            }
                        }

                        await transaction.CommitAsync();
                        return formId;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
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

        public async Task<List<FormFieldDto>> GetFormStructureAsync(int form_Id)
        {
            var fields = new List<FormFieldDto>();

            var query = @"
                SELECT id, name, label, type, required, ordenation, options
                FROM form_fields
                WHERE form_id = @Form_Id";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Form_Id", form_Id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            fields.Add(new FormFieldDto
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Type = reader.GetString(reader.GetOrdinal("type")),
                                Label = reader.GetString(reader.GetOrdinal("label")),
                                Required = reader.GetBoolean(reader.GetOrdinal("required")),
                                Options = reader["options"]?.ToString(),
                                Ordenation = reader.GetInt32(reader.GetOrdinal("ordenation"))
                            });
                        }
                    }
                }
            }
            return fields;
        }

        public async Task<bool> ValidateExistenceFeedbacks(int form_Id)
        {
            var query = @"
            SELECT COUNT(Id)
            FROM feedbacks
            WHERE form_id = @Form_Id";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Form_Id", form_Id);
                    var count = (long)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        public async Task<bool> SaveFormEditsAsync(EditFormDto formDto)
        {
            var formQuery = @"
        UPDATE forms
        SET updated_at = NOW()
        WHERE id = @FormId";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Atualiza o formulário
                        using (var formCommand = new NpgsqlCommand(formQuery, connection, transaction))
                        {
                            formCommand.Parameters.AddWithValue("@FormId", formDto.FormId);

                            await formCommand.ExecuteNonQueryAsync();
                        }

                        // Exclui campos com base na lista FieldsDeletedsWithFeedbacks
                        if (formDto.FieldsDeletedsWithFeedbacks.Any())
                        {
                            var deleteFieldsQuery = "DELETE FROM form_fields WHERE id = ANY(@FieldIds)";
                            using (var deleteFieldsCommand = new NpgsqlCommand(deleteFieldsQuery, connection, transaction))
                            {
                                deleteFieldsCommand.Parameters.AddWithValue("@FieldIds", formDto.FieldsDeletedsWithFeedbacks);
                                await deleteFieldsCommand.ExecuteNonQueryAsync();
                            }

                            // Remove objetos correspondentes dos feedbacks, apenas se houver FieldsDeletedsWithFeedbacks
                            var updateFeedbacksQuery = @"
                        WITH updated_answers AS (
                            SELECT id, jsonb_agg(answer) AS new_answers
                            FROM (
                                SELECT id, jsonb_array_elements(answers) AS answer
                                FROM feedbacks
                                WHERE form_id = @FormId
                            ) sub
                            WHERE NOT (answer->>'id_form_field')::int = ANY(@FieldIds)
                            GROUP BY id
                        )
                        UPDATE feedbacks
                        SET answers = updated_answers.new_answers
                        FROM updated_answers
                        WHERE feedbacks.id = updated_answers.id;";

                            using (var updateFeedbacksCommand = new NpgsqlCommand(updateFeedbacksQuery, connection, transaction))
                            {
                                updateFeedbacksCommand.Parameters.AddWithValue("@FormId", formDto.FormId);
                                updateFeedbacksCommand.Parameters.AddWithValue("@FieldIds", formDto.FieldsDeletedsWithFeedbacks);
                                await updateFeedbacksCommand.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Remove apenas os campos da tabela form_fields
                            var deleteFieldsQuery = "DELETE FROM form_fields WHERE id = ANY(@FieldIds)";
                            using (var deleteFieldsCommand = new NpgsqlCommand(deleteFieldsQuery, connection, transaction))
                            {
                                deleteFieldsCommand.Parameters.AddWithValue("@FieldIds", formDto.FieldsDeletedsWithFeedbacks);
                                await deleteFieldsCommand.ExecuteNonQueryAsync();
                            }
                        }

                        // Insere novos campos em form_fields, apenas se IsNew for verdadeiro
                        if (formDto.Fields.Any(f => f.IsNew))
                        {
                            var insertFieldsQuery = @"
                        INSERT INTO form_fields (form_id, name, label, type, required, ordenation, options, field_type_id)
                        VALUES (@FormId, @Name, @Label, @Type, @Required, @Ordenation, @Options, @Field_Type_Id)
                        RETURNING id;";

                            foreach (var field in formDto.Fields.Where(f => f.IsNew))
                            {
                                using (var fieldCommand = new NpgsqlCommand(insertFieldsQuery, connection, transaction))
                                {
                                    fieldCommand.Parameters.AddWithValue("@FormId", formDto.FormId);
                                    fieldCommand.Parameters.AddWithValue("@Name", field.Name);
                                    fieldCommand.Parameters.AddWithValue("@Label", field.Label);
                                    fieldCommand.Parameters.AddWithValue("@Type", field.Type);
                                    fieldCommand.Parameters.AddWithValue("@Required", field.Required);
                                    fieldCommand.Parameters.AddWithValue("@Ordenation", field.Ordenation);
                                    fieldCommand.Parameters.AddWithValue("@Options", string.IsNullOrWhiteSpace(field.Options) ? (object)DBNull.Value : field.Options);
                                    fieldCommand.Parameters.AddWithValue("@Field_Type_Id", field.Field_Type_Id);

                                    var newFieldId = (int)await fieldCommand.ExecuteScalarAsync();

                                    // Adiciona objetos vazios na tabela feedbacks
                                    var updateFeedbacksQuery = @"
                                UPDATE feedbacks
                                SET answers = answers || jsonb_build_object('id_form_field', @FieldId, 'value', '')
                                WHERE form_id = @FormId;";

                                    using (var updateFeedbacksCommand = new NpgsqlCommand(updateFeedbacksQuery, connection, transaction))
                                    {
                                        updateFeedbacksCommand.Parameters.AddWithValue("@FieldId", newFieldId);
                                        updateFeedbacksCommand.Parameters.AddWithValue("@FormId", formDto.FormId);
                                        await updateFeedbacksCommand.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        // Confirma a transação
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception e)
                    {
                        // Reverte a transação em caso de erro
                        Console.WriteLine(e.Message);
                        await transaction.RollbackAsync();
                        return false;
                    }
                }
            }
        }
    }
}
