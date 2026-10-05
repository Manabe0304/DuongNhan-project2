using System.Net.Http.Headers;
using System.Text.Json;
using DuongNhan.Admin.Models;

namespace DuongNhan.Admin.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private static string? _accessToken;

    public ApiService(IHttpClientFactory httpClientFactory, ILogger<ApiService> logger, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _logger = logger;
        _httpClient.BaseAddress = new Uri(config["ApiSettings:BaseUrl"] ?? "http://localhost:5417");
    }

    public static void SetAccessToken(string? token)
    {
        _accessToken = token;
    }

    private async Task AddAuthHeaderAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    public async Task<DashboardStats?> GetDashboardStatsAsync()
    {
        try
        {
            await AddAuthHeaderAsync();
            var response = await _httpClient.GetAsync("/api/admin/stats");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DashboardStats>();
            }
            _logger.LogWarning("Failed to get dashboard stats: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
        }
        return null;
    }

    public async Task<List<ProductViewModel>?> GetProductsAsync()
    {
        try
        {
            await AddAuthHeaderAsync();
            var response = await _httpClient.GetAsync("/api/products");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ProductViewModel>>();
            }
            _logger.LogWarning("Failed to get products: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
        }
        return null;
    }

    public async Task<ProductViewModel?> GetProductAsync(Guid id)
    {
        try
        {
            await AddAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"/api/products/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductViewModel>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {Id}", id);
        }
        return null;
    }

    public async Task<bool> UpdateProductAsync(Guid id, ProductEditModel model)
    {
        try
        {
            await AddAuthHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync($"/api/products/{id}", model);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Id}", id);
            return false;
        }
    }

    public async Task<bool> CreateProductAsync(ProductEditModel model)
    {
        try
        {
            await AddAuthHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync("/api/products", model);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return false;
        }
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        try
        {
            await AddAuthHeaderAsync();
            var response = await _httpClient.DeleteAsync($"/api/products/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {Id}", id);
            return false;
        }
    }
}
