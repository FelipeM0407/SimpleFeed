using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Application.Services
{
    public class FeedbackFormService
    {
        private readonly IFeedbackFormRepository _feedbackFormRepository;

        public FeedbackFormService(IFeedbackFormRepository feedbackFormRepository)
        {
            _feedbackFormRepository = feedbackFormRepository;
        }

        public async Task<bool> CheckAccessAsync(string formId)
        {
            // Valida se o usuário já respondeu
            return await _feedbackFormRepository.CheckAccessAsync(formId);
        }

        public async Task<object> GetFormAsync(string formId, string uniqueId)
        {
            // Retorna os dados do formulário
            return await _feedbackFormRepository.GetFormAsync(formId, uniqueId);
        }

        public async Task SubmitFeedbackAsync(string formId, FeedbackInputDto feedback)
        {
            // Salvar feedback no banco
            await _feedbackFormRepository.SaveFeedbackAsync(formId, feedback);
        }
    }
}