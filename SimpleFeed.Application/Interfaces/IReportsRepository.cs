using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Domain.Entities;
using SimpleFeed.Domain.Enums;

namespace SimpleFeed.Application.Interfaces
{
    public interface IReportsRepository
    {
        Task<(int ClientId, string FormName, IEnumerable<FeedbackDetailDto> Feedbacks, IEnumerable<FormFieldDto> Fields)>
            GetReportDataAsync(int formId, DateTime? dataInicio, DateTime? dataFim, int clientId);

        Task<int> LogClientActionAsync(
            int clientId,
            int formId,
            ClientActionType actionType,
            object details,
            RelatorioDTO report,
            string rangeDataRequested);

        Task<List<ReportsIAs>> GetReportsIaAsync(int formId, DateTime? startDate, DateTime? endDate);

        Task<DetailReportIA> GetReportByIdAsync(int reportId);

    }
}