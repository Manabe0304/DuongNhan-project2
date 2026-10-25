using DuongNhan.ApiService.Models;
using DuongNhan.Shared.Dtos.Payments;

namespace DuongNhan.ApiService.Services;

internal static class PaymentOrderMapper
{
    public static PaymentOrderDto ToDto(PaymentOrder order, PaymentOptions options)
    {
        var content = PaymentOrderCodes.Display(order.OrderCode, options.CodePrefix);

        return new PaymentOrderDto(
            order.Id,
            order.PlanCode,
            order.PlanName,
            order.Amount,
            content,
            order.Status,
            order.CreatedAt,
            order.ExpiresAt,
            order.PaidAt,
            options.BankName,
            options.AccountNumber,
            options.AccountName,
            BuildQrUrl(order, options, content));
    }

    /// <summary>
    /// Fills the QR template with this order's amount and transfer content, so scanning the QR in a banking app
    /// pre-fills both. Returns null when no template is configured (the page then shows a placeholder).
    /// </summary>
    private static string? BuildQrUrl(PaymentOrder order, PaymentOptions options, string content)
    {
        if (string.IsNullOrWhiteSpace(options.QrUrlTemplate)) return null;

        return options.QrUrlTemplate
            .Replace("{bank}", Uri.EscapeDataString(options.BankCode), StringComparison.Ordinal)
            .Replace("{account}", Uri.EscapeDataString(options.AccountNumber), StringComparison.Ordinal)
            .Replace("{amount}", decimal.Truncate(order.Amount).ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{content}", Uri.EscapeDataString(content), StringComparison.Ordinal)
            .Replace("{name}", Uri.EscapeDataString(options.AccountName), StringComparison.Ordinal);
    }
}
