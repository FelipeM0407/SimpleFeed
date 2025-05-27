using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FormSettingsDto
    {
        public DateTime? InativationDate { get; set; }
        public bool Is_Active { get; set; }

    }
}