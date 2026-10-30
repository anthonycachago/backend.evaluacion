namespace Products.Core.Dto.Product;

/// <summary>Movimiento de stock solicitado por el servicio de Transacciones. Positivo suma, negativo resta.</summary>
public class StockAdjustmentDto
{
    public int Quantity { get; set; }
}
