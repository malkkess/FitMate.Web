namespace DomainLayer.Models
{
    public class PasswordResetCode : BaseEntity<int>
    {
        public string Email { get; set; } = null!;
        public string CodeHash { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }
}
