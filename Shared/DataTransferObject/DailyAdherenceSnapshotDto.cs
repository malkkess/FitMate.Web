namespace Shared.DataTransferObject
{
    public class DailyAdherenceSnapshotDto
    {
        public DateTime LogDate { get; set; }
        public double PlannedCalories { get; set; }
        public double EatenCalories { get; set; }
        public double CalorieDelta { get; set; }
        public double AdherenceScore { get; set; }
    }
}
