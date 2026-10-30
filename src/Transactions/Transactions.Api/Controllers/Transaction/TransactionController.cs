using Microsoft.AspNetCore.Mvc;
using Transactions.Business.Transaction.Interface;
using Transactions.Core.Dto.Common;
using Transactions.Core.Dto.Transaction;

namespace Transactions.Api.Controllers.Transaction;

[ApiController]
[Route("api/transactions")]
[Produces("application/json")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionController(ITransactionService transactionService)
    {
        _service = transactionService;
    }

    /// <summary>Listado paginado con filtros dinámicos y ordenamiento.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<TransactionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] TransactionFilterDto filter)
    {
        var result = await _service.SearchAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var transaction = await _service.GetByIdAsync(id);
        return Ok(transaction);
    }

    /// <summary>Historial de transacciones de un producto (con nombre y stock), filtrable por fechas y tipo.</summary>
    [HttpGet("products/{productId:guid}/history")]
    [ProducesResponseType<ProductTransactionHistoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductHistory(Guid productId, [FromQuery] TransactionFilterDto filter)
    {
        var history = await _service.GetProductHistoryAsync(productId, filter);
        return Ok(history);
    }

    [HttpPost]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create([FromBody] TransactionCreateDto dto)
    {
        var transaction = await _service.CreateAsync(dto);
        return CreatedAtRoute(nameof(GetById), new { id = transaction.Id }, transaction);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TransactionUpdateDto dto)
    {
        var transaction = await _service.UpdateAsync(id, dto);
        return Ok(transaction);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
