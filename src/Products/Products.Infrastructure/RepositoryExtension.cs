using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Products.Core.Repository;
using Products.Infrastructure.DataBase;
using Products.Infrastructure.Repository.Product;

namespace Products.Infrastructure;

public static class RepositoryExtension
{
    public static IServiceCollection AddRepository(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }

    public static IServiceCollection AddDataBase(this IServiceCollection services, IConfiguration config)
    {
        var productsConnection = config.GetConnectionString("ProductsConnection")
            ?? throw new InvalidOperationException("No se configuró la cadena de conexión 'ProductsConnection'.");

        services.AddDbContext<ProductsContext>(options =>
            options.UseSqlServer(productsConnection));

        return services;
    }
}
