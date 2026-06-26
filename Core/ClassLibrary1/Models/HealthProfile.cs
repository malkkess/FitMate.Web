using Shared.Enums;

namespace DomainLayer.Models
{
    public class HealthProfile:BaseEntity<int>
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DiabetesStatus DiabetesStatus { get; set; } = DiabetesStatus.None;
        public bool HasHypertension { get; set; }
        public bool HasHeartDisease { get; set; }

        public bool IsLactoseIntolerant { get; set; }
        public bool IsGlutenAllergic { get; set; }
        public bool IsNutsAllergic { get; set; }

        public string? OtherAllergies { get; set; }

    }
}
