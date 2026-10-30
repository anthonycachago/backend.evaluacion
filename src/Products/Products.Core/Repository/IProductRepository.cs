using Products.Core.Dto.Common;
using Products.Core.Dto.Product;
using Products.Core.Models;

namespace Products.Core.Repository;

public interface IProductRepository : IModelBaseRepository<ProductEntity>
{
    Task<PagedResultDto<ProductEntity>> SearchAsync(ProductFilterDto filter);

    /// <summary>Devuelve productos incluyendo los eliminados (para el historial de transacciones).</summary>
    Task<List<ProductEntity>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    Task<List<string>> GetCategoriesAsync();

    Task<bool> ExistsByNameAsync(string name, Guid? excludeId);

    /// <summary>
    /// Suma (o resta) stock en una sola sentencia SQL atómica que solo se aplica si el stock
    /// resultante no queda negativo. Devuelve <c>false</c> si no se aplicó.
    /// </summary>
    Task<bool> TryAdjustStockAsync(Guid id, int quantity);
}
