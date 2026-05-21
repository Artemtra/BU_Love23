using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BU_Love.API.DB;

public partial class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public int CategoryId { get; set; }

    [JsonIgnore] 
    public Category? Category { get; set; } 

    public int StockQuantity { get; set; }
    public string Condition { get; set; }
    public virtual ICollection<Orderitem> Orderitems { get; set; } = new List<Orderitem>();
}
