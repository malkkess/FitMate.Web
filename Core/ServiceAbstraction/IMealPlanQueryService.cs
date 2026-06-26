using Shared.DataTransferObject;

namespace ServiceAbstraction
{
    public interface IMealPlanQueryService
    {
        Task<DailyMealPlanDto?> GetTodayPlanAsync(int userId);
        Task<DailyMealPlanDto?> GetPlanByIdAsync(int userId, int planId);
        Task<IReadOnlyList<MealPlanSummaryDto>> GetUserPlansAsync(int userId);
    }
}
