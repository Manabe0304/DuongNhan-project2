namespace DuongNhan.ApiService.Services;

/// <summary>Bound from the "Payment" configuration section (appsettings.json / Railway variables Payment__*).</summary>
public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    public string BankName { get; set; } = "VietinBank";

    /// <summary>Bank id used by the QR image service: short code (ICB) or the 6-digit BIN (970415).</summary>
    public string BankCode { get; set; } = "970415";

    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Start of the transfer content. Must match the payment-code prefix configured in SePay
    /// (Cong ty -> Cau hinh chung -> Cau truc ma thanh toan), e.g. SEVQR.
    /// </summary>
    public string CodePrefix { get; set; } = "SEVQR";

    /// <summary>How long an order is held before it expires.</summary>
    public int OrderMinutes { get; set; } = 20;

    /// <summary>Secret the webhook caller must send as "Authorization: Apikey {value}". Empty = webhook disabled.</summary>
    public string SepayApiKey { get; set; } = string.Empty;

    /// <summary>
    /// QR image URL template; empty = the page shows a placeholder instead of a QR. Placeholders:
    /// {bank} {account} {amount} {content} {name} (values are URL-encoded). The QR must carry amount and
    /// content so the banking app pre-fills them, e.g.
    /// https://img.vietqr.io/image/{bank}-{account}-compact2.png?amount={amount}&amp;addInfo={content}&amp;accountName={name}
    /// </summary>
    public string QrUrlTemplate { get; set; } = string.Empty;
}
