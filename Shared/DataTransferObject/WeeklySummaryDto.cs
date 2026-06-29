using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObject
{
    public class WeeklyProgressSummaryDto
    {
        public int UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double AverageSleepHours { get; set; }
        public double AverageWaterIntakeLiters { get; set; }
        public double CommitmentScore { get; set; }
        public bool ShouldGenerateNewPlan { get; set; }
        public string? PlanRecommendationMessage { get; set; }
        public List<DailyProgressPointDto> DailyProgress { get; set; } = new();
    }

    public class DailyProgressPointDto
    {
        public DateTime LogDate { get; set; }
        public double SleepHours { get; set; }
        public double WaterIntakeLiters { get; set; }
        public int MoodScore { get; set; }
        public bool CompletedWorkout { get; set; }
    }
}
