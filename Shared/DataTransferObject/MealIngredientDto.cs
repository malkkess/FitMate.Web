using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObject
{
    public class MealIngredientDto
    {
        public int Id { get; set; }
        public string FoodName { get; set; }= null!;
        public double Grams { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fats { get; set; }
        public double Carbs { get; set; }
        public double Fibers { get; set; }
        public bool IsEaten { get; set; }
    }
}
