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
            // await _feedbackRepository.MarkFeedbacksAsReadAsync(formId);

            return feedbacks;
        }

        //metodo de filtrar os feedbacks
        public async Task<IEnumerable<FeedbackDetailDto>> FilterFeedbacksAsync(int formId, DateTime? submitted_Start, DateTime? submitted_End)
        {
            // Buscar os feedbacks
            var feedbacks = await _feedbackRepository.FilterFeedbacksAsync(formId, submitted_Start, submitted_End);

            // Marcar os feedbacks como "não novos"
            await _feedbackRepository.MarkFeedbacksAsReadAsync(feedbacks.Select(f => f.Id).ToArray());


            return feedbacks;
        }

        public async Task DeleteFeedbacksAsync(int[] feedbackIds)
        {
            await _feedbackRepository.DeleteFeedbacksAsync(feedbackIds);
        }

        public async Task<int> GetNewFeedbacksCountAsync(int formId)
        {
            return await _feedbackRepository.GetNewFeedbacksCountAsync(formId);
        }

        public async Task<int> GetAllFeedbacksCountAsync(int formId)
        {
            return await _feedbackRepository.GetAllFeedbacksCountAsync(formId);
        }

        public async Task<int> GetTodayFeedbacksCountAsync(int formId)
        {
            return await _feedbackRepository.GetTodayFeedbacksCountAsync(formId);
        }

        public async Task<List<FeedbacksChartDto>> GetFeedbacksCountLast30DaysByClientAsync(int clientId)
        {
            return await _feedbackRepository.GetFeedbacksCountLast30DaysByClientAsync(clientId);
        }
    }
}