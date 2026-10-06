using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace DuongNhan.Web.Services;

public sealed class UiSessionService(
    TokenStorage tokens,
    BrowserStorage storage,
    AuthenticationStateProvider authState)
{
    private const string UserKey = "duongnhan_client_user";

    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Email);

    public async Task LoadAsync()
    {
        Name = await storage.GetAsync(UserKey + ".name");
        Email = await storage.GetAsync(UserKey + ".email");

        if (!string.IsNullOrWhiteSpace(Email) && authState is AuthStateProvider provider)
            provider.SetUser(Name ?? Email.Split('@')[0], Email);

        try
        {
            IsAdmin = HasAdminRole(await tokens.GetAccessTokenAsync());
        }
        catch (Exception)
        {
            IsAdmin = false;
        }
    }

    public async Task SignInAsync(string name, string email, string accessToken, string? refreshToken)
    {
        Name = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name;
        Email = email;
        await storage.SetAsync(UserKey + ".name", Name);
        await storage.SetAsync(UserKey + ".email", Email);
        if (!string.IsNullOrWhiteSpace(accessToken))
            await tokens.StoreAsync(accessToken, refreshToken ?? string.Empty);
        IsAdmin = HasAdminRole(accessToken);

        if (authState is AuthStateProvider provider)
            provider.SetUser(Name, Email);
    }

    public async Task SignOutAsync()
    {
        await tokens.ClearAsync();
        await storage.RemoveAsync(UserKey + ".name");
        await storage.RemoveAsync(UserKey + ".email");
        Name = Email = null;
        IsAdmin = false;

        if (authState is AuthStateProvider provider)
            provider.ClearUser();
    }

    /// <summary>
    /// Reads the role claim from the access token purely to decide whether to show admin buttons.
    /// It is not a security check: the API enforces the Admin policy on every admin endpoint.
    /// </summary>
    private static bool HasAdminRole(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt)) return false;
        var parts = jwt.Split('.');
        if (parts.Length < 2) return false;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');
            using var doc = System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(payload));
            if (!doc.RootElement.TryGetProperty(DuongNhan.Shared.Contracts.AppClaimTypes.Role, out var role))
                return false;

            return role.ValueKind == System.Text.Json.JsonValueKind.Array
                ? role.EnumerateArray().Any(r => string.Equals(r.GetString(), "Admin", StringComparison.OrdinalIgnoreCase))
                : string.Equals(role.GetString(), "Admin", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
