using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class DailyLog : BaseEntity<int>
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime LogDate { get; set; }
        public double WaterIntakeLiters { get; set; }
        public double SleepHours { get; set; }
        public int MoodScore { get; set; }
        public bool CompletedWorkout { get; set; }
    }
}
