namespace Shared.DataTransferObject
{
    /// <summary>
    /// Historical adherence context passed to the Python optimizer for adaptive calorie re-calibration.
    /// </summary>
    public class AdherenceContextDto
    {
        public double AverageAdherenceScore { get; set; }
        public double AverageCalorieDelta { get; set; }
        public double CalorieAdjustment { get; set; }
        public int LoggedDays { get; set; }
        public List<DailyAdherenceSnapshotDto> RecentDays { get; set; } = new();
    }
}
