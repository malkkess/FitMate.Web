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
            var user = await _uow.GetRepository<User, int>().GetByIdAsync(userId)
                       ?? throw new Exception($"User {userId} not found");

            var health = await GetHealthProfileAsync(userId);
            return HealthProfileMapper.MapUserHealthProfile(user, health);
        }

        private async Task<HealthProfile?> GetHealthProfileAsync(int userId)
        {
            var profiles = await _uow.GetRepository<HealthProfile, int>().GetAllAsync();
            return profiles.FirstOrDefault(h => h.UserId == userId);
        }
    }
}
