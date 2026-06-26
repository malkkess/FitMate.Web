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
    public class MealIngredientConfiguration : IEntityTypeConfiguration<MealIngredient>
    {
        public void Configure(EntityTypeBuilder<MealIngredient> builder)
        {
            builder.HasKey(mi => mi.Id);
            builder.Property(mi => mi.Id).ValueGeneratedOnAdd();
            builder.HasIndex(mi => new { mi.MealId, mi.FoodItemId }).IsUnique();

            builder.Property(mi => mi.Quantity).HasPrecision(18, 2);

            builder.HasOne(mi => mi.Meal)
                   .WithMany(m => m.Ingredients)
                   .HasForeignKey(mi => mi.MealId);

            builder.HasOne(mi => mi.FoodItem)
                   .WithMany() 
                   .HasForeignKey(mi => mi.FoodItemId);
        }
    }
}
