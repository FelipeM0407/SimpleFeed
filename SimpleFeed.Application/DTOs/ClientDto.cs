using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PlanId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Cpf { get; set; }
        public string Cnpj { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}