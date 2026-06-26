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
            var request = HealthProfileMapper.MapToPythonRequest(user, health, days: 1, adherence);

            var pythonResult = await _pythonLinker.GetOptimizedPlanAsync(request);

            if (pythonResult is null || !pythonResult.Success || pythonResult.Plan is null)
                return null;

            var mealPlan = new MealPlan
            {
                UserId = userId,
                Date = DateTime.Today,
            };

            var mealTypeMap = new Dictionary<string, MealType>
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
                    var foodItems = await _uow.GetRepository<FoodItem, int>().GetAllAsync();
                    var foodItem = foodItems.FirstOrDefault(f =>
                        string.Equals(f.Name, item.Food, StringComparison.OrdinalIgnoreCase));

                    if (foodItem is null) continue;

                    meal.Ingredients.Add(new MealIngredient
                    {
                        FoodItemId = foodItem.Id,
                        Quantity = item.Grams,
                        Calories = item.Calories,
                        Protein = item.Protein,
                        Fats = item.Fat,
                        Carbs = item.Carbs,
                    });
                }

                mealPlan.Meals.Add(meal);
            }

            await _uow.GetRepository<MealPlan, int>().AddAsync(mealPlan);
            await _uow.SaveChangesAsync();

            return MapToDto(mealPlan, pythonResult.Plan);
        }

        private async Task<HealthProfile?> GetHealthProfileAsync(int userId)
        {
            var profiles = await _uow.GetRepository<HealthProfile, int>().GetAllAsync();
            return profiles.FirstOrDefault(h => h.UserId == userId);
        }

        private DailyMealPlanDto MapToDto(
            MealPlan plan,
            Dictionary<string, List<PythonMealItemDto>> pythonPlan)
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
            };
        }
    }
}
