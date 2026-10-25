namespace DuongNhan.Shared.Dtos.Payments;

/// <summary>Body of POST /api/payments/orders: the plan the user wants to buy (e.g. "plan_pro").</summary>
public sealed record CreatePaymentOrderRequest(string PlanCode);

/// <summary>
/// A bank-transfer order. <see cref="Status"/> is pending | paid | expired | canceled.
/// <see cref="QrUrl"/> is null until a QR template is configured (Payment:QrUrlTemplate).
/// </summary>
public sealed record PaymentOrderDto(
    Guid Id,
    string PlanCode,
    string PlanName,
    decimal Amount,
    string OrderCode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? PaidAt,
    string BankName,
    string AccountNumber,
    string AccountName,
    string? QrUrl);
