using System.Text.Json;

namespace DuongNhan.Web.Services;

/// <summary>Reads claims from a JWT without validating it (the API does the real validation).</summary>
internal static class JwtPayload
{
    public static DateTimeOffset? ExpiresAt(string? jwt)
        => TryRead(jwt, out var root) && root.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;

    public static string? Claim(string? jwt, string name)
        => TryRead(jwt, out var root) && root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool TryRead(string? jwt, out JsonElement root)
    {
        root = default;
        if (string.IsNullOrWhiteSpace(jwt)) return false;

        var parts = jwt.Split('.');
        if (parts.Length < 2) return false;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            root = doc.RootElement.Clone();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
