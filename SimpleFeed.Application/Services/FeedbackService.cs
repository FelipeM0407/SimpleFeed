using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Application.Services
{
    public class FeedbackService
    {
        private readonly IFeedbackRepository _feedbackRepository;

        public FeedbackService(IFeedbackRepository feedbackRepository)
        {
            _feedbackRepository = feedbackRepository;
        }

        public async Task<IEnumerable<FeedbackDetailDto>> GetFeedbacksAsync(int formId)
        {
            // Buscar os feedbacks
            var feedbacks = await _feedbackRepository.GetFeedbacksByFormAsync(formId);

            // Marcar os feedbacks como "não novos"
            await _feedbackRepository.MarkFeedbacksAsReadAsync(formId);

            return feedbacks;
        }
    }
}