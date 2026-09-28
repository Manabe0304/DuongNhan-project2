using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using Refit;

namespace DuongNhan.Web.Api;

public interface IAuthApi
{
    [Post(ApiRoutes.Auth.Register)]
    Task<AuthResponse> RegisterAsync([Body] RegisterRequest request, CancellationToken ct = default);

    [Post(ApiRoutes.Auth.Login)]
    Task<AuthResponse> LoginAsync([Body] LoginRequest request, CancellationToken ct = default);

    [Post(ApiRoutes.Auth.Refresh)]
    Task<AuthResponse> RefreshAsync([Body] RefreshTokenRequest request, CancellationToken ct = default);

    [Post(ApiRoutes.Auth.Logout)]
    Task LogoutAsync([Body] RefreshTokenRequest request, CancellationToken ct = default);

    [Post(ApiRoutes.Auth.Google)]
    Task<AuthResponse> GoogleLoginAsync([Body] GoogleLoginRequest request, CancellationToken ct = default);

    [Post(ApiRoutes.Auth.SendOtp)]
    Task<OtpResponse> SendOtpAsync([Body] SendOtpRequest request, CancellationToken ct = default);

    [Post(ApiRoutes.Auth.VerifyOtp)]
    Task<VerifyOtpResponse> VerifyOtpAsync([Body] VerifyOtpRequest request, CancellationToken ct = default);
}
