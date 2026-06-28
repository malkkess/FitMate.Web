using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shared.Enums;

namespace Shared.DataTransferObject
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int Age { get; set; }
        public double Height { get; set; }
        public double Weight { get; set; }
        public Gender Gender { get; set; }
        public ActivityLevel ActivityLevel { get; set; }
        public Goal Goal { get; set; }

        public DiabetesStatus DiabetesStatus { get; set; } = DiabetesStatus.None;
        public bool HasCardiovascularMode { get; set; }
        public List<string> MedicalConditions { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
    }
}
