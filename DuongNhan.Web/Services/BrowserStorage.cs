using Microsoft.JSInterop;

namespace DuongNhan.Web.Services;

public sealed class BrowserStorage(IJSRuntime js)
{
    public async Task SetAsync(string key, string value)
        => await js.InvokeVoidAsync("localStorage.setItem", key, value);

    public async Task<string?> GetAsync(string key)
        => await js.InvokeAsync<string?>("localStorage.getItem", key);

    public async Task RemoveAsync(string key)
        => await js.InvokeVoidAsync("localStorage.removeItem", key);
}