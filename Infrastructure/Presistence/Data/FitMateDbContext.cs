using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace Presistence.Data
{
    public class FitMateDbContext : DbContext
    {
        public FitMateDbContext(DbContextOptions<FitMateDbContext> options) : base(options)
        {
        }
        //User
        public DbSet<User> Users { get; set; }
        public DbSet<HealthProfile> HealthProfiles { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }


        public DbSet<FoodItem> FoodItems { get; set; }
        public DbSet<FoodExchange> FoodExchanges { get; set; }

        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<Meal> Meals { get; set; }
        public DbSet<MealIngredient> MealIngredients { get; set; }
        public DbSet<DailyLog> DailyLogs { get; set; }
        public DbSet<MonthlyWeightLog> MonthlyWeightLogs { get; set; }
        public DbSet<MealAdherenceLog> MealAdherenceLogs { get; set; }
        public DbSet<MealAdherenceItem> MealAdherenceItems { get; set; }
        public DbSet<PasswordResetCode> PasswordResetCodes { get; set; }
        public DbSet<UserOptimizerState> UserOptimizerStates { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            // Apply all configurations from the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssemblyReference).Assembly);

        }
    }
}
