using Microsoft.Extensions.DependencyInjection;
using Transactions.Business.Transaction.Interface;
using Transactions.Business.Transaction.Service;

namespace Transactions.Business;

public static class ServiceExtension
{
    public static IServiceCollection AddServicesExtension(this IServiceCollection services)
    {
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}
