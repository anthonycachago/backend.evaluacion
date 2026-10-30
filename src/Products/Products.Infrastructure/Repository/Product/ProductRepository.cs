using Microsoft.EntityFrameworkCore;
using Products.Core.Dto.Common;
using Products.Core.Dto.Product;
using Products.Core.Enums;
using Products.Core.Models;
using Products.Core.Repository;
using Products.Infrastructure.DataBase;

namespace Products.Infrastructure.Repository.Product;

public class ProductRepository : BaseRepository<ProductEntity>, IProductRepository
{
    public ProductRepository(ProductsContext db) : base(db)
    {
    }

    public async Task<PagedResultDto<ProductEntity>> SearchAsync(ProductFilterDto filter)
    {
        var query = ApplyFilters(List(), filter);

        var totalCount = await query.CountAsync();
        var items = await ApplySorting(query, filter)
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResultDto<ProductEntity>(items, filter.PageNumber, filter.PageSize, totalCount);
    }

    public Task<List<ProductEntity>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        return _db.Set<ProductEntity>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
    }

    public Task<List<string>> GetCategoriesAsync()
    {
        return List()
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId)
    {
        return List().AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId));
    }

    public async Task<bool> TryAdjustStockAsync(Guid id, int quantity)
    {
        // UPDATE Products SET Stock = Stock + @quantity WHERE Id = @id AND Stock + @quantity >= 0
        // La condición se evalúa en la misma sentencia, así dos ventas simultáneas no dejan stock negativo.
        var affectedRows = await _db.Set<ProductEntity>()
            .Where(x => x.Id == id && x.Stock + quantity >= 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Stock, x => x.Stock + quantity)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

        return affectedRows > 0;
    }

    private static IQueryable<ProductEntity> ApplyFilters(IQueryable<ProductEntity> query, ProductFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(x => x.Category == filter.Category.Trim());

        if (filter.MinPrice.HasValue)
            query = query.Where(x => x.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(x => x.Price <= filter.MaxPrice.Value);

        if (filter.MinStock.HasValue)
            query = query.Where(x => x.Stock >= filter.MinStock.Value);

        if (filter.MaxStock.HasValue)
            query = query.Where(x => x.Stock <= filter.MaxStock.Value);

        return query;
    }

    private static IQueryable<ProductEntity> ApplySorting(IQueryable<ProductEntity> query, ProductFilterDto filter)
    {
        var descending = filter.SortDirection == SortDirection.Desc;

        var ordered = filter.SortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "category" => descending ? query.OrderByDescending(x => x.Category) : query.OrderBy(x => x.Category),
            "price" => descending ? query.OrderByDescending(x => x.Price) : query.OrderBy(x => x.Price),
            "stock" => descending ? query.OrderByDescending(x => x.Stock) : query.OrderBy(x => x.Stock),
            "createdat" => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        // Desempate por Id para que la paginación sea estable.
        return ordered.ThenBy(x => x.Id);
    }
}
