using Transactions.Core.Dto.Common;
using Transactions.Core.Dto.Transaction;
using Transactions.Core.Models;

namespace Transactions.Core.Repository;

public interface ITransactionRepository : IModelBaseRepository<TransactionEntity>
{
    Task<PagedResultDto<TransactionEntity>> SearchAsync(TransactionFilterDto filter);
}
