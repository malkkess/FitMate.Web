using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Enums;

namespace DomainLayer.Models
{
    public class User:BaseEntity<int>
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public int Age { get; set; }
        public double CurrentWeight { get; set; } 
        public double Height { get; set; } 
        public Gender Gender { get; set; } 

        public ActivityLevel ActivityLevel { get; set; } 

        public Goal Goal { get; set; } 

       
        public HealthProfile HealthProfile { get; set; } = null!;
        //public ICollection<MealPlan> MealPlans { get; set; } // 1-to-Many
        public virtual ICollection<UserPreference> Preferences { get; set; } = new List<UserPreference>();
        public virtual ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
        public virtual ICollection<DailyLog> DailyLogs { get; set; } = new List<DailyLog>();
        public virtual ICollection<MonthlyWeightLog> MonthlyWeightLogs { get; set; } = new List<MonthlyWeightLog>();
        public virtual ICollection<ProgressWeightLog> ProgressWeightLogs { get; set; } = new List<ProgressWeightLog>();
        public virtual ICollection<MealAdherenceLog> MealAdherenceLogs { get; set; } = new List<MealAdherenceLog>();
    }
}
