namespace DuongNhan.Web.Extensions;

public static class BlazorExtensions
{
    public static IServiceCollection AddWebBlazor(this IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();
        services.AddOutputCache();
        return services;
    }
}