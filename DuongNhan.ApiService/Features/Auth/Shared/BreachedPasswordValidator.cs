using HaveIBeenPwned.Client;

namespace DuongNhan.ApiService.Features.Auth.Shared;

internal sealed class BreachedPasswordValidator(
    IPwnedPasswordsClient pwnedPasswordsClient,
    ILogger<BreachedPasswordValidator> logger)
{
    public async Task<bool> IsBreachedAsync(string password, CancellationToken ct = default)
    {
        try
        {
            var result = await pwnedPasswordsClient.GetPwnedPasswordAsync(password, ct);
            return result.PwnedCount > 0;
        }
        catch (Exception ex)
        {
            BreachedPasswordValidatorLogs.BreachCheckFailed(logger, ex);
            return false;
        }
    }
}

internal static partial class BreachedPasswordValidatorLogs
{
    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Warning,
        Message = "HIBP breach check failed; allowing password through.")]
    public static partial void BreachCheckFailed(ILogger logger, Exception exception);
}