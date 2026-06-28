using Microsoft.AspNetCore.Mvc;
using Shared.DataTransferObject;

namespace FitMate.Web.Controllers
{
    [ApiController]
    [Route("api/lookups")]
    public class LookupsController : ControllerBase
    {
        [HttpGet("profile-options")]
        public ActionResult<ProfileOptionsDto> GetProfileOptions()
        {
            return Ok(new ProfileOptionsDto
            {
                DiabetesStatuses = new List<LookupOptionDto>
                {
                    Option("none", "None"),
                    Option("prediabetic", "Prediabetic"),
                    Option("type2", "Type 2 Diabetes"),
                },
                MedicalConditions = new List<LookupOptionDto>
                {
                    Option("hypertension", "Hypertension"),
                    Option("heart_disease", "Heart Disease"),
                },
                Allergies = new List<LookupOptionDto>
                {
                    Option("lactose", "Lactose Intolerance"),
                    Option("gluten", "Gluten Allergy"),
                    Option("nuts", "Nuts Allergy"),
                },
                ActivityLevels = new List<LookupOptionDto>
                {
                    Option("sedentary", "Sedentary", "sedentary"),
                    Option("lightlyActive", "Lightly Active", "light"),
                    Option("moderatelyActive", "Moderately Active", "moderate"),
                    Option("veryActive", "Very Active", "active"),
                    Option("extraActive", "Extra Active", "very_active"),
                },
                Goals = new List<LookupOptionDto>
                {
                    Option("loseWeight", "Lose Weight", "lose"),
                    Option("maintainWeight", "Maintain Weight", "maintain"),
                    Option("gainMuscle", "Gain Muscle", "gain"),
                },
                Genders = new List<LookupOptionDto>
                {
                    Option("male", "Male"),
                    Option("female", "Female"),
                },
            });
        }

        private static LookupOptionDto Option(string value, string label, string? optimizerValue = null)
        {
            return new LookupOptionDto
            {
                Value = value,
                Label = label,
                OptimizerValue = optimizerValue,
            };
        }
    }
}
