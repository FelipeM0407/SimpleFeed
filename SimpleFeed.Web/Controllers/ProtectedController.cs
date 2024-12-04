using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimpleFeed.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProtectedController : ControllerBase
    {
        [Authorize] // Restringe acesso apenas a usuários autenticados
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new
            {
                Message = "This is a protected endpoint.",
                UserId = userId,
                UserEmail = userEmail
            });
        }

        [Authorize(Roles = "Admin")] // Restringe acesso a usuários com a role "Admin"
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok("This endpoint is only accessible to admins.");
        }
    }
}