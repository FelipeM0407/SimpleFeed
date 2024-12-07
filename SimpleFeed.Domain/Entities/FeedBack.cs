using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Domain.Entities
{
    public class Feedback
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int FormId { get; set; }
        public string Answers { get; set; } // JSON em formato de string
        public DateTime SubmittedAt { get; set; }
        public string IpAddress { get; set; }
        public bool IsNew { get; set; } // Indica se o feedback ainda não foi visualizado
    }
}