using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Products.Api.Middleware;

/// <summary>
/// Respuesta 400 cuando fallan las validaciones de los DTO (DataAnnotations) o el JSON es inválido.
/// Devuelve los errores por campo en camelCase para que el frontend los muestre junto a cada input.
/// </summary>
public static class InvalidModelStateResponseFactory
{
    private const string InvalidValueMessage = "El valor tiene un formato inválido.";

    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry => new
            {
                Field = NormalizeKey(entry.Key),
                Messages = entry.Value!.Errors.Select(error => ToMessage(entry.Key, error))
            })
            // La entrada del parámetro completo ("dto") solo repite que el JSON no se pudo leer.
            .Where(error => error.Field.Length > 0 && !IsActionParameter(context, error.Field))
            .GroupBy(error => error.Field)
            .ToDictionary(g => g.Key, g => g.SelectMany(e => e.Messages).Distinct().ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Error de validación",
            Detail = errors.Count > 0 ? "Uno o más campos no son válidos." : "El cuerpo de la solicitud no es válido."
        };

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    // Las claves que empiezan con "$" vienen del deserializador JSON; sus mensajes son técnicos y no se exponen.
    private static string ToMessage(string key, ModelError error) =>
        error.Exception is not null || key.StartsWith('$') || string.IsNullOrWhiteSpace(error.ErrorMessage)
            ? InvalidValueMessage
            : error.ErrorMessage;

    private static string NormalizeKey(string key)
    {
        var normalized = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key.TrimStart('$');
        return normalized.Length == 0 ? normalized : char.ToLowerInvariant(normalized[0]) + normalized[1..];
    }

    private static bool IsActionParameter(ActionContext context, string field) =>
        context.ActionDescriptor.Parameters.Any(p =>
            string.Equals(p.Name, field, StringComparison.OrdinalIgnoreCase) && p.ParameterType.IsClass && p.ParameterType != typeof(string));
}
