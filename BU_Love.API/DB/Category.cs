using System;
using System.Collections.Generic;

namespace BU_Love.API.DB;

public partial class Category
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
