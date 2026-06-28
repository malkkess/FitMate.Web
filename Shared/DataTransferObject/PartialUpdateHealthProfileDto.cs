using Shared.Enums;

namespace Shared.DataTransferObject
{
    public class PartialUpdateHealthProfileDto
    {
        public DiabetesStatus? DiabetesStatus { get; set; }
        public List<string>? Allergies { get; set; }
        public List<string>? MedicalConditions { get; set; }
        public string? OtherAllergies { get; set; }
    }
}
