namespace ServiceAbstraction
{
    public interface ITokenService
    {
        string GenerateToken(int userId, string email, string fullName, out DateTime expiresAt);
    }
}
