using System.Security.Cryptography;
using System.Text;
using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DuongNhan.ApiService.Features.Payments;

/// <summary>Body SePay POSTs for every bank transaction (only the fields used here).</summary>
public sealed class SepayWebhookRequest
{
    public long Id { get; init; }
    public string? Gateway { get; init; }
    public string? Code { get; init; }
    public string? Content { get; init; }

    /// <summary>"in" = money received, "out" = money sent.</summary>
    public string? TransferType { get; init; }

    public decimal TransferAmount { get; init; }
    public string? ReferenceCode { get; init; }
}

public sealed record SepayWebhookResponse(bool Success);

/// <summary>
/// Receives SePay's "money arrived" call, matches the transfer content to a pending order and activates the plan.
/// Protected by "Authorization: Apikey {Payment:SepayApiKey}". SePay needs {"success": true} with HTTP 200/201 to stop
/// retrying, so anything that is not a payment for us (outgoing money, unknown content, a duplicate) answers success.
/// </summary>
internal sealed class SepayWebhookEndpoint(
    AppDbContext db,
    IOptions<PaymentOptions> options,
    TimeProvider clock,
    ILogger<SepayWebhookEndpoint> logger) : Endpoint<SepayWebhookRequest, SepayWebhookResponse>
{
    private const string ApiKeyScheme = "Apikey ";

    public override void Configure()
    {
        Post(ApiRoutes.Payments.SepayWebhook);
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "SePay bank-transfer webhook";
            s.Description = "Marks the matching order paid and activates the purchased plan. Idempotent per SePay transaction id.";
            s.Responses[200] = "Processed (or ignored on purpose).";
            s.Responses[401] = "Wrong or missing API key.";
        });
    }

    public override async Task HandleAsync(SepayWebhookRequest req, CancellationToken ct)
    {
        var settings = options.Value;

        if (!IsAuthorized(settings.SepayApiKey))
        {
            SepayWebhookLogs.Unauthorized(logger);
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!string.Equals(req.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            await Send.OkAsync(new SepayWebhookResponse(true), ct);
            return;
        }

        var codes = PaymentOrderCodes.Extract(settings.CodePrefix, req.Code, req.Content);
        if (codes.Count == 0)
        {
            SepayWebhookLogs.NoOrderCode(logger, req.Id);
            await Send.OkAsync(new SepayWebhookResponse(true), ct);
            return;
        }

        // SePay retries on failure, and may call twice: a transaction id we already stored is done.
        if (await db.PaymentOrders.AnyAsync(o => o.SepayTransactionId == req.Id, ct))
        {
            await Send.OkAsync(new SepayWebhookResponse(true), ct);
            return;
        }

        var order = await db.PaymentOrders.FirstOrDefaultAsync(o => codes.Contains(o.OrderCode), ct);
        if (order is null)
        {
            SepayWebhookLogs.UnknownOrder(logger, req.Id);
            await Send.OkAsync(new SepayWebhookResponse(true), ct);
            return;
        }

        if (order.Status == PaymentOrder.Paid)
        {
            await Send.OkAsync(new SepayWebhookResponse(true), ct);
            return;
        }

        if (req.TransferAmount < order.Amount)
        {
            // Not enough money: leave the order unpaid so the user can see it is still waiting.
            SepayWebhookLogs.Underpaid(logger, order.Id, req.TransferAmount, order.Amount);
            await Send.OkAsync(new SepayWebhookResponse(true), ct);
            return;
        }

        var plan = await db.Plans
            .IgnoreQueryFilters([AppQueryFilters.SoftDelete])
            .FirstOrDefaultAsync(p => p.Id == order.PlanId, ct);
        if (plan is null)
        {
            SepayWebhookLogs.PlanMissing(logger, order.Id);
            await Send.OkAsync(new SepayWebhookResponse(true), ct);
            return;
        }

        // Also activates orders that expired or were canceled before the money arrived: the user did pay.
        var now = clock.GetUtcNow();
        order.Status = PaymentOrder.Paid;
        order.PaidAt = now;
        order.PaidAmount = req.TransferAmount;
        order.SepayTransactionId = req.Id;
        order.ReferenceCode = req.ReferenceCode is { Length: > 100 } ? req.ReferenceCode[..100] : req.ReferenceCode;

        await SubscriptionActivator.ActivateAsync(db, order.UserId, plan, order.Id, now, ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Two deliveries of the same transaction raced: the unique index let only one through.
            SepayWebhookLogs.DuplicateDelivery(logger, req.Id, ex);
            await Send.OkAsync(new SepayWebhookResponse(true), ct);
            return;
        }

        SepayWebhookLogs.Paid(logger, order.Id, order.PlanCode, req.TransferAmount);
        await Send.OkAsync(new SepayWebhookResponse(true), ct);
    }

    private bool IsAuthorized(string expectedKey)
    {
        // No key configured = webhook switched off, never "open to everyone".
        if (string.IsNullOrWhiteSpace(expectedKey)) return false;

        var header = HttpContext.Request.Headers.Authorization.ToString();
        if (!header.StartsWith(ApiKeyScheme, StringComparison.OrdinalIgnoreCase)) return false;

        var given = Encoding.UTF8.GetBytes(header[ApiKeyScheme.Length..].Trim());
        var expected = Encoding.UTF8.GetBytes(expectedKey.Trim());
        return CryptographicOperations.FixedTimeEquals(given, expected);
    }
}

internal static partial class SepayWebhookLogs
{
    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning, Message = "SePay webhook rejected: missing or wrong API key.")]
    public static partial void Unauthorized(ILogger logger);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Information, Message = "SePay transaction {TransactionId} has no order code in its content; ignored.")]
    public static partial void NoOrderCode(ILogger logger, long transactionId);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Warning, Message = "SePay transaction {TransactionId} carries an order code that matches no order.")]
    public static partial void UnknownOrder(ILogger logger, long transactionId);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Warning, Message = "Order {OrderId} received {Received} but needs {Required}; not activated.")]
    public static partial void Underpaid(ILogger logger, Guid orderId, decimal received, decimal required);

    [LoggerMessage(EventId = 4005, Level = LogLevel.Error, Message = "Order {OrderId} was paid but its plan no longer exists; not activated.")]
    public static partial void PlanMissing(ILogger logger, Guid orderId);

    [LoggerMessage(EventId = 4006, Level = LogLevel.Warning, Message = "SePay transaction {TransactionId} was delivered twice at the same time; the second copy was dropped.")]
    public static partial void DuplicateDelivery(ILogger logger, long transactionId, Exception exception);

    [LoggerMessage(EventId = 4007, Level = LogLevel.Information, Message = "Order {OrderId} paid ({PlanCode}, {Amount}); plan activated.")]
    public static partial void Paid(ILogger logger, Guid orderId, string planCode, decimal amount);
}
