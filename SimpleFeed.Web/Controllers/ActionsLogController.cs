using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Services;

namespace SimpleFeed.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActionsLogController : ControllerBase
    {
        private readonly ActionsLogService _logService;

        public ActionsLogController(ActionsLogService logServcice)
        {
            _logService = logServcice;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] ActionLogFilterDto filter)
        {
            if (filter.ClientId <= 0)
                return BadRequest("O campo 'ClientId' é obrigatório.");

            var logs = await _logService.GetLogsAsync(filter);
            return Ok(logs);
        }
    }
}