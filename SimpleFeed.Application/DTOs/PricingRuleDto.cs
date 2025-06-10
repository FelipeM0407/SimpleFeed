using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class PricingRuleDto
    {
        
        public string Item { get; set; }
        public int UnitSize { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; }

    }
}