using DomainLayer.Contracts;
using DomainLayer.Models;
using ServiceAbstraction;
using Shared.DataTransferObject;

namespace Service
{
    public class MealPlanQueryService : IMealPlanQueryService
    {
        private readonly IUnitOfWork _uow;

        public MealPlanQueryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<DailyMealPlanDto?> GetTodayPlanAsync(int userId)
        {
            var today = DateTime.Today;
            var plans = await _uow.GetRepository<MealPlan, int>().GetAllAsync();
            var plan = plans
                .Where(p => p.UserId == userId && !p.IsDeleted && p.Date.Date == today)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            return plan is null ? null : await MapPlanAsync(plan);
        }

        public async Task<DailyMealPlanDto?> GetPlanByIdAsync(int userId, int planId)
        {
            var plan = await _uow.GetRepository<MealPlan, int>().GetByIdAsync(planId);
            if (plan is null || plan.UserId != userId || plan.IsDeleted)
            {
                return null;
            }

            return await MapPlanAsync(plan);
        }

        public async Task<IReadOnlyList<MealPlanSummaryDto>> GetUserPlansAsync(int userId)
        {
            var plans = (await _uow.GetRepository<MealPlan, int>().GetAllAsync())
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .OrderByDescending(p => p.Date)
                .ToList();

            if (plans.Count == 0)
            {
                return Array.Empty<MealPlanSummaryDto>();
            }

            var planIds = plans.Select(p => p.Id).ToHashSet();
            var meals = (await _uow.GetRepository<Meal, int>().GetAllAsync())
                .Where(m => planIds.Contains(m.MealPlanId))
                .ToList();

            var mealIds = meals.Select(m => m.Id).ToHashSet();
            var ingredients = (await _uow.GetRepository<MealIngredient, int>().GetAllAsync())
                .Where(i => mealIds.Contains(i.MealId))
                .ToList();

            var foodIds = ingredients.Select(i => i.FoodItemId).Distinct().ToHashSet();
            var foods = (await _uow.GetRepository<FoodItem, int>().GetAllAsync())
                .Where(f => foodIds.Contains(f.Id))
                .ToDictionary(f => f.Id);

            return plans.Select(plan =>
            {
                var planMeals = meals
                    .Where(m => m.MealPlanId == plan.Id)
                    .OrderBy(m => m.Type)
                    .ToList();

                var planMealIds = planMeals.Select(m => m.Id).ToHashSet();
                var planIngredients = ingredients
                    .Where(i => planMealIds.Contains(i.MealId))
                    .ToList();

                var totalCalories = planIngredients
                    .Sum(i => MapIngredient(i, foods[i.FoodItemId]).Calories);

                return new MealPlanSummaryDto
                {
                    PlanId = plan.Id,
                    Date = plan.Date,
                    TotalDayCalories = Math.Round(totalCalories, 2),
                    Meals = planMeals.Select(meal =>
                    {
                        var mealIngredients = planIngredients
                            .Where(i => i.MealId == meal.Id)
                            .ToList();

                        return new MealPlanSummaryMealDto
                        {
                            MealId = meal.Id,
                            MealType = meal.Type.ToString(),
                            TotalCalories = Math.Round(
                                mealIngredients.Sum(i => MapIngredient(i, foods[i.FoodItemId]).Calories),
                                2),
                            FoodItems = mealIngredients
                                .Where(i => foods.ContainsKey(i.FoodItemId))
                                .Select(i => foods[i.FoodItemId].Name)
                                .ToList(),
                        };
                    }).ToList(),
                };
            }).ToList();
        }

        private async Task<DailyMealPlanDto> MapPlanAsync(MealPlan plan)
        {
            var meals = (await _uow.GetRepository<Meal, int>().GetAllAsync())
                .Where(m => m.MealPlanId == plan.Id)
                .OrderBy(m => m.Type)
                .ToList();

            var mealIds = meals.Select(m => m.Id).ToHashSet();
            var ingredients = (await _uow.GetRepository<MealIngredient, int>().GetAllAsync())
                .Where(i => mealIds.Contains(i.MealId))
                .ToList();

            var foodIds = ingredients.Select(i => i.FoodItemId).Distinct().ToHashSet();
            var foods = (await _uow.GetRepository<FoodItem, int>().GetAllAsync())
                .Where(f => foodIds.Contains(f.Id))
                .ToDictionary(f => f.Id);

            var mealDtos = meals.Select(meal =>
            {
                var ingredientDtos = ingredients
                    .Where(i => i.MealId == meal.Id)
                    .Select(i => MapIngredient(i, foods[i.FoodItemId]))
                    .ToList();

                return new MealDto
                {
                    Id = meal.Id,
                    MealType = meal.Type.ToString(),
                    TotalCalories = ingredientDtos.Sum(x => x.Calories),
                    Ingredients = ingredientDtos,
                };
            }).ToList();

            var allIngredients = mealDtos.SelectMany(m => m.Ingredients).ToList();

            return new DailyMealPlanDto
            {
                PlanId = plan.Id,
                Date = plan.Date,
                TotalDayCalories = mealDtos.Sum(m => m.TotalCalories),
                TotalDayProtein = allIngredients.Sum(i => i.Protein),
                TotalDayCarbs = allIngredients.Sum(i => i.Carbs),
                TotalDayFats = allIngredients.Sum(i => i.Fats),
                Meals = mealDtos,
            };
        }

        private static MealIngredientDto MapIngredient(
            MealIngredient ingredient,
            FoodItem food)
        {
            var factor = ingredient.Quantity / 100.0;

            return new MealIngredientDto
            {
                Id = ingredient.Id,
                FoodName = food.Name,
                Grams = ingredient.Quantity,
                Calories = ingredient.Calories > 0 ? ingredient.Calories : food.Calories * factor,
                Protein = ingredient.Protein > 0 ? ingredient.Protein : food.Protein * factor,
                Fats = ingredient.Fats > 0 ? ingredient.Fats : food.Fats * factor,
                Carbs = ingredient.Carbs > 0 ? ingredient.Carbs : food.Carbs * factor,
                Fibers = food.Fiber * factor,
                IsEaten = false,
            };
        }
    }
}
