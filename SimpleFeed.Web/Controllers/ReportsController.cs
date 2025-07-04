using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Services;

namespace SimpleFeed.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ReportsService _reportsService;

        public ReportsController(ReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [HttpPost("gerar-report-ia")]
        public async Task<IActionResult> GerarRelatorio([FromBody] OpenAiRequestDTO dto)
        {
            if (dto.FormId <= 0 || string.IsNullOrWhiteSpace(dto.ContextoNegocio))
                return BadRequest("Formulário e contexto do negócio são obrigatórios.");

            var report = await _reportsService.GerarRelatorioAsync(dto, dto.ClientId);
            return Ok(report);
        }

        [HttpGet("reports-ia/{formId}")]
        public async Task<IActionResult> GetReportsIa(int formId, [FromQuery] string? startDate, [FromQuery] string? endDate)
        {
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            bool isStartValid = DateTime.TryParse(startDate, out var parsedStartDate);
            bool isEndValid = DateTime.TryParse(endDate, out var parsedEndDate);

            if (isStartValid && isEndValid)
            {
                startDateTime = parsedStartDate;
                endDateTime = parsedEndDate;
            }
            else
            {
                startDateTime = null;
                endDateTime = null;
            }

            var reports = await _reportsService.GetReportsIaAsync(formId, startDateTime, endDateTime);
            return Ok(reports);
        }


        [HttpGet("report-ia/{reportId}")]
        public async Task<IActionResult> GetReportById(int reportId)
        {
            if (reportId <= 0)
                return BadRequest("ID do relatório é obrigatório.");

            var report = await _reportsService.GetReportByIdAsync(reportId);
            if (report == null)
                return NotFound("Relatório não encontrado.");

            return Ok(report);
        }

        [HttpGet("{clientId}/services-available-ia-reports")]
        public async Task<IActionResult> GetServicesAvailableByPlan(string clientId)
        {
            try
            {
                var result = await _reportsService.GetServicesAvailableByPlanAsync(clientId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}