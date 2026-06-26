using Shared.Enums;

namespace Shared.DataTransferObject
{
    public class UpdateUserProfileDto
    {
        public string FullName { get; set; } = null!;
        public int Age { get; set; }
        public double Height { get; set; }
        public double Weight { get; set; }
        public Gender Gender { get; set; }
        public ActivityLevel ActivityLevel { get; set; }
        public Goal Goal { get; set; }
        public DiabetesStatus DiabetesStatus { get; set; } = DiabetesStatus.None;
        public List<string> Allergies { get; set; } = new();
        public List<string> MedicalConditions { get; set; } = new();
        public string? OtherAllergies { get; set; }
    }
}
