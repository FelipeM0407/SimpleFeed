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
    public class TemplatesController : ControllerBase
    {
        private readonly TemplateService _templateService;

        public TemplatesController(TemplateService templateService)
        {
            _templateService = templateService;
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetTemplatesByClientId(int clientId)
        {
            var templates = await _templateService.GetTemplatesByPlanIdAsync(clientId);
            return Ok(templates);
        }

        [HttpGet("{templateId}")]
        public async Task<IActionResult> GetTemplateById(int templateId)
        {
            var template = await _templateService.GetTemplateByIdAsync(templateId);
            if (template == null)
                return NotFound();

            return Ok(template);
        }
    }
}