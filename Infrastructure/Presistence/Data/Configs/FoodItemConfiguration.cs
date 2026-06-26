using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Presistence.Data.Configs
{
    public class FoodItemConfiguration : IEntityTypeConfiguration<FoodItem>
    {
        public void Configure(EntityTypeBuilder<FoodItem> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.Cost).HasPrecision(18, 2);
            builder.Property(f => f.Calories).HasPrecision(18, 2);
            builder.Property(f => f.NetCarbs).HasPrecision(18, 2);
            builder.Property(f => f.Fiber).HasPrecision(18, 2);
            builder.Property(f => f.SaturatedFats).HasPrecision(18, 4);
            builder.Property(f => f.PortionLimitMax).HasPrecision(18, 2);
            builder.Property(f => f.PortionLimitMin).HasPrecision(18, 2);
            builder.Property(f => f.Category).HasMaxLength(10);
            builder.Property(f => f.FoodFamily).HasMaxLength(50);

            builder.HasOne(f => f.FoodExchange)
                    .WithMany(fe => fe.FoodItems)
                    .HasForeignKey(f => f.FoodExchangeId)
                    .OnDelete(DeleteBehavior.Restrict);
            

        }
    }
}
