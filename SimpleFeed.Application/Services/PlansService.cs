using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Application.Services
{
    public class PlansService
    {

        private readonly IPlansRepository _plansRepository;

        public PlansService(IPlansRepository plansRepository)
        {
            _plansRepository = plansRepository;
        }

        public async Task<List<PlansDto>> GetAllPlansAsync()
        {
            return await _plansRepository.GetAllPlansAsync();
        }

        public async Task<FormCreationStatusDto> GetServicesAvailableByPlanAsync(string clientGuid)
        {
            return await _plansRepository.GetServicesAvailableByPlanAsync(clientGuid);
        }

    }
}