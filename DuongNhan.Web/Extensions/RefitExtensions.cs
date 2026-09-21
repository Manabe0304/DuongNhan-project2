using DuongNhan.Web.Api;
using Refit;

namespace DuongNhan.Web.Extensions;

public static class RefitExtensions
{
    public static IServiceCollection AddApiClients(this IServiceCollection services)
    {
        var baseUrl = new Uri("https+http://apiservice");

        services.AddRefitClient<IAuthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl);

        services.AddRefitClient<IUserApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl);

        services.AddRefitClient<ISkinApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl);

        services.AddRefitClient<IProductApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl);

        services.AddRefitClient<ISubscriptionApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl);

        return services;
    }
}