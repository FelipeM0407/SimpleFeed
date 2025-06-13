using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface IPlansRepository
    {
        Task<List<PlansDto>> GetAllPlansAsync();
        Task<FormCreationStatusDto> GetServicesAvailableByPlanAsync(string clientGuid);
    }
}