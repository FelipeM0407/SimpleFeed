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
    public class FieldTypesController : ControllerBase
    {
        private readonly FieldTypeService _fieldTypeService;

        public FieldTypesController(FieldTypeService fieldTypeService)
        {
            _fieldTypeService = fieldTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFieldTypes()
        {
            var fieldTypes = await _fieldTypeService.GetFieldTypesAsync();
            return Ok(fieldTypes);
        }

        [HttpGet("{clientId}")]
        public async Task<IActionResult> GetFieldTypesByClientId(Guid clientId)
        {
            var fieldTypes = await _fieldTypeService.GetFieldTypesByClientIdAsync(clientId);
            return Ok(fieldTypes);
        }

        


    }
}