using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface IAccountRepository
    {
        Task<AccountDto> GetAccountByIdAsync(Guid accountId);
        Task<bool> UpdateAccountAsync(Guid accountId, UpdateAccountDTO accountDto);
    }
}