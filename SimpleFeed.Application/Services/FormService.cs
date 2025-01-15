using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

        public async Task<int> CreateFormAsync(CreateFormDto formDto)
        {
            return await _formRepository.CreateFormAsync(formDto);
        }

        public async Task<List<FormFieldDto>> GetFormStructureAsync(int formId)
        {
            return await _formRepository.GetFormStructureAsync(formId);
        }

        public async Task<bool> ValidateExistenceFeedbacks(int formId)
        {
            return await _formRepository.ValidateExistenceFeedbacks(formId);
        }

        public async Task<bool> SaveFormEditsAsync(EditFormDto editFormDto)
        {
            return await _formRepository.SaveFormEditsAsync(editFormDto);
        }

    }
}