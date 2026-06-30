namespace DomainLayer.Models
{
    public class ProgressWeightLog : BaseEntity<int>
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime LogDate { get; set; }
        public double WeightKg { get; set; }
    }
}
