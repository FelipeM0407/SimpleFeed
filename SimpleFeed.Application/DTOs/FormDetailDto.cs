using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FormDetailDto
    {
        public int Id { get; set; } // ID do formulário
        public string Name { get; set; } // Nome do formulário
        public bool IsActive { get; set; } // Indica se o formulário está ativo
        public DateTime CreatedAt { get; set; } // Data de criação
        public DateTime UpdatedAt { get; set; } // Data de última atualização
    }
}