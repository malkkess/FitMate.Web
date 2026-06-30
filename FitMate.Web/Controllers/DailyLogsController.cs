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
    [Route("api/users/{userId:int}/daily-logs")]
    public class DailyLogsController : ControllerBase
    {
        private readonly IDailyLogService _dailyLogService;

        public DailyLogsController(IDailyLogService dailyLogService)
        {
            _dailyLogService = dailyLogService;
        }

        [HttpPost]
        public async Task<ActionResult<MessageResponseDto>> LogActivity(
            int userId,
            [FromBody] DailyLogActivityDto activityDto)
        {
            try
            {
                await _dailyLogService.LogActivityAsync(userId, activityDto);
                return Ok(new MessageResponseDto { Message = "Daily activity logged." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new MessageResponseDto { Message = ex.Message });
            }
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
                return BadRequest(new MessageResponseDto { Message = ex.Message });
            }
        }

        [HttpGet("meal-adherence")]
        public async Task<ActionResult<MealAdherenceResponseDto>> GetMealAdherence(
            int userId,
            [FromQuery] DateTime? logDate = null)
        {
            try
            {
                var result = await _dailyLogService.GetMealAdherenceAsync(userId, logDate);
                return result is null ? NotFound() : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new MessageResponseDto { Message = ex.Message });
            }
        }

        [HttpPost("monthly-weight")]
        public async Task<ActionResult<MessageResponseDto>> LogMonthlyWeight(
            int userId,
            [FromBody] MonthlyWeightLogDto monthlyWeightDto)
        {
            try
            {
                await _dailyLogService.LogMonthlyWeightAsync(userId, monthlyWeightDto);
                return Ok(new MessageResponseDto
                {
                    Message = "Weight updated. Generate a new plan.",
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new MessageResponseDto { Message = ex.Message });
            }
        }

        [HttpPost("progress-weight")]
        public async Task<ActionResult<MessageResponseDto>> LogProgressWeight(
            int userId,
            [FromBody] ProgressWeightLogDto progressWeightDto)
        {
            try
            {
                await _dailyLogService.LogProgressWeightAsync(userId, progressWeightDto);
                return Ok(new MessageResponseDto
                {
                    Message = "Progress weight logged.",
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new MessageResponseDto { Message = ex.Message });
            }
        }

        [HttpGet("weekly-summary")]
        public async Task<ActionResult<WeeklyProgressSummaryDto>> GetWeeklySummary(int userId)
        {
            var summary = await _dailyLogService.GetWeeklySummaryAsync(userId);
            return Ok(summary);
        }
    }
}
