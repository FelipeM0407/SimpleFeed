using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface IFeedbackRepository
    {
        Task<IEnumerable<FeedbackDetailDto>> GetFeedbacksByFormAsync(int formId);
        Task MarkFeedbacksAsReadAsync(int[] feedbacksId);
        Task<IEnumerable<FeedbackDetailDto>> FilterFeedbacksAsync(int formId, DateTime? submitted_Start, DateTime? submitted_End);
        Task DeleteFeedbacksAsync(int[] feedbackIds);
    }
}