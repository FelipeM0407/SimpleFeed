using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class BillingSummaryDto
    {
        public int ClientId { get; set; }
        public int PlanId { get; set; }
        public DateTime ReferenceMonth { get; set; }

        public int TotalFormsMes { get; set; }
        public int? FormsDentroPlano { get; set; }
        public int FormsExcedentes { get; set; }

        public int TotalRespostasArmazenadas { get; set; }
        public int? RespostasDentroPlano { get; set; }
        public int RespostasExcedentes { get; set; }

        public int TotalAiReports { get; set; }
        public int AiReportsLimite { get; set; }
        public int ExtraAiReports { get; set; }

        public decimal FormExcessCharge { get; set; }
        public decimal ResponseExcessCharge { get; set; }
        public decimal AiReportExcessCharge { get; set; }

        public decimal ValorFaturaAteAgora { get; set; }
        public decimal ValorBaseFatura { get; set; }
        public string NomePlano { get; set; }
    }

}