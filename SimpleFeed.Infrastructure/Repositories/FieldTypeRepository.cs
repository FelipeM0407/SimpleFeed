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
                SELECT id, name, description, settings_schema
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
                                SettingsSchema = reader.GetString(reader.GetOrdinal("settings_schema"))
                            });
                        }
                    }
                }
            }

            return fieldTypes;
        }
    }
}