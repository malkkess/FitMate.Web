using Shared.DataTransferObject;

namespace ServiceAbstraction
{
    public interface IDailyLogService
    {
        Task LogActivityAsync(int userId, DailyLogActivityDto activityDto);
        Task<MealAdherenceResponseDto> LogMealAdherenceAsync(int userId, MealAdherenceRequestDto request);
        Task<MealAdherenceResponseDto?> GetMealAdherenceAsync(int userId, DateTime? logDate = null);
        Task<AdherenceContextDto> GetAdherenceContextAsync(int userId);
        Task LogMonthlyWeightAsync(int userId, MonthlyWeightLogDto monthlyWeightDto);
        Task<WeeklyProgressSummaryDto> GetWeeklySummaryAsync(int userId);
    }
}
