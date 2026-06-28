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
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{userId:int}")]
        public async Task<ActionResult<UserProfileDto>> GetProfile(int userId)
        {
            var profile = await _userService.GetProfileAsync(userId);
            return Ok(profile);
        }

        [HttpPut("{userId:int}")]
        public async Task<ActionResult<UserProfileDto>> UpdateProfile(
            int userId,
            [FromBody] UpdateUserProfileDto updateDto)
        {
            var profile = await _userService.UpdateProfileAsync(userId, updateDto);
            return Ok(profile);
        }

        [HttpPatch("{userId:int}")]
        public async Task<ActionResult<UserProfileDto>> PatchProfile(
            int userId,
            [FromBody] PartialUpdateUserProfileDto updateDto)
        {
            var profile = await _userService.PatchProfileAsync(userId, updateDto);
            return Ok(profile);
        }
    }
}
