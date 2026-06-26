namespace DomainLayer.Models
{
    public class MealAdherenceLog : BaseEntity<int>
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime LogDate { get; set; }
        public int MealPlanId { get; set; }
        public MealPlan MealPlan { get; set; } = null!;

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

        /// <summary>Fraction of planned calories consumed (0–1+).</summary>
        public double AdherenceScore { get; set; }

        public ICollection<MealAdherenceItem> Items { get; set; } = new List<MealAdherenceItem>();
    }
}
