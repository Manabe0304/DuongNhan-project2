using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace DuongNhan.Web.Services;

/// <summary>
/// Caches the public plan list for a few minutes. Plans are identical for every visitor and
/// change rarely, so navigating between pages (and the prerender -> interactive re-render)
/// should not hit the API and database each time.
/// </summary>
public sealed class PlanCatalogService(DuongNhanApiService api, IMemoryCache cache)
{
    private const string CacheKey = "plans:v1";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<List<JsonElement>> GetPlansAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out List<JsonElement>? cached) && cached is not null)
            return cached;

        var plans = await api.GetPlansAsync(ct);

        // Never cache an empty result: it usually means the API was not ready yet.
        if (plans.Count > 0)
            cache.Set(CacheKey, plans, Ttl);

        return plans;
    }
}
