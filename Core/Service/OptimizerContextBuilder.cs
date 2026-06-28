using DomainLayer.Models;
using Shared.DataTransferObject;
using Shared.Enums;

namespace Service
{
    internal static class OptimizerContextBuilder
    {
        public const int ExclusionLookbackDays = 7;

        private static readonly Dictionary<MealType, string> MealTypeNames = new()
        {
            [MealType.Breakfast] = "Breakfast",
            [MealType.Lunch] = "Lunch",
            [MealType.Dinner] = "Dinner",
            [MealType.Snack] = "Snack",
        };

        public static List<SlotExclusionDto> BuildSlotExclusions(
            IEnumerable<MealPlan> recentPlans,
            IEnumerable<Meal> meals,
            IEnumerable<MealIngredient> ingredients,
            IReadOnlyDictionary<int, FoodItem> foods)
        {
            var exclusions = new Dictionary<(string Meal, int Slot), HashSet<string>>();

            var planIds = recentPlans.Select(p => p.Id).ToHashSet();
            var planMeals = meals.Where(m => planIds.Contains(m.MealPlanId)).ToList();
            var mealLookup = planMeals.ToDictionary(m => m.Id);

            foreach (var ingredient in ingredients)
            {
                if (!mealLookup.TryGetValue(ingredient.MealId, out var meal))
                    continue;

                if (ingredient.SlotIndex is null)
                    continue;

                if (!foods.TryGetValue(ingredient.FoodItemId, out var food))
                    continue;

                if (!MealTypeNames.TryGetValue(meal.Type, out var mealName))
                    continue;

                var key = (mealName, ingredient.SlotIndex.Value);
                if (!exclusions.TryGetValue(key, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    exclusions[key] = set;
                }

                set.Add(food.Name);
            }

            return exclusions
                .Select(kvp => new SlotExclusionDto
                {
                    Meal = kvp.Key.Meal,
                    Slot = kvp.Key.Slot,
                    ExcludedFoods = kvp.Value.OrderBy(n => n).ToList(),
                })
                .OrderBy(x => x.Meal)
                .ThenBy(x => x.Slot)
                .ToList();
        }

        public static int ResolveMasterSeed(UserOptimizerState? state)
        {
            if (state is not null && state.MasterSeed > 0)
                return state.MasterSeed;

            return Random.Shared.Next(1, 100_000);
        }

        public static int ResolveDayNumber(UserOptimizerState? state, int existingPlanCount)
        {
            if (state is not null && state.PlanDayCounter > 0)
                return state.PlanDayCounter + 1;

            return existingPlanCount + 1;
        }
    }
}
