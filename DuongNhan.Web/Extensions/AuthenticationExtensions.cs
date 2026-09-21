namespace DuongNhan.Web.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddWebAuthentication(this IServiceCollection services)
    {
        services.AddCascadingAuthenticationState();
        return services;
    }
}