using DomainLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configs
{
    public class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode>
    {
        public void Configure(EntityTypeBuilder<PasswordResetCode> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Email).IsRequired().HasMaxLength(150);
            builder.Property(c => c.CodeHash).IsRequired().HasMaxLength(256);
            builder.HasIndex(c => c.Email);
        }
    }
}
