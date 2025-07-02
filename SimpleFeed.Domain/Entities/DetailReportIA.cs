using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Domain.Entities
{
    public class DetailReportIA
    {
        public int Id { get; set; }
        public int FormId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RangeDataSolicited { get; set; }
        public string Report { get; set; }
    }
}