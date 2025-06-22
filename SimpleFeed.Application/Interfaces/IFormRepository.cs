using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface IFormRepository
    {
        Task<IEnumerable<FormDashboardDto>> GetActiveFormsWithResponsesAsync(int clientId, StatusFormDto statusFormDto);
        Task<bool> DuplicateFormAsync(int formId, string formName);
        Task RenameFormAsync(int formId, string newName);
        Task DeleteFormWithFeedbacksAsync(int formId);
        Task<int> CreateFormAsync(CreateFormDto formDto);
        Task<string> GetClientPlanAsync(int clientId);
        Task<List<FormFieldDto>> GetFormStructureAsync(int formId);
        Task<bool> ValidateExistenceFeedbacks(int formId);
        Task<bool> SaveFormEditsAsync(EditFormDto editFormDto);
        Task<string> GetLogoBase64ByFormIdAsync(int formId);
        Task<FormSettingsDto> GetSettingsByFormIdAsync(int formId);
        Task<int> GetAllActiveFormsCountAsync(int clientId);
        Task<bool> InactivateFormAsync(int formId);
        Task<bool> ActivateFormAsync(int formId);
        Task<FormQRCodeDto> GetQrCodeLogoBase64ByFormIdAsync(int formId);
        Task<bool> SaveQrCodeSettingsAsync(int formId, string? color, string? qrCodeLogoBase64);
    }
}