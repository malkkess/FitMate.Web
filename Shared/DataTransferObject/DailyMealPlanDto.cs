using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataTransferObject
{
    public class DailyMealPlanDto
    {
        public int PlanId { get; set; }
        public DateTime Date { get; set; }
        public double TotalDayCalories { get; set; }
        public double TotalDayProtein { get; set; }
        public double TotalDayCarbs { get; set; }
        public double TotalDayFats { get; set; }
        public PythonAnalysisDto? Analysis { get; set; }

        public List<MealDto> Meals { get; set; } = new();
    }
}
