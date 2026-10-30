using System.ComponentModel.DataAnnotations;
using Transactions.Core.Dto.Common;
using Transactions.Core.Enums;

namespace Transactions.Core.Dto.Transaction;

/// <summary>Filtros dinámicos del listado de transacciones. Todos son opcionales y combinables.</summary>
public class TransactionFilterDto : PagedFilterDto, IValidatableObject
{
    public static readonly string[] SortFields = ["date", "type", "quantity", "unitPrice", "totalPrice"];

    public Guid? ProductId { get; set; }

    [EnumDataType(typeof(TransactionType), ErrorMessage = "El tipo debe ser Purchase (compra) o Sale (venta).")]
    public TransactionType? Type { get; set; }

    /// <summary>Fecha inicial (inclusive).</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Fecha final (inclusive). Si no tiene hora, incluye todo el día.</summary>
    public DateTime? DateTo { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El total mínimo no puede ser negativo.")]
    public decimal? MinTotal { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El total máximo no puede ser negativo.")]
    public decimal? MaxTotal { get; set; }

    /// <summary>Texto libre que se busca en el detalle.</summary>
    public string? Search { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom > DateTo)
            yield return new ValidationResult("La fecha final debe ser mayor o igual a la fecha inicial.", [nameof(DateTo)]);

        if (MinTotal > MaxTotal)
            yield return new ValidationResult("El total máximo debe ser mayor o igual al total mínimo.", [nameof(MaxTotal)]);

        if (!string.IsNullOrWhiteSpace(SortBy) && !SortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult(
                $"Campo de ordenamiento inválido. Valores permitidos: {string.Join(", ", SortFields)}.", [nameof(SortBy)]);
    }
}
