using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.Interfaces
{
    public interface IClientRepository
    {
        Task<int> GetClientPlanIdAsync(int clientId);
    }
}