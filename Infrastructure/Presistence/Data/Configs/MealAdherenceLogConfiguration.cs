using DomainLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configs
{
    public class MealAdherenceLogConfiguration : IEntityTypeConfiguration<MealAdherenceLog>
    {
        public void Configure(EntityTypeBuilder<MealAdherenceLog> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.PlannedCalories).HasPrecision(18, 2);
            builder.Property(a => a.PlannedProtein).HasPrecision(18, 2);
            builder.Property(a => a.PlannedCarbs).HasPrecision(18, 2);
            builder.Property(a => a.PlannedFats).HasPrecision(18, 2);
            builder.Property(a => a.PlannedFiber).HasPrecision(18, 2);
            builder.Property(a => a.EatenCalories).HasPrecision(18, 2);
            builder.Property(a => a.EatenProtein).HasPrecision(18, 2);
            builder.Property(a => a.EatenCarbs).HasPrecision(18, 2);
            builder.Property(a => a.EatenFats).HasPrecision(18, 2);
            builder.Property(a => a.EatenFiber).HasPrecision(18, 2);
            builder.Property(a => a.AdherenceScore).HasPrecision(18, 4);

            builder.HasOne(a => a.User)
                .WithMany(u => u.MealAdherenceLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.MealPlan)
                .WithMany(p => p.AdherenceLogs)
                .HasForeignKey(a => a.MealPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new { a.UserId, a.LogDate }).IsUnique();
        }
    }
}
