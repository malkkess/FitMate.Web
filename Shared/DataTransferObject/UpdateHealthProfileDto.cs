using Shared.Enums;

namespace Shared.DataTransferObject
{
    public class UpdateHealthProfileDto
    {
        public DiabetesStatus DiabetesStatus { get; set; } = DiabetesStatus.None;
        public List<string> Allergies { get; set; } = new();
        public List<string> MedicalConditions { get; set; } = new();
        public string? OtherAllergies { get; set; }
    }
}
