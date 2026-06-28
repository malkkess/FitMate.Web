using DomainLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configs
{
    public class UserOptimizerStateConfiguration : IEntityTypeConfiguration<UserOptimizerState>
    {
        public void Configure(EntityTypeBuilder<UserOptimizerState> builder)
        {
            builder.HasKey(s => s.Id);
            builder.HasIndex(s => s.UserId).IsUnique();

            builder.HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<UserOptimizerState>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
