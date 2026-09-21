namespace DuongNhan.Shared.Contracts;

public static class CustomHeaders
{
    public const string RequestId = "X-Request-Id";
    public const string ClientVersion = "X-Client-Version";
    public const string IdempotencyKey = "Idempotency-Key";
}