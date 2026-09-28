using System.Net;
using System.Net.Mail;

namespace DuongNhan.ApiService.Services;

/// <summary>
/// Sends mail through any SMTP server configured under the "Smtp" section
/// (Host, Port, Username, Password, From, EnableSsl). For Gmail use smtp.gmail.com:587
/// with an App Password, not your normal account password.
/// </summary>
internal sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    private string? Host => configuration["Smtp:Host"];
    private string? Username => configuration["Smtp:Username"];
    private string? Password => configuration["Smtp:Password"];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !Username.StartsWith("REPLACE_", StringComparison.Ordinal)
        && !Password.StartsWith("REPLACE_", StringComparison.Ordinal);

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("SMTP is not configured (Smtp:Host / Username / Password).");

        var port = configuration.GetValue("Smtp:Port", 587);
        var enableSsl = configuration.GetValue("Smtp:EnableSsl", true);
        var from = configuration["Smtp:From"] is { Length: > 0 } f ? f : Username!;

        using var message = new MailMessage
        {
            From = new MailAddress(from, "Dưỡng Nhan"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(Host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(Username, Password)
        };

        await client.SendMailAsync(message, ct);
    }
}
