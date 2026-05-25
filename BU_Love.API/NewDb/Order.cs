using System;
using System.Collections.Generic;

namespace BU_Love.API.NewDb;

public partial class Order
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Address { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public int? UserId { get; set; }

    public virtual ICollection<Orderitem> Orderitems { get; set; } = new List<Orderitem>();

    public virtual User? User { get; set; }
}
