using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class EditFormDto
    {
        public int FormId { get; set; }
        public List<FormFieldDto> Fields { get; set; }
        public List<int> FieldsDeletedsWithFeedbacks { get; set; }
    }
}