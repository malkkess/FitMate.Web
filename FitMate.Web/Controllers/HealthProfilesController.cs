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
    [Route("api/users/{userId:int}/health-profile")]
    public class HealthProfilesController : ControllerBase
    {
        private readonly IHealthProfileService _healthProfileService;

        public HealthProfilesController(IHealthProfileService healthProfileService)
        {
            _healthProfileService = healthProfileService;
        }

        [HttpGet]
        public async Task<ActionResult<HealthProfileAnalysisDto>> GetProfile(int userId)
        {
            var profile = await _healthProfileService.GetProfileAsync(userId);
            return Ok(profile);
        }

        [HttpGet("analysis")]
        public async Task<ActionResult<HealthProfileAnalysisDto>> GetAnalysis(int userId)
        {
            var analysis = await _healthProfileService.AnalyzeAsync(userId);
            return Ok(analysis);
        }

        [HttpPut]
        public async Task<ActionResult<HealthProfileAnalysisDto>> UpdateProfile(
            int userId,
            [FromBody] UpdateHealthProfileDto updateDto)
        {
            try
            {
                var profile = await _healthProfileService.UpdateProfileAsync(userId, updateDto);
                return Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new MessageResponseDto { Message = ex.Message });
            }
        }

        [HttpPatch]
        public async Task<ActionResult<HealthProfileAnalysisDto>> PatchProfile(
            int userId,
            [FromBody] PartialUpdateHealthProfileDto updateDto)
        {
            try
            {
                var profile = await _healthProfileService.PatchProfileAsync(userId, updateDto);
                return Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new MessageResponseDto { Message = ex.Message });
            }
        }
    }
}
