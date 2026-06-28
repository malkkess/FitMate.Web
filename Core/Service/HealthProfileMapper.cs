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

                HypertensionStatus = MapHypertensionStatusToPython(health),

                Allergies = BuildAllergiesList(health),

                MedicalConditions = BuildMedicalConditionsList(health),

            };

        }



        public static PythonRequestDto MapToPythonRequest(

            User user,

            HealthProfile? health,

            int days = 1,

            AdherenceContextDto? adherence = null,

            int? masterSeed = null,

            int dayNumber = 1,

            IReadOnlyList<SlotExclusionDto>? slotExclusions = null,

            double? budget = null)

        {

            var profile = MapUserHealthProfile(user, health);

            var calorieAdjustment = adherence?.CalorieAdjustment ?? 0;



            return new PythonRequestDto

            {

                Age = profile.Age,

                Gender = profile.Gender,

                HeightCm = profile.Height,

                WeightKg = profile.Weight,

                ActivityLevel = profile.ActivityLevel,

                Goal = profile.Goal,

                DiabetesStatus = MapDiabetesStatusToPython(profile.DiabetesStatus),

                HypertensionStatus = profile.HypertensionStatus,

                Allergies = profile.Allergies,

                Budget = budget,

                Days = days,

                MasterSeed = masterSeed,

                DayNumber = dayNumber,

                CalorieAdjustment = calorieAdjustment,

                SlotExclusions = slotExclusions?.ToList() ?? new List<SlotExclusionDto>(),

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



        /// <summary>Maps to Python DIABETES_PARAMS keys: none | prediabetic | type2</summary>

        public static string MapDiabetesStatusToPython(DiabetesStatus status) => status switch

        {

            DiabetesStatus.Prediabetic => "prediabetic",

            DiabetesStatus.Type2 => "type2",

            _ => "none",

        };



        /// <summary>

        /// Maps hypertension / heart disease to Python cardiovascular mode (w_health = 1.0).

        /// </summary>

        public static string MapHypertensionStatusToPython(HealthProfile? health)

        {

            if (health is null)

                return "none";



            return health.HasHypertension || health.HasHeartDisease

                ? "cardiovascular"

                : "none";

        }



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

            ApplyHealthProfile(health, new UpdateHealthProfileDto

            {

                DiabetesStatus = updateDto.DiabetesStatus,

                Allergies = updateDto.Allergies,

                MedicalConditions = updateDto.MedicalConditions,

                OtherAllergies = updateDto.OtherAllergies,

            });

        }



        public static void ApplyHealthProfile(HealthProfile health, PartialUpdateUserProfileDto updateDto)

        {

            ApplyHealthProfile(health, new PartialUpdateHealthProfileDto

            {

                DiabetesStatus = updateDto.DiabetesStatus,

                Allergies = updateDto.Allergies,

                MedicalConditions = updateDto.MedicalConditions,

                OtherAllergies = updateDto.OtherAllergies,

            });

        }



        public static void ApplyHealthProfile(HealthProfile health, UpdateHealthProfileDto updateDto)

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



        public static void ApplyHealthProfile(HealthProfile health, PartialUpdateHealthProfileDto updateDto)

        {

            if (updateDto.Allergies is not null)

            {

                var allergies = updateDto.Allergies

                    .Select(a => a.Trim().ToLowerInvariant())

                    .ToHashSet();



                health.IsLactoseIntolerant = allergies.Contains("lactose");

                health.IsGlutenAllergic = allergies.Contains("gluten");

                health.IsNutsAllergic = allergies.Contains("nuts");

            }



            if (updateDto.MedicalConditions is not null)

            {

                var conditions = updateDto.MedicalConditions

                    .Select(c => c.Trim().ToLowerInvariant())

                    .ToHashSet();



                health.HasHypertension = conditions.Contains("hypertension");

                health.HasHeartDisease = conditions.Contains("heart_disease");

            }



            if (updateDto.DiabetesStatus.HasValue)

                health.DiabetesStatus = updateDto.DiabetesStatus.Value;



            if (updateDto.OtherAllergies is not null)

                health.OtherAllergies = updateDto.OtherAllergies;



            health.UpdatedAt = DateTime.UtcNow;

        }



        public static void ApplyHealthProfile(HealthProfile health, RegisterRequestDto request)

        {

            ApplyHealthProfile(health, new UpdateHealthProfileDto

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

                HasCardiovascularMode = MapHypertensionStatusToPython(health) == "cardiovascular",

                Allergies = BuildAllergiesList(health),

                MedicalConditions = BuildMedicalConditionsList(health),

            };

        }

    }

}


