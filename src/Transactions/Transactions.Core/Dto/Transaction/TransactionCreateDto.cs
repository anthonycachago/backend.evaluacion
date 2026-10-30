using System.ComponentModel.DataAnnotations;
using Transactions.Core.Enums;

namespace Transactions.Core.Dto.Transaction;

public class TransactionCreateDto : IValidatableObject
{
    /// <summary>Fecha de la transacción. Si se omite se usa la fecha actual.</summary>
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "El tipo de transacción es obligatorio.")]
    [EnumDataType(typeof(TransactionType), ErrorMessage = "El tipo debe ser Purchase (compra) o Sale (venta).")]
    public TransactionType? Type { get; set; }

    [Required(ErrorMessage = "El producto es obligatorio.")]
    public Guid? ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Quantity { get; set; }

    /// <summary>Precio unitario. Si se omite se usa el precio actual del producto.</summary>
    [Range(typeof(decimal), "0.01", "9999999999999999", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El precio unitario debe ser mayor a cero.")]
    public decimal? UnitPrice { get; set; }

    [StringLength(500, ErrorMessage = "El detalle no puede superar 500 caracteres.")]
    public string? Detail { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProductId == Guid.Empty)
            yield return new ValidationResult("El producto es obligatorio.", [nameof(ProductId)]);

        if (Date.HasValue && TransactionDateRules.IsInFuture(Date.Value))
            yield return new ValidationResult(TransactionDateRules.FutureDateMessage, [nameof(Date)]);
    }
}
