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

            return MapToProfileDto(user, health);
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

        private static UserProfileDto MapToProfileDto(User user, HealthProfile? health)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Age = user.Age,
                Height = user.Height,
                Weight = user.CurrentWeight,
                Gender = user.Gender,
                ActivityLevel = user.ActivityLevel,
                Goal = user.Goal,
                DiabetesStatus = health?.DiabetesStatus ?? DiabetesStatus.None,
                Allergies = BuildAllergiesList(health),
                MedicalConditions = BuildMedicalConditionsList(health),
            };
        }

        private static void ApplyHealthProfile(HealthProfile health, UpdateUserProfileDto updateDto)
        {
            var allergies = updateDto.Allergies
                .Select(a => a.Trim().ToLowerInvariant())
                .ToHashSet();

            var conditions = updateDto.MedicalConditions
                .Select(c => c.Trim().ToLowerInvariant())
                .ToHashSet();

            health.IsLactoseIntolerant = allergies.Contains("lactose");
            health.IsGlutenAllergic = allergies.Contains("gluten");
            health.IsNutsAllergic = allergies.Contains("nuts");
            health.OtherAllergies = updateDto.OtherAllergies;
            health.DiabetesStatus = updateDto.DiabetesStatus;

            health.HasHypertension = conditions.Contains("hypertension");
            health.HasHeartDisease = conditions.Contains("heart_disease");
            health.UpdatedAt = DateTime.UtcNow;
        }

        private static List<string> BuildAllergiesList(HealthProfile? health)
        {
            var list = new List<string>();
            if (health is null) return list;
            if (health.IsLactoseIntolerant) list.Add("lactose");
            if (health.IsGlutenAllergic) list.Add("gluten");
            if (health.IsNutsAllergic) list.Add("nuts");
            if (!string.IsNullOrWhiteSpace(health.OtherAllergies))
            {
                list.AddRange(health.OtherAllergies
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            }
            return list;
        }

        private static List<string> BuildMedicalConditionsList(HealthProfile? health)
        {
            var list = new List<string>();
            if (health is null) return list;
            if (health.DiabetesStatus != DiabetesStatus.None) list.Add("diabetes");
            if (health.HasHypertension) list.Add("hypertension");
            if (health.HasHeartDisease) list.Add("heart_disease");
            return list;
        }
    }
}
