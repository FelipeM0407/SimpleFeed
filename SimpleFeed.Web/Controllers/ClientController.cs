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
    public class ClientController : ControllerBase
    {
        private readonly ClientService _clientService;

        public ClientController(ClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet("{guid}")]
        public async Task<IActionResult> GetClientByGuid(Guid guid)
        {
            var client = await _clientService.GetClientByGuidAsync(guid);
            if (client == null)
            {
                return NotFound(new { Message = "Client not found." });
            }

            return Ok(client);
        }
    }
}