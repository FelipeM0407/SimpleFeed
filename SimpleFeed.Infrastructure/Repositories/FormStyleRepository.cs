using Microsoft.Extensions.Configuration;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;
using System.Data;
using System.Threading.Tasks;

public class FormStyleRepository : IFormStyleRepository
{
    private readonly string _connectionString;

    public FormStyleRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<FormStyleDto?> GetByFormIdAsync(int formId)
    {
        var query = @"SELECT id, form_id, color, color_button, color_text_button, background_color, font_color, font_family, font_size
                  FROM form_style
                  WHERE form_id = @FormId
                  LIMIT 1";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@FormId", formId);

        using var reader = await command.ExecuteReaderAsync();

        FormStyleDto? dto = null;

        if (await reader.ReadAsync())
        {
            dto = new FormStyleDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                FormId = reader.GetInt32(reader.GetOrdinal("form_id")),
                Color = reader["color"] as string,
                ColorButton = reader["color_button"] as string,
                ColorTextButton = reader["color_text_button"] as string,
                BackgroundColor = reader["background_color"] as string,
                FontColor = reader["font_color"] as string,
                FontFamily = reader["font_family"] as string,
                FontSize = reader.GetInt32(reader.GetOrdinal("font_size"))
            };
        }

        await reader.CloseAsync(); // fechamento explícito

        return dto;
    }


    public async Task SaveAsync(FormStyleDto dto)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // Usamos uma transação para garantir atomicidade
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    var existsQuery = @"SELECT COUNT(1) FROM form_style WHERE form_id = @FormId";
                    bool exists;

                    // Usar uma conexão separada ou garantir que o comando execute totalmente antes de outro
                    using (var existsCommand = new NpgsqlCommand(existsQuery, connection, transaction))
                    {
                        existsCommand.Parameters.AddWithValue("@FormId", dto.FormId);

                        // Executa e aguarda completamente ANTES de prosseguir
                        var result = await existsCommand.ExecuteScalarAsync();
                        exists = (result != null && Convert.ToInt64(result) > 0);
                    }

                    if (exists)
                    {
                        var updateSql = @"UPDATE form_style SET
                                        color = @Color,
                                        color_button = @ColorButton,
                                        color_text_button = @ColorTextButton,
                                        background_color = @BackgroundColor,
                                        font_color = @FontColor,
                                        font_family = @FontFamily,
                                        font_size = @FontSize,
                                        updated_at = CURRENT_TIMESTAMP
                                      WHERE form_id = @FormId";

                        using (var command = new NpgsqlCommand(updateSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Color", (object?)dto.Color ?? DBNull.Value);
                            command.Parameters.AddWithValue("@ColorButton", (object?)dto.ColorButton ?? DBNull.Value);
                            command.Parameters.AddWithValue("@BackgroundColor", (object?)dto.BackgroundColor ?? DBNull.Value);
                            command.Parameters.AddWithValue("@ColorTextButton", (object?)dto.ColorTextButton ?? DBNull.Value);
                            command.Parameters.AddWithValue("@FontColor", (object?)dto.FontColor ?? DBNull.Value);
                            command.Parameters.AddWithValue("@FontFamily", (object?)dto.FontFamily ?? DBNull.Value);
                            command.Parameters.AddWithValue("@FontSize", (object?)dto.FontSize ?? DBNull.Value);
                            command.Parameters.AddWithValue("@FormId", dto.FormId);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        var insertSql = @"INSERT INTO form_style 
                                        (form_id, color, color_button, color_text_button,  background_color, font_color, font_family, font_size)
                                      VALUES 
                                        (@FormId, @Color, @ColorButton, @ColorTextButton, @BackgroundColor, @FontColor, @FontFamily, @FontSize)";

                        using (var command = new NpgsqlCommand(insertSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@FormId", dto.FormId);
                            command.Parameters.AddWithValue("@Color", (object?)dto.Color ?? DBNull.Value);
                            command.Parameters.AddWithValue("@ColorButton", (object?)dto.ColorButton ?? DBNull.Value);
                            command.Parameters.AddWithValue("@ColorTextButton", (object?)dto.ColorTextButton ?? DBNull.Value);
                            command.Parameters.AddWithValue("@BackgroundColor", (object?)dto.BackgroundColor ?? DBNull.Value);
                            command.Parameters.AddWithValue("@FontColor", (object?)dto.FontColor ?? DBNull.Value);
                            command.Parameters.AddWithValue("@FontFamily", (object?)dto.FontFamily ?? DBNull.Value);
                            command.Parameters.AddWithValue("@FontSize", (object?)dto.FontSize ?? DBNull.Value);

                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }

}
