namespace Shared.DataTransferObject
{
    public class PythonAnalysisDto
    {
        public double Bmi { get; set; }
        public string BmiCategory { get; set; } = string.Empty;
        public double Tdee { get; set; }
        public double CalTarget { get; set; }
        public double ProteinTarget { get; set; }
        public double FatTarget { get; set; }
        public double CarbsTarget { get; set; }
        public double FiberTarget { get; set; }
        public double? NetCarbsMaxDay { get; set; }
    }
}
