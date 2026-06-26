using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class FoodExchange : BaseEntity<int>
    {
        public string Name { get; set; } = null!;
        public double StandardCalories { get; set; }
        public double StandardCarbs { get; set; }
        public double StandardProtein { get; set; }
        public double StandardFats { get; set; }
        public ICollection<FoodItem> FoodItems { get; set; } = new List<FoodItem>();
    }
}
