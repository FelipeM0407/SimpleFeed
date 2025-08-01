using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;
using SimpleFeed.Domain.Enums;
using System.Collections.Generic;
using System.Text.Encodings.Web;
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


        public async Task<IEnumerable<FormDashboardDto>> GetActiveFormsWithResponsesAsync(int clientId, StatusFormDto statusFormDto)
        {
            try
            {
                var forms = new List<FormDashboardDto>();

                // Inicia a query base
                var query = @"
        SELECT 
            f.id AS Id, 
            f.name AS Name, 
            COUNT(fe.id) AS ResponseCount,
            (SELECT count(*) FROM feedbacks fe WHERE fe.is_new = true AND fe.client_id = f.client_id AND fe.form_id = f.id) AS NewFeedbackCount,
            f.updated_at AS LastUpdated,
            f.created_at AS CreatedAt,
            fse.inativation_date AS InativationDate,
            f.is_active AS Status
        FROM forms f
        LEFT JOIN form_settings fse ON f.id = fse.form_id
        LEFT JOIN feedbacks fe ON fe.form_id = f.id
        WHERE f.client_id = @ClientId";

                // Condição para "Ativo" ou "Inativo"
                if (statusFormDto.isActive)
                {
                    query += " AND f.is_active = @IsActive";
                }
                else if (statusFormDto.isInativo)
                {
                    query += " AND f.is_active = @IsInativo";
                }

                // Condição para "Não Lido"
                if (statusFormDto.isNaoLido)
                {
                    query += " AND fe.is_new = true";
                }

                // Finaliza a query
                query += @"
        GROUP BY f.id, f.name, f.updated_at, f.created_at, fse.inativation_date
        ORDER BY f.created_at DESC;";

                // Executa a query no banco
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", clientId);

                        // Parâmetros para Ativo e Inativo
                        if (statusFormDto.isActive)
                        {
                            command.Parameters.AddWithValue("@IsActive", true);
                        }
                        else if (statusFormDto.isInativo)
                        {
                            command.Parameters.AddWithValue("@IsInativo", false);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                forms.Add(new FormDashboardDto
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Name = reader["Name"].ToString(),
                                    ResponseCount = reader.GetInt32(reader.GetOrdinal("ResponseCount")),
                                    NewFeedbackCount = reader.GetInt32(reader.GetOrdinal("NewFeedbackCount")),
                                    Status = reader.GetBoolean(reader.GetOrdinal("Status")),
                                    LastUpdated = reader.GetDateTime(reader.GetOrdinal("LastUpdated")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                    InativationDate = reader.IsDBNull(reader.GetOrdinal("InativationDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("InativationDate"))
                                });
                            }
                        }
                    }
                }

                return forms;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao recuperar formulários ativos com respostas.", ex);
            }
        }



        public async Task<bool> DuplicateFormAsync(int formId, string formName)
        {
            try
            {
                var getFormQuery = "SELECT name, client_id, template_id, is_active FROM forms WHERE id = @FormId";
                var insertFormQuery = @"
            INSERT INTO forms (name, client_id, template_id, is_active, created_at, updated_at)
            VALUES (@Name, @ClientId, @TemplateId, @IsActive, NOW(), NOW())
            RETURNING id;";
                var duplicateFieldsQuery = @"
            INSERT INTO form_fields (form_id, name, type, label, required, ordenation, options, field_type_id)
            SELECT @NewFormId, name, type, label, required, ordenation, options, field_type_id
            FROM form_fields
            WHERE form_id = @OriginalFormId;";
                var getStyleQuery = @"
            SELECT color, color_button, color_text_button, background_color, font_color, font_family, font_size
            FROM form_style
            WHERE form_id = @FormId;";
                var insertStyleQuery = @"
            INSERT INTO form_style (form_id, color, color_button, color_text_button, background_color, font_color, font_family, font_size, created_at, updated_at)
            VALUES (@NewFormId, @Color, @ColorButton, @ColorTextButton, @BackgroundColor, @FontColor, @FontFamily, @FontSize, NOW(), NOW());";

                var insertQrCodeQuery = @"
            INSERT INTO form_qrcode (form_id, qrcode_logo_base64, color)
            VALUES (@NewFormId, @QrCodeLogo, @Color);";

                var insertLogoQuery = @"
            INSERT INTO form_logo (form_id, logo_base64, created_at, updated_at)
            VALUES (@NewFormId, @LogoBase64, NOW(), NOW())";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            string originalName;
                            int clientId, templateId;
                            string? color = null, colorButton = null, colorTextButton = null, backgroundColor = null, fontColor = null, fontFamily = null;
                            int? fontSize = null;

                            // Buscar dados do formulário original
                            using var getFormCommand = new NpgsqlCommand(getFormQuery, connection, transaction);
                            getFormCommand.Parameters.AddWithValue("@FormId", formId);

                            using var reader = await getFormCommand.ExecuteReaderAsync();
                            if (!await reader.ReadAsync())
                                throw new Exception("Formulário não encontrado.");

                            originalName = reader.GetString(0);
                            clientId = reader.GetInt32(1);
                            templateId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            var isActive = reader.GetBoolean(3);
                            reader.Close();

                            // Inserir novo formulário
                            int newFormId;
                            using (var insertFormCommand = new NpgsqlCommand(insertFormQuery, connection, transaction))
                            {
                                insertFormCommand.Parameters.AddWithValue("@Name", formName);
                                insertFormCommand.Parameters.AddWithValue("@ClientId", clientId);
                                insertFormCommand.Parameters.AddWithValue("@TemplateId", templateId == 0 ? DBNull.Value : templateId);
                                insertFormCommand.Parameters.AddWithValue("@IsActive", isActive);
                                newFormId = (int)await insertFormCommand.ExecuteScalarAsync();
                            }

                            // Duplicar campos
                            using (var duplicateFieldsCommand = new NpgsqlCommand(duplicateFieldsQuery, connection, transaction))
                            {
                                duplicateFieldsCommand.Parameters.AddWithValue("@NewFormId", newFormId);
                                duplicateFieldsCommand.Parameters.AddWithValue("@OriginalFormId", formId);
                                await duplicateFieldsCommand.ExecuteNonQueryAsync();
                            }

                            // Estilo
                            using (var getStyleCommand = new NpgsqlCommand(getStyleQuery, connection, transaction))
                            {
                                getStyleCommand.Parameters.AddWithValue("@FormId", formId);
                                using var styleReader = await getStyleCommand.ExecuteReaderAsync();
                                if (await styleReader.ReadAsync())
                                {
                                    color = styleReader["color"]?.ToString();
                                    colorButton = styleReader["color_button"]?.ToString();
                                    colorTextButton = styleReader["color_text_button"]?.ToString();
                                    backgroundColor = styleReader["background_color"]?.ToString();
                                    fontColor = styleReader["font_color"]?.ToString();
                                    fontFamily = styleReader["font_family"]?.ToString();
                                    fontSize = styleReader.IsDBNull(styleReader.GetOrdinal("font_size")) ? null : styleReader.GetInt32(styleReader.GetOrdinal("font_size"));
                                }
                            }

                            if (color != null || colorButton != null || backgroundColor != null)
                            {
                                using var insertStyleCommand = new NpgsqlCommand(insertStyleQuery, connection, transaction);
                                insertStyleCommand.Parameters.AddWithValue("@NewFormId", newFormId);
                                insertStyleCommand.Parameters.AddWithValue("@Color", (object?)color ?? DBNull.Value);
                                insertStyleCommand.Parameters.AddWithValue("@ColorButton", (object?)colorButton ?? DBNull.Value);
                                insertStyleCommand.Parameters.AddWithValue("@ColorTextButton", (object?)colorTextButton ?? DBNull.Value);
                                insertStyleCommand.Parameters.AddWithValue("@BackgroundColor", (object?)backgroundColor ?? DBNull.Value);
                                insertStyleCommand.Parameters.AddWithValue("@FontColor", (object?)fontColor ?? DBNull.Value);
                                insertStyleCommand.Parameters.AddWithValue("@FontFamily", (object?)fontFamily ?? DBNull.Value);
                                insertStyleCommand.Parameters.AddWithValue("@FontSize", (object?)fontSize ?? DBNull.Value);
                                await insertStyleCommand.ExecuteNonQueryAsync();
                            }

                            // Duplicar QR Code, se existir
                            var qrCodeLogoBase64 = await GetQrCodeLogoBase64ByFormIdAsync(formId);
                            if (qrCodeLogoBase64 != null)
                            {
                                using var insertQrCodeCommand = new NpgsqlCommand(insertQrCodeQuery, connection, transaction);
                                insertQrCodeCommand.Parameters.AddWithValue("@NewFormId", newFormId);
                                insertQrCodeCommand.Parameters.AddWithValue("@QrCodeLogo", qrCodeLogoBase64.QrCodeLogoBase64 ?? (object)DBNull.Value);
                                insertQrCodeCommand.Parameters.AddWithValue("@Color", qrCodeLogoBase64.Color ?? (object)DBNull.Value);
                                await insertQrCodeCommand.ExecuteNonQueryAsync();
                            }

                            // Duplicar logo, se existir
                            var logoBase64 = await GetLogoBase64ByFormIdAsync(formId);
                            if (logoBase64 != null)
                            {
                                using var insertLogoCommand = new NpgsqlCommand(insertLogoQuery, connection, transaction);
                                insertLogoCommand.Parameters.AddWithValue("@NewFormId", newFormId);
                                insertLogoCommand.Parameters.AddWithValue("@LogoBase64", logoBase64 ?? (object)DBNull.Value);
                                await insertLogoCommand.ExecuteNonQueryAsync();
                            }

                            // Log da duplicação
                            await LogClientActionAsync(connection, transaction, clientId, newFormId, ClientActionType.DuplicateForm, new
                            {
                                original_id = formId,
                                original_name_form = originalName,
                                new_form_name = formName,
                                template_id = templateId
                            });

                            await transaction.CommitAsync();
                            return true;
                        }
                        catch (Exception e)
                        {
                            await transaction.RollbackAsync();
                            Console.WriteLine(e.Message);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao duplicar o formulário.", ex);
            }
        }


        public async Task RenameFormAsync(int formId, string newName)
        {
            try
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
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao renomear o formulário.", ex);
            }
        }

        public async Task DeleteFormWithFeedbacksAsync(int formId)
        {
            try
            {
                var getClientQuery = "SELECT client_id, name FROM forms WHERE id = @FormId";
                var query = @"
            DELETE FROM feedbacks WHERE form_id = @FormId;
            DELETE FROM forms WHERE id = @FormId;";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            int clientId;
                            string formName;
                            using (var getClientCmd = new NpgsqlCommand(getClientQuery, connection, transaction))
                            {
                                getClientCmd.Parameters.AddWithValue("@FormId", formId);
                                using (var reader = await getClientCmd.ExecuteReaderAsync())
                                {
                                    if (!await reader.ReadAsync())
                                        throw new Exception("Formulário não encontrado.");
                                    clientId = reader.GetInt32(reader.GetOrdinal("client_id"));
                                    formName = reader.GetString(reader.GetOrdinal("name"));
                                }
                            }

                            using (var command = new NpgsqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@FormId", formId);
                                await command.ExecuteNonQueryAsync();
                            }

                            await LogClientActionAsync(connection, transaction, clientId, formId, ClientActionType.DeleteForm, new
                            {
                                form_id = formId,
                                form_name = formName,
                                reason = "Solicitação via painel"
                            });

                            await transaction.CommitAsync();
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw new Exception("Erro ao excluir o formulário com feedbacks.", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao excluir o formulário com feedbacks.", ex);
            }
        }

        public async Task<int> CreateFormAsync(CreateFormDto formDto)
        {
            try
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

                            if (formDto.FormStyle != null)
                            {
                                var styleQuery = @"
                            INSERT INTO form_style (form_id, color, color_button, color_text_button, background_color, font_color, font_family, font_size, created_at, updated_at)
                            VALUES (@FormId, @Color, @ColorButton, @ColorTextButton, @BackgroundColor, @FontColor, @FontFamily, @FontSize, NOW(), NOW());";

                                using (var styleCommand = new NpgsqlCommand(styleQuery, connection, transaction))
                                {
                                    styleCommand.Parameters.AddWithValue("@FormId", formId);
                                    styleCommand.Parameters.AddWithValue("@Color", formDto.FormStyle.Color ?? (object)DBNull.Value);
                                    styleCommand.Parameters.AddWithValue("@ColorButton", formDto.FormStyle.ColorButton ?? (object)DBNull.Value);
                                    styleCommand.Parameters.AddWithValue("@ColorTextButton", formDto.FormStyle.ColorTextButton ?? (object)DBNull.Value);
                                    styleCommand.Parameters.AddWithValue("@BackgroundColor", formDto.FormStyle.BackgroundColor ?? (object)DBNull.Value);
                                    styleCommand.Parameters.AddWithValue("@FontColor", formDto.FormStyle.FontColor ?? (object)DBNull.Value);
                                    styleCommand.Parameters.AddWithValue("@FontFamily", formDto.FormStyle.FontFamily ?? (object)DBNull.Value);
                                    styleCommand.Parameters.AddWithValue("@FontSize", formDto.FormStyle.FontSize ?? (object)DBNull.Value);

                                    await styleCommand.ExecuteNonQueryAsync();
                                }
                            }

                            // Log de criação de formulário
                            await LogClientActionAsync(connection, transaction, formDto.Client_Id, formId, ClientActionType.CreateForm, new
                            {
                                form_name = formDto.Name,
                                template_id = formDto.Template_Id
                            });

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
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao criar o formulário.", ex);
            }
        }

        public async Task<FormTemplateDto> GetTemplateByIdAsync(int templateId)
        {
            try
            {
                var query = @"
        SELECT id, name, description, fields
        FROM form_templates
        WHERE id = @TemplateId";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                FormTemplateDto? result = null;

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@TemplateId", templateId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    result = new FormTemplateDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Description = reader.GetString(reader.GetOrdinal("description")),
                        Fields = reader.GetString(reader.GetOrdinal("fields"))
                    };
                }

                await reader.CloseAsync(); // garante fechamento explícito

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao recuperar o template pelo ID.", ex);
            }
        }


        public async Task<string> GetClientPlanAsync(int clientId)
        {
            try
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
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao recuperar o plano do cliente.", ex);
            }
        }

        public async Task<List<FormFieldDto>> GetFormStructureAsync(int form_Id)
        {
            try
            {
                var fields = new List<FormFieldDto>();

                var query = @"
            SELECT fr.name as form_name, ff.id, ff.name, ff.label, ff.type, ff.required, ff.ordenation, ff.options, fr.client_id
            FROM form_fields ff
            INNER JOIN forms fr ON fr.id = ff.form_id
            WHERE form_id = @Form_Id";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Form_Id", form_Id);

                using var reader = await command.ExecuteReaderAsync();

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
                        Ordenation = reader.GetInt32(reader.GetOrdinal("ordenation")),
                        Client_Id = reader.GetInt32(reader.GetOrdinal("client_id")),
                        FormName = reader.GetString(reader.GetOrdinal("form_name"))
                    });
                }

                await reader.CloseAsync(); // fechamento explícito

                return fields;
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao recuperar a estrutura do formulário.", ex);
            }
        }


        public async Task<bool> ValidateExistenceFeedbacks(int form_Id)
        {
            try
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
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao validar a existência de feedbacks.", ex);
            }
        }

        public async Task<bool> SaveFormEditsAsync(EditFormDto formDto)
        {
            try
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
                            else if (formDto.FieldsDeleteds.Any())
                            {
                                // Remove apenas os campos da tabela form_fields
                                var deleteFieldsQuery = "DELETE FROM form_fields WHERE id = ANY(@FieldIds)";
                                using (var deleteFieldsCommand = new NpgsqlCommand(deleteFieldsQuery, connection, transaction))
                                {
                                    deleteFieldsCommand.Parameters.AddWithValue("@FieldIds", formDto.FieldsDeleteds);
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
                                        fieldCommand.Parameters.AddWithValue("@Options", string.IsNullOrWhiteSpace(field.Options) ? DBNull.Value : JsonDocument.Parse(field.Options)).NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
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

                            // Atualiza todos os campos antigos
                            foreach (var field in formDto.Fields.Where(f => !f.IsNew))
                            {
                                var updateFieldQuery = @"
                        UPDATE form_fields
                        SET name = @Name,
                        label = @Label,
                        type = @Type,
                        required = @Required,
                        ordenation = @Ordenation,
                        options = @Options
                        WHERE id = @FieldId";

                                using (var updateFieldCommand = new NpgsqlCommand(updateFieldQuery, connection, transaction))
                                {
                                    updateFieldCommand.Parameters.AddWithValue("@Name", field.Name);
                                    updateFieldCommand.Parameters.AddWithValue("@Label", field.Label);
                                    updateFieldCommand.Parameters.AddWithValue("@Type", field.Type);
                                    updateFieldCommand.Parameters.AddWithValue("@Required", field.Required);
                                    updateFieldCommand.Parameters.AddWithValue("@Ordenation", field.Ordenation);
                                    updateFieldCommand.Parameters.AddWithValue("@Options", string.IsNullOrWhiteSpace(field.Options) ? DBNull.Value : JsonDocument.Parse(field.Options)).NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
                                    updateFieldCommand.Parameters.AddWithValue("@FieldId", field.Id);
                                    await updateFieldCommand.ExecuteNonQueryAsync();
                                }
                            }

                            // Insere ou atualiza o logo na tabela form_logo
                            var upsertLogoQuery = @"
                    INSERT INTO form_logo (form_id, logo_base64, created_at, updated_at)
                    VALUES (@FormId, @LogoBase64, NOW(), NOW())
                    ON CONFLICT (form_id)
                    DO UPDATE SET
                        logo_base64 = EXCLUDED.logo_base64,
                        updated_at = NOW();";

                            using (var upsertLogoCommand = new NpgsqlCommand(upsertLogoQuery, connection, transaction))
                            {
                                upsertLogoCommand.Parameters.AddWithValue("@FormId", formDto.FormId);
                                upsertLogoCommand.Parameters.AddWithValue("@LogoBase64", formDto.LogoBase64 ?? string.Empty);
                                await upsertLogoCommand.ExecuteNonQueryAsync();
                            }

                            // Insere ou atualiza a data de expiração na tabela form_settings
                            var upsertSettingsQuery = @"
                    INSERT INTO form_settings (form_id, inativation_date, created_at, updated_at)
                    VALUES (@FormId, @InativationDate, NOW(), NOW())
                    ON CONFLICT (form_id)
                    DO UPDATE SET
                        inativation_date = EXCLUDED.inativation_date,
                        updated_at = NOW();";

                            using (var upsertSettingsCommand = new NpgsqlCommand(upsertSettingsQuery, connection, transaction))
                            {
                                upsertSettingsCommand.Parameters.AddWithValue("@FormId", formDto.FormId);

                                if (formDto.InativationDate == null)
                                    upsertSettingsCommand.Parameters.AddWithValue("@InativationDate", DBNull.Value);
                                else
                                    upsertSettingsCommand.Parameters.AddWithValue("@InativationDate", formDto.InativationDate);

                                await upsertSettingsCommand.ExecuteNonQueryAsync();
                            }

                            // Recupera o clientId e o nome do formulário
                            var getClientQuery = "SELECT client_id, name FROM forms WHERE id = @FormId";
                            int clientId;
                            string formName;
                            using (var getClientCmd = new NpgsqlCommand(getClientQuery, connection, transaction))
                            {
                                getClientCmd.Parameters.AddWithValue("@FormId", formDto.FormId);
                                using (var reader = await getClientCmd.ExecuteReaderAsync())
                                {
                                    if (!await reader.ReadAsync())
                                        throw new Exception("Cliente não encontrado.");
                                    clientId = reader.GetInt32(reader.GetOrdinal("client_id"));
                                    formName = reader.GetString(reader.GetOrdinal("name"));
                                }
                            }

                            await LogClientActionAsync(connection, transaction, clientId, formDto.FormId, ClientActionType.EditForm, new
                            {
                                form_id = formDto.FormId,
                                form_name = formName
                            });

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
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao salvar as edições do formulário.", ex);
            }
        }

        public async Task<string> GetLogoBase64ByFormIdAsync(int formId)
        {
            try
            {
                var query = @"
            SELECT logo_base64
            FROM form_logo
            WHERE form_id = @FormId";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FormId", formId);
                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao recuperar o logo pelo ID do formulário.", ex);
            }
        }

        public async Task<FormSettingsDto> GetSettingsByFormIdAsync(int formId)
        {
            try
            {
                var query = @"
            SELECT f.is_active, fs.inativation_date
            FROM forms f
            LEFT JOIN form_settings fs ON f.id = fs.form_id
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
                                return new FormSettingsDto
                                {
                                    InativationDate = reader.IsDBNull(reader.GetOrdinal("inativation_date"))
                                    ? (DateTime?)null
                                    : reader.GetDateTime(reader.GetOrdinal("inativation_date")),
                                    Is_Active = reader.GetBoolean(reader.GetOrdinal("is_active"))
                                };
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao recuperar as configurações do formulário.", ex);
            }
        }

        public async Task<int> GetAllActiveFormsCountAsync(int clientId)
        {
            try
            {
                var query = @"
                SELECT COUNT(*)
                FROM forms
                WHERE client_id = @ClientId and is_active = true";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        var count = (long)await command.ExecuteScalarAsync();
                        return (int)count;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao contar os formulários.", ex);
            }
        }

        public async Task<bool> InactivateFormAsync(int formId)
        {
            try
            {
                var getClientQuery = "SELECT client_id, name FROM forms WHERE id = @FormId";
                var query = @"
            UPDATE forms
            SET is_active = false, updated_at = NOW()
            WHERE id = @FormId";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            int clientId;
                            string formName;
                            using (var getClientCmd = new NpgsqlCommand(getClientQuery, connection, transaction))
                            {
                                getClientCmd.Parameters.AddWithValue("@FormId", formId);
                                using (var reader = await getClientCmd.ExecuteReaderAsync())
                                {
                                    if (!await reader.ReadAsync())
                                        throw new Exception("Formulário não encontrado.");
                                    clientId = reader.GetInt32(reader.GetOrdinal("client_id"));
                                    formName = reader.GetString(reader.GetOrdinal("name"));
                                }
                            }

                            using (var command = new NpgsqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@FormId", formId);
                                var rowsAffected = await command.ExecuteNonQueryAsync();

                                if (rowsAffected > 0)
                                {
                                    await LogClientActionAsync(connection, transaction, clientId, formId, ClientActionType.InactivateForm, new
                                    {
                                        form_id = formId,
                                        form_name = formName,
                                        reason = "Solicitação via painel",
                                        inactivated_at = DateTime.UtcNow
                                    });
                                    await transaction.CommitAsync();
                                    return true;
                                }
                                else
                                {
                                    await transaction.RollbackAsync();
                                    return false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw new Exception("Erro ao inativar o formulário.", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao inativar o formulário.", ex);
            }
        }

        public async Task<bool> ActivateFormAsync(int formId)
        {
            try
            {
                var getClientQuery = "SELECT client_id, name FROM forms WHERE id = @FormId";
                var query = @"
            UPDATE forms
            SET is_active = true, updated_at = NOW()
            WHERE id = @FormId";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            int clientId;
                            string formName;
                            using (var getClientCmd = new NpgsqlCommand(getClientQuery, connection, transaction))
                            {
                                getClientCmd.Parameters.AddWithValue("@FormId", formId);
                                using (var reader = await getClientCmd.ExecuteReaderAsync())
                                {
                                    if (!await reader.ReadAsync())
                                        throw new Exception("Formulário não encontrado.");
                                    clientId = reader.GetInt32(reader.GetOrdinal("client_id"));
                                    formName = reader.GetString(reader.GetOrdinal("name"));
                                }
                            }

                            using (var command = new NpgsqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@FormId", formId);
                                var rowsAffected = await command.ExecuteNonQueryAsync();

                                if (rowsAffected > 0)
                                {
                                    await LogClientActionAsync(connection, transaction, clientId, formId, ClientActionType.ActivateForm, new
                                    {
                                        form_id = formId,
                                        form_name = formName,
                                        activation_method = "manual",
                                        activated_at = DateTime.UtcNow
                                    });
                                    await transaction.CommitAsync();
                                    return true;
                                }
                                else
                                {
                                    await transaction.RollbackAsync();
                                    return false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw new Exception("Erro ao ativar o formulário.", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao ativar o formulário.", ex);
            }
        }

        public async Task<FormQRCodeDto> GetQrCodeLogoBase64ByFormIdAsync(int formId)
        {
            try
            {
                var query = @"
            SELECT id, form_id, color, qrcode_logo_base64
            FROM form_qrcode
            WHERE form_id = @FormId";

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
                                return new FormQRCodeDto
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    FormId = reader.GetInt32(reader.GetOrdinal("form_id")),
                                    Color = reader.IsDBNull(reader.GetOrdinal("color")) ? null : reader.GetString(reader.GetOrdinal("color")),
                                    QrCodeLogoBase64 = reader.IsDBNull(reader.GetOrdinal("qrcode_logo_base64")) ? null : reader.GetString(reader.GetOrdinal("qrcode_logo_base64"))
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao recuperar o logo do qr code pelo ID do formulário.", ex);
            }
        }

        public async Task<bool> SaveQrCodeSettingsAsync(int formId, string? color, string? qrCodeLogoBase64)
        {
            try
            {
                var query = @"
                INSERT INTO form_qrcode (form_id, color, qrcode_logo_base64, updatedat)
                VALUES (@FormId, @Color, @QrCodeLogoBase64, NOW())
                ON CONFLICT (form_id)
                DO UPDATE SET
                    color = EXCLUDED.color,
                    qrcode_logo_base64 = EXCLUDED.qrcode_logo_base64,
                    updatedat = NOW();";

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            using (var command = new NpgsqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Color", (object?)color ?? DBNull.Value);
                                command.Parameters.AddWithValue("@QrCodeLogoBase64", (object?)qrCodeLogoBase64 ?? DBNull.Value);
                                command.Parameters.AddWithValue("@FormId", formId);

                                var rows = await command.ExecuteNonQueryAsync();
                                if (rows > 0)
                                {
                                    // Recupera clientId para log
                                    var getClientQuery = "SELECT client_id, name FROM forms WHERE id = @FormId";
                                    int clientId;
                                    string formName;
                                    using (var getClientCmd = new NpgsqlCommand(getClientQuery, connection, transaction))
                                    {
                                        getClientCmd.Parameters.AddWithValue("@FormId", formId);
                                        using (var reader = await getClientCmd.ExecuteReaderAsync())
                                        {
                                            if (!await reader.ReadAsync())
                                                throw new Exception("Formulário não encontrado.");
                                            clientId = reader.GetInt32(reader.GetOrdinal("client_id"));
                                            formName = reader.GetString(reader.GetOrdinal("name"));
                                        }
                                    }

                                    await LogClientActionAsync(connection, transaction, clientId, formId, ClientActionType.EditQrCode, new
                                    {
                                        form_id = formId,
                                        form_name = formName
                                    });

                                    await transaction.CommitAsync();
                                    return true;
                                }
                                else
                                {
                                    await transaction.RollbackAsync();
                                    return false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw new Exception("Ocorreu um erro ao salvar as configurações do QR Code.", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ocorreu um erro ao salvar as configurações do QR Code.", ex);
            }
        }
    }
}
