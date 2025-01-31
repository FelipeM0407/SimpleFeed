using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly string _connectionString;

        public ClientRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> GetClientPlanIdAsync(int clientId)
        {
            try
            {
                const string query = @"
                    SELECT plan_id 
                    FROM clients 
                    WHERE id = @ClientId";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ClientId", clientId);

                return (int)(await command.ExecuteScalarAsync() ?? throw new KeyNotFoundException("Client not found."));
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("An error occurred while retrieving the client plan ID.", ex);
            }
        }

        public async Task<ClientDto> GetClientByGuidAsync(Guid userId)
        {
            try
            {
                ClientDto client = null;

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new NpgsqlCommand(
                        @"SELECT ""Id"", ""Name"", ""PlanId"", ""ExpiryDate"", ""Cpf"", ""Cnpj"", ""CreatedAt"", ""UpdatedAt"" 
                          FROM Clients 
                          WHERE ""UserId"" = @UserId", connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId.ToString());

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                client = new ClientDto
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader["Name"]?.ToString()?.Trim().Replace("\n", "").Replace("\r", ""),
                                    PlanId = reader.GetInt32(2),
                                    ExpiryDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                                    Cpf = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Cnpj = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    CreatedAt = reader.GetDateTime(6),
                                    UpdatedAt = reader.GetDateTime(7)
                                };
                            }
                        }
                    }
                }

                return client;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("An error occurred while retrieving the client by GUID.", ex);
            }
        }
    }
}