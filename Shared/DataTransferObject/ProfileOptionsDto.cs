namespace Shared.DataTransferObject
{
    public class ProfileOptionsDto
    {
        public List<MedicalConditionOptionDto> MedicalConditions { get; set; } = new();
        public List<LookupOptionDto> Allergies { get; set; } = new();
        public List<LookupOptionDto> ActivityLevels { get; set; } = new();
        public List<LookupOptionDto> Goals { get; set; } = new();
        public List<LookupOptionDto> Genders { get; set; } = new();
    }
}
