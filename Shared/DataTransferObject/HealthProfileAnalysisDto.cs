using Shared.Enums;

namespace Shared.DataTransferObject
{
    public class HealthProfileAnalysisDto
    {
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Weight { get; set; }
        public string ActivityLevel { get; set; } = string.Empty;
        public string Goal { get; set; } = string.Empty;
        public DiabetesStatus DiabetesStatus { get; set; } = DiabetesStatus.None;
        public List<string> Allergies { get; set; } = new();
        public List<string> MedicalConditions { get; set; } = new();
    }
}
