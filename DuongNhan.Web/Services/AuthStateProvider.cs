using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace DuongNhan.Web.Services;

public sealed class AuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _user = new(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_user));

    public void SetUser(string name, string email)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, email)
        ], "DuongNhan");
        _user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void ClearUser()
    {
        _user = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void NotifyAuthChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
