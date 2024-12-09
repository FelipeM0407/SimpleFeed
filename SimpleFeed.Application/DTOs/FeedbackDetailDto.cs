using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FeedbackDetailDto
    {
        public DateTime SubmittedAt { get; set; }
        public Dictionary<string, object> Answers { get; set; } // Alterado para aceitar object
        public bool IsNew { get; set; }
    }
}