using DomainLayer.Contracts;
using DomainLayer.Models;
using ServiceAbstraction;
using Shared.DataTransferObject;
using Shared.Enums;

namespace Service
{
    public class DailyLogService : IDailyLogService
    {
        private readonly IUnitOfWork _uow;

        public DailyLogService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task LogActivityAsync(int userId, DailyLogActivityDto activityDto)
        {
            var logDate = (activityDto.LogDate ?? DateTime.UtcNow).Date;
            var logRepo = _uow.GetRepository<DailyLog, int>();
            var logs = await logRepo.GetAllAsync();

            var existingLog = logs.FirstOrDefault(l => l.UserId == userId && l.LogDate.Date == logDate);
            if (existingLog is null)
            {
                var newLog = new DailyLog
                {
                    UserId = userId,
                    LogDate = logDate,
                    WaterIntakeLiters = activityDto.WaterIntakeLiters,
                    SleepHours = activityDto.SleepHours,
                    MoodScore = activityDto.MoodScore,
                    CompletedWorkout = activityDto.CompletedWorkout,
                };

                await logRepo.AddAsync(newLog);
            }
            else
            {
                existingLog.WaterIntakeLiters = activityDto.WaterIntakeLiters;
                existingLog.SleepHours = activityDto.SleepHours;
                existingLog.MoodScore = activityDto.MoodScore;
                existingLog.CompletedWorkout = activityDto.CompletedWorkout;
                existingLog.UpdatedAt = DateTime.UtcNow;
                logRepo.Update(existingLog);
            }

            await _uow.SaveChangesAsync();
        }

        public async Task<MealAdherenceResponseDto> LogMealAdherenceAsync(
            int userId,
            MealAdherenceRequestDto request)
        {
            var logDate = (request.LogDate ?? DateTime.UtcNow).Date;

            var plan = await _uow.GetRepository<MealPlan, int>().GetByIdAsync(request.MealPlanId)
                       ?? throw new InvalidOperationException($"Meal plan {request.MealPlanId} not found.");

            if (plan.UserId != userId)
                throw new InvalidOperationException("Meal plan does not belong to this user.");

            var planIngredients = await GetPlanIngredientsAsync(plan.Id);
            if (planIngredients.Count == 0)
                throw new InvalidOperationException("Meal plan has no ingredients to track.");

            var foods = await LoadFoodsForIngredientsAsync(planIngredients);

            ValidateIngredientIds(planIngredients, request.Items);

            var eatenLookup = request.Items.ToDictionary(i => i.MealIngredientId, i => i.IsEaten);

            var planned = SumMacros(planIngredients, _ => true, foods);
            var eaten = SumMacros(
                planIngredients,
                i => eatenLookup.GetValueOrDefault(i.Id, false),
                foods);

            var adherenceRepo = _uow.GetRepository<MealAdherenceLog, int>();
            var itemRepo = _uow.GetRepository<MealAdherenceItem, int>();
            var allLogs = await adherenceRepo.GetAllAsync();
            var existing = allLogs.FirstOrDefault(l => l.UserId == userId && l.LogDate.Date == logDate);

            if (existing is null)
            {
                existing = new MealAdherenceLog
                {
                    UserId = userId,
                    LogDate = logDate,
                    MealPlanId = plan.Id,
                };
                await adherenceRepo.AddAsync(existing);
            }
            else
            {
                existing.MealPlanId = plan.Id;
                existing.UpdatedAt = DateTime.UtcNow;
                adherenceRepo.Update(existing);
            }

            ApplyPlannedTotals(existing, planned);
            ApplyEatenTotals(existing, eaten);
            existing.AdherenceScore = ComputeAdherenceScore(existing.PlannedCalories, existing.EatenCalories);

            await _uow.SaveChangesAsync();

            var allItems = await itemRepo.GetAllAsync();
            var existingItems = allItems.Where(i => i.MealAdherenceLogId == existing.Id).ToList();

            foreach (var ingredient in planIngredients)
            {
                var isEaten = eatenLookup.GetValueOrDefault(ingredient.Id, false);
                var item = existingItems.FirstOrDefault(i => i.MealIngredientId == ingredient.Id);

                if (item is null)
                {
                    await itemRepo.AddAsync(new MealAdherenceItem
                    {
                        MealAdherenceLogId = existing.Id,
                        MealIngredientId = ingredient.Id,
                        IsEaten = isEaten,
                    });
                }
                else
                {
                    item.IsEaten = isEaten;
                    item.UpdatedAt = DateTime.UtcNow;
                    itemRepo.Update(item);
                }
            }

            await _uow.SaveChangesAsync();

            return await MapAdherenceResponseAsync(existing, planIngredients);
        }

