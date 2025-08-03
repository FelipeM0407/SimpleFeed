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
        private readonly IFormStyleRepository _formStyleRepo;


        public FormService(IFormRepository formRepository, IFormStyleRepository formStyleRepo)
        {
            _formRepository = formRepository;
            _formStyleRepo = formStyleRepo;
        }

        public async Task<IEnumerable<FormDashboardDto>> GetActiveFormsWithResponsesAsync(int clientId, StatusFormDto statusFormDto)
        {
            return await _formRepository.GetActiveFormsWithResponsesAsync(clientId, statusFormDto);
        }

        public async Task<int> DuplicateFormAsync(int formId, string formName, int? qrCodeId = null)
        {
            if (qrCodeId > 0)
            {
                bool disponivel = await _formRepository.IsQrCodeIdAvailableAsync(qrCodeId.Value);
                if (!disponivel)
                    throw new InvalidOperationException("QR Code ID is not available.");
            }


            return await _formRepository.DuplicateFormAsync(formId, formName, qrCodeId);
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
            if (formDto.QrCodeId > 0 && !await _formRepository.IsQrCodeIdAvailableAsync(formDto.QrCodeId))
            {
                throw new InvalidOperationException("QR Code ID is not available.");
            }

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

        public async Task<string> GetLogoBase64ByFormIdAsync(int formId)
        {
            return await _formRepository.GetLogoBase64ByFormIdAsync(formId);
        }

        public async Task<FormSettingsDto> GetSettingsByFormIdAsync(int formId)
        {
            return await _formRepository.GetSettingsByFormIdAsync(formId);
        }

        public async Task<FormStyleDto?> GetFormStyleAsync(int formId)
        {
            return await _formStyleRepo.GetByFormIdAsync(formId);
        }

        public async Task SaveFormStyleAsync(FormStyleDto dto)
        {
            await _formStyleRepo.SaveAsync(dto);
        }

        public async Task<int> GetAllActiveFormsCountAsync(int clientId)
        {
            return await _formRepository.GetAllActiveFormsCountAsync(clientId);
        }

        public async Task<bool> InactivateFormAsync(int formId)
        {
            return await _formRepository.InactivateFormAsync(formId);
        }

        public async Task<bool> ActivateFormAsync(int formId)
        {
            return await _formRepository.ActivateFormAsync(formId);
        }

        public async Task<FormQRCodeDto> GetQrCodeLogoBase64ByFormIdAsync(int formId)
        {
            return await _formRepository.GetQrCodeLogoBase64ByFormIdAsync(formId);
        }

        public async Task<bool> SaveQrCodeSettingsAsync(int formId, string? color, string? qrCodeLogoBase64)
        {
            return await _formRepository.SaveQrCodeSettingsAsync(formId, color, qrCodeLogoBase64);
        }
    }
}