using Microsoft.EntityFrameworkCore;
using Transactions.Core.Dto.Common;
using Transactions.Core.Dto.Transaction;
using Transactions.Core.Enums;
using Transactions.Core.Models;
using Transactions.Core.Repository;
using Transactions.Infrastructure.DataBase;

namespace Transactions.Infrastructure.Repository.Transaction;

public class TransactionRepository : BaseRepository<TransactionEntity>, ITransactionRepository
{
    public TransactionRepository(TransactionsContext db) : base(db)
    {
    }

    public async Task<PagedResultDto<TransactionEntity>> SearchAsync(TransactionFilterDto filter)
    {
        var query = ApplyFilters(List(), filter);

        var totalCount = await query.CountAsync();
        var items = await ApplySorting(query, filter)
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResultDto<TransactionEntity>(items, filter.PageNumber, filter.PageSize, totalCount);
    }

    private static IQueryable<TransactionEntity> ApplyFilters(IQueryable<TransactionEntity> query, TransactionFilterDto filter)
    {
        if (filter.ProductId.HasValue)
            query = query.Where(x => x.ProductId == filter.ProductId.Value);

        if (filter.Type.HasValue)
            query = query.Where(x => x.Type == filter.Type.Value);

        if (filter.DateFrom.HasValue)
            query = query.Where(x => x.Date >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
        {
            var dateTo = filter.DateTo.Value;
            query = dateTo.TimeOfDay == TimeSpan.Zero
                ? query.Where(x => x.Date < dateTo.AddDays(1))
                : query.Where(x => x.Date <= dateTo);
        }

        if (filter.MinTotal.HasValue)
            query = query.Where(x => x.TotalPrice >= filter.MinTotal.Value);

        if (filter.MaxTotal.HasValue)
            query = query.Where(x => x.TotalPrice <= filter.MaxTotal.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.Detail != null && x.Detail.Contains(search));
        }

        return query;
    }

    private static IQueryable<TransactionEntity> ApplySorting(IQueryable<TransactionEntity> query, TransactionFilterDto filter)
    {
        var descending = filter.SortDirection == SortDirection.Desc;

        var ordered = filter.SortBy?.ToLowerInvariant() switch
        {
            "date" => descending ? query.OrderByDescending(x => x.Date) : query.OrderBy(x => x.Date),
            "type" => descending ? query.OrderByDescending(x => x.Type) : query.OrderBy(x => x.Type),
            "quantity" => descending ? query.OrderByDescending(x => x.Quantity) : query.OrderBy(x => x.Quantity),
            "unitprice" => descending ? query.OrderByDescending(x => x.UnitPrice) : query.OrderBy(x => x.UnitPrice),
            "totalprice" => descending ? query.OrderByDescending(x => x.TotalPrice) : query.OrderBy(x => x.TotalPrice),
            _ => query.OrderByDescending(x => x.Date)
        };

        // Desempate por Id para que la paginación sea estable.
        return ordered.ThenBy(x => x.Id);
    }
}
