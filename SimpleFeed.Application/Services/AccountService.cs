using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Application.Services
{
    public class AccountService
    {
        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<AccountDto> GetAccountByIdAsync(Guid accountId)
        {
            return await _accountRepository.GetAccountByIdAsync(accountId);
        }

        public async Task<bool> UpdateAccountAsync(Guid accountId, AccountDto accountDto)
        {
            return await _accountRepository.UpdateAccountAsync(accountId, accountDto);
        }
    }
}