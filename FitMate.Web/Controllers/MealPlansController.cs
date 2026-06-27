using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FitMate.Web.Filters;
using ServiceAbstraction;
using Shared.DataTransferObject;

namespace FitMate.Web.Controllers
{
    [ApiController]
    [Authorize]
    [EnsureUserMatch]
    [Route("api/users/{userId:int}/meal-plans")]
    public class MealPlansController : ControllerBase
    {
        private readonly IMealPlanQueryService _mealPlanQueryService;
        private readonly IMealPlanService _mealPlanService;

        public MealPlansController(
            IMealPlanQueryService mealPlanQueryService,
            IMealPlanService mealPlanService)
        {
            _mealPlanQueryService = mealPlanQueryService;
            _mealPlanService = mealPlanService;
        }

        [HttpGet("today")]
        public async Task<ActionResult<DailyMealPlanDto>> GetTodayPlan(int userId)
        {
            var plan = await _mealPlanQueryService.GetTodayPlanAsync(userId);
            return plan is null ? NotFound() : Ok(plan);
        }

        [HttpGet("{planId:int}")]
        public async Task<ActionResult<DailyMealPlanDto>> GetPlanById(int userId, int planId)
        {
            var plan = await _mealPlanQueryService.GetPlanByIdAsync(userId, planId);
            return plan is null ? NotFound() : Ok(plan);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MealPlanSummaryDto>>> GetUserPlans(int userId)
        {
            var plans = await _mealPlanQueryService.GetUserPlansAsync(userId);
            return Ok(plans);
        }

        [HttpPost("generate")]
        public async Task<ActionResult<DailyMealPlanDto>> GeneratePlan(int userId)
        {
            var plan = await _mealPlanService.GeneratePlanAsync(userId);
            return plan is null ? BadRequest("Failed to generate meal plan.") : Ok(plan);
        }
    }
}
