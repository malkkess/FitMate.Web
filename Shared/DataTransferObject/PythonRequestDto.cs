namespace Shared.DataTransferObject
{
    public class PythonRequestDto
    {
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public double HeightCm { get; set; }
        public double WeightKg { get; set; }
        public string ActivityLevel { get; set; } = string.Empty;
        public string Goal { get; set; } = string.Empty;
        public string DiabetesStatus { get; set; } = "none";
        public List<string> Allergies { get; set; } = new();
        public double? Budget { get; set; }
        public int Days { get; set; } = 1;
        public AdherenceContextDto? Adherence { get; set; }
    }
}
