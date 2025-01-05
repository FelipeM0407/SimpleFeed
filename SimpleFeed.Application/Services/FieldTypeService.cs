using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Application.Services
{
    public class FieldTypeService
    {
        private readonly IFieldTypeRepository _fieldTypeRepository;

        public FieldTypeService(IFieldTypeRepository fieldTypeRepository)
        {
            _fieldTypeRepository = fieldTypeRepository;
        }

        public async Task<IEnumerable<FieldTypeDto>> GetFieldTypesAsync()
        {
            return await _fieldTypeRepository.GetFieldTypesAsync();
        }

        public async Task<IEnumerable<FieldTypeDto>> GetFieldTypesByClientIdAsync(Guid clientId)
        {
            return await _fieldTypeRepository.GetFieldTypesByClientIdAsync(clientId);
        }

       
    }
}