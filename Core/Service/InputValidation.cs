using Shared.DataTransferObject;
using Shared.Enums;

namespace Service
{
    internal static class InputValidation
    {
        private const int MinAge = 13;
        private const int MaxAge = 120;
        private const double MinHeightCm = 100;
        private const double MaxHeightCm = 250;
        private const double MinWeightKg = 30;
        private const double MaxWeightKg = 300;

        public static void ValidateProfile(
            string? fullName,
            int age,
            double height,
            double weight,
            Gender gender,
            ActivityLevel activityLevel,
            Goal goal,
            DiabetesStatus diabetesStatus)
        {
            ValidateFullName(fullName);
            ValidateAge(age);
            ValidateHeight(height);
            ValidateWeight(weight);
            ValidateEnum(gender, "Invalid gender value.");
            ValidateEnum(activityLevel, "Invalid activity level value.");
            ValidateEnum(goal, "Invalid goal value.");
            ValidateEnum(diabetesStatus, "Invalid diabetes status value.");
        }

        public static void ValidatePartialProfile(PartialUpdateUserProfileDto updateDto)
        {
            if (updateDto.FullName is not null)
                ValidateFullName(updateDto.FullName);

            if (updateDto.Age.HasValue)
                ValidateAge(updateDto.Age.Value);

            if (updateDto.Height.HasValue)
                ValidateHeight(updateDto.Height.Value);

            if (updateDto.Weight.HasValue)
                ValidateWeight(updateDto.Weight.Value);

            if (updateDto.Gender.HasValue)
                ValidateEnum(updateDto.Gender.Value, "Invalid gender value.");

            if (updateDto.ActivityLevel.HasValue)
                ValidateEnum(updateDto.ActivityLevel.Value, "Invalid activity level value.");

            if (updateDto.Goal.HasValue)
                ValidateEnum(updateDto.Goal.Value, "Invalid goal value.");

            if (updateDto.DiabetesStatus.HasValue)
                ValidateEnum(updateDto.DiabetesStatus.Value, "Invalid diabetes status value.");
        }

        public static void ValidateWeight(double weightKg)
        {
            if (double.IsNaN(weightKg) || double.IsInfinity(weightKg) ||
                weightKg < MinWeightKg || weightKg > MaxWeightKg)
            {
                throw new InvalidOperationException($"Weight must be between {MinWeightKg} and {MaxWeightKg} kg.");
            }
        }

        public static void ValidateMonthlyWeight(MonthlyWeightLogDto dto)
        {
            ValidateWeight(dto.WeightKg);

            if (dto.Month.HasValue && (dto.Month.Value < 1 || dto.Month.Value > 12))
                throw new InvalidOperationException("Month must be between 1 and 12.");

            if (dto.Year.HasValue && (dto.Year.Value < 2000 || dto.Year.Value > 2100))
                throw new InvalidOperationException("Year must be between 2000 and 2100.");

            var now = DateTime.UtcNow;
            var year = dto.Year ?? now.Year;
            var month = dto.Month ?? now.Month;
            if (new DateTime(year, month, 1) > new DateTime(now.Year, now.Month, 1))
                throw new InvalidOperationException("Monthly weight cannot be logged for a future month.");
        }

        public static void ValidateDailyActivity(DailyLogActivityDto dto)
        {
            ValidateLogDate(dto.LogDate);

            if (dto.WaterIntakeLiters < 0 || dto.WaterIntakeLiters > 15)
                throw new InvalidOperationException("Water intake must be between 0 and 15 liters.");

            if (dto.SleepHours < 0 || dto.SleepHours > 24)
                throw new InvalidOperationException("Sleep hours must be between 0 and 24.");

            if (dto.MoodScore < 0 || dto.MoodScore > 10)
                throw new InvalidOperationException("Mood score must be between 0 and 10.");
        }

        public static void ValidateProgressWeight(ProgressWeightLogDto dto)
        {
            ValidateWeight(dto.WeightKg);
            ValidateLogDate(dto.LogDate);
        }

        public static void ValidateMealAdherence(MealAdherenceRequestDto dto)
        {
            if (dto.MealPlanId <= 0)
                throw new InvalidOperationException("Meal plan id is required.");

            ValidateLogDate(dto.LogDate);

            if (dto.Items.Any(i => i.MealIngredientId <= 0))
                throw new InvalidOperationException("Meal ingredient ids must be valid.");

            var duplicateIngredientIds = dto.Items
                .GroupBy(i => i.MealIngredientId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIngredientIds.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Duplicate meal ingredient ids: {string.Join(", ", duplicateIngredientIds)}");
            }
        }

        private static void ValidateFullName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new InvalidOperationException("Full name is required.");

            if (fullName.Trim().Length > 100)
                throw new InvalidOperationException("Full name must be 100 characters or fewer.");
        }

        private static void ValidateAge(int age)
        {
            if (age < MinAge || age > MaxAge)
                throw new InvalidOperationException($"Age must be between {MinAge} and {MaxAge}.");
        }

        private static void ValidateHeight(double height)
        {
            if (double.IsNaN(height) || double.IsInfinity(height) ||
                height < MinHeightCm || height > MaxHeightCm)
            {
                throw new InvalidOperationException($"Height must be between {MinHeightCm} and {MaxHeightCm} cm.");
            }
        }

        private static void ValidateEnum<TEnum>(TEnum value, string message)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
                throw new InvalidOperationException(message);
        }

        public static void ValidateLogDate(DateTime? logDate)
        {
            if (logDate.HasValue && logDate.Value.Date > DateTime.UtcNow.Date)
                throw new InvalidOperationException("Log date cannot be in the future.");
        }
    }
}
