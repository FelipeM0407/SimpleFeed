using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class PlansRepository : IPlansRepository
    {
        private readonly string _connectionString;

        public PlansRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<PlansDto>> GetAllPlansAsync()
        {
            var plans = new Dictionary<int, PlansDto>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                SELECT 
                p.id, p.name, p.max_forms, p.max_responses, p.base_price, 
                p.ai_reports_per_form, p.ai_reports_discount, p.unlimited_responses, p.plan_type,
                pr.item, pr.unit_size, pr.price, pr.discounted_price
                FROM plans p
                LEFT JOIN pricing_rules pr ON pr.plan_id = p.id
                ORDER BY p.id, pr.item;
            ";

                using var command = new NpgsqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32(0);

                    if (!plans.ContainsKey(id))
                    {
                        plans[id] = new PlansDto
                        {
                            Id = id,
                            Name = reader.GetString(1),
                            MaxForms = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                            MaxResponses = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                            BasePrice = reader.GetDecimal(4),
                            AiReportsPerForm = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                            AiReportsDiscount = reader.GetBoolean(6),
                            UnlimitedResponses = reader.GetBoolean(7),
                            PlanType = reader.GetString(8),
                            Pricing = new List<PricingRuleDto>()
                        };
                    }

                    if (!reader.IsDBNull(9))
                    {
                        plans[id].Pricing.Add(new PricingRuleDto
                        {
                            Item = reader.GetString(9),
                            UnitSize = reader.GetInt32(10),
                            Price = reader.GetDecimal(11),
                            DiscountedPrice = reader.IsDBNull(12) ? null : reader.GetDecimal(12)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle as needed
                throw new Exception("Error fetching plans from the database.", ex);
            }

            return new List<PlansDto>(plans.Values);
        }
    }
}