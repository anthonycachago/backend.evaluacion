using System.Text.Json.Serialization;
using Transactions.Api.Middleware;
using Transactions.Business;
using Transactions.Infrastructure;
using Transactions.Infrastructure.DataBase;

namespace Transactions.Api;

public class Program
{
    private const string FrontendCorsPolicy = "Frontend";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponseFactory.Create);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDataBase(builder.Configuration);
        builder.Services.AddRepository();
        builder.Services.AddExternalServices(builder.Configuration);
        builder.Services.AddServicesExtension();

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddHealthChecks().AddDbContextCheck<TransactionsContext>();

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

        var app = builder.Build();

        app.UseExceptionHandler();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseCors(FrontendCorsPolicy);

        app.MapControllers();
        app.MapHealthChecks("/health");

        app.Run();
    }
}
