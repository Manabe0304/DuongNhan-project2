using Microsoft.JSInterop;

namespace DuongNhan.Web.Services;

/// <summary>
/// Thin wrapper over wwwroot/js/analytics.js (window.ga4). Sends Google Analytics 4 events from Blazor.
/// Analytics must never break the app: every call swallows JS-interop failures (prerendering,
/// a closed circuit, an ad blocker, GA not configured).
///
/// Privacy: never pass emails, names, face images or diagnosis results as parameters.
/// </summary>
public sealed class AnalyticsService(IJSRuntime js)
{
    private string? lastPageViewUrl;

    /// <summary>Sends a GA4 page_view. Consecutive identical URLs are sent once.</summary>
    public async Task PageViewAsync(string url, string? title = null)
    {
        if (string.Equals(lastPageViewUrl, url, StringComparison.Ordinal)) return;
        lastPageViewUrl = url;
        await InvokeAsync("ga4.pageView", url, title);
    }

    /// <summary>Sends a GA4 event, e.g. <c>EventAsync("login", new { method = "email" })</c>.</summary>
    public Task EventAsync(string name, object? parameters = null)
        => InvokeAsync("ga4.event", name, parameters ?? new { });

    private async Task InvokeAsync(string identifier, params object?[] args)
    {
        try
        {
            await js.InvokeVoidAsync(identifier, args);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException
                                      or TaskCanceledException or OperationCanceledException)
        {
            // Analytics is best effort.
        }
    }
}
