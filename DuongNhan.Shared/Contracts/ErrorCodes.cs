namespace DuongNhan.Shared.Contracts;

public static class ErrorCodes
{
    public const string InvalidCredentials = "auth.invalid_credentials";
    public const string EmailAlreadyExists = "auth.email_already_exists";
    public const string EmailNotVerified = "auth.email_not_verified";
    public const string TokenExpired = "auth.token_expired";
    public const string TokenRevoked = "auth.token_revoked";
    public const string ValidationFailed = "validation.failed";
    public const string InvalidFileType = "validation.invalid_file_type";
    public const string FileTooLarge = "validation.file_too_large";
    public const string SkinImageNotFound = "skin.image_not_found";
    public const string SkinImageAlreadyAnalyzed = "skin.image_already_analyzed";
    public const string DiagnosisFailed = "skin.diagnosis_failed";
    public const string DiagnosisLowConfidence = "skin.diagnosis_low_confidence";
    public const string SubscriptionNotFound = "subscription.not_found";
    public const string SubscriptionInactive = "subscription.inactive";
    public const string QuotaExceeded = "subscription.quota_exceeded";
    public const string NotFound = "resource.not_found";
    public const string Forbidden = "resource.forbidden";
    public const string InternalError = "resource.internal_error";
}