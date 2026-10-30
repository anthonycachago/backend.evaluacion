using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transactions.Core.ExternalServices;
using Transactions.Core.Repository;
using Transactions.Infrastructure.DataBase;
using Transactions.Infrastructure.ExternalServices;
using Transactions.Infrastructure.Repository.Transaction;

namespace Transactions.Infrastructure;

public static class RepositoryExtension
{
    public static IServiceCollection AddRepository(this IServiceCollection services)
    {
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        return services;
    }

    public static IServiceCollection AddDataBase(this IServiceCollection services, IConfiguration config)
    {
        var transactionsConnection = config.GetConnectionString("TransactionsConnection")
            ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'TransactionsConnection'.");

        services.AddDbContext<TransactionsContext>(options =>
            options.UseSqlServer(transactionsConnection));

        return services;
    }

    /// <summary>Registra el cliente HTTP hacia el microservicio de Productos.</summary>
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration config)
    {
        var productsApiUrl = config["Services:ProductsApi:BaseUrl"]
            ?? throw new InvalidOperationException("No se configuró 'Services:ProductsApi:BaseUrl'.");
        var timeoutSeconds = config.GetValue("Services:ProductsApi:TimeoutSeconds", 10);

        // Sin reintentos automáticos: un ajuste de stock repetido se aplicaría dos veces.
        services.AddHttpClient<IProductApiClient, ProductApiClient>(client =>
        {
            client.BaseAddress = new Uri(productsApiUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        return services;
    }
}
