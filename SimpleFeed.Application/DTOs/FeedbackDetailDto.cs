using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FeedbackDetailDto
    {
        public DateTime SubmittedAt { get; set; }
        public Dictionary<string, string> Answers { get; set; } // Cada coluna dinâmica do formulário
        public bool IsNew { get; set; } // Indica se é novo
    }
}