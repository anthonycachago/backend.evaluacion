using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Transactions.Core.Dto.Product;
using Transactions.Core.Exceptions;
using Transactions.Core.ExternalServices;

namespace Transactions.Infrastructure.ExternalServices;

/// <summary>Cliente REST del microservicio de Productos. Traduce los errores HTTP a excepciones de la aplicación.</summary>
public class ProductApiClient : IProductApiClient
{
    private const string ProductsPath = "api/products";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductApiClient> _logger;

    public ProductApiClient(HttpClient httpClient, ILogger<ProductApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductInfoDto?> GetProductAsync(Guid productId)
    {
        using var response = await SendAsync(() => _httpClient.GetAsync($"{ProductsPath}/{productId}"));

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        return await ReadSuccessAsync<ProductInfoDto>(response);
    }

    public async Task<Dictionary<Guid, ProductInfoDto>> GetProductsAsync(IReadOnlyCollection<Guid> productIds)
    {
        if (productIds.Count == 0)
            return [];

        var queryString = string.Join("&", productIds.Distinct().Select(id => $"ids={id}"));
        using var response = await SendAsync(() => _httpClient.GetAsync($"{ProductsPath}/lookup?{queryString}"));

        var products = await ReadSuccessAsync<List<ProductInfoDto>>(response);
        return products.ToDictionary(x => x.Id);
    }

    public async Task<ProductInfoDto> AdjustStockAsync(Guid productId, int quantity)
    {
        using var response = await SendAsync(() =>
            _httpClient.PostAsJsonAsync($"{ProductsPath}/{productId}/stock-adjustments", new { quantity }, JsonOptions));

        return await ReadSuccessAsync<ProductInfoDto>(response);
    }

    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            return await send();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "No se pudo conectar con el servicio de Productos");
            throw new ExternalServiceException("El servicio de productos no está disponible. Intente nuevamente más tarde.");
        }
    }

    private async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions)
                ?? throw new ExternalServiceException("El servicio de productos devolvió una respuesta vacía.");
        }

        var detail = await ReadProblemDetailAsync(response);

        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                throw new NotFoundException(detail ?? "El producto no existe.");

            case HttpStatusCode.BadRequest:
            case HttpStatusCode.UnprocessableEntity:
                throw new BusinessRuleException(detail ?? "El servicio de productos rechazó la operación.");

            default:
                _logger.LogError("El servicio de Productos respondió {StatusCode}: {Detail}", (int)response.StatusCode, detail);
                throw new ExternalServiceException("El servicio de productos no pudo procesar la solicitud.");
        }
    }

    private static async Task<string?> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions);
            if (problem?.Errors is { Count: > 0 })
                return string.Join(" ", problem.Errors.SelectMany(e => e.Value));

            return problem?.Detail;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private class ProblemResponse
    {
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
