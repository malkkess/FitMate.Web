namespace Shared.DataTransferObject
{
    /// <summary>
    /// Cross-day slot exclusion (C21) — foods banned from a specific meal slot.
    /// Matches Python key (meal, slot_idx) → excluded food names.
    /// </summary>
    public class SlotExclusionDto
    {
        public string Meal { get; set; } = string.Empty;
        public int Slot { get; set; }
        public List<string> ExcludedFoods { get; set; } = new();
    }
}
