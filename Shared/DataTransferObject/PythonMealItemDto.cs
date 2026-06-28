namespace Shared.DataTransferObject
{
    public class PythonMealItemDto
    {
        public string Food { get; set; } = string.Empty;
        public int Slot { get; set; }
        public string Category { get; set; } = string.Empty;
        public string FoodFamily { get; set; } = string.Empty;
        public double Grams { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbs { get; set; }
        public double Fiber { get; set; }
        public double NetCarbs { get; set; }
        public double SatFat { get; set; }
    }
}
