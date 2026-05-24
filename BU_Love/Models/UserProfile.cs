using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BU_Love.Models
{
    public class UserProfile
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal BonusPoints { get; set; }
        public string BonusPointsDisplay => $"{BonusPoints:N0} бонусов";
    }
}
