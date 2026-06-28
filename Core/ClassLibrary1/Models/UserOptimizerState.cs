namespace DomainLayer.Models
{
    public class UserOptimizerState : BaseEntity<int>
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int MasterSeed { get; set; }
        public int PlanDayCounter { get; set; }
    }
}
