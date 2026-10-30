using Transactions.Core.Dto.Common;
using Transactions.Core.Dto.Transaction;

namespace Transactions.Business.Transaction.Interface;

public interface ITransactionService
{
    Task<PagedResultDto<TransactionDto>> SearchAsync(TransactionFilterDto filter);
    Task<TransactionDto> GetByIdAsync(Guid id);
    Task<ProductTransactionHistoryDto> GetProductHistoryAsync(Guid productId, TransactionFilterDto filter);
    Task<TransactionDto> CreateAsync(TransactionCreateDto dto);
    Task<TransactionDto> UpdateAsync(Guid id, TransactionUpdateDto dto);
    Task DeleteAsync(Guid id);
}
