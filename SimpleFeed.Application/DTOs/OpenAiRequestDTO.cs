using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class OpenAiRequestDTO
    {
        public int ClientId { get; set; }
        public int FormId { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string? ContextoNegocio { get; set; }
    }
}