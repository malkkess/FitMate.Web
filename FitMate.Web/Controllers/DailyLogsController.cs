using Microsoft.AspNetCore.Mvc;
using ServiceAbstraction;
using Shared.DataTransferObject;

namespace FitMate.Web.Controllers
{
    [ApiController]
    [Route("api/users/{userId:int}/daily-logs")]
    public class DailyLogsController : ControllerBase
    {
        private readonly IDailyLogService _dailyLogService;

        public DailyLogsController(IDailyLogService dailyLogService)
        {
            _dailyLogService = dailyLogService;
        }

        [HttpPost]
        public async Task<IActionResult> LogActivity(int userId, [FromBody] DailyLogActivityDto activityDto)
        {
            await _dailyLogService.LogActivityAsync(userId, activityDto);
            return Ok();
        }

        [HttpPost("meal-adherence")]
        public async Task<ActionResult<MealAdherenceResponseDto>> LogMealAdherence(
            int userId,
            [FromBody] MealAdherenceRequestDto request)
        {
            try
            {
                var result = await _dailyLogService.LogMealAdherenceAsync(userId, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("meal-adherence")]
        public async Task<ActionResult<MealAdherenceResponseDto>> GetMealAdherence(
            int userId,
            [FromQuery] DateTime? logDate = null)
        {
            var result = await _dailyLogService.GetMealAdherenceAsync(userId, logDate);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("monthly-weight")]
        public async Task<IActionResult> LogMonthlyWeight(int userId, [FromBody] MonthlyWeightLogDto monthlyWeightDto)
        {
            await _dailyLogService.LogMonthlyWeightAsync(userId, monthlyWeightDto);
            return Ok();
        }

        [HttpGet("weekly-summary")]
        public async Task<ActionResult<WeeklyProgressSummaryDto>> GetWeeklySummary(int userId)
        {
            var summary = await _dailyLogService.GetWeeklySummaryAsync(userId);
            return Ok(summary);
        }
    }
}
