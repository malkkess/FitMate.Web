using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class MealIngredient : BaseEntity<int>
    {
        public int MealId { get; set; }
        public virtual Meal Meal { get; set; } = null!;

        public int FoodItemId { get; set; }
        public virtual FoodItem FoodItem { get; set; } = null!;

       
        public double Quantity { get; set; }

        
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fats { get; set; }
        public double Carbs { get; set; }
    }
}
