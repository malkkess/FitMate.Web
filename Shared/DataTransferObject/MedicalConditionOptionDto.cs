namespace Shared.DataTransferObject
{
    public class MedicalConditionOptionDto
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string InputType { get; set; } = string.Empty;
        public List<LookupOptionDto> Options { get; set; } = new();
    }
}
