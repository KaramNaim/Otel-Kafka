namespace OTel.Application.DTO.Product;

public class ProductDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool IsActive { get; set; }
}
