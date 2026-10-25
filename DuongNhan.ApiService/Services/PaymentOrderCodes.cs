using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace DuongNhan.ApiService.Services;

/// <summary>Creates and recognises the transfer content ("SEVQR CKKT2U29QG") that ties a bank transfer to an order.</summary>
internal static class PaymentOrderCodes
{
    public const int SuffixLength = 10;

    // No 0/O/1/I so the code can be read from a screen and typed by hand without mistakes.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Upper-cases and strips everything except letters and digits (banks may drop or add spaces and dots).</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var buffer = new char[text.Length];
        var length = 0;
        foreach (var ch in text)
        {
            if (char.IsAsciiLetterOrDigit(ch))
                buffer[length++] = char.ToUpperInvariant(ch);
        }

        return new string(buffer, 0, length);
    }

    public static string Generate(string prefix)
        => Normalize(prefix) + RandomNumberGenerator.GetString(Alphabet, SuffixLength);

    /// <summary>"SEVQRCKKT2U29QG" is shown to the user as "SEVQR CKKT2U29QG", the same way the bank content is usually typed.</summary>
    public static string Display(string orderCode, string prefix)
    {
        var normalizedPrefix = Normalize(prefix);
        return orderCode.StartsWith(normalizedPrefix, StringComparison.Ordinal) && orderCode.Length > normalizedPrefix.Length
            ? $"{normalizedPrefix} {orderCode[normalizedPrefix.Length..]}"
            : orderCode;
    }

    /// <summary>All order codes found in a transfer content / SePay "code" field (spaces and punctuation ignored).</summary>
    public static List<string> Extract(string prefix, params string?[] texts)
    {
        var result = new List<string>();
        var pattern = new Regex(
            Regex.Escape(Normalize(prefix)) + "[A-Z0-9]{" + SuffixLength + "}",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        foreach (var text in texts)
        {
            foreach (Match match in pattern.Matches(Normalize(text)))
            {
                if (!result.Contains(match.Value, StringComparer.Ordinal))
                    result.Add(match.Value);
            }
        }

        return result;
    }
}
