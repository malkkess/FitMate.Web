using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceAbstraction
{
    public interface IServiceManager
    {
        public IUserService UserService { get; }
        public IDailyLogService DailyLogService { get; }
        public IHealthProfileService HealthProfileService { get; }
        public IMealPlanService MealPlanService { get; }
        public IMealPlanQueryService MealPlanQueryService { get; }
        public IPythonLinker PythonLinker { get; }
    }
}
