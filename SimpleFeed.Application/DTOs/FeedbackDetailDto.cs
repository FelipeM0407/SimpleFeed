using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FeedbackDetailDto
    {
        public int Id { get; set; }
        public DateTime SubmittedAt { get; set; }
        public List<AnswerItem> Answers { get; set; }
        public bool IsNew { get; set; }
    }

    public class AnswerItem
    {
        public int Order { get; set; }
        public object Value { get; set; }
    }
}