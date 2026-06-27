namespace ServiceAbstraction
{
    public interface IEmailService
    {
        Task SendPasswordResetCodeAsync(string email, string code);
    }
}
