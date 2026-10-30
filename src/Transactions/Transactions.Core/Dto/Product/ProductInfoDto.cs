namespace Transactions.Core.Dto.Product;

/// <summary>Datos del producto que devuelve el microservicio de Productos.</summary>
public class ProductInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsDeleted { get; set; }
}
