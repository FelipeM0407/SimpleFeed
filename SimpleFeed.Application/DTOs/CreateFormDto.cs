using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class CreateFormDto
    {
        public string Name { get; set; }
        public int Client_Id { get; set; }
        public bool Is_Active { get; set; }
        public int Template_Id { get; set; }
        public List<FormFieldDto> Fields { get; set; }
        public FormStyleDto FormStyle { get; set; }
    }

    public class FormFieldDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public string Label { get; set; }
        public string Name { get; set; }
        public int Ordenation { get; set; }
        public string? Options { get; set; }
        public int Field_Type_Id { get; set; }
        public bool IsNew { get; set; }
        public int Client_Id { get; set; }
        public string? FormName { get; set; }
    }
}