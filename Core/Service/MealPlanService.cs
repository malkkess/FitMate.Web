using AutoMapper;
using DomainLayer.Contracts;
using DomainLayer.Models;
using ServiceAbstraction;
using Shared.DataTransferObject;
using Shared.Enums;

namespace Service
{
    public class MealPlanService : IMealPlanService
    {
        private const int GeneratedPlanDays = 6;

        private readonly IUnitOfWork _uow;
        private readonly IPythonLinker _pythonLinker;
        private readonly IDailyLogService _dailyLogService;
        private readonly IMapper _mapper;

        public MealPlanService(
            IUnitOfWork uow,
            IPythonLinker pythonLinker,
            IDailyLogService dailyLogService,
            IMapper mapper)
        {
            _uow = uow;
            _pythonLinker = pythonLinker;
            _dailyLogService = dailyLogService;
            _mapper = mapper;
        }

        public async Task<DailyMealPlanDto?> GeneratePlanAsync(int userId)
        {
            var user = await _uow.GetRepository<User, int>().GetByIdAsync(userId)
                       ?? throw new Exception($"User {userId} not found");

            var today = DateTime.Today;
            var allUserPlans = (await _uow.GetRepository<MealPlan, int>().GetAllAsync())
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .ToList();

            var latestPlan = allUserPlans
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            var latestMonthlyWeight = await GetLatestMonthlyWeightAsync(userId);
            var hasNewMonthlyWeight = latestPlan is not null
                && latestMonthlyWeight is not null
                && (latestMonthlyWeight.UpdatedAt ?? latestMonthlyWeight.CreatedAt) > latestPlan.CreatedAt;

            var activePlanEndDate = allUserPlans
                .Where(p => p.Date.Date >= today)
                .Select(p => (DateTime?)p.Date.Date)
                .Max();

            if (activePlanEndDate is not null && !hasNewMonthlyWeight)
            {
                throw new InvalidOperationException(
                    $"You already have an active plan until {activePlanEndDate.Value:dd MMMM yyyy}. Generate a new plan after it ends.");
            }

            if (hasNewMonthlyWeight)
            {
                SoftDeletePlansFromDate(allUserPlans, today);
            }

            var health = await GetHealthProfileAsync(userId);
            var adherence = await _dailyLogService.GetAdherenceContextAsync(userId);
            var optimizerState = await GetOptimizerStateAsync(userId);
            var slotExclusions = await BuildSlotExclusionsAsync(userId);

            var existingPlanCount = allUserPlans.Count(p => p.Date.Date < today);

            var masterSeed = OptimizerContextBuilder.ResolveMasterSeed(optimizerState);
            var dayNumber = OptimizerContextBuilder.ResolveDayNumber(optimizerState, existingPlanCount);

            var request = HealthProfileMapper.MapToPythonRequest(
                user,
                health,
                days: GeneratedPlanDays,
                adherence: adherence,
                masterSeed: masterSeed,
                dayNumber: dayNumber,
                slotExclusions: slotExclusions);

            var pythonResult = await _pythonLinker.GetOptimizedPlanAsync(request);

            if (pythonResult is null || !pythonResult.Success || pythonResult.Plan is null)
                return null;

            var foodsByName = (await _uow.GetRepository<FoodItem, int>().GetAllAsync())
                .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var mealTypeMap = new Dictionary<string, MealType>(StringComparer.OrdinalIgnoreCase)
            {
                ["Breakfast"] = MealType.Breakfast,
                ["Lunch"] = MealType.Lunch,
                ["Dinner"] = MealType.Dinner,
                ["Snack"] = MealType.Snack,
            };

            var generatedPlans = ResolveGeneratedPlans(pythonResult, dayNumber);
            if (generatedPlans.Count != GeneratedPlanDays)
                return null;

            var savedPlans = new List<(MealPlan Plan, Dictionary<string, List<PythonMealItemDto>> PythonPlan)>();

            foreach (var generatedPlan in generatedPlans)
            {
                var pythonPlan = generatedPlan.Plan;
                var offset = Math.Max(0, generatedPlan.DayNumber - dayNumber);
                var mealPlan = new MealPlan
                {
                    UserId = userId,
                    Date = today.AddDays(offset),
                    TotalCalories = pythonPlan.Values.SelectMany(i => i).Sum(i => i.Calories),
                };

                foreach (var (mealName, items) in pythonPlan)
                {
                    if (!mealTypeMap.TryGetValue(mealName, out var mealType)) continue;

                    var meal = new Meal { Type = mealType };

                    foreach (var item in items)
                    {
                        if (!foodsByName.TryGetValue(item.Food, out var foodItem))
                            continue;

                        meal.Ingredients.Add(new MealIngredient
                        {
                            FoodItemId = foodItem.Id,
                            Quantity = item.Grams,
                            Calories = item.Calories,
                            Protein = item.Protein,
                            Fats = item.Fat,
                            Carbs = item.Carbs,
                            NetCarbs = item.NetCarbs,
                            SaturatedFats = item.SatFat,
                            SlotIndex = item.Slot,
                            Category = string.IsNullOrWhiteSpace(item.Category) ? foodItem.Category : item.Category,
                            FoodFamily = string.IsNullOrWhiteSpace(item.FoodFamily) ? foodItem.FoodFamily : item.FoodFamily,
                        });
                    }

                    mealPlan.Meals.Add(meal);
                }

                await _uow.GetRepository<MealPlan, int>().AddAsync(mealPlan);
                savedPlans.Add((mealPlan, pythonPlan));
            }

            var lastGeneratedDay = generatedPlans.Max(p => p.DayNumber);
            await UpsertOptimizerStateAsync(userId, optimizerState, masterSeed, lastGeneratedDay);
            await _uow.SaveChangesAsync();

            var firstPlan = savedPlans.First();
            return MapToDto(firstPlan.Plan, firstPlan.PythonPlan, pythonResult.Analysis);
        }

        private void SoftDeletePlansFromDate(IEnumerable<MealPlan> plans, DateTime fromDate)
        {
            var repo = _uow.GetRepository<MealPlan, int>();

            foreach (var plan in plans.Where(p => p.Date.Date >= fromDate.Date))
            {
                plan.IsDeleted = true;
                plan.UpdatedAt = DateTime.UtcNow;
                repo.Update(plan);
            }
        }

        private static List<(int DayNumber, Dictionary<string, List<PythonMealItemDto>> Plan)> ResolveGeneratedPlans(
            PythonOutputDto pythonResult,
            int fallbackDayNumber)
        {
            var plans = pythonResult.Plans
                .Where(p => p.Success && p.Plan is not null)
                .Select(p => (p.DayNumber, p.Plan!))
                .ToList();

            if (plans.Count > 0)
                return plans;

            return pythonResult.Plan.Count == 0
                ? new List<(int DayNumber, Dictionary<string, List<PythonMealItemDto>> Plan)>()
                : new List<(int DayNumber, Dictionary<string, List<PythonMealItemDto>> Plan)>
                {
                    (fallbackDayNumber, pythonResult.Plan),
                };
        }

        private async Task UpsertOptimizerStateAsync(
            int userId,
            UserOptimizerState? state,
            int masterSeed,
            int dayNumber)
        {
            var repo = _uow.GetRepository<UserOptimizerState, int>();

            if (state is null)
            {
                await repo.AddAsync(new UserOptimizerState
                {
                    UserId = userId,
                    MasterSeed = masterSeed,
                    PlanDayCounter = dayNumber,
                });
                return;
            }

            state.MasterSeed = masterSeed;
            state.PlanDayCounter = dayNumber;
            state.UpdatedAt = DateTime.UtcNow;
            repo.Update(state);
        }

        private async Task<List<SlotExclusionDto>> BuildSlotExclusionsAsync(int userId)
        {
            var cutoff = DateTime.UtcNow.Date.AddDays(-OptimizerContextBuilder.ExclusionLookbackDays);

            var plans = (await _uow.GetRepository<MealPlan, int>().GetAllAsync())
                .Where(p => p.UserId == userId && !p.IsDeleted && p.Date.Date >= cutoff)
                .OrderByDescending(p => p.Date)
                .ToList();

            if (plans.Count == 0)
                return new List<SlotExclusionDto>();

            var meals = await _uow.GetRepository<Meal, int>().GetAllAsync();
            var ingredients = await _uow.GetRepository<MealIngredient, int>().GetAllAsync();
            var foods = (await _uow.GetRepository<FoodItem, int>().GetAllAsync())
                .ToDictionary(f => f.Id);

            return OptimizerContextBuilder.BuildSlotExclusions(plans, meals, ingredients, foods);
        }

        private async Task<UserOptimizerState?> GetOptimizerStateAsync(int userId)
        {
            var states = await _uow.GetRepository<UserOptimizerState, int>().GetAllAsync();
            return states.FirstOrDefault(s => s.UserId == userId);
        }

        private async Task<MonthlyWeightLog?> GetLatestMonthlyWeightAsync(int userId)
        {
            var logs = await _uow.GetRepository<MonthlyWeightLog, int>().GetAllAsync();
            return logs
                .Where(l => l.UserId == userId && !l.IsDeleted)
                .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
                .FirstOrDefault();
        }

        private async Task<HealthProfile?> GetHealthProfileAsync(int userId)
        {
            var profiles = await _uow.GetRepository<HealthProfile, int>().GetAllAsync();
            return profiles.FirstOrDefault(h => h.UserId == userId);
        }

        private DailyMealPlanDto MapToDto(
            MealPlan plan,
            Dictionary<string, List<PythonMealItemDto>> pythonPlan,
            PythonAnalysisDto? analysis)
        {
            var meals = _mapper.Map<List<MealDto>>(pythonPlan.ToList());

            return new DailyMealPlanDto
            {
                PlanId = plan.Id,
                Date = plan.Date,
                TotalDayCalories = meals.Sum(m => m.TotalCalories),
                TotalDayProtein = pythonPlan.Values.SelectMany(i => i).Sum(i => i.Protein),
                TotalDayCarbs = pythonPlan.Values.SelectMany(i => i).Sum(i => i.Carbs),
                TotalDayFats = pythonPlan.Values.SelectMany(i => i).Sum(i => i.Fat),
                Meals = meals,
                Analysis = analysis,
            };
        }
    }
}
