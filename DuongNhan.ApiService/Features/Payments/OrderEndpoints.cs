using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Payments;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Payments;

public sealed class PaymentOrderIdRequest
{
    public Guid Id { get; init; }
}

/// <summary>Polled by the payment dialog to learn whether the transfer has arrived.</summary>
internal sealed class GetOrderEndpoint(
    AppDbContext db,
    IOptions<PaymentOptions> options,
    TimeProvider clock) : Endpoint<PaymentOrderIdRequest, PaymentOrderDto>
{
    public override void Configure()
    {
        Get(ApiRoutes.Payments.Order);
        Policies(AuthPolicies.RequireUser);

        Summary(s =>
        {
            s.Summary = "Get the status of one of your payment orders";
            s.Responses[200] = "The order (status pending | paid | expired | canceled).";
            s.Responses[404] = "No such order for this user.";
        });
    }

    public override async Task HandleAsync(PaymentOrderIdRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(AppClaimTypes.UserId)?.Value, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var order = await db.PaymentOrders.FirstOrDefaultAsync(o => o.Id == req.Id && o.UserId == userId, ct);
        if (order is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Orders expire lazily: nothing runs in the background, the first look after the deadline closes it.
        if (order.Status == PaymentOrder.Pending && order.ExpiresAt <= clock.GetUtcNow())
        {
            order.Status = PaymentOrder.Expired;
            await db.SaveChangesAsync(ct);
        }

        await Send.OkAsync(PaymentOrderMapper.ToDto(order, options.Value), ct);
    }
}

internal sealed class CancelOrderEndpoint(
    AppDbContext db,
    IOptions<PaymentOptions> options) : Endpoint<PaymentOrderIdRequest, PaymentOrderDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.Payments.CancelOrder);
        Policies(AuthPolicies.RequireUser);

        Summary(s =>
        {
            s.Summary = "Cancel one of your unpaid orders";
            s.Description = "Paid orders are left untouched and returned as they are.";
            s.Responses[200] = "The order after the cancel.";
            s.Responses[404] = "No such order for this user.";
        });
    }

    public override async Task HandleAsync(PaymentOrderIdRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(AppClaimTypes.UserId)?.Value, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var order = await db.PaymentOrders.FirstOrDefaultAsync(o => o.Id == req.Id && o.UserId == userId, ct);
        if (order is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (order.Status == PaymentOrder.Pending)
        {
            order.Status = PaymentOrder.Canceled;
            await db.SaveChangesAsync(ct);
        }

        await Send.OkAsync(PaymentOrderMapper.ToDto(order, options.Value), ct);
    }
}
