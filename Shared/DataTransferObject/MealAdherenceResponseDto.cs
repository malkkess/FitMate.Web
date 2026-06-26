namespace Shared.DataTransferObject
{
    public class MealAdherenceResponseDto
    {
        public int MealPlanId { get; set; }
        public DateTime LogDate { get; set; }

        public double PlannedCalories { get; set; }
        public double PlannedProtein { get; set; }
        public double PlannedCarbs { get; set; }
        public double PlannedFats { get; set; }
        public double PlannedFiber { get; set; }

        public double EatenCalories { get; set; }
        public double EatenProtein { get; set; }
        public double EatenCarbs { get; set; }
        public double EatenFats { get; set; }
        public double EatenFiber { get; set; }

        public double AdherenceScore { get; set; }
        public double CalorieDelta { get; set; }

        public List<MealAdherenceItemResponseDto> Items { get; set; } = new();
    }
}
