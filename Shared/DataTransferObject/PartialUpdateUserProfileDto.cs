using Shared.Enums;

namespace Shared.DataTransferObject
{
    public class PartialUpdateUserProfileDto
    {
        public string? FullName { get; set; }
        public int? Age { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public Gender? Gender { get; set; }
        public ActivityLevel? ActivityLevel { get; set; }
        public Goal? Goal { get; set; }
        public DiabetesStatus? DiabetesStatus { get; set; }
        public List<string>? Allergies { get; set; }
        public List<string>? MedicalConditions { get; set; }
        public string? OtherAllergies { get; set; }
    }
}
