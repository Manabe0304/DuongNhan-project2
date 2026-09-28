namespace DuongNhan.ApiService.Services;

public interface IEmailSender
{
    /// <summary>True when SMTP settings are present, so <see cref="SendAsync"/> can actually deliver mail.</summary>
    bool IsConfigured { get; }

    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
