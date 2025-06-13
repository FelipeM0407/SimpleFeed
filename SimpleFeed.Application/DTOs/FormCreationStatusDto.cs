using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeed.Application.DTOs
{
    public class FormCreationStatusDto
    {
        public string PlanoNome { get; set; }
        public int PlanoId { get; set; }
        public int TotalFormulariosAtivos { get; set; }
        public int LimiteFormularios { get; set; }
        public bool PodeExcederFormulario { get; set; }
        public bool CriacaoGeraraCobranca { get; set; }
    }

}