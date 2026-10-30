using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Transactions.Infrastructure.DataBase.Converters;

namespace Transactions.Infrastructure.DataBase;

public class TransactionsContext : DbContext
{
    public TransactionsContext(DbContextOptions<TransactionsContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todas las fechas se guardan y se devuelven en UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
