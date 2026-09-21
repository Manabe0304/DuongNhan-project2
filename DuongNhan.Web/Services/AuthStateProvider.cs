using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace DuongNhan.Web.Services;

public sealed class AuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(Anonymous);
    }

    public void NotifyAuthChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}