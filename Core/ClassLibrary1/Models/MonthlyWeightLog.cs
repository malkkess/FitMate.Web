using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class MonthlyWeightLog : BaseEntity<int>
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int Year { get; set; }
        public int Month { get; set; }
        public double WeightKg { get; set; }
    }
}
