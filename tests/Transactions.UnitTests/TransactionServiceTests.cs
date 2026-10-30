using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Transactions.Business.Transaction.Service;
using Transactions.Core.Dto.Product;
using Transactions.Core.Dto.Transaction;
using Transactions.Core.Enums;
using Transactions.Core.Exceptions;
using Transactions.Core.ExternalServices;
using Transactions.Core.Models;
using Transactions.Core.Repository;

namespace Transactions.UnitTests;

public class TransactionServiceTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid OtherProductId = Guid.NewGuid();

    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly IProductApiClient _productApi = Substitute.For<IProductApiClient>();
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _service = new TransactionService(_repository, _productApi, NullLogger<TransactionService>.Instance);

        _productApi.AdjustStockAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns(call => Product(call.ArgAt<Guid>(0), stock: 0));
    }

    [Fact]
    public async Task CreateAsync_Purchase_IncreasesStockAndSaves()
    {
        GivenActiveProduct(ProductId, stock: 5, price: 10m);

        var result = await _service.CreateAsync(CreateDto(TransactionType.Purchase, ProductId, 3, unitPrice: null));

        await _productApi.Received(1).AdjustStockAsync(ProductId, 3);
        await _repository.Received(1).CreateAsync(Arg.Any<TransactionEntity>());
        Assert.Equal(10m, result.UnitPrice);
        Assert.Equal(30m, result.TotalPrice);
    }

    [Fact]
    public async Task CreateAsync_SaleGreaterThanStock_ThrowsAndDoesNotTouchStock()
    {
        GivenActiveProduct(ProductId, stock: 2);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(CreateDto(TransactionType.Sale, ProductId, 3)));

        Assert.Contains("Stock insuficiente", exception.Message);
        await _productApi.DidNotReceiveWithAnyArgs().AdjustStockAsync(default, default);
        await _repository.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [Fact]
    public async Task CreateAsync_UnknownProduct_ThrowsNotFound()
    {
        _productApi.GetProductAsync(ProductId).Returns((ProductInfoDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateAsync(CreateDto(TransactionType.Purchase, ProductId, 1)));
    }

    [Fact]
    public async Task CreateAsync_WhenSaveFails_RevertsStock()
    {
        GivenActiveProduct(ProductId, stock: 10);
        _repository.CreateAsync(Arg.Any<TransactionEntity>()).ThrowsAsync(new InvalidOperationException("db caída"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(CreateDto(TransactionType.Sale, ProductId, 4)));

        Received.InOrder(() =>
        {
            _productApi.AdjustStockAsync(ProductId, -4);
            _productApi.AdjustStockAsync(ProductId, 4);
        });
    }

    [Fact]
    public async Task UpdateAsync_SameProduct_AdjustsOnlyTheDifference()
    {
        var existing = GivenExistingTransaction(TransactionType.Sale, ProductId, quantity: 10);
        GivenActiveProduct(ProductId, stock: 3);

        // Disponible = 3 + 10 (se devuelve la venta anterior) = 13
        await _service.UpdateAsync(existing.Id, UpdateDto(TransactionType.Sale, ProductId, 13));

        await _productApi.Received(1).AdjustStockAsync(ProductId, -3);
        Assert.Equal(13, existing.Quantity);
        Assert.Equal(130m, existing.TotalPrice);
    }

    [Fact]
    public async Task UpdateAsync_SameProduct_SaleAboveAvailableStock_Throws()
    {
        var existing = GivenExistingTransaction(TransactionType.Sale, ProductId, quantity: 10);
        GivenActiveProduct(ProductId, stock: 3);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.UpdateAsync(existing.Id, UpdateDto(TransactionType.Sale, ProductId, 14)));

        await _productApi.DidNotReceiveWithAnyArgs().AdjustStockAsync(default, default);
    }

    [Fact]
    public async Task UpdateAsync_ChangingProduct_RevertsOldAndAppliesNew()
    {
        var existing = GivenExistingTransaction(TransactionType.Purchase, ProductId, quantity: 5);
        GivenActiveProduct(OtherProductId, stock: 20);

        await _service.UpdateAsync(existing.Id, UpdateDto(TransactionType.Sale, OtherProductId, 2));

        Received.InOrder(() =>
        {
            _productApi.AdjustStockAsync(ProductId, -5);
            _productApi.AdjustStockAsync(OtherProductId, -2);
        });
    }

    [Fact]
    public async Task UpdateAsync_ChangingProduct_WhenSecondMovementFails_RevertsFirst()
    {
        var existing = GivenExistingTransaction(TransactionType.Purchase, ProductId, quantity: 5);
        GivenActiveProduct(OtherProductId, stock: 20);
        _productApi.AdjustStockAsync(OtherProductId, Arg.Any<int>()).ThrowsAsync(new ExternalServiceException("timeout"));

        await Assert.ThrowsAsync<ExternalServiceException>(() =>
            _service.UpdateAsync(existing.Id, UpdateDto(TransactionType.Sale, OtherProductId, 2)));

        await _productApi.Received(1).AdjustStockAsync(ProductId, 5);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
    }

    [Fact]
    public async Task DeleteAsync_PurchaseWhenStockWouldBeNegative_Throws()
    {
        var existing = GivenExistingTransaction(TransactionType.Purchase, ProductId, quantity: 5);
        GivenLookupProduct(ProductId, stock: 2, isDeleted: false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteAsync(existing.Id));

        await _repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!);
    }

    [Fact]
    public async Task DeleteAsync_Sale_RestoresStock()
    {
        var existing = GivenExistingTransaction(TransactionType.Sale, ProductId, quantity: 4);
        GivenLookupProduct(ProductId, stock: 0, isDeleted: false);

        await _service.DeleteAsync(existing.Id);

        await _productApi.Received(1).AdjustStockAsync(ProductId, 4);
        await _repository.Received(1).DeleteAsync(existing);
    }

    [Fact]
    public async Task DeleteAsync_TransactionOfDeletedProduct_DoesNotAdjustStock()
    {
        var existing = GivenExistingTransaction(TransactionType.Sale, ProductId, quantity: 4);
        GivenLookupProduct(ProductId, stock: 0, isDeleted: true);

        await _service.DeleteAsync(existing.Id);

        await _productApi.DidNotReceiveWithAnyArgs().AdjustStockAsync(default, default);
        await _repository.Received(1).DeleteAsync(existing);
    }

    [Fact]
    public async Task SearchAsync_WhenProductsServiceIsDown_ReturnsTransactionsWithoutProductData()
    {
        var entity = new TransactionEntity { Id = Guid.NewGuid(), ProductId = ProductId, Type = TransactionType.Sale, Quantity = 1, UnitPrice = 1, TotalPrice = 1 };
        _repository.SearchAsync(Arg.Any<TransactionFilterDto>())
            .Returns(new Core.Dto.Common.PagedResultDto<TransactionEntity>([entity], 1, 10, 1));
        _productApi.GetProductsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).ThrowsAsync(new ExternalServiceException("caído"));

        var result = await _service.SearchAsync(new TransactionFilterDto());

        Assert.Single(result.Items);
        Assert.Null(result.Items[0].ProductName);
    }

    private void GivenActiveProduct(Guid id, int stock, decimal price = 10m) =>
        _productApi.GetProductAsync(id).Returns(Product(id, stock, price));

    private void GivenLookupProduct(Guid id, int stock, bool isDeleted)
    {
        var product = Product(id, stock);
        product.IsDeleted = isDeleted;
        _productApi.GetProductsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns(new Dictionary<Guid, ProductInfoDto> { [id] = product });
    }

    private TransactionEntity GivenExistingTransaction(TransactionType type, Guid productId, int quantity)
    {
        var entity = new TransactionEntity
        {
            Id = Guid.NewGuid(),
            Date = DateTime.UtcNow.AddDays(-1),
            Type = type,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = 10m,
            TotalPrice = quantity * 10m,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _repository.GetAsync(entity.Id).Returns(entity);
        return entity;
    }

    private static TransactionCreateDto CreateDto(TransactionType type, Guid productId, int quantity, decimal? unitPrice = 10m) =>
        new() { Type = type, ProductId = productId, Quantity = quantity, UnitPrice = unitPrice };

    private static TransactionUpdateDto UpdateDto(TransactionType type, Guid productId, int quantity) =>
        new() { Date = DateTime.UtcNow.AddHours(-1), Type = type, ProductId = productId, Quantity = quantity, UnitPrice = 10m };

    private static ProductInfoDto Product(Guid id, int stock, decimal price = 10m) =>
        new() { Id = id, Name = "Producto de prueba", Category = "Categoría", Price = price, Stock = stock };
}
