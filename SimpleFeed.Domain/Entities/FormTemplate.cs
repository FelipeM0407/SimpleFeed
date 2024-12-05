using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Domain.Entities
{
    public class FormTemplate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public string Fields { get; set; }  // Armazenará o JSON como string

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}