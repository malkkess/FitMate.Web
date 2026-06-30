using DomainLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configs
{
    public class ProgressWeightLogConfiguration : IEntityTypeConfiguration<ProgressWeightLog>
    {
        public void Configure(EntityTypeBuilder<ProgressWeightLog> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.WeightKg).HasPrecision(18, 2);

            builder.HasOne(x => x.User)
                .WithMany(u => u.ProgressWeightLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.LogDate }).IsUnique();
        }
    }
}
