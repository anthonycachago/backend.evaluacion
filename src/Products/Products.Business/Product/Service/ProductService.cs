using Microsoft.Extensions.Logging;
using Products.Business.Product.Interface;
using Products.Core.Dto.Common;
using Products.Core.Dto.Product;
using Products.Core.Exceptions;
using Products.Core.Mappers;
using Products.Core.Models;
using Products.Core.Repository;

namespace Products.Business.Product.Service;

public class ProductService : IProductService
{
    private const string ResourceName = "Producto";
    private const int MaxIdsPerLookup = 200;

    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repository, ILogger<ProductService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PagedResultDto<ProductDto>> SearchAsync(ProductFilterDto filter)
    {
        var page = await _repository.SearchAsync(filter);
        return page.Map(x => x.ToDto());
    }

    public async Task<ProductDto> GetByIdAsync(Guid id)
    {
        var product = await GetExistingAsync(id);
        return product.ToDto();
    }

    public async Task<List<ProductDto>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        var distinctIds = ids.Distinct().ToList();
        if (distinctIds.Count == 0)
            return [];

        if (distinctIds.Count > MaxIdsPerLookup)
            throw new BusinessRuleException($"Se pueden consultar como máximo {MaxIdsPerLookup} productos por solicitud.");

        var products = await _repository.GetByIdsAsync(distinctIds);
        return products.Select(x => x.ToDto()).ToList();
    }

    public Task<List<string>> GetCategoriesAsync()
    {
        return _repository.GetCategoriesAsync();
    }

    public async Task<ProductDto> CreateAsync(ProductCreateDto dto)
    {
        await EnsureUniqueNameAsync(dto.Name, excludeId: null);

        var entity = dto.ToEntity();
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        await _repository.CreateAsync(entity);
        _logger.LogInformation("Producto creado. Id={Id}, Nombre={Name}", entity.Id, entity.Name);

        return entity.ToDto();
    }

    public async Task<ProductDto> UpdateAsync(Guid id, ProductUpdateDto dto)
    {
        var entity = await GetExistingAsync(id);
        await EnsureUniqueNameAsync(dto.Name, excludeId: id);

        dto.ApplyTo(entity);
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);
        _logger.LogInformation("Producto actualizado. Id={Id}", entity.Id);

        return entity.ToDto();
    }

    /// <summary>Eliminación lógica: el producto se oculta pero se conserva para el historial de transacciones.</summary>
    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetExistingAsync(id);

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);
        _logger.LogInformation("Producto eliminado. Id={Id}", entity.Id);
    }

    public async Task<ProductDto> AdjustStockAsync(Guid id, StockAdjustmentDto dto)
    {
        if (dto.Quantity == 0)
            throw new BusinessRuleException("La cantidad a ajustar no puede ser cero.");

        var adjusted = await _repository.TryAdjustStockAsync(id, dto.Quantity);
        var product = await GetExistingAsync(id);

        if (!adjusted)
            throw new BusinessRuleException(
                $"Stock insuficiente para '{product.Name}'. Disponible: {product.Stock}, solicitado: {Math.Abs(dto.Quantity)}.");

        _logger.LogInformation("Stock ajustado. Id={Id}, Cantidad={Quantity}, StockActual={Stock}", id, dto.Quantity, product.Stock);
        return product.ToDto();
    }

    private async Task<ProductEntity> GetExistingAsync(Guid id)
    {
        return await _repository.GetAsync(id)
            ?? throw NotFoundException.For(ResourceName, id);
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? excludeId)
    {
        if (await _repository.ExistsByNameAsync(name.Trim(), excludeId))
            throw new BusinessRuleException($"Ya existe un producto con el nombre '{name.Trim()}'.");
    }
}
