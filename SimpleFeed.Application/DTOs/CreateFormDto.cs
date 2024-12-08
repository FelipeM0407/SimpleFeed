using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class CreateFormDto
    {
        public string Name { get; set; }
        public int ClientId { get; set; }
        public bool IsActive { get; set; }
        public List<FormFieldDto> Fields { get; set; }
    }

    public class FormFieldDto
    {
        public int FieldTypeId { get; set; }
        public string Label { get; set; }
        public bool Required { get; set; }
        public int Order { get; set; }
        public Dictionary<string, object>? Options { get; set; } // Para configurações como opções de dropdown, etc.
    }
}