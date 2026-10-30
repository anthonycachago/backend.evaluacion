using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Products.Infrastructure.DataBase.Converters;

/// <summary>Guarda las fechas en UTC y las marca como UTC al leerlas, para que la API las devuelva con "Z".</summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
