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
        private readonly FeedbackService _feedbackService;

        public FormsController(FormService formService, FeedbackService feedbackService)
        {
            _formService = formService;
            _feedbackService = feedbackService;
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

        [AllowAnonymous]
        [HttpGet("{formId}/logo")]
        public async Task<IActionResult> GetLogoBase64ByFormId(int formId)
        {
            var logoBase64 = await _formService.GetLogoBase64ByFormIdAsync(formId);
            return Ok(new { logoBase64 });
        }

        [AllowAnonymous]
        [HttpGet("{formId}/settings")]
        public async Task<IActionResult> GetSettingsByFormIdAsync(int formId)
        {
            var settings = await _formService.GetSettingsByFormIdAsync(formId);
            return Ok(settings);
        }

        [AllowAnonymous]
        [HttpGet("{id}/style")]
        public async Task<IActionResult> GetFormStyle(int id)
        {
            var style = await _formService.GetFormStyleAsync(id);
            return Ok(style);
        }

        [HttpPost("{id}/style")]
        public async Task<IActionResult> SaveFormStyle(int id, [FromBody] FormStyleDto dto)
        {
            dto.FormId = id;
            await _formService.SaveFormStyleAsync(dto);
            return NoContent();
        }

        [HttpGet("{formId}/metrics")]
        public async Task<IActionResult> GetMetrics(int formId, int clientId)
        {
            var newFeedbacksCount = await _feedbackService.GetNewFeedbacksCountAsync(formId);
            var allFeedbacksCount = await _feedbackService.GetAllFeedbacksCountAsync(formId);
            var todayFeedbacksCount = await _feedbackService.GetTodayFeedbacksCountAsync(formId);
            var allActiveFormsCount = await _formService.GetAllFormsCountAsync(clientId);
            var feedbacksCountLast30Days = await _feedbackService.GetFeedbacksCountLast30DaysByClientAsync(clientId);

            return Ok(new
            {
                NewFeedbacksCount = newFeedbacksCount,
                AllFeedbacksCount = allFeedbacksCount,
                TodayFeedbacksCount = todayFeedbacksCount,
                AllActiveFormsCount = allActiveFormsCount,
                FeedbacksCountLast30Days = feedbacksCountLast30Days
            });
        }

    }
}