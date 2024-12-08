using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class CreateFormFromTemplateDto
{
    public string Name { get; set; }
    public int ClientId { get; set; }
    public int TemplateId { get; set; }
    public bool IsActive { get; set; }
}
}