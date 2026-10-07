using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using DuongNhan.Admin.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;

namespace DuongNhan.Admin.Services;

public class AdminAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdminAuthService> _logger;

    public AdminAuthService(IHttpClientFactory httpClientFactory, ILogger<AdminAuthService> logger, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _logger = logger;
        _httpClient.BaseAddress = new Uri(config["ApiSettings:BaseUrl"] ?? "http://localhost:5417");
    }

    public async Task<(bool Success, string? Token, string? Error)> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new { email, password });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result?.AccessToken != null)
                {
                    ApiService.SetAccessToken(result.AccessToken);
                    return (true, result.AccessToken, null);
                }
            }
            var error = await response.Content.ReadAsStringAsync();
            return (false, null, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return (false, null, "Lỗi kết nối server");
        }
    }

    public async Task SignInHttpContextAsync(HttpContext httpContext, string token, string email, string role, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("AccessToken", token)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    public async Task SignOutHttpContextAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        ApiService.SetAccessToken(null);
    }

    private class LoginResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
