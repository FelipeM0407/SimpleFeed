using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly string _connectionString;

        public AccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<AccountDto> GetAccountByIdAsync(Guid accountId)
        {
            try
            {
                using (var connection = new Npgsql.NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                SELECT 
                    Users.""FirstName"", 
                    Users.""LastName"", 
                    Users.""Email"", 
                    Users.""PhoneNumber"", 
                    Cl.""Name"", 
                    Cl.""Cpf"", 
                    Cl.""Cnpj""
                FROM ""AspNetUsers"" Users
                INNER JOIN Clients Cl
                ON Cl.""UserId"" = Users.""Id""
                WHERE Users.""Id"" = @UserGuid";

                    var command = new Npgsql.NpgsqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserGuid", accountId.ToString());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new AccountDto
                            {
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                Email = reader["Email"].ToString(),
                                PhoneNumber = reader["PhoneNumber"].ToString(),
                                Name = reader["Name"].ToString(),
                                Cpf = reader["Cpf"].ToString(),
                                Cnpj = reader["Cnpj"].ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (implement logging as needed)
                throw new Exception("Ocorreu um erro ao recuperar a conta pelo GUID.", ex);
            }

            return null;
        }
    }
}