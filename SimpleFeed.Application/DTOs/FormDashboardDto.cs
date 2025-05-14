using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FormDashboardDto
    {
        public int Id {get; set;}
        public string Name { get; set; }
        public int ResponseCount { get; set; }
        public int NewFeedbackCount { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool Status { get; set; }

    }
}