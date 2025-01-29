using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FeedbackInputDto
    {
        public int Form_Id { get; set; }
        public int Client_id { get; set; }
        public object Answers { get; set; }

    }
}