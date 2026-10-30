using Transactions.Core.Dto.Common;
using Transactions.Core.Dto.Product;

namespace Transactions.Core.Dto.Transaction;

/// <summary>Historial de transacciones de un producto, con su nombre y stock actual.</summary>
public class ProductTransactionHistoryDto
{
    public ProductInfoDto Product { get; set; } = null!;
    public PagedResultDto<TransactionDto> Transactions { get; set; } = null!;
}
