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
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Email);

    public async Task LoadAsync()
    {
        Name = await storage.GetAsync(UserKey + ".name");
        Email = await storage.GetAsync(UserKey + ".email");

        if (!string.IsNullOrWhiteSpace(Email) && authState is AuthStateProvider provider)
            provider.SetUser(Name ?? Email.Split('@')[0], Email);
    }

    public async Task SignInAsync(string name, string email, string accessToken, string? refreshToken)
    {
        Name = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name;
        Email = email;
        await storage.SetAsync(UserKey + ".name", Name);
        await storage.SetAsync(UserKey + ".email", Email);
        if (!string.IsNullOrWhiteSpace(accessToken))
            await tokens.StoreAsync(accessToken, refreshToken ?? string.Empty);

        if (authState is AuthStateProvider provider)
            provider.SetUser(Name, Email);
    }

    public async Task SignOutAsync()
    {
        await tokens.ClearAsync();
        await storage.RemoveAsync(UserKey + ".name");
        await storage.RemoveAsync(UserKey + ".email");
        Name = Email = null;

        if (authState is AuthStateProvider provider)
            provider.ClearUser();
    }
}
