using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;

namespace SimpleFeed.Application.Interfaces
{
    public interface IFieldTypeRepository
    {
        Task<IEnumerable<FieldTypeDto>> GetFieldTypesAsync();
        Task<IEnumerable<FieldTypeDto>> GetFieldTypesByClientIdAsync(Guid clientId);
    }
}