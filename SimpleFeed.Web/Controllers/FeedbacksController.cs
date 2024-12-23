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
            var feedbacks = await _feedbackService.FilterFeedbacksAsync(formId, dateRange.Submitted_Start, dateRange.Submitted_End);
            return Ok(feedbacks);
        }

    }
}