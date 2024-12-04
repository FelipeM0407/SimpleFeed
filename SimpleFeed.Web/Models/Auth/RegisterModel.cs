using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Domain.Entities.Enum;

namespace SimpleFeed.Web.Models.Auth
{
    public class RegisterModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } // Campo obrigatório para o telefone

        // Plano selecionado pelo usuário (valor do enum)
        public PlanType Plan { get; set; } = PlanType.Free; // Padrão: Free

        // Campos opcionais para o Client
        public string? Name { get; set; }
        [StringLength(11, ErrorMessage = "O CPF deve ter 11 caracteres.")]
        public string? Cpf { get; set; }
        [StringLength(14, ErrorMessage = "O CNPJ deve ter 14 caracteres.")]
        public string? Cnpj { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}