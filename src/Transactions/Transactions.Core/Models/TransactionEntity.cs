using Transactions.Core.Enums;

namespace Transactions.Core.Models;

public class TransactionEntity : ModelBase
{
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }

    /// <summary>Referencia lógica al producto del microservicio de Productos (otra base de datos).</summary>
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Cantidad con signo que esta transacción aplica al stock: compra suma, venta resta.</summary>
    public int StockEffect => CalculateStockEffect(Type, Quantity);

    public static int CalculateStockEffect(TransactionType type, int quantity) =>
        type == TransactionType.Purchase ? quantity : -quantity;
}
