using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class TemplateRepository : ITemplateRepository
    {
        private readonly string _connectionString;

        public TemplateRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<FormTemplateDto>> GetTemplatesByPlanIdAsync(int planId)
        {
            var templates = new List<FormTemplateDto>();

            var query = @"
                SELECT id, name, description, fields, plan_id, created_at, updated_at
                FROM form_templates
                WHERE plan_id = @PlanId";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlanId", planId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            templates.Add(new FormTemplateDto
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Description = reader.GetString(reader.GetOrdinal("description")),
                                PlanId = reader.GetInt32(reader.GetOrdinal("plan_id")),
                                Fields = reader["fields"] != DBNull.Value ? reader["fields"].ToString() : "[]"

                            });
                        }
                    }
                }
            }

            return templates;
        }

        public async Task<FormTemplateDto?> GetTemplateByIdAsync(int templateId)
        {
            FormTemplateDto? template = null;

            var query = @"
                SELECT id, name, description, fields, plan_id, created_at, updated_at
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
                            template = new FormTemplateDto
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Description = reader.GetString(reader.GetOrdinal("description")),
                                PlanId = reader.GetInt32(reader.GetOrdinal("plan_id")),
                                Fields = reader["fields"] != DBNull.Value ? reader["fields"].ToString() : "[]"

                            };
                        }
                    }
                }
            }

            return template;
        }
    }
}