using DuongNhan.Web.Api;
using DuongNhan.Web.Services;
using Refit;

namespace DuongNhan.Web.Extensions;

public static class RefitExtensions
{
    public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = new Uri(configuration["Api:BaseUrl"] ?? "https+http://apiservice");

        services.AddRefitClient<IAuthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
            .AddHttpMessageHandler<AuthHeaderHandler>();

        services.AddRefitClient<IUserApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
            .AddHttpMessageHandler<AuthHeaderHandler>();

        services.AddRefitClient<ISkinApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
            .AddHttpMessageHandler<AuthHeaderHandler>();

        services.AddRefitClient<IProductApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
            .AddHttpMessageHandler<AuthHeaderHandler>();

        services.AddRefitClient<IAdminApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
            .AddHttpMessageHandler<AuthHeaderHandler>();
        
        services.AddRefitClient<ISubscriptionApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
            .AddHttpMessageHandler<AuthHeaderHandler>();

        return services;
    }
}
