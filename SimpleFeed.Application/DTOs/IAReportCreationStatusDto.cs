using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class IAReportCreationStatusDto
    {
        public string PlanoNome { get; set; }
        public int PlanoId { get; set; }
        public int TotalRelatoriosIAMes { get; set; }
        public int LimiteRelatoriosIAMes { get; set; }
        public bool PodeExcederRelatorios { get; set; }
        public bool CriacaoGeraraCobranca { get; set; }
    }

}