using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Application.Services
{
    public class ClientService
    {
        private readonly IClientRepository _clientRepository;

        public ClientService(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task<int> GetClientPlanIdAsync(int clientId)
        {
            return await _clientRepository.GetClientPlanIdAsync(clientId);
        }
    }
}