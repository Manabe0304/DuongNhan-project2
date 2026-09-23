using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace DuongNhan.Web.Services;

/// <summary>
/// Attaches the stored access token to outgoing API calls.
/// </summary>
public sealed class AuthHeaderHandler(TokenStorage tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await TryGetAccessTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Tokens live in browser storage, which cannot be read while a component is
    /// being prerendered. The interactive render re-issues the request with the
    /// token attached, so sending an anonymous attempt here is harmless — and
    /// avoids logging a JavaScript interop failure.
    /// </summary>
    private async Task<string?> TryGetAccessTokenAsync()
    {
        try
        {
            return await tokens.GetAccessTokenAsync();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (JSException)
        {
            return null;
        }
    }
}