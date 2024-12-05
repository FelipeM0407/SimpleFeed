using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FormDashboardDto
    {
        public string FormName { get; set; }
        public int ResponseCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}