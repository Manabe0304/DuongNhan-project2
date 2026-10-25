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

internal sealed class CreateOrderEndpoint(
    AppDbContext db,
    IOptions<PaymentOptions> options,
    TimeProvider clock) : Endpoint<CreatePaymentOrderRequest, PaymentOrderDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.Payments.Orders);
        Policies(AuthPolicies.RequireUser);

        Summary(s =>
        {
            s.Summary = "Create a bank-transfer order for a plan";
            s.Description = "Returns the amount, transfer content and bank details to pay. Re-uses the user's open order for the same plan.";
            s.Responses[200] = "The order to pay.";
            s.Responses[400] = "Unknown or free plan.";
            s.Responses[401] = "Missing or invalid access token.";
        });
    }

    public override async Task HandleAsync(CreatePaymentOrderRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(AppClaimTypes.UserId)?.Value, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var settings = options.Value;
        var planCode = (req.PlanCode ?? string.Empty).Trim();

        var plan = await db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.Code == planCode, ct);

        if (plan is null || plan.Price <= 0)
        {
            AddError("Gói không tồn tại hoặc không cần thanh toán.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
            return;
        }

        var now = clock.GetUtcNow();

        // Same user, same plan, still open and priced the same: keep showing the same QR and countdown.
        var existing = await db.PaymentOrders
            .FirstOrDefaultAsync(o => o.UserId == userId
                                      && o.PlanId == plan.Id
                                      && o.Status == PaymentOrder.Pending
                                      && o.ExpiresAt > now
                                      && o.Amount == plan.Price, ct);
        if (existing is not null)
        {
            await Send.OkAsync(PaymentOrderMapper.ToDto(existing, settings), ct);
            return;
        }

        // A different open order (another plan, or an old price) is replaced by the new one.
        var stale = await db.PaymentOrders
            .Where(o => o.UserId == userId && o.Status == PaymentOrder.Pending)
            .ToListAsync(ct);
        foreach (var old in stale) old.Status = PaymentOrder.Canceled;

        var order = new PaymentOrder
        {
            UserId = userId,
            PlanId = plan.Id,
            PlanCode = plan.Code,
            PlanName = plan.Name,
            Amount = plan.Price,
            OrderCode = PaymentOrderCodes.Generate(settings.CodePrefix),
            ExpiresAt = now.AddMinutes(Math.Max(1, settings.OrderMinutes))
        };

        db.PaymentOrders.Add(order);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(PaymentOrderMapper.ToDto(order, settings), ct);
    }
}
