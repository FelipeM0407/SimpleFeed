using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface IFormStyleRepository
    {
        Task<FormStyleDto?> GetByFormIdAsync(int formId);
        Task SaveAsync(FormStyleDto dto);
    }

}