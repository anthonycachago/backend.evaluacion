using System.ComponentModel.DataAnnotations;

namespace Products.Core.Dto.Product;

/// <summary>
/// Datos editables de un producto. El stock no se incluye: solo cambia mediante
/// transacciones de compra/venta para que el historial sea coherente.
/// </summary>
public class ProductUpdateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
    public string Name { get; set; } = null!;

    [StringLength(1000, ErrorMessage = "La descripción no puede superar 1000 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [StringLength(100, ErrorMessage = "La categoría no puede superar 100 caracteres.")]
    public string Category { get; set; } = null!;

    [Url(ErrorMessage = "La imagen debe ser una URL válida.")]
    [StringLength(500, ErrorMessage = "La URL de la imagen no puede superar 500 caracteres.")]
    public string? ImageUrl { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El precio debe ser mayor a cero.")]
    public decimal Price { get; set; }
}
