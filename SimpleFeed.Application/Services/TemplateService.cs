using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Application.Services
{
    public class TemplateService
    {
        private readonly ITemplateRepository _templateRepository;

        public TemplateService(ITemplateRepository templateRepository)
        {
            _templateRepository = templateRepository;
        }

        public async Task<IEnumerable<FormTemplateDto>> GetTemplatesByPlanIdAsync(int planId)
        {
            return await _templateRepository.GetTemplatesByPlanIdAsync(planId);
        }

        public async Task<FormTemplateDto?> GetTemplateByIdAsync(int templateId)
        {
            return await _templateRepository.GetTemplateByIdAsync(templateId);
        }

         public async Task<IEnumerable<FormTemplateDto>> GetTemplatesByClientIdAsync(Guid clientId)
        {
            return await _templateRepository.GetTemplatesByClientIdAsync(clientId);
        }
    }
}