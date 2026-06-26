using DomainLayer.Models;
using Shared.DataTransferObject;

namespace Service
{
    internal static class AdherenceContextBuilder
    {
        private const int LookbackDays = 7;
        private const double MaxCalorieAdjustment = 300;

        public static AdherenceContextDto Build(IEnumerable<MealAdherenceLog> logs)
        {
            var recent = logs
                .OrderByDescending(l => l.LogDate)
                .Take(LookbackDays)
                .OrderBy(l => l.LogDate)
                .ToList();

            if (recent.Count == 0)
            {
                return new AdherenceContextDto();
            }

            var snapshots = recent.Select(l => new DailyAdherenceSnapshotDto
            {
                LogDate = l.LogDate,
                PlannedCalories = l.PlannedCalories,
                EatenCalories = l.EatenCalories,
                CalorieDelta = l.EatenCalories - l.PlannedCalories,
                AdherenceScore = l.AdherenceScore,
            }).ToList();

            var avgScore = recent.Average(l => l.AdherenceScore);
            var avgDelta = recent.Average(l => l.EatenCalories - l.PlannedCalories);

            return new AdherenceContextDto
            {
                AverageAdherenceScore = Math.Round(avgScore, 4),
                AverageCalorieDelta = Math.Round(avgDelta, 2),
                CalorieAdjustment = Math.Round(ComputeCalorieAdjustment(avgDelta), 2),
                LoggedDays = recent.Count,
                RecentDays = snapshots,
            };
        }

        /// <summary>
        /// Negative adjustment when user over-consumed; positive when under-consumed.
        /// Python applies this to remaining-day calorie targets.
        /// </summary>
        private static double ComputeCalorieAdjustment(double averageCalorieDelta)
        {
            var adjustment = -averageCalorieDelta * 0.5;
            return Math.Clamp(adjustment, -MaxCalorieAdjustment, MaxCalorieAdjustment);
        }
    }
}
