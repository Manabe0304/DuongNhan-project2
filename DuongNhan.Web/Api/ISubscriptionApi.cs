using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Subscriptions;
using Refit;

namespace DuongNhan.Web.Api;

public interface ISubscriptionApi
{
    [Get(ApiRoutes.Subscriptions.Current)]
    Task<SubscriptionDto> GetCurrentAsync(CancellationToken ct = default);

    [Get(ApiRoutes.Subscriptions.Plans)]
    Task<List<PlanDto>> GetPlansAsync(CancellationToken ct = default);

    [Get(ApiRoutes.Subscriptions.Usage)]
    Task<UsageCounterDto> GetUsageAsync(CancellationToken ct = default);
}