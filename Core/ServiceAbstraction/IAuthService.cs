using Shared.DataTransferObject;

namespace ServiceAbstraction
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<MessageResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<MessageResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
    }
}
