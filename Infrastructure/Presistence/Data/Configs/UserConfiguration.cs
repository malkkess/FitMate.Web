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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.HasIndex(u => u.Email).IsUnique(); // Ensure email uniqueness

            builder.HasOne(u => u.HealthProfile)
                   .WithOne(hp => hp.User)
                   .HasForeignKey<HealthProfile>(hp => hp.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
