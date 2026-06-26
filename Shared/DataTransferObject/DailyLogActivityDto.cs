using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObject
{
    public class DailyLogActivityDto
    {
        public DateTime? LogDate { get; set; }
        public double WaterIntakeLiters { get; set; }
        public double SleepHours { get; set; }
        public int MoodScore { get; set; }
        public bool CompletedWorkout { get; set; }
    }
}
