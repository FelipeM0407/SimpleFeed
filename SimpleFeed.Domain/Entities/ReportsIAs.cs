using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Domain.Entities
{
    public class ReportsIAs
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int FormId { get; set; }
        public string? RangeDataSolicited { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}