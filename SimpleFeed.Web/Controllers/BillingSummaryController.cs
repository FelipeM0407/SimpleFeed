using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimpleFeed.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BillingSummaryController : ControllerBase
    {
        private readonly BillingSummaryService _service;

        public BillingSummaryController(BillingSummaryService service)
        {
            _service = service;
        }

        [HttpGet("{clientId}")]
        public async Task<IActionResult> GetSummary(int clientId, [FromQuery] DateTime referenceMonth)
        {
            if (clientId <= 0 || referenceMonth == default)
                return BadRequest("Parâmetros inválidos.");

            var result = await _service.GetSummaryAsync(clientId, referenceMonth);
            return Ok(result);
        }

        [HttpPost("migrate-plan")]
        public async Task<IActionResult> MigratePlan([FromBody] MigratePlanDto request)
        {
            if (request == null || request.ClientId <= 0 || request.NewPlanId <= 0)
                return BadRequest("Parâmetros inválidos.");

            var result = await _service.MigratePlanAsync(request.ClientId, request.NewPlanId);
            if (!result)
                return BadRequest("Não foi possível migrar o plano.");

            return Ok(new { Message = "Plano migrado com sucesso." });
        }
    }
}