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

        [HttpGet("{formId}/{uniqueId}/check")]
        public async Task<IActionResult> CheckAccess(string formId)
        {
            try
            {
                var result = await _feedbackFormService.CheckAccessAsync(formId);
                if (!result)
                    return Forbid("Você já respondeu a este formulário hoje.");

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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

        [HttpPost("{formId}/feedback")]
        public async Task<IActionResult> SubmitFeedback(string formId, [FromBody] FeedbackInputDto feedback)
        {
            try
            {
                await _feedbackFormService.SubmitFeedbackAsync(formId, feedback);

                // Configurar o cookie com duração de 24 horas
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddHours(24),
                    HttpOnly = false,
                    Secure = true
                };

                Response.Cookies.Append($"feedback_{formId}", "submitted", cookieOptions);

                return Ok("Feedback enviado com sucesso!");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}