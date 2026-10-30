using Transactions.Core.Dto.Product;
using Transactions.Core.Dto.Transaction;
using Transactions.Core.Models;

namespace Transactions.Core.Mappers;

/// <summary>Conversión entre la entidad Transaction y sus DTOs.</summary>
public static class TransactionMapper
{
    /// <param name="product">Datos del producto para mostrar nombre y stock; puede ser null.</param>
    public static TransactionDto ToDto(this TransactionEntity entity, ProductInfoDto? product) => new()
    {
        Id = entity.Id,
        Date = entity.Date,
        Type = entity.Type,
        ProductId = entity.ProductId,
        ProductName = product?.Name,
        ProductStock = product?.Stock,
        ProductIsDeleted = product?.IsDeleted,
        Quantity = entity.Quantity,
        UnitPrice = entity.UnitPrice,
        TotalPrice = entity.TotalPrice,
        Detail = entity.Detail,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    /// <param name="defaultUnitPrice">Precio del producto, usado si el DTO no trae precio unitario.</param>
    public static TransactionEntity ToEntity(this TransactionCreateDto dto, decimal defaultUnitPrice)
    {
        var entity = new TransactionEntity
        {
            Date = dto.Date ?? DateTime.UtcNow,
            Type = dto.Type!.Value,
            ProductId = dto.ProductId!.Value,
            Quantity = dto.Quantity,
            UnitPrice = decimal.Round(dto.UnitPrice ?? defaultUnitPrice, 2),
            Detail = NullIfEmpty(dto.Detail)
        };
        entity.TotalPrice = CalculateTotal(entity.Quantity, entity.UnitPrice);
        return entity;
    }

    public static void ApplyTo(this TransactionUpdateDto dto, TransactionEntity entity)
    {
        entity.Date = dto.Date!.Value;
        entity.Type = dto.Type!.Value;
        entity.ProductId = dto.ProductId!.Value;
        entity.Quantity = dto.Quantity;
        entity.UnitPrice = decimal.Round(dto.UnitPrice!.Value, 2);
        entity.TotalPrice = CalculateTotal(entity.Quantity, entity.UnitPrice);
        entity.Detail = NullIfEmpty(dto.Detail);
    }

    private static decimal CalculateTotal(int quantity, decimal unitPrice) =>
        decimal.Round(quantity * unitPrice, 2);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
