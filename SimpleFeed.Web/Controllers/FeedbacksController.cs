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
    public class FeedbacksController : ControllerBase
    {
        private readonly FeedbackService _feedbackService;

        public FeedbacksController(FeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpGet("{formId}")]
        public async Task<IActionResult> GetFeedbacks(int formId)
        {
            var feedbacks = await _feedbackService.GetFeedbacksAsync(formId);
            return Ok(feedbacks);
        }

        [HttpPost("{formId}/filter")]
        public async Task<IActionResult> FilterFeedbacks(int formId, [FromBody] DateRangeDto dateRange)
        {
            DateTime? start = null;
            DateTime? end = null;

            if (!string.IsNullOrEmpty(dateRange.Submitted_Start) && !string.IsNullOrEmpty(dateRange.Submitted_End))
            {
                start = DateTime.Parse(dateRange.Submitted_Start);
                end = DateTime.Parse(dateRange.Submitted_End);
            }

            var feedbacks = await _feedbackService.FilterFeedbacksAsync(formId, start, end);
            return Ok(feedbacks);
        }


        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFeedbacks([FromBody] int[] feedbackIds)
        {
            await _feedbackService.DeleteFeedbacksAsync(feedbackIds);
            return NoContent();
        }
    }
}