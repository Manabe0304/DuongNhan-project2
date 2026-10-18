namespace DuongNhan.Web.Extensions;

public static class BlazorExtensions
{
    public static IServiceCollection AddWebBlazor(this IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            // Default is 32 KB. Images move between browser and server as base64 data URLs, which are far larger;
            // anything over the limit drops the SignalR connection and the JS call hangs until it times out.
            .AddHubOptions(options => options.MaximumReceiveMessageSize = 10 * 1024 * 1024);
        services.AddOutputCache();
        return services;
    }
}
