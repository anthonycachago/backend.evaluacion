namespace Products.Core.Exceptions;

/// <summary>Se incumplió una regla de negocio (HTTP 422).</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

/// <summary>El recurso solicitado no existe (HTTP 404).</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException For(string resource, object key) =>
        new($"{resource} con id '{key}' no existe.");
}
