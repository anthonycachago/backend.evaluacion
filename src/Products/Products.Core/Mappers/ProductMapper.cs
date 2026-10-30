using Products.Core.Dto.Product;
using Products.Core.Models;

namespace Products.Core.Mappers;

/// <summary>Conversión entre la entidad Product y sus DTOs.</summary>
public static class ProductMapper
{
    public static ProductDto ToDto(this ProductEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        Category = entity.Category,
        ImageUrl = entity.ImageUrl,
        Price = entity.Price,
        Stock = entity.Stock,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static ProductEntity ToEntity(this ProductCreateDto dto) => new()
    {
        Name = dto.Name.Trim(),
        Description = NullIfEmpty(dto.Description),
        Category = dto.Category.Trim(),
        ImageUrl = NullIfEmpty(dto.ImageUrl),
        Price = decimal.Round(dto.Price, 2),
        Stock = dto.Stock
    };

    /// <summary>Copia los campos editables sobre una entidad existente (el stock no se modifica).</summary>
    public static void ApplyTo(this ProductUpdateDto dto, ProductEntity entity)
    {
        entity.Name = dto.Name.Trim();
        entity.Description = NullIfEmpty(dto.Description);
        entity.Category = dto.Category.Trim();
        entity.ImageUrl = NullIfEmpty(dto.ImageUrl);
        entity.Price = decimal.Round(dto.Price, 2);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
