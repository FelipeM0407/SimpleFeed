using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FeedbackDetailDto
    {
        public int Id { get; set; }
        public DateTime Submitted_At { get; set; }
        public string Answers { get; set; }
        public bool IsNew { get; set; }
    }
}