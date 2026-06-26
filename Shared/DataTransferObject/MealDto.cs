using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObject
{
    public class MealDto
    {
        public int Id { get; set; }
        public string MealType { get; set; } 
        public double TotalCalories { get; set; } 

        public List<MealIngredientDto> Ingredients { get; set; } = new();
    }
}
