using ServiceAbstraction;

namespace Service
{
    public class ServiceManager : IServiceManager
    {
        public ServiceManager(
            IUserService userService,
            IAuthService authService,
            IDailyLogService dailyLogService,
            IHealthProfileService healthProfileService,
            IMealPlanService mealPlanService,
            IMealPlanQueryService mealPlanQueryService,
            IPythonLinker pythonLinker)
        {
            UserService = userService;
            AuthService = authService;
            DailyLogService = dailyLogService;
            HealthProfileService = healthProfileService;
            MealPlanService = mealPlanService;
            MealPlanQueryService = mealPlanQueryService;
            PythonLinker = pythonLinker;
        }

        public IUserService UserService { get; }
        public IAuthService AuthService { get; }
        public IDailyLogService DailyLogService { get; }
        public IHealthProfileService HealthProfileService { get; }
        public IMealPlanService MealPlanService { get; }
        public IMealPlanQueryService MealPlanQueryService { get; }
        public IPythonLinker PythonLinker { get; }
    }
}
