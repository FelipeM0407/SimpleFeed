using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Application.Services
{
    public class FormService
    {
        private readonly IFormRepository _formRepository;

        public FormService(IFormRepository formRepository)
        {
            _formRepository = formRepository;
        }

        public async Task<IEnumerable<FormDashboardDto>> GetActiveFormsWithResponsesAsync(int clientId)
        {
            return await _formRepository.GetActiveFormsWithResponsesAsync(clientId);
        }

        public async Task<int> DuplicateFormAsync(int formId)
        {
            return await _formRepository.DuplicateFormAsync(formId);
        }

        public async Task RenameFormAsync(int formId, string newName)
        {
            await _formRepository.RenameFormAsync(formId, newName);
        }

        public async Task DeleteFormWithFeedbacksAsync(int formId)
        {
            await _formRepository.DeleteFormWithFeedbacksAsync(formId);
        }
    }
}