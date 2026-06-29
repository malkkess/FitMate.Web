namespace Shared.DataTransferObject
{
    public class PythonDayPlanDto
    {
        public int DayNumber { get; set; }
        public bool Success { get; set; }
        public string? Status { get; set; }
        public bool RelaxationApplied { get; set; }
        public List<string> RelaxedConstraints { get; set; } = new();
        public Dictionary<string, List<PythonMealItemDto>>? Plan { get; set; }
    }
}
