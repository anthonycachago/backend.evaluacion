using Microsoft.Extensions.Logging;
using Transactions.Business.Transaction.Interface;
using Transactions.Core.Dto.Common;
using Transactions.Core.Dto.Product;
using Transactions.Core.Dto.Transaction;
using Transactions.Core.Enums;
using Transactions.Core.Exceptions;
using Transactions.Core.ExternalServices;
using Transactions.Core.Mappers;
using Transactions.Core.Models;
using Transactions.Core.Repository;

namespace Transactions.Business.Transaction.Service;

/// <summary>
/// El stock vive en el microservicio de Productos. Por eso cada escritura primero ajusta el stock
/// (llamada REST) y luego guarda la transacción en la base local. Si el guardado local falla,
/// los ajustes de stock se revierten (compensación) para que ambos servicios queden consistentes.
/// </summary>
public class TransactionService : ITransactionService
{
    private const string ResourceName = "Transacción";

    private readonly ITransactionRepository _repository;
    private readonly IProductApiClient _productApi;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionRepository repository,
        IProductApiClient productApi,
        ILogger<TransactionService> logger)
    {
        _repository = repository;
        _productApi = productApi;
        _logger = logger;
    }

    public async Task<PagedResultDto<TransactionDto>> SearchAsync(TransactionFilterDto filter)
    {
        var page = await _repository.SearchAsync(filter);
        var products = await TryGetProductsAsync(page.Items.Select(x => x.ProductId).ToList());

        return page.Map(x => x.ToDto(products.GetValueOrDefault(x.ProductId)));
    }

    public async Task<TransactionDto> GetByIdAsync(Guid id)
    {
        var transaction = await GetExistingAsync(id);
        var products = await TryGetProductsAsync([transaction.ProductId]);

        return transaction.ToDto(products.GetValueOrDefault(transaction.ProductId));
    }

    public async Task<ProductTransactionHistoryDto> GetProductHistoryAsync(Guid productId, TransactionFilterDto filter)
    {
        var products = await _productApi.GetProductsAsync([productId]);
        var product = products.GetValueOrDefault(productId)
            ?? throw NotFoundException.For("Producto", productId);

        filter.ProductId = productId;
        var page = await _repository.SearchAsync(filter);

        return new ProductTransactionHistoryDto
        {
            Product = product,
            Transactions = page.Map(x => x.ToDto(product))
        };
    }

    public async Task<TransactionDto> CreateAsync(TransactionCreateDto dto)
    {
        var product = await GetActiveProductAsync(dto.ProductId!.Value);
        EnsureStockAvailable(product, dto.Type!.Value, dto.Quantity, availableStock: product.Stock);

        var entity = dto.ToEntity(defaultUnitPrice: product.Price);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        var movements = new List<StockMovement> { new(entity.ProductId, entity.StockEffect) };
        var adjusted = await ApplyStockMovementsAndSaveAsync(movements, () => _repository.CreateAsync(entity));

        _logger.LogInformation("Transacción creada. Id={Id}, Tipo={Type}, Producto={ProductId}, Cantidad={Quantity}",
            entity.Id, entity.Type, entity.ProductId, entity.Quantity);

        return entity.ToDto(adjusted.GetValueOrDefault(product.Id) ?? product);
    }

    public async Task<TransactionDto> UpdateAsync(Guid id, TransactionUpdateDto dto)
    {
        var entity = await GetExistingAsync(id);
        var product = await GetActiveProductAsync(dto.ProductId!.Value);

        var oldProductId = entity.ProductId;
        var oldEffect = entity.StockEffect;
        var newEffect = TransactionEntity.CalculateStockEffect(dto.Type!.Value, dto.Quantity);
        var sameProduct = oldProductId == dto.ProductId.Value;

        // Si es el mismo producto, el movimiento anterior se "devuelve" antes de validar el stock disponible.
        var availableStock = sameProduct ? product.Stock - oldEffect : product.Stock;
        EnsureStockAvailable(product, dto.Type.Value, dto.Quantity, availableStock);

        var movements = sameProduct
            ? new List<StockMovement> { new(product.Id, newEffect - oldEffect) }
            : new List<StockMovement> { new(oldProductId, -oldEffect), new(product.Id, newEffect) };

        dto.ApplyTo(entity);
        entity.UpdatedAt = DateTime.UtcNow;

        var adjusted = await ApplyStockMovementsAndSaveAsync(movements, () => _repository.UpdateAsync(entity));

        _logger.LogInformation("Transacción actualizada. Id={Id}", entity.Id);

        return entity.ToDto(adjusted.GetValueOrDefault(product.Id) ?? product);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetExistingAsync(id);

        // Si el producto ya fue eliminado no se devuelve stock: ya no forma parte del inventario.
        var products = await _productApi.GetProductsAsync([entity.ProductId]);
        var product = products.GetValueOrDefault(entity.ProductId);
        var productIsActive = product is { IsDeleted: false };

        if (productIsActive && entity.Type == TransactionType.Purchase && product!.Stock < entity.Quantity)
            throw new BusinessRuleException(
                $"No se puede eliminar la compra: el stock de '{product.Name}' quedaría negativo. " +
                $"Disponible: {product.Stock}, a revertir: {entity.Quantity}.");

        var movements = productIsActive
            ? new List<StockMovement> { new(entity.ProductId, -entity.StockEffect) }
            : new List<StockMovement>();

        await ApplyStockMovementsAndSaveAsync(movements, () => _repository.DeleteAsync(entity));

        _logger.LogInformation("Transacción eliminada. Id={Id}", id);
    }

    /// <summary>
    /// Aplica los movimientos de stock en Productos y luego guarda localmente.
    /// Si algo falla, revierte los movimientos ya aplicados.
    /// </summary>
    private async Task<Dictionary<Guid, ProductInfoDto>> ApplyStockMovementsAndSaveAsync(
        List<StockMovement> movements,
        Func<Task> saveLocalChanges)
    {
        var applied = new List<StockMovement>();
        var adjustedProducts = new Dictionary<Guid, ProductInfoDto>();

        try
        {
            foreach (var movement in movements.Where(x => x.Quantity != 0))
            {
                adjustedProducts[movement.ProductId] = await _productApi.AdjustStockAsync(movement.ProductId, movement.Quantity);
                applied.Add(movement);
            }

            await saveLocalChanges();
            return adjustedProducts;
        }
        catch (Exception ex) when (applied.Count > 0)
        {
            _logger.LogWarning(ex, "Falló la operación; se revierten {Count} movimiento(s) de stock", applied.Count);
            await CompensateAsync(applied);
            throw;
        }
    }

    private async Task CompensateAsync(List<StockMovement> applied)
    {
        // Se revierten en orden inverso al que se aplicaron.
        for (var i = applied.Count - 1; i >= 0; i--)
        {
            var movement = applied[i];
            try
            {
                await _productApi.AdjustStockAsync(movement.ProductId, -movement.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "No se pudo revertir el stock del producto {ProductId} ({Quantity}). Requiere conciliación manual",
                    movement.ProductId, -movement.Quantity);
            }
        }
    }

    private static void EnsureStockAvailable(ProductInfoDto product, TransactionType type, int quantity, int availableStock)
    {
        if (type == TransactionType.Sale && quantity > availableStock)
            throw new BusinessRuleException(
                $"Stock insuficiente para '{product.Name}'. Disponible: {availableStock}, solicitado: {quantity}.");
    }

    private async Task<ProductInfoDto> GetActiveProductAsync(Guid productId)
    {
        return await _productApi.GetProductAsync(productId)
            ?? throw NotFoundException.For("Producto", productId);
    }

    private async Task<TransactionEntity> GetExistingAsync(Guid id)
    {
        return await _repository.GetAsync(id)
            ?? throw NotFoundException.For(ResourceName, id);
    }

    /// <summary>
    /// En los listados el nombre del producto es informativo: si Productos no responde,
    /// se devuelven las transacciones sin esos datos en lugar de fallar.
    /// </summary>
    private async Task<Dictionary<Guid, ProductInfoDto>> TryGetProductsAsync(List<Guid> productIds)
    {
        if (productIds.Count == 0)
            return [];

        try
        {
            return await _productApi.GetProductsAsync(productIds.Distinct().ToList());
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogWarning(ex, "Servicio de Productos no disponible; se devuelven transacciones sin datos del producto");
            return [];
        }
    }

    private record StockMovement(Guid ProductId, int Quantity);
}
