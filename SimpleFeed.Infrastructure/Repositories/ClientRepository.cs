using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
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
    }
}