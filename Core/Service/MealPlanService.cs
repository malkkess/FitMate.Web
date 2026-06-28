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

            var health = await GetHealthProfileAsync(userId);
            var adherence = await _dailyLogService.GetAdherenceContextAsync(userId);
            var optimizerState = await GetOptimizerStateAsync(userId);
            var slotExclusions = await BuildSlotExclusionsAsync(userId);

            var existingPlanCount = (await _uow.GetRepository<MealPlan, int>().GetAllAsync())
                .Count(p => p.UserId == userId);

            var masterSeed = OptimizerContextBuilder.ResolveMasterSeed(optimizerState);
            var dayNumber = OptimizerContextBuilder.ResolveDayNumber(optimizerState, existingPlanCount);

            var request = HealthProfileMapper.MapToPythonRequest(
                user,
                health,
                days: 1,
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

            var mealPlan = new MealPlan
            {
                UserId = userId,
                Date = DateTime.Today,
            };

            var mealTypeMap = new Dictionary<string, MealType>(StringComparer.OrdinalIgnoreCase)
            {
                ["Breakfast"] = MealType.Breakfast,
                ["Lunch"] = MealType.Lunch,
                ["Dinner"] = MealType.Dinner,
                ["Snack"] = MealType.Snack,
            };

            foreach (var (mealName, items) in pythonResult.Plan)
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
            await UpsertOptimizerStateAsync(userId, optimizerState, masterSeed, dayNumber);
            await _uow.SaveChangesAsync();

            return MapToDto(mealPlan, pythonResult.Plan, pythonResult.Analysis);
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
                .Where(p => p.UserId == userId && p.Date.Date >= cutoff)
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
