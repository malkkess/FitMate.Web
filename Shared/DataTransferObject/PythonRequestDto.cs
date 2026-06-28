namespace Shared.DataTransferObject
{
    /// <summary>
    /// Request payload for the Python MIGP optimizer (UserBiometrics + C21 state).
    /// </summary>
    public class PythonRequestDto
    {
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public double HeightCm { get; set; }
        public double WeightKg { get; set; }
        public string ActivityLevel { get; set; } = string.Empty;
        public string Goal { get; set; } = string.Empty;

        /// <summary>none | prediabetic | type2</summary>
        public string DiabetesStatus { get; set; } = "none";

        /// <summary>none | cardiovascular (hypertension / heart disease)</summary>
        public string HypertensionStatus { get; set; } = "none";

        public List<string> Allergies { get; set; } = new();
        public double? Budget { get; set; }
        public int Days { get; set; } = 1;
        public int? MasterSeed { get; set; }
        public int DayNumber { get; set; } = 1;
        public double CalorieAdjustment { get; set; }
        public List<SlotExclusionDto> SlotExclusions { get; set; } = new();
        public AdherenceContextDto? Adherence { get; set; }
    }
}
