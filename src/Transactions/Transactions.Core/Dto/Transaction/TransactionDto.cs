using Transactions.Core.Enums;

namespace Transactions.Core.Dto.Transaction;

public class TransactionDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>Nombre del producto (obtenido del microservicio de Productos). Puede ser null si no está disponible.</summary>
    public string? ProductName { get; set; }

    /// <summary>Stock actual del producto.</summary>
    public int? ProductStock { get; set; }

    public bool? ProductIsDeleted { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
