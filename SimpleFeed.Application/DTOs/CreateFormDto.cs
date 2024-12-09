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
        public int ClientId { get; set; }
        public bool IsActive { get; set; }
        public List<FormFieldDto> Fields { get; set; }
    }

    public class FormFieldDto
    {
        [JsonPropertyName("fieldTypeId")]
        public int FieldTypeId { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("options")]
        public Dictionary<string, object>? Options { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("max")]
        public int? Max { get; set; }

        [JsonPropertyName("min")]
        public int? Min { get; set; }
    }
}