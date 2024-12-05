using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Domain.Entities
{
    public class Form
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public int TemplateId { get; set; }

        public string CustomQuestions { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}