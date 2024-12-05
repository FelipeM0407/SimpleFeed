using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Domain.Entities
{
    public class FeedbackReport
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public DateTime GeneratedAt { get; set; }

        [MaxLength(50)]
        public string ReportType { get; set; }

        public string ReportData { get; set; }  // Armazenará os dados do relatório em JSON

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }   
}