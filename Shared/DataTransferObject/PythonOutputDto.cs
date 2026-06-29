using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObject
{
    public class PythonOutputDto
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public bool RelaxationApplied { get; set; }
        public List<string> RelaxedConstraints { get; set; } = new();
        public PythonAnalysisDto? Analysis { get; set; }
        public Dictionary<string, List<PythonMealItemDto>> Plan { get; set; } = new();
        public List<PythonDayPlanDto> Plans { get; set; } = new();
    }
}
