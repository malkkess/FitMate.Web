namespace Shared.DataTransferObject
{
    public class MealAdherenceItemResponseDto
    {
        public int MealIngredientId { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public bool IsEaten { get; set; }
    }
}
