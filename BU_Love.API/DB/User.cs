using System;
using System.Collections.Generic;

namespace BU_Love.API.DB;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Role { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public int? BonusPoints { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
