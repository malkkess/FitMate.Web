using Shared.DataTransferObject;

namespace ServiceAbstraction
{
    public interface IHealthProfileService
    {
        Task<HealthProfileAnalysisDto> AnalyzeAsync(int userId);
        Task<HealthProfileAnalysisDto> GetProfileAsync(int userId);
        Task<HealthProfileAnalysisDto> UpdateProfileAsync(int userId, UpdateHealthProfileDto updateDto);
        Task<HealthProfileAnalysisDto> PatchProfileAsync(int userId, PartialUpdateHealthProfileDto updateDto);
    }
}
