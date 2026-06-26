using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Enums;

namespace DomainLayer.Models
{
    public class Meal : BaseEntity<int>
    {
        public MealType Type { get; set; } 
        public int MealPlanId { get; set; }
        public double TotalCalories { get; set; }
        public MealPlan MealPlan { get; set; } = null!;

        public ICollection<MealIngredient> Ingredients { get; set; } = new List<MealIngredient>();
    }
}
