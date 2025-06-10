using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface IBillingSummaryRepository
    {
        Task<BillingSummaryDto> GetBillingSummaryAsync(int clientId, DateTime referenceMonth);
        Task<BillingSummaryDto?> GetBillingSummaryFromStoredAsync(int clientId, DateTime referenceMonth);
        Task<bool> MigratePlanAsync(int clientId, int newPlanId);
    }
}