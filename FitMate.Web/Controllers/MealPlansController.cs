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
        [ProducesResponseType(typeof(DailyMealPlanDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DailyMealPlanDto>> GetTodayPlan(int userId)
        {
            var plan = await _mealPlanQueryService.GetTodayPlanAsync(userId);
            return plan is null
                ? NotFound(new MessageResponseDto { Message = "No plan for today. Generate a new plan." })
                : Ok(plan);
        }

        [HttpGet("{planId:int}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<DailyMealPlanDto>> GetPlanById(int userId, int planId)
        {
            var plan = await _mealPlanQueryService.GetPlanByIdAsync(userId, planId);
            return plan is null ? NotFound() : Ok(plan);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<MealPlanSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<MealPlanSummaryDto>>> GetUserPlans(int userId)
        {
            var plans = await _mealPlanQueryService.GetUserPlansAsync(userId);
            return Ok(plans);
        }

        [HttpPost("generate")]
        public async Task<ActionResult<DailyMealPlanDto>> GeneratePlan(int userId)
        {
            try
            {
                var plan = await _mealPlanService.GeneratePlanAsync(userId);
                return plan is null
                    ? BadRequest(new MessageResponseDto { Message = "Failed to generate meal plan." })
                    : Ok(plan);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new MessageResponseDto { Message = ex.Message });
            }
        }
    }
}
