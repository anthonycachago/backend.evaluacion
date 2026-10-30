using Microsoft.Extensions.DependencyInjection;
using Products.Business.Product.Interface;
using Products.Business.Product.Service;

namespace Products.Business;

public static class ServiceExtension
{
    public static IServiceCollection AddServicesExtension(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
