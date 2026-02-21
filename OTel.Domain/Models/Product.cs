using OTel.Domain.Common;

namespace OTel.Domain.Models;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}
