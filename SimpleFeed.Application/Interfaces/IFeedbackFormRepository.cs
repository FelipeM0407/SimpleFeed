using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface IFeedbackFormRepository
    {
        Task<bool> CheckAccessAsync(string formId);
        Task<FormDetailDto> GetFormAsync(string formId, string uniqueId);
        Task SaveFeedbackAsync(string formId, FeedbackInputDto feedback);
    }
}