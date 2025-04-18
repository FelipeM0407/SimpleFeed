using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface IClientRepository
    {
        Task<int> GetClientPlanIdAsync(int clientId);
        Task<ClientDto> GetClientByGuidAsync(Guid userId);

    }
}