namespace DuongNhan.ApiService.Services;

internal static class RejectedCredentials
{
    /// <summary>
    /// True when the caller sent an Authorization header but did not end up authenticated (expired or revoked token).
    /// These endpoints allow anonymous guests, so without this check an expired token quietly turns a signed-in
    /// user into the shared guest account: the scan is stored under the guest and is never counted for the user.
    /// </summary>
    public static bool Check(HttpContext http)
        => http.Request.Headers.ContainsKey("Authorization") && http.User.Identity?.IsAuthenticated != true;
}
