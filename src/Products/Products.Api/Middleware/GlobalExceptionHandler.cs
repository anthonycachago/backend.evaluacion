using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Products.Core.Exceptions;

namespace Products.Api.Middleware;

/// <summary>
/// Convierte las excepciones en respuestas ProblemDetails (RFC 7807), así los controladores
/// no necesitan try/catch y todos los errores tienen el mismo formato.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            NotFoundException => Create(StatusCodes.Status404NotFound, "Recurso no encontrado", exception.Message),
            BusinessRuleException => Create(StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida", exception.Message),
            BadHttpRequestException => Create(StatusCodes.Status400BadRequest, "Solicitud inválida", exception.Message),
            _ => Create(StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado. Intente nuevamente.")
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Error no controlado en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            _logger.LogWarning("{Method} {Path} rechazado: {Message}", httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Create(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
