using System.ComponentModel.DataAnnotations;
using Transactions.Core.Enums;

namespace Transactions.Core.Dto.Common;

/// <summary>Parámetros comunes de paginación y ordenamiento.</summary>
public abstract class PagedFilterDto
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "El número de página debe ser mayor a cero.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "El tamaño de página debe estar entre 1 y 100.")]
    public int PageSize { get; set; } = DefaultPageSize;

    public string? SortBy { get; set; }

    public SortDirection SortDirection { get; set; } = SortDirection.Asc;

    public int Skip => (PageNumber - 1) * PageSize;
}
