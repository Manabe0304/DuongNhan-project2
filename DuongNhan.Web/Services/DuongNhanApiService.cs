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
    ISubscriptionApi subscriptions)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

    public async Task<ApiResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var request = NewRequest<DuongNhan.Shared.Dtos.Auth.RefreshTokenRequest>(
            ("RefreshToken", refreshToken));
        var response = await auth.RefreshAsync(request, ct);
        return ApiResult.From(response);
    }

    public async Task<JsonElement?> GetMeAsync(CancellationToken ct = default)
        => await ToJsonAsync(await users.GetMeAsync(ct));

    public async Task<List<JsonElement>> GetProductsAsync(CancellationToken ct = default)
        => ToJsonList(await products.ListAsync(ct));

    public async Task<List<JsonElement>> GetRecommendationsAsync(Guid diagnosisId, CancellationToken ct = default)
        => ToJsonList(await products.RecommendAsync(diagnosisId, ct));

    public async Task<ApiResult> UploadAsync(IBrowserFile file, CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream(Constants.AppConstants.MaxUploadBytes, ct);
        var response = await skin.UploadAsync(new StreamPart(stream, file.Name, file.ContentType), ct);
        return ApiResult.From(response);
    }

    public async Task<JsonElement?> DiagnoseAsync(Guid id, CancellationToken ct = default)
        => await ToJsonAsync(await skin.DiagnoseAsync(id, ct));

    public async Task<DiagnosisDto?> DiagnoseTypedAsync(Guid id, CancellationToken ct = default)
        => await skin.DiagnoseAsync(id, ct);

    /// <summary>Uploads a normalised capture (camera or file) as multipart/form-data.</summary>
    public async Task<UploadSkinResponse> UploadImageAsync(SkinImageInput input, CancellationToken ct = default)
    {
        using var stream = new MemoryStream(input.Bytes, writable: false);
        return await skin.UploadAsync(new StreamPart(stream, input.FileName, input.ContentType), ct);
    }

    public async Task<List<DiagnosisDto>> GetHistoryTypedAsync(CancellationToken ct = default)
        => await skin.GetHistoryAsync(ct);

    public async Task<List<JsonElement>> GetHistoryAsync(CancellationToken ct = default)
        => ToJsonList(await skin.GetHistoryAsync(ct));

    public async Task<List<JsonElement>> GetPlansAsync(CancellationToken ct = default)
        => ToJsonList(await subscriptions.GetPlansAsync(ct));

    public async Task<JsonElement?> GetCurrentSubscriptionAsync(CancellationToken ct = default)
        => await ToJsonAsync(await subscriptions.GetCurrentAsync(ct));

    public async Task<JsonElement?> GetUsageAsync(CancellationToken ct = default)
        => await ToJsonAsync(await subscriptions.GetUsageAsync(ct));

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
    public string? UserName => GetString("userName", "UserName", "name", "Name", "displayName", "DisplayName");
    public string? Email => GetString("email", "Email");

    public string? GetString(params string[] names)
    {
        foreach (var name in names)
        {
            if (Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
        }
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
