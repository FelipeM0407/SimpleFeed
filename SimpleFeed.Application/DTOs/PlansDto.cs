using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class PlansDto
    {
        
        public int Id { get; set; }
        public string Name { get; set; }
        public int? MaxForms { get; set; }
        public int? MaxResponses { get; set; }
        public decimal BasePrice { get; set; }
        public int? AiReportsPerForm { get; set; }
        public bool AiReportsDiscount { get; set; }
        public bool UnlimitedResponses { get; set; }
        public string PlanType { get; set; }
        public List<PricingRuleDto> Pricing { get; set; } = new List<PricingRuleDto>();
    }
}