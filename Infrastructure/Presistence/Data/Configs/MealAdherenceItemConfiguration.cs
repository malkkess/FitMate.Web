using DomainLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configs
{
    public class MealAdherenceItemConfiguration : IEntityTypeConfiguration<MealAdherenceItem>
    {
        public void Configure(EntityTypeBuilder<MealAdherenceItem> builder)
        {
            builder.HasKey(i => i.Id);

            builder.HasOne(i => i.MealAdherenceLog)
                .WithMany(l => l.Items)
                .HasForeignKey(i => i.MealAdherenceLogId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.MealIngredient)
                .WithMany()
                .HasForeignKey(i => i.MealIngredientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(i => new { i.MealAdherenceLogId, i.MealIngredientId }).IsUnique();
        }
    }
}
