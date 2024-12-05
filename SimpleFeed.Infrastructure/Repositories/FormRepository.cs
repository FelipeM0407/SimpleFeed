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
                SELECT f.name AS FormName, 
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
    }
}
