using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface ITemplateRepository
    {
        Task<IEnumerable<FormTemplateDto>> GetTemplatesByPlanIdAsync(int planId);
        Task<FormTemplateDto?> GetTemplateByIdAsync(int templateId);
    }
}