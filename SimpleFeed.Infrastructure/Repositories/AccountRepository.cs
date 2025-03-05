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

        public async Task<bool> UpdateAccountAsync(Guid accountId, UpdateAccountDTO accountDto)
        {
            try
            {
                using (var connection = new Npgsql.NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            var updateUserQuery = @"
                                UPDATE ""AspNetUsers""
                                SET ""FirstName"" = @FirstName,
                                    ""LastName"" = @LastName,
                                    -- ""Email"" = @Email,
                                    ""PhoneNumber"" = @PhoneNumber,
                                    ""UpdatedAt"" = NOW()
                                WHERE ""Id"" = @UserGuid";

                            var updateClientQuery = @"
                                UPDATE Clients
                                SET ""Name"" = @Name,
                                    ""Cpf"" = @Cpf,
                                    ""Cnpj"" = @Cnpj,
                                    ""UpdatedAt"" = NOW()
                                WHERE ""UserId"" = @UserGuid";

                            using (var updateUserCommand = new Npgsql.NpgsqlCommand(updateUserQuery, connection, transaction))
                            {
                                updateUserCommand.Parameters.AddWithValue("@FirstName", accountDto.FirstName);
                                updateUserCommand.Parameters.AddWithValue("@LastName", accountDto.LastName);
                                // updateUserCommand.Parameters.AddWithValue("@Email", accountDto.Email);
                                updateUserCommand.Parameters.AddWithValue("@PhoneNumber", accountDto.PhoneNumber);
                                updateUserCommand.Parameters.AddWithValue("@UserGuid", accountId.ToString());

                                await updateUserCommand.ExecuteNonQueryAsync();
                            }

                            using (var updateClientCommand = new Npgsql.NpgsqlCommand(updateClientQuery, connection, transaction))
                            {
                                updateClientCommand.Parameters.AddWithValue("@Name", accountDto.Name);
                                if (accountDto.DocumentType == "CPF")
                                {
                                    updateClientCommand.Parameters.AddWithValue("@Cpf", string.IsNullOrWhiteSpace(accountDto.Document) ? (object)DBNull.Value : accountDto.Document);
                                    updateClientCommand.Parameters.AddWithValue("@Cnpj", DBNull.Value);
                                }
                                else if (accountDto.DocumentType == "CNPJ")
                                {
                                    updateClientCommand.Parameters.AddWithValue("@Cpf", DBNull.Value);
                                    updateClientCommand.Parameters.AddWithValue("@Cnpj", string.IsNullOrWhiteSpace(accountDto.Document) ? (object)DBNull.Value : accountDto.Document);
                                }
                                else
                                {
                                    updateClientCommand.Parameters.AddWithValue("@Cpf", DBNull.Value);
                                    updateClientCommand.Parameters.AddWithValue("@Cnpj", DBNull.Value);
                                }
                                updateClientCommand.Parameters.AddWithValue("@UserGuid", accountId.ToString());

                                await updateClientCommand.ExecuteNonQueryAsync();
                            }

                            await transaction.CommitAsync();
                            return true;
                        }
                        catch (Exception)
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("Ocorreu um erro ao atualizar a conta de usuário.", ex);
            }
        }
    }
}