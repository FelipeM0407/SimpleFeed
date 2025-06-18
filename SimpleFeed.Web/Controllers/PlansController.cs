using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleFeed.Application.Services;

namespace SimpleFeed.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlansController : ControllerBase
    {

        private readonly PlansService _plansService;

        public PlansController(PlansService plansService)
        {
            _plansService = plansService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPlans()
        {
            try
            {
                var plans = await _plansService.GetAllPlansAsync();
                return Ok(plans);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{clientId}/services-available")]
        public async Task<IActionResult> GetServicesAvailableByPlan(string clientId)
        {
            try
            {
                var result = await _plansService.GetServicesAvailableByPlanAsync(clientId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{formId}/reactivation-status")]
        public async Task<IActionResult> GetFormReactivationStatus(int formId)
        {
            try
            {
                var result = await _plansService.GetFormReactivationStatusAsync(formId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }
}