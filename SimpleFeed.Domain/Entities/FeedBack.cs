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

        [Required]
        public string Answers { get; set; }  // Armazenará as respostas como JSON

        public DateTime SubmittedAt { get; set; }

        [MaxLength(50)]
        public string IPAddress { get; set; }
    }
}