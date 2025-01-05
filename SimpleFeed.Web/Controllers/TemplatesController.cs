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

        [HttpGet("{clientId}/templates")]
        public async Task<IActionResult> GetTemplatesByClientId(Guid clientId)
        {
            var fieldTypes = await _templateService.GetTemplatesByClientIdAsync(clientId);
            return Ok(fieldTypes);
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