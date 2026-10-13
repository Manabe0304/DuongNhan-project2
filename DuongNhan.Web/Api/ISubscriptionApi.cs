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

    /// <summary>
    /// The bearer token is passed explicitly: the HttpClientFactory handler runs outside the Blazor
    /// circuit and cannot read it from browser storage, so without this the call goes out anonymous (401)
    /// and the top-bar scan counter never appears.
    /// </summary>
    [Get(ApiRoutes.Subscriptions.Usage)]
    Task<UsageCounterDto> GetUsageAsync(
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);
}
