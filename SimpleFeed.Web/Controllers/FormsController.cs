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


        [HttpGet("{clientId}")]
        public async Task<IActionResult> GetFormDashboard(int clientId)
        {
            var result = await _formService.GetActiveFormsWithResponsesAsync(clientId);
            return Ok(result);
        }

        [HttpPost("{formId}/duplicate")]
        public async Task<IActionResult> DuplicateForm(int formId, [FromBody] DuplicateFormDto duplicateFormDto)
        {
            var newFormId = await _formService.DuplicateFormAsync(formId, duplicateFormDto.FormName);
            return Ok(new { NewFormId = newFormId });
        }

        [HttpPost("{formId}/rename")]
        public async Task<IActionResult> RenameForm(int formId, [FromBody] RenameFormDto renameFormDto)
        {
            await _formService.RenameFormAsync(formId, renameFormDto.Name);
            return Ok();
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

        [AllowAnonymous]
        [HttpGet("{formId}/structure")]
        public async Task<IActionResult> GetFormStructure(int formId)
        {
            var structure = await _formService.GetFormStructureAsync(formId);
            if (structure == null || !structure.Any())
                return NotFound(new { Message = "Form structure not found." });

            return Ok(structure);
        }

        [HttpGet("{formId}/feedbacks")]
        public async Task<IActionResult> ValidateExistenceFeedbacks(int formId)
        {
            var hasFeedbacks = await _formService.ValidateExistenceFeedbacks(formId);
            return Ok(hasFeedbacks);
        }

        [HttpPost("save-edits")]
        public async Task<IActionResult> SaveFormEdits([FromBody] EditFormDto editFormDto)
        {
            var result = await _formService.SaveFormEditsAsync(editFormDto);
            return Ok(new { Success = result });
        }

        [HttpGet("{formId}/logo")]
        public async Task<IActionResult> GetLogoBase64ByFormId(int formId)
        {
            var logoBase64 = await _formService.GetLogoBase64ByFormIdAsync(formId);
            return Ok(new { logoBase64 });
        }

    }
}