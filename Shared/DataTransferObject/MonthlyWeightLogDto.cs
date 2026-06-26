using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObject
{
    public class MonthlyWeightLogDto
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public double WeightKg { get; set; }
    }
}
