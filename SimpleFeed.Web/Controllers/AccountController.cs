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
    public class AccountController : ControllerBase
    {
        private readonly AccountService _accountService;

        public AccountController(AccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetAccountById(Guid accountId)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null)
            {
                return NotFound(new { Message = "Account not found." });
            }

            return Ok(account);
        }

        [HttpPost("{accountId}")]
        public async Task<IActionResult> UpdateAccount(Guid accountId, [FromBody] UpdateAccountDTO accountDto)
        {
            try
            {
                var result = await _accountService.UpdateAccountAsync(accountId, accountDto);
                if (!result)
                {
                    return NotFound(new { Message = "Account not found." });
                }

                return Ok(new { Message = "Account updated successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                return StatusCode(500, new { Message = "An error occurred while updating the account.", Details = ex.Message });
            }
        }
    }
}