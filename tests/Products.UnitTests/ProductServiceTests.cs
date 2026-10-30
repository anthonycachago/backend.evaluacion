using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Products.Business.Product.Service;
using Products.Core.Dto.Product;
using Products.Core.Exceptions;
using Products.Core.Models;
using Products.Core.Repository;

namespace Products.UnitTests;

public class ProductServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _service = new ProductService(_repository, NullLogger<ProductService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidProduct_SavesTrimmedAndRoundedValues()
    {
        var dto = new ProductCreateDto { Name = "  Mouse  ", Category = " Accesorios ", Description = "  ", Price = 10.555m, Stock = 4 };

        var result = await _service.CreateAsync(dto);

        await _repository.Received(1).CreateAsync(Arg.Is<ProductEntity>(x =>
            x.Name == "Mouse" && x.Category == "Accesorios" && x.Description == null && x.Price == 10.56m && x.Stock == 4));
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateAsync_DuplicatedName_ThrowsBusinessRule()
    {
        _repository.ExistsByNameAsync("Mouse", null).Returns(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new ProductCreateDto { Name = "Mouse", Category = "Accesorios", Price = 10m }));

        await _repository.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotChangeStock()
    {
        var entity = ExistingProduct(stock: 7);

        await _service.UpdateAsync(entity.Id, new ProductUpdateDto { Name = "Nuevo", Category = "Cat", Price = 20m });

        Assert.Equal("Nuevo", entity.Name);
        Assert.Equal(7, entity.Stock);
        Assert.NotNull(entity.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_MarksProductAsDeleted()
    {
        var entity = ExistingProduct(stock: 1);

        await _service.DeleteAsync(entity.Id);

        Assert.True(entity.IsDeleted);
        await _repository.Received(1).UpdateAsync(entity);
        await _repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!);
    }

    [Fact]
    public async Task DeleteAsync_UnknownProduct_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AdjustStockAsync_Applied_ReturnsUpdatedProduct()
    {
        var entity = ExistingProduct(stock: 3);
        _repository.TryAdjustStockAsync(entity.Id, -2).Returns(true);

        var result = await _service.AdjustStockAsync(entity.Id, new StockAdjustmentDto { Quantity = -2 });

        Assert.Equal(entity.Id, result.Id);
    }

    [Fact]
    public async Task AdjustStockAsync_InsufficientStock_ThrowsBusinessRule()
    {
        var entity = ExistingProduct(stock: 1);
        _repository.TryAdjustStockAsync(entity.Id, -5).Returns(false);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.AdjustStockAsync(entity.Id, new StockAdjustmentDto { Quantity = -5 }));

        Assert.Contains("Stock insuficiente", exception.Message);
    }

    [Fact]
    public async Task AdjustStockAsync_ZeroQuantity_ThrowsBusinessRule()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.AdjustStockAsync(Guid.NewGuid(), new StockAdjustmentDto { Quantity = 0 }));

        await _repository.DidNotReceiveWithAnyArgs().TryAdjustStockAsync(default, default);
    }

    private ProductEntity ExistingProduct(int stock)
    {
        var entity = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = "Mouse",
            Category = "Accesorios",
            Price = 10m,
            Stock = stock,
            CreatedAt = DateTime.UtcNow
        };
        _repository.GetAsync(entity.Id).Returns(entity);
        return entity;
    }
}
