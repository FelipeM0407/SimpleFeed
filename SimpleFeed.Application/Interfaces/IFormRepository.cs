using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface IFormRepository
    {
        Task<IEnumerable<FormDashboardDto>> GetActiveFormsWithResponsesAsync(int clientId);
        Task<bool> DuplicateFormAsync(int formId);
        Task RenameFormAsync(int formId, string newName);
        Task DeleteFormWithFeedbacksAsync(int formId);
        Task<int> CreateFormAsync(CreateFormDto formDto);
        Task<string> GetClientPlanAsync(int clientId);
        Task<List<FormFieldDto>> GetFormStructureAsync(int formId);
        Task<bool> ValidateExistenceFeedbacks(int formId);
        Task<bool> SaveFormEditsAsync(EditFormDto editFormDto);

    }
}