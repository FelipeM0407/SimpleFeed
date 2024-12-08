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

        [HttpPost("{formId}/duplicate")]
        public async Task<IActionResult> DuplicateForm(int formId)
        {
            var newFormId = await _formService.DuplicateFormAsync(formId);
            return Ok(new { NewFormId = newFormId });
        }

        [HttpPatch("{formId}/rename")]
        public async Task<IActionResult> RenameForm(int formId, [FromBody] string newName)
        {
            await _formService.RenameFormAsync(formId, newName);
            return NoContent();
        }

        [HttpDelete("{formId}")]
        public async Task<IActionResult> DeleteFormWithFeedbacks(int formId)
        {
            await _formService.DeleteFormWithFeedbacksAsync(formId);
            return Ok(new { Message = "Form and feedbacks successfully deleted." });
        }

        [HttpPost]
        public async Task<IActionResult> CreateForm([FromBody] CreateFormDto formDto)
        {
            var formId = await _formService.CreateFormAsync(formDto);
            return CreatedAtAction(nameof(CreateForm), new { id = formId }, new { FormId = formId });
        }

    }
}