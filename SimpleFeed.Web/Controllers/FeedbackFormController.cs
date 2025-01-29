using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Services;

namespace SimpleFeed.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackFormController : ControllerBase
    {
        private readonly FeedbackFormService _feedbackFormService;

        public FeedbackFormController(FeedbackFormService feedbackFormService)
        {
            _feedbackFormService = feedbackFormService;
        }

        [HttpGet("{formId}/{uniqueId}")]
        public async Task<IActionResult> LoadForm(string formId, string uniqueId)
        {
            try
            {
                var form = await _feedbackFormService.GetFormAsync(formId, uniqueId);
                if (form == null)
                    return NotFound("Formulário não encontrado.");

                return Ok(form);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackInputDto feedback)
        {
            try
            {
                await _feedbackFormService.SubmitFeedbackAsync(feedback);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}