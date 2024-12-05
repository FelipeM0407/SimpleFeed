using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleFeed.Application.Services;

namespace SimpleFeed.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FormsController : ControllerBase
    {
        private readonly FormService _formService;

        public FormsController(FormService formService)
        {
            _formService = formService;
        }

        [HttpGet("dashboard/{clientId}")]
        public async Task<IActionResult> GetFormDashboard(int clientId)
        {
            var result = await _formService.GetActiveFormsWithResponsesAsync(clientId);
            return Ok(result);
        }
    }
}