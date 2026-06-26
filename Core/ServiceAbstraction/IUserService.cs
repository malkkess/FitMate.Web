using Shared.DataTransferObject;

namespace ServiceAbstraction
{
    public interface IUserService
    {
        Task<UserProfileDto> GetProfileAsync(int userId);
        Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateUserProfileDto updateDto);
    }
}
