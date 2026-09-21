using PhoneNumbers;

namespace DuongNhan.ApiService.Features.Auth.Shared;

internal static class PhoneNumberValidation
{
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

    public static bool IsValidE164(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (!value.StartsWith('+')) return false;
        if (value.Length is < 2 or > 16) return false;

        try
        {
            var parsed = PhoneUtil.Parse(value, null);
            return PhoneUtil.IsValidNumber(parsed);
        }
        catch (NumberParseException)
        {
            return false;
        }
    }

    public static bool TryNormalize(string? input, string defaultRegion, out string? e164)
    {
        e164 = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        try
        {
            var parsed = PhoneUtil.Parse(input, defaultRegion);
            if (!PhoneUtil.IsValidNumber(parsed)) return false;

            e164 = PhoneUtil.Format(parsed, PhoneNumberFormat.E164);
            return true;
        }
        catch (NumberParseException)
        {
            return false;
        }
    }
}