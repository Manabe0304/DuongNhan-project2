using System.Security.Cryptography;
using System.Text;

namespace DuongNhan.ApiService.Features.Auth.Shared;

internal static class AuthHashing
{
    // Hashes a normalized email for privacy-preserving audit/telemetry keys.
    public static string HashEmail(string normalizedEmail)
        => Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(normalizedEmail))).ToLowerInvariant();
}