        public async Task<MealAdherenceResponseDto?> GetMealAdherenceAsync(int userId, DateTime? logDate = null)
        {
            var date = (logDate ?? DateTime.UtcNow).Date;
            var allLogs = await _uow.GetRepository<MealAdherenceLog, int>().GetAllAsync();
            var log = allLogs.FirstOrDefault(l => l.UserId == userId && l.LogDate.Date == date);
            if (log is null) return null;

            var planIngredients = await GetPlanIngredientsAsync(log.MealPlanId);
            return await MapAdherenceResponseAsync(log, planIngredients);
        }

        public async Task<AdherenceContextDto> GetAdherenceContextAsync(int userId)
        {
            var cutoff = DateTime.UtcNow.Date.AddDays(-7);
            var logs = (await _uow.GetRepository<MealAdherenceLog, int>().GetAllAsync())
                .Where(l => l.UserId == userId && l.LogDate.Date >= cutoff)
                .ToList();

            return AdherenceContextBuilder.Build(logs);
        }

        public async Task LogMonthlyWeightAsync(int userId, MonthlyWeightLogDto monthlyWeightDto)
        {
            var now = DateTime.UtcNow;
            var year = monthlyWeightDto.Year ?? now.Year;
            var month = monthlyWeightDto.Month ?? now.Month;

            var monthlyRepo = _uow.GetRepository<MonthlyWeightLog, int>();
            var allMonthly = await monthlyRepo.GetAllAsync();
            var existing = allMonthly.FirstOrDefault(x =>
                x.UserId == userId && x.Year == year && x.Month == month);

            if (existing is null)
            {
                await monthlyRepo.AddAsync(new MonthlyWeightLog
                {
                    UserId = userId,
                    Year = year,
                    Month = month,
                    WeightKg = monthlyWeightDto.WeightKg,
                });
            }
            else
            {
                existing.WeightKg = monthlyWeightDto.WeightKg;
                existing.UpdatedAt = DateTime.UtcNow;
                monthlyRepo.Update(existing);
            }

            await _uow.SaveChangesAsync();
        }

        public async Task<WeeklyProgressSummaryDto> GetWeeklySummaryAsync(int userId)
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-6);

            var logs = (await _uow.GetRepository<DailyLog, int>().GetAllAsync())
                .Where(l => l.UserId == userId && l.LogDate.Date >= startDate && l.LogDate.Date <= endDate)
                .OrderBy(l => l.LogDate)
                .ToList();

            var weeklyLogs = logs.Select(l => new DailyProgressPointDto
            {
                LogDate = l.LogDate,
                SleepHours = l.SleepHours,
                WaterIntakeLiters = l.WaterIntakeLiters,
                MoodScore = l.MoodScore,
                CompletedWorkout = l.CompletedWorkout,
            }).ToList();

            var avgSleep = logs.Count == 0 ? 0 : logs.Average(l => l.SleepHours);
            var avgWater = logs.Count == 0 ? 0 : logs.Average(l => l.WaterIntakeLiters);

            var committedDays = logs.Count(l => l.CompletedWorkout || l.SleepHours > 0 || l.WaterIntakeLiters > 0);
            var commitmentScore = Math.Round((committedDays / 7.0) * 100, 2);

            return new WeeklyProgressSummaryDto
            {
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                AverageSleepHours = Math.Round(avgSleep, 2),
                AverageWaterIntakeLiters = Math.Round(avgWater, 2),
                CommitmentScore = commitmentScore,
                DailyProgress = weeklyLogs,
            };
        }

        private async Task<Dictionary<int, FoodItem>> LoadFoodsForIngredientsAsync(
            IEnumerable<MealIngredient> ingredients)
        {
            var foodIds = ingredients.Select(i => i.FoodItemId).Distinct().ToHashSet();
            return (await _uow.GetRepository<FoodItem, int>().GetAllAsync())
                .Where(f => foodIds.Contains(f.Id))
                .ToDictionary(f => f.Id);
        }

        private static MacroTotals SumMacros(
            IEnumerable<MealIngredient> ingredients,
            Func<MealIngredient, bool> predicate,
            IReadOnlyDictionary<int, FoodItem> foods)
        {
            var selected = ingredients.Where(predicate).ToList();
            return new MacroTotals
            {
                Calories = selected.Sum(i => i.Calories),
                Protein = selected.Sum(i => i.Protein),
                Carbs = selected.Sum(i => i.Carbs),
                Fats = selected.Sum(i => i.Fats),
                Fiber = selected.Sum(i =>
                    foods.TryGetValue(i.FoodItemId, out var food)
                        ? food.Fiber * i.Quantity / 100.0
                        : 0),
            };
        }

        private async Task<List<MealIngredient>> GetPlanIngredientsAsync(int mealPlanId)
        {
            var meals = (await _uow.GetRepository<Meal, int>().GetAllAsync())
                .Where(m => m.MealPlanId == mealPlanId)
                .ToList();

            var mealIds = meals.Select(m => m.Id).ToHashSet();
            return (await _uow.GetRepository<MealIngredient, int>().GetAllAsync())
                .Where(i => mealIds.Contains(i.MealId))
                .ToList();
        }

        private static void ValidateIngredientIds(
            IReadOnlyCollection<MealIngredient> planIngredients,
            IReadOnlyCollection<MealAdherenceItemRequestDto> items)
        {
            var validIds = planIngredients.Select(i => i.Id).ToHashSet();
            var invalid = items.Select(i => i.MealIngredientId).Where(id => !validIds.Contains(id)).ToList();
            if (invalid.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Invalid meal ingredient ids for this plan: {string.Join(", ", invalid)}");
            }
        }

        private static void ApplyPlannedTotals(MealAdherenceLog log, MacroTotals planned)
        {
            log.PlannedCalories = Math.Round(planned.Calories, 2);
            log.PlannedProtein = Math.Round(planned.Protein, 2);
            log.PlannedCarbs = Math.Round(planned.Carbs, 2);
            log.PlannedFats = Math.Round(planned.Fats, 2);
            log.PlannedFiber = Math.Round(planned.Fiber, 2);
        }

        private static void ApplyEatenTotals(MealAdherenceLog log, MacroTotals eaten)
        {
            log.EatenCalories = Math.Round(eaten.Calories, 2);
            log.EatenProtein = Math.Round(eaten.Protein, 2);
            log.EatenCarbs = Math.Round(eaten.Carbs, 2);
            log.EatenFats = Math.Round(eaten.Fats, 2);
            log.EatenFiber = Math.Round(eaten.Fiber, 2);
        }

        private static double ComputeAdherenceScore(double plannedCalories, double eatenCalories)
        {
            if (plannedCalories <= 0) return 0;
            return Math.Round(eatenCalories / plannedCalories, 4);
        }

        private async Task<MealAdherenceResponseDto> MapAdherenceResponseAsync(
            MealAdherenceLog log,
            IReadOnlyList<MealIngredient> planIngredients)
        {
            var allItems = await _uow.GetRepository<MealAdherenceItem, int>().GetAllAsync();
            var eatenLookup = allItems
                .Where(i => i.MealAdherenceLogId == log.Id)
                .ToDictionary(i => i.MealIngredientId, i => i.IsEaten);

            var foodIds = planIngredients.Select(i => i.FoodItemId).Distinct().ToHashSet();
            var foods = (await _uow.GetRepository<FoodItem, int>().GetAllAsync())
                .Where(f => foodIds.Contains(f.Id))
                .ToDictionary(f => f.Id);

            return new MealAdherenceResponseDto
            {
                MealPlanId = log.MealPlanId,
                LogDate = log.LogDate,
                PlannedCalories = log.PlannedCalories,
                PlannedProtein = log.PlannedProtein,
                PlannedCarbs = log.PlannedCarbs,
                PlannedFats = log.PlannedFats,
                PlannedFiber = log.PlannedFiber,
                EatenCalories = log.EatenCalories,
                EatenProtein = log.EatenProtein,
                EatenCarbs = log.EatenCarbs,
                EatenFats = log.EatenFats,
                EatenFiber = log.EatenFiber,
                AdherenceScore = log.AdherenceScore,
                CalorieDelta = Math.Round(log.EatenCalories - log.PlannedCalories, 2),
                Items = planIngredients.Select(i => new MealAdherenceItemResponseDto
                {
                    MealIngredientId = i.Id,
                    FoodName = foods.TryGetValue(i.FoodItemId, out var food) ? food.Name : string.Empty,
                    IsEaten = eatenLookup.GetValueOrDefault(i.Id, false),
                }).ToList(),
            };
        }

        private sealed class MacroTotals
        {
            public double Calories { get; set; }
            public double Protein { get; set; }
            public double Carbs { get; set; }
            public double Fats { get; set; }
            public double Fiber { get; set; }
        }
    }
}
