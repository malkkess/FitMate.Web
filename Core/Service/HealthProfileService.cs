using DomainLayer.Contracts;
using DomainLayer.Models;
using ServiceAbstraction;
using Shared.DataTransferObject;

namespace Service
{
    public class HealthProfileService : IHealthProfileService
    {
        private readonly IUnitOfWork _uow;

        public HealthProfileService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<HealthProfileAnalysisDto> AnalyzeAsync(int userId)
        {
            var user = await GetUserOrThrowAsync(userId);
            var health = await GetHealthProfileAsync(userId);
            return HealthProfileMapper.MapUserHealthProfile(user, health);
        }

        public async Task<HealthProfileAnalysisDto> GetProfileAsync(int userId)
        {
            return await AnalyzeAsync(userId);
        }

        public async Task<HealthProfileAnalysisDto> UpdateProfileAsync(
            int userId,
            UpdateHealthProfileDto updateDto)
        {
            var user = await GetUserOrThrowAsync(userId);
            var health = await GetHealthProfileAsync(userId)
                         ?? throw new InvalidOperationException($"Health profile for user {userId} not found.");

            HealthProfileMapper.ApplyHealthProfile(health, updateDto);
            _uow.GetRepository<HealthProfile, int>().Update(health);
            await _uow.SaveChangesAsync();

            return HealthProfileMapper.MapUserHealthProfile(user, health);
        }

        public async Task<HealthProfileAnalysisDto> PatchProfileAsync(
            int userId,
            PartialUpdateHealthProfileDto updateDto)
        {
            var user = await GetUserOrThrowAsync(userId);
            var health = await GetHealthProfileAsync(userId)
                         ?? throw new InvalidOperationException($"Health profile for user {userId} not found.");

            HealthProfileMapper.ApplyHealthProfile(health, updateDto);
            _uow.GetRepository<HealthProfile, int>().Update(health);
            await _uow.SaveChangesAsync();

            return HealthProfileMapper.MapUserHealthProfile(user, health);
        }

        private async Task<User> GetUserOrThrowAsync(int userId)
        {
            return await _uow.GetRepository<User, int>().GetByIdAsync(userId)
                   ?? throw new InvalidOperationException($"User {userId} not found.");
        }

        private async Task<HealthProfile?> GetHealthProfileAsync(int userId)
        {
            var profiles = await _uow.GetRepository<HealthProfile, int>().GetAllAsync();
            return profiles.FirstOrDefault(h => h.UserId == userId);
        }
    }
}
