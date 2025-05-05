using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FormStyleDto
    {
        public int? Id { get; set; }
        public int FormId { get; set; }
        public string? Color { get; set; }
        public string? ColorButton { get; set; }
        public string? BackgroundColor { get; set; }
        public string? FontColor { get; set; }
        public string? FontFamily { get; set; }
        public int? FontSize { get; set; }
    }

}