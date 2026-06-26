using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class UserPreference:BaseEntity<int>
    {
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public int FoodItemId { get; set; }
        public virtual FoodItem FoodItem { get; set; } = null!;

        public int PreferenceLevel { get; set; }
        public bool IsDisliked { get; set; }
    }
}
