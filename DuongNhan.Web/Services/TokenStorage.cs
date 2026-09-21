using Microsoft.JSInterop;

namespace DuongNhan.Web.Services;

public sealed class TokenStorage(IJSRuntime js)
{
    private const string AccessKey = "dn.access_token";
    private const string RefreshKey = "dn.refresh_token";

    public async Task StoreAsync(string accessToken, string refreshToken)
    {
        await js.InvokeVoidAsync("localStorage.setItem", AccessKey, accessToken);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshKey, refreshToken);
    }

    public Task<string?> GetAccessTokenAsync()
        => js.InvokeAsync<string?>("localStorage.getItem", AccessKey).AsTask();

    public Task<string?> GetRefreshTokenAsync()
        => js.InvokeAsync<string?>("localStorage.getItem", RefreshKey).AsTask();

    public async Task ClearAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", AccessKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshKey);
    }
}