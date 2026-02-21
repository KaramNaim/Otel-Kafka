using OTel.Domain.Common;

namespace OTel.Domain.Models;

public class Order : BaseEntity
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Pending";

    public Product Product { get; set; } = null!;
}
