using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleFeed.Domain.Enums;

namespace SimpleFeed.Application.DTOs
{
    public class ActionLogFilterDto
{
    public int ClientId { get; set; }
    public List<ClientActionType>? ActionTypes { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

}