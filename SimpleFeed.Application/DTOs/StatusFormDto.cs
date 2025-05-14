using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class StatusFormDto
    {
        public bool isActive { get; set; }
        public bool isInativo { get; set; }
        public bool isExpirado { get; set; }
        public bool isNaoLido { get; set; }
    }
}