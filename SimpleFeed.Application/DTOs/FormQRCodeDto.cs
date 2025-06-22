using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FormQRCodeDto
    {
        public int Id { get; set; }
        public int FormId { get; set; }
        public string? Color { get; set; }
        public string? QrCodeLogoBase64 { get; set; }
    }
}