using Transactions.Core.Dto.Product;

namespace Transactions.Core.ExternalServices;

/// <summary>Comunicación síncrona (REST) con el microservicio de Productos.</summary>
public interface IProductApiClient
{
    /// <summary>Devuelve un producto activo o <c>null</c> si no existe o fue eliminado.</summary>
    Task<ProductInfoDto?> GetProductAsync(Guid productId);

    /// <summary>Consulta varios productos (incluidos los eliminados) en una sola llamada.</summary>
    Task<Dictionary<Guid, ProductInfoDto>> GetProductsAsync(IReadOnlyCollection<Guid> productIds);

    /// <summary>Aplica un movimiento de stock con signo. El servicio de Productos garantiza que el stock no quede negativo.</summary>
    /// <exception cref="Exceptions.BusinessRuleException">Stock insuficiente o movimiento inválido.</exception>
    /// <exception cref="Exceptions.NotFoundException">El producto no existe.</exception>
    /// <exception cref="Exceptions.ExternalServiceException">El servicio de Productos no está disponible.</exception>
    Task<ProductInfoDto> AdjustStockAsync(Guid productId, int quantity);
}
