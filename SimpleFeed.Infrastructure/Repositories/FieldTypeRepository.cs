using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class FieldTypeRepository : IFieldTypeRepository
    {
        private readonly string _connectionString;

        public FieldTypeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<FieldTypeDto>> GetFieldTypesAsync()
        {
            var fieldTypes = new List<FieldTypeDto>();

            var query = @"
                SELECT id, name, description, settings_schema, field_type, associated_plan
                FROM field_types
                ORDER BY id";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            fieldTypes.Add(new FieldTypeDto
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Description = reader.GetString(reader.GetOrdinal("description")),
                                FieldType = reader.GetString(reader.GetOrdinal("field_type")),
                                SettingsSchema = reader.GetString(reader.GetOrdinal("settings_schema")),
                                PlanId = reader.GetInt32(reader.GetOrdinal("associated_plan"))
                            });
                        }
                    }
                }
            }

            return fieldTypes;
        }

        public async Task<IEnumerable<FieldTypeDto>> GetFieldTypesByClientIdAsync(Guid clientId)
        {
            var fieldTypes = new List<FieldTypeDto>();

            var query = @"
                SELECT ft.id, ft.name, ft.description, ft.settings_schema, ft.field_type, ft.plan_id
                FROM clients cl
                INNER JOIN field_types ft ON ft.plan_id = cl.""PlanId""
                WHERE cl.""UserId"" = @ClientId";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", clientId.ToString());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            fieldTypes.Add(new FieldTypeDto
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                Description = reader.GetString(reader.GetOrdinal("description")),
                                FieldType = reader.GetString(reader.GetOrdinal("field_type")),
                                SettingsSchema = reader.GetString(reader.GetOrdinal("settings_schema")),
                                PlanId = reader.GetInt32(reader.GetOrdinal("plan_id"))
                            });
                        }
                    }
                }
            }

            return fieldTypes;
        }

        
    }
}