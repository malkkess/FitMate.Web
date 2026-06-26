namespace DomainLayer.Models
{
    public class MealAdherenceItem : BaseEntity<int>
    {
        public int MealAdherenceLogId { get; set; }
        public MealAdherenceLog MealAdherenceLog { get; set; } = null!;
        public int MealIngredientId { get; set; }
        public MealIngredient MealIngredient { get; set; } = null!;
        public bool IsEaten { get; set; }
    }
}
