using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObject
{
    public class FoodItemDto
    {
        public string Name { get; set; } =null!;
        public double Amount { get; set; } 
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fats { get; set; }
        public double Carbs { get; set; }
        public double Fibers { get; set; }
    }
}
