using DuongNhan.ApiService.Data;

namespace DuongNhan.ApiService.Models;

/// <summary>
/// One bank-transfer order for a plan. The user transfers <see cref="Amount"/> with <see cref="OrderCode"/> as the
/// transfer content; the bank webhook (SePay) matches that code and marks the order paid.
/// </summary>
internal sealed class PaymentOrder : IAuditable
{
    public const string Pending = "pending";
    public const string Paid = "paid";
    public const string Expired = "expired";
    public const string Canceled = "canceled";

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public required string PlanCode { get; set; }
    public required string PlanName { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Normalised transfer content: prefix + 10 characters, upper case, no spaces (e.g. SEVQRCKKT2U29QG).</summary>
    public required string OrderCode { get; set; }

    public string Status { get; set; } = Pending;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>Money actually received (from the bank webhook). Revenue reports use this, falling back to Amount.</summary>
    public decimal? PaidAmount { get; set; }

    /// <summary>SePay transaction id: unique, so a retried webhook can never activate a plan twice.</summary>
    public long? SepayTransactionId { get; set; }

    public string? ReferenceCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
