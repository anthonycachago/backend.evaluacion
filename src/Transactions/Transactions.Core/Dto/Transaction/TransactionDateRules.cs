namespace Transactions.Core.Dto.Transaction;

internal static class TransactionDateRules
{
    public const string FutureDateMessage = "La fecha de la transacción no puede ser futura.";

    // Un día de margen evita rechazar fechas válidas por diferencias de zona horaria entre cliente y servidor.
    private static readonly TimeSpan Tolerance = TimeSpan.FromDays(1);

    public static bool IsInFuture(DateTime date) => date.ToUniversalTime() > DateTime.UtcNow.Add(Tolerance);
}
