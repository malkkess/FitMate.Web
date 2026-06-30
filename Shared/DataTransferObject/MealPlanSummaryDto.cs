namespace Shared.DataTransferObject
{
    public class MealPlanSummaryDto
    {
        public int PlanId { get; set; }
        public DateTime Date { get; set; }
        public double TotalDayCalories { get; set; }
        public List<MealPlanSummaryMealDto> Meals { get; set; } = new();
    }

    public class MealPlanSummaryMealDto
    {
        public int MealId { get; set; }
        public string MealType { get; set; } = null!;
        public double TotalCalories { get; set; }
        public List<string> FoodItems { get; set; } = new();
    }
}
