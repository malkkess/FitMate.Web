namespace DomainLayer.Models
{
    public class FoodItem : BaseEntity<int>
    {
        public string Name { get; set; } = null!;
        public string Category { get; set; } = string.Empty;
        public string FoodFamily { get; set; } = string.Empty;
        public double Cost { get; set; } 
        public double Calories { get; set; } 
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fats { get; set; }
        public double Fiber { get; set; }
        public double NetCarbs { get; set; } 
        public double SaturatedFats { get; set; }
        // Portion control

        public double PortionLimitMax { get; set; }
        public double PortionLimitMin { get; set; }
        // Meal types
        public int IsBreakfast { get; set; }
        public int IsLunch { get; set; }
        public int IsDinner { get; set; }
        public int IsSnack { get; set; }
        // Dietary restrictions
        public int HasLactose { get; set; }
        public int HasGluten { get; set; }
        public int HasNuts { get; set; }

        public int FoodExchangeId { get; set; }
        public virtual FoodExchange FoodExchange { get; set; } = null!;
    }
}

