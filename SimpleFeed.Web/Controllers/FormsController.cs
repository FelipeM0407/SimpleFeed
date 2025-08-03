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


        [HttpPost("{clientId}")]
        public async Task<IActionResult> GetFormDashboard(int clientId, [FromBody] StatusFormDto statusFormDto)
        {
            var result = await _formService.GetActiveFormsWithResponsesAsync(clientId, statusFormDto);
            return Ok(result);
        }

        [HttpPost("{formId}/duplicate")]
        public async Task<IActionResult> DuplicateForm(int formId, [FromBody] DuplicateFormDto duplicateFormDto)
        {
            try
            {
                var newFormId = await _formService.DuplicateFormAsync(formId, duplicateFormDto.FormName, duplicateFormDto.QrCodeId);
                return Ok(new { NewFormId = newFormId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
            try
            {
                var formId = await _formService.CreateFormAsync(formDto);
                return CreatedAtAction(nameof(CreateForm), new { id = formId }, new { FormId = formId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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

        [HttpGet("{clientId}/metrics")]
        public async Task<IActionResult> GetMetrics(int clientId)
        {
            var newFeedbacksCount = await _feedbackService.GetNewFeedbacksCountAsync(clientId);
            var allFeedbacksCount = await _feedbackService.GetAllFeedbacksCountAsync(clientId);
            var todayFeedbacksCount = await _feedbackService.GetTodayFeedbacksCountAsync(clientId);
            var allActiveFormsCount = await _formService.GetAllActiveFormsCountAsync(clientId);
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
        [HttpPost("{formId}/inactivate")]
        public async Task<IActionResult> InactivateForm(int formId)
        {
            var result = await _formService.InactivateFormAsync(formId);
            if (result)
            {
                return Ok(new { Message = "Form successfully inactivated." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to inactivate the form." });
            }
        }

        [HttpPost("{formId}/activate")]
        public async Task<IActionResult> ActivateForm(int formId)
        {
            var result = await _formService.ActivateFormAsync(formId);
            if (result)
            {
                return Ok(new { Message = "Form successfully activated." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to activate the form." });
            }
        }

        [HttpGet("{formId}/qrcode-settings")]
        public async Task<IActionResult> GetQrCodeLogoBase64ByFormId(int formId)
        {
            var qrCodeSettings = await _formService.GetQrCodeLogoBase64ByFormIdAsync(formId);
            return Ok(qrCodeSettings);
        }

        [HttpPost("save-qrcode-settings")]
        public async Task<IActionResult> SaveQrCodeSettings([FromBody] FormQRCodeDto dto)
        {
            var result = await _formService.SaveQrCodeSettingsAsync(dto.FormId, dto.Color, dto.QrCodeLogoBase64);
            return Ok(result);
        }
    }
}