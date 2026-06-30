using DomainLayer.Contracts;
using DomainLayer.Models;
using ServiceAbstraction;
using Shared.DataTransferObject;
using Shared.Enums;

namespace Service
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;

        public UserService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            var user = await GetUserOrThrowAsync(userId);
            var health = await GetHealthProfileAsync(userId);

            return MapToProfileDto(user, health);
        }

        public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateUserProfileDto updateDto)
        {
            InputValidation.ValidateProfile(
                updateDto.FullName,
                updateDto.Age,
                updateDto.Height,
                updateDto.Weight,
                updateDto.Gender,
                updateDto.ActivityLevel,
                updateDto.Goal,
                updateDto.DiabetesStatus);

            var user = await GetUserOrThrowAsync(userId);
            var health = await GetHealthProfileAsync(userId)
                         ?? throw new Exception($"Health profile for user {userId} not found");

            user.FullName = updateDto.FullName;
            user.Age = updateDto.Age;
            user.Height = updateDto.Height;
            user.CurrentWeight = updateDto.Weight;
            user.Gender = updateDto.Gender;
            user.ActivityLevel = updateDto.ActivityLevel;
            user.Goal = updateDto.Goal;
            user.UpdatedAt = DateTime.UtcNow;

            ApplyHealthProfile(health, updateDto);

            _uow.GetRepository<User, int>().Update(user);
            _uow.GetRepository<HealthProfile, int>().Update(health);
            await _uow.SaveChangesAsync();

            return HealthProfileMapper.MapToProfileDto(user, health);
        }

        public async Task<UserProfileDto> PatchProfileAsync(int userId, PartialUpdateUserProfileDto updateDto)
        {
            InputValidation.ValidatePartialProfile(updateDto);

            var user = await GetUserOrThrowAsync(userId);
            var health = await GetHealthProfileAsync(userId)
                         ?? throw new Exception($"Health profile for user {userId} not found");

            if (!string.IsNullOrWhiteSpace(updateDto.FullName))
                user.FullName = updateDto.FullName;

            if (updateDto.Age.HasValue)
                user.Age = updateDto.Age.Value;

            if (updateDto.Height.HasValue)
                user.Height = updateDto.Height.Value;

            if (updateDto.Weight.HasValue)
                user.CurrentWeight = updateDto.Weight.Value;

            if (updateDto.Gender.HasValue)
                user.Gender = updateDto.Gender.Value;

            if (updateDto.ActivityLevel.HasValue)
                user.ActivityLevel = updateDto.ActivityLevel.Value;

            if (updateDto.Goal.HasValue)
                user.Goal = updateDto.Goal.Value;

            user.UpdatedAt = DateTime.UtcNow;

            HealthProfileMapper.ApplyHealthProfile(health, updateDto);

            _uow.GetRepository<User, int>().Update(user);
            _uow.GetRepository<HealthProfile, int>().Update(health);
            await _uow.SaveChangesAsync();

            return HealthProfileMapper.MapToProfileDto(user, health);
        }

        private async Task<User> GetUserOrThrowAsync(int userId)
        {
            return await _uow.GetRepository<User, int>().GetByIdAsync(userId)
                   ?? throw new Exception($"User {userId} not found");
        }

        private async Task<HealthProfile?> GetHealthProfileAsync(int userId)
        {
            var profiles = await _uow.GetRepository<HealthProfile, int>().GetAllAsync();
            return profiles.FirstOrDefault(h => h.UserId == userId);
        }

        private static UserProfileDto MapToProfileDto(User user, HealthProfile? health) =>
            HealthProfileMapper.MapToProfileDto(user, health);

        private static void ApplyHealthProfile(HealthProfile health, UpdateUserProfileDto updateDto) =>
            HealthProfileMapper.ApplyHealthProfile(health, updateDto);
    }
}
