using Microsoft.AspNetCore.Mvc;
using ServiceAbstraction;
using Shared.DataTransferObject;

namespace FitMate.Web.Controllers
{
    [ApiController]
    [Route("api/users/{userId:int}/health-profile")]
    public class HealthProfilesController : ControllerBase
    {
        private readonly IHealthProfileService _healthProfileService;

        public HealthProfilesController(IHealthProfileService healthProfileService)
        {
            _healthProfileService = healthProfileService;
        }

        [HttpGet("analysis")]
        public async Task<ActionResult<HealthProfileAnalysisDto>> GetAnalysis(int userId)
        {
            var analysis = await _healthProfileService.AnalyzeAsync(userId);
            return Ok(analysis);
        }
    }
}
