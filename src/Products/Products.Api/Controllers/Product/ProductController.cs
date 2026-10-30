using Microsoft.AspNetCore.Mvc;
using Products.Business.Product.Interface;
using Products.Core.Dto.Common;
using Products.Core.Dto.Product;

namespace Products.Api.Controllers.Product;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductController : ControllerBase
{
    private readonly IProductService _service;

    public ProductController(IProductService productService)
    {
        _service = productService;
    }

    /// <summary>Listado paginado con filtros dinámicos y ordenamiento.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] ProductFilterDto filter)
    {
        var result = await _service.SearchAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _service.GetByIdAsync(id);
        return Ok(product);
    }

    /// <summary>Consulta varios productos a la vez (incluye eliminados). Lo usa el servicio de Transacciones.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType<List<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Lookup([FromQuery] Guid[] ids)
    {
        var products = await _service.GetByIdsAsync(ids);
        return Ok(products);
    }

    [HttpGet("categories")]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _service.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        var product = await _service.CreateAsync(dto);
        return CreatedAtRoute(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductUpdateDto dto)
    {
        var product = await _service.UpdateAsync(id, dto);
        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Aplica un movimiento de stock (+ compra / − venta). Lo usa el servicio de Transacciones.</summary>
    [HttpPost("{id:guid}/stock-adjustments")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] StockAdjustmentDto dto)
    {
        var product = await _service.AdjustStockAsync(id, dto);
        return Ok(product);
    }
}
