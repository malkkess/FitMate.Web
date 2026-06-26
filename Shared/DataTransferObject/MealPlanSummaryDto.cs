namespace Shared.DataTransferObject
{
    public class MealPlanSummaryDto
    {
        public int PlanId { get; set; }
        public DateTime Date { get; set; }
        public double TotalDayCalories { get; set; }
    }
}
