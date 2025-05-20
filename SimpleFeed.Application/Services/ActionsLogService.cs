using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Application.Services
{
    public class ActionsLogService
    {
        private readonly IActionsLogRepository _repository;

        public ActionsLogService(IActionsLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ActionLogResultDto>> GetLogsAsync(ActionLogFilterDto filter)
        {
            return await _repository.GetLogsAsync(filter);
        }
    }

}