using System.Reflection;
using System.Text.Json;
using DuongNhan.Shared.Dtos.Skin;
using DuongNhan.Web.Api;
using Microsoft.AspNetCore.Components.Forms;
using Refit;

namespace DuongNhan.Web.Services;

/// <summary>
/// Application-facing service layer. UI components call this service instead of
/// talking directly to Refit interfaces, keeping API/DTO details out of the UI.
/// </summary>
public sealed class DuongNhanApiService(
    IAuthApi auth,
    IUserApi users,
    ISkinApi skin,
    IProductApi products,
    ISubscriptionApi subscriptions,
    TokenStorage tokens) : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // One refresh at a time per circuit: refresh tokens rotate, so two parallel refreshes would revoke each other.
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public void Dispose() => _refreshLock.Dispose();

    public async Task<ApiResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var request = NewRequest<DuongNhan.Shared.Dtos.Auth.LoginRequest>(
            ("Email", email), ("Password", password));
        var response = await auth.LoginAsync(request, ct);
        return ApiResult.From(response);
    }

    public async Task<ApiResult> RegisterAsync(string name, string email, string password, CancellationToken ct = default)
    {
        var request = NewRequest<DuongNhan.Shared.Dtos.Auth.RegisterRequest>(
            ("Name", name), ("DisplayName", name), ("Email", email), ("Password", password));
        var response = await auth.RegisterAsync(request, ct);
        return ApiResult.From(response);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var request = NewRequest<DuongNhan.Shared.Dtos.Auth.RefreshTokenRequest>(
            ("RefreshToken", refreshToken));
        await auth.LogoutAsync(request, ct);
    }

    public async Task<ApiResult> GoogleLoginAsync(string idToken, CancellationToken ct = default)
    {
        var response = await auth.GoogleLoginAsync(new DuongNhan.Shared.Dtos.Auth.GoogleLoginRequest(idToken), ct);
        return ApiResult.From(response);
    }

    /// <summary>Asks the API to email a verification code. Returns false if the server could not send it.</summary>
    public async Task<bool> SendOtpAsync(string email, CancellationToken ct = default)
    {
        var response = await auth.SendOtpAsync(new DuongNhan.Shared.Dtos.Auth.SendOtpRequest(email), ct);
        return response.Sent;
    }

    public async Task<bool> VerifyOtpAsync(string email, string code, CancellationToken ct = default)
    {
        var response = await auth.VerifyOtpAsync(new DuongNhan.Shared.Dtos.Auth.VerifyOtpRequest(email, code), ct);
        return response.Verified;
    }

    public async Task<ApiResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var request = NewRequest<DuongNhan.Shared.Dtos.Auth.RefreshTokenRequest>(
            ("RefreshToken", refreshToken));
        var response = await auth.RefreshAsync(request, ct);
        return ApiResult.From(response);
    }

    public async Task<JsonElement?> GetMeAsync(CancellationToken ct = default)
        => await ToJsonAsync(await users.GetMeAsync(ct));

    public async Task<DuongNhan.Shared.Dtos.Users.UserDto> GetMeTypedAsync(CancellationToken ct = default)
        => await users.GetMeAsync(ct);

    public async Task<DuongNhan.Shared.Dtos.Users.UserDto> UpdateProfileAsync(
        string displayName, string? phoneNumber, CancellationToken ct = default)
        => await users.UpdateMeAsync(new DuongNhan.Shared.Dtos.Users.UpdateProfileRequest(displayName, phoneNumber), ct);

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
        => await users.ChangePasswordAsync(new DuongNhan.Shared.Dtos.Users.ChangePasswordRequest(currentPassword, newPassword), ct);

    public async Task<List<JsonElement>> GetProductsAsync(CancellationToken ct = default)
        => ToJsonList(await products.ListAsync(ct));

    /// <summary>
    /// Admin-only bulk import. The token is attached explicitly because the HttpClientFactory handler
    /// runs outside the Blazor circuit and cannot read it from browser storage.
    /// </summary>
    public async Task<DuongNhan.Shared.Dtos.Products.ImportProductsResponse> ImportProductsAsync(
        List<DuongNhan.Shared.Dtos.Products.ImportProductRow> rows, CancellationToken ct = default)
    {
        var token = await tokens.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Bạn cần đăng nhập bằng tài khoản Admin.");

        return await products.ImportAsync(
            new DuongNhan.Shared.Dtos.Products.ImportProductsRequest(rows), $"Bearer {token}", ct);
    }

    public async Task<List<JsonElement>> GetRecommendationsAsync(Guid diagnosisId, CancellationToken ct = default)
        => ToJsonList(await products.RecommendAsync(diagnosisId, ct));

    public async Task<ApiResult> UploadAsync(IBrowserFile file, CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream(Constants.AppConstants.MaxUploadBytes, ct);
        var response = await skin.UploadAsync(new StreamPart(stream, file.Name, file.ContentType), await TryBearerAsync(), ct);
        return ApiResult.From(response);
    }

    public async Task<JsonElement?> DiagnoseAsync(Guid id, CancellationToken ct = default)
        => await ToJsonAsync(await skin.DiagnoseAsync(id, await TryBearerAsync(), ct));

    public async Task<DiagnosisDto?> DiagnoseTypedAsync(Guid id, CancellationToken ct = default)
        => await skin.DiagnoseAsync(id, await TryBearerAsync(), ct);

    /// <summary>Uploads a normalised capture (camera or file) as multipart/form-data.</summary>
    public async Task<UploadSkinResponse> UploadImageAsync(SkinImageInput input, CancellationToken ct = default)
    {
        using var stream = new MemoryStream(input.Bytes, writable: false);
        return await skin.UploadAsync(new StreamPart(stream, input.FileName, input.ContentType), await TryBearerAsync(), ct);
    }

    public async Task<List<DiagnosisDto>> GetHistoryTypedAsync(CancellationToken ct = default)
        => await skin.GetHistoryAsync(await TryBearerAsync(), ct);

    public async Task<List<JsonElement>> GetHistoryAsync(CancellationToken ct = default)
        => ToJsonList(await skin.GetHistoryAsync(await TryBearerAsync(), ct));

    public async Task<List<JsonElement>> GetPlansAsync(CancellationToken ct = default)
        => ToJsonList(await subscriptions.GetPlansAsync(ct));

    public async Task<JsonElement?> GetCurrentSubscriptionAsync(CancellationToken ct = default)
        => await ToJsonAsync(await subscriptions.GetCurrentAsync(ct));

    public async Task<DuongNhan.Shared.Dtos.Subscriptions.UsageCounterDto> GetUsageTypedAsync(CancellationToken ct = default)
        => await subscriptions.GetUsageAsync(await BearerAsync(), ct);

    public async Task<JsonElement?> GetUsageAsync(CancellationToken ct = default)
        => await ToJsonAsync(await subscriptions.GetUsageAsync(await BearerAsync(), ct));

    /// <summary>Reads the access token from browser storage (inside the circuit) for an explicit Authorization header.</summary>
    private async Task<string> BearerAsync()
    {
        var token = await GetFreshAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.");

        return $"Bearer {token}";
    }

    /// <summary>Like <see cref="BearerAsync"/> but returns null for signed-out visitors (guest scans).</summary>
    private async Task<string?> TryBearerAsync()
    {
        try
        {
            var token = await GetFreshAccessTokenAsync();
            return string.IsNullOrWhiteSpace(token) ? null : $"Bearer {token}";
        }
        catch (InvalidOperationException)
        {
            // Prerendering: browser storage is not reachable yet.
            return null;
        }
    }

    /// <summary>E-mail of the account the stored access token belongs to (null when signed out).</summary>
    public async Task<string?> GetTokenEmailAsync()
        => JwtPayload.Claim(await tokens.GetAccessTokenAsync(), DuongNhan.Shared.Contracts.AppClaimTypes.Email);

    /// <summary>
    /// Access tokens live 15 minutes and nothing else renews them. An expired token makes the API treat the
    /// caller as an anonymous guest, so scans were saved to the guest account and never counted for the user.
    /// This renews the token with the refresh token shortly before it expires.
    /// </summary>
    private async Task<string?> GetFreshAccessTokenAsync()
    {
        var token = await tokens.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token) || !IsExpiring(token)) return token;

        await _refreshLock.WaitAsync();
        try
        {
            // Another call (or another tab) may already have renewed it.
            token = await tokens.GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token) || !IsExpiring(token)) return token;

            var refresh = await tokens.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refresh)) return token;

            try
            {
                var result = await RefreshAsync(refresh);
                if (!string.IsNullOrWhiteSpace(result.AccessToken))
                {
                    await tokens.StoreAsync(result.AccessToken, result.RefreshToken ?? refresh);
                    return result.AccessToken;
                }
            }
            catch (ApiException)
            {
                // Refresh token revoked or expired: fall through, the API will answer 401 and the UI asks to sign in again.
            }

            return token;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static bool IsExpiring(string jwt)
        => JwtPayload.ExpiresAt(jwt) is { } exp && exp <= DateTimeOffset.UtcNow.AddSeconds(60);

    private static T NewRequest<T>(params (string Name, object? Value)[] values) where T : class
    {
        try
        {
            var obj = Activator.CreateInstance<T>();
            if (obj is not null)
            {
                foreach (var (name, value) in values)
                {
                    var prop = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)
                                          && p.CanWrite);
                    if (prop is null || value is null) continue;
                    if (prop.PropertyType.IsAssignableFrom(value.GetType()))
                        prop.SetValue(obj, value);
                }
                return obj;
            }
        }
        catch { /* Fall through to JSON constructor binding. */ }

        var dict = values.Where(v => v.Value is not null)
            .ToDictionary(v => v.Name, v => v.Value);
        return JsonSerializer.Deserialize<T>(
            JsonSerializer.Serialize(dict, JsonOptions), JsonOptions)
            ?? throw new InvalidOperationException($"Cannot create {typeof(T).Name}.");
    }

    private static async Task<JsonElement?> ToJsonAsync<T>(T value)
    {
        if (value is null) return null;
        await Task.CompletedTask;
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value, JsonOptions));
        return doc.RootElement.Clone();
    }

    private static List<JsonElement> ToJsonList<T>(IEnumerable<T> values)
        => values.Select(v => JsonSerializer.SerializeToElement(v, JsonOptions)).ToList();
}

public sealed record ApiResult(JsonElement Data)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ApiResult From<T>(T data)
        => new(JsonSerializer.SerializeToElement(data, JsonOptions));

    public string? AccessToken => GetString("accessToken", "AccessToken", "token");
    public string? RefreshToken => GetString("refreshToken", "RefreshToken");
    public string? UserName => GetString("userName", "UserName", "name", "Name", "displayName", "DisplayName") ?? GetNestedUserString("displayName", "DisplayName");
    public string? Email => GetString("email", "Email") ?? GetNestedUserString("email", "Email");

    public string? GetString(params string[] names)
    {
        foreach (var name in names)
        {
            if (Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
        }
        return null;
    }

    private string? GetNestedUserString(params string[] names)
    {
        if (Data.ValueKind != JsonValueKind.Object) return null;
        if (!Data.TryGetProperty("user", out var user) && !Data.TryGetProperty("User", out user)) return null;
        if (user.ValueKind != JsonValueKind.Object) return null;

        foreach (var name in names)
            if (user.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
        return null;
    }

    public Guid? GetGuid(params string[] names)
    {
        foreach (var name in names)
        {
            if (Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty(name, out var p))
            {
                if (p.ValueKind == JsonValueKind.String && Guid.TryParse(p.GetString(), out var g)) return g;
            }
        }
        return null;
    }
}
