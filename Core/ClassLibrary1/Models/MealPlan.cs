using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class MealPlan : BaseEntity<int>
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime Date { get; set; }
        public double TotalCalories { get; set; }
        public ICollection<Meal> Meals { get; set; } = new List<Meal>();
        public ICollection<MealAdherenceLog> AdherenceLogs { get; set; } = new List<MealAdherenceLog>();
    }
}
