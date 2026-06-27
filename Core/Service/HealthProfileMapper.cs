using DomainLayer.Models;
using Shared.DataTransferObject;
using Shared.Enums;

namespace Service
{
    internal static class HealthProfileMapper
    {
        public static HealthProfileAnalysisDto MapUserHealthProfile(User user, HealthProfile? health)
        {
            return new HealthProfileAnalysisDto
            {
                Age = user.Age,
                Gender = user.Gender == Gender.Male ? "male" : "female",
                Height = user.Height,
                Weight = user.CurrentWeight,
                ActivityLevel = MapActivityLevel(user.ActivityLevel),
                Goal = MapGoal(user.Goal),
                DiabetesStatus = health?.DiabetesStatus ?? DiabetesStatus.None,
                Allergies = BuildAllergiesList(health),
                MedicalConditions = BuildMedicalConditionsList(health),
            };
        }

        public static PythonRequestDto MapToPythonRequest(
            User user,
            HealthProfile? health,
            int days = 1,
            AdherenceContextDto? adherence = null)
        {
            var profile = MapUserHealthProfile(user, health);

            return new PythonRequestDto
            {
                Age = profile.Age,
                Gender = profile.Gender,
                HeightCm = profile.Height,
                WeightKg = profile.Weight,
                ActivityLevel = profile.ActivityLevel,
                Goal = profile.Goal,
                DiabetesStatus = MapDiabetesStatusToPython(profile.DiabetesStatus),
                Allergies = profile.Allergies,
                Days = days,
                Adherence = adherence,
            };
        }

        public static string MapActivityLevel(ActivityLevel level) => level switch
        {
            ActivityLevel.Sedentary => "sedentary",
            ActivityLevel.LightlyActive => "light",
            ActivityLevel.ModeratelyActive => "moderate",
            ActivityLevel.VeryActive => "active",
            ActivityLevel.ExtraActive => "very_active",
            _ => "moderate",
        };

        public static string MapGoal(Goal goal) => goal switch
        {
            Goal.LoseWeight => "lose",
            Goal.MaintainWeight => "maintain",
            Goal.GainMuscle => "gain",
            _ => "maintain",
        };

        public static string MapDiabetesStatusToPython(DiabetesStatus status) => status switch
        {
            DiabetesStatus.Prediabetic => "prediabetic",
            DiabetesStatus.Type2 => "type2",
            _ => "none",
        };

        public static List<string> BuildAllergiesList(HealthProfile? health)
        {
            var list = new List<string>();
            if (health is null) return list;
            if (health.IsLactoseIntolerant) list.Add("lactose");
            if (health.IsGlutenAllergic) list.Add("gluten");
            if (health.IsNutsAllergic) list.Add("nuts");
            if (!string.IsNullOrWhiteSpace(health.OtherAllergies))
            {
                list.AddRange(health.OtherAllergies
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            }
            return list;
        }

        public static List<string> BuildMedicalConditionsList(HealthProfile? health)
        {
            var list = new List<string>();
            if (health is null) return list;
            if (health.DiabetesStatus != DiabetesStatus.None) list.Add("diabetes");
            if (health.HasHypertension) list.Add("hypertension");
            if (health.HasHeartDisease) list.Add("heart_disease");
            return list;
        }

        public static void ApplyHealthProfile(HealthProfile health, UpdateUserProfileDto updateDto)
        {
            var allergies = updateDto.Allergies
                .Select(a => a.Trim().ToLowerInvariant())
                .ToHashSet();

            var conditions = updateDto.MedicalConditions
                .Select(c => c.Trim().ToLowerInvariant())
                .ToHashSet();

            health.IsLactoseIntolerant = allergies.Contains("lactose");
            health.IsGlutenAllergic = allergies.Contains("gluten");
            health.IsNutsAllergic = allergies.Contains("nuts");
            health.OtherAllergies = updateDto.OtherAllergies;
            health.DiabetesStatus = updateDto.DiabetesStatus;
            health.HasHypertension = conditions.Contains("hypertension");
            health.HasHeartDisease = conditions.Contains("heart_disease");
            health.UpdatedAt = DateTime.UtcNow;
        }

        public static void ApplyHealthProfile(HealthProfile health, RegisterRequestDto request)
        {
            ApplyHealthProfile(health, new UpdateUserProfileDto
            {
                DiabetesStatus = request.DiabetesStatus,
                Allergies = request.Allergies,
                MedicalConditions = request.MedicalConditions,
                OtherAllergies = request.OtherAllergies,
            });
        }

        public static UserProfileDto MapToProfileDto(User user, HealthProfile? health)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Age = user.Age,
                Height = user.Height,
                Weight = user.CurrentWeight,
                Gender = user.Gender,
                ActivityLevel = user.ActivityLevel,
                Goal = user.Goal,
                DiabetesStatus = health?.DiabetesStatus ?? DiabetesStatus.None,
                Allergies = BuildAllergiesList(health),
                MedicalConditions = BuildMedicalConditionsList(health),
            };
        }
    }
}
