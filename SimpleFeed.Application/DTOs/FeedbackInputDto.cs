using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FeedbackInputDto
    {
        public int FormId { get; set; } // ID do formulário respondido
        public string Answers { get; set; } // Respostas do formulário (JSON formatado como string)
        public string IpAddress { get; set; } // Endereço IP do usuário
        public Guid UniqueId { get; set; } // Identificador único do usuário

    }
}