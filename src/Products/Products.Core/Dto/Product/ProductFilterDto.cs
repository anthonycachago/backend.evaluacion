using System.ComponentModel.DataAnnotations;
using Products.Core.Dto.Common;

namespace Products.Core.Dto.Product;

/// <summary>Filtros dinámicos del listado de productos. Todos son opcionales y combinables.</summary>
public class ProductFilterDto : PagedFilterDto, IValidatableObject
{
    public static readonly string[] SortFields = ["name", "category", "price", "stock", "createdAt"];

    /// <summary>Texto libre que se busca en nombre y descripción.</summary>
    public string? Search { get; set; }

    public string? Category { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio mínimo no puede ser negativo.")]
    public decimal? MinPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio máximo no puede ser negativo.")]
    public decimal? MaxPrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
    public int? MinStock { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock máximo no puede ser negativo.")]
    public int? MaxStock { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice > MaxPrice)
            yield return new ValidationResult("El precio máximo debe ser mayor o igual al precio mínimo.", [nameof(MaxPrice)]);

        if (MinStock > MaxStock)
            yield return new ValidationResult("El stock máximo debe ser mayor o igual al stock mínimo.", [nameof(MaxStock)]);

        if (!string.IsNullOrWhiteSpace(SortBy) && !SortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult(
                $"Campo de ordenamiento inválido. Valores permitidos: {string.Join(", ", SortFields)}.", [nameof(SortBy)]);
    }
}
