namespace Shared.DataTransferObject
{
    public class MealAdherenceRequestDto
    {
        public DateTime? LogDate { get; set; }
        public int MealPlanId { get; set; }
        public List<MealAdherenceItemRequestDto> Items { get; set; } = new();
    }
}
