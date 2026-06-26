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
        public PythonAnalysisDto? Analysis { get; set; }
        public Dictionary<string, List<PythonMealItemDto>> Plan { get; set; } = new();
    }
}
