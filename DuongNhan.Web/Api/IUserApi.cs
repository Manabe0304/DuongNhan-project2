using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Users;
using Refit;

namespace DuongNhan.Web.Api;

public interface IUserApi
{
    [Get(ApiRoutes.Users.Me)]
    Task<UserDto> GetMeAsync(CancellationToken ct = default);

    [Put(ApiRoutes.Users.UpdateProfile)]
    Task<UserDto> UpdateMeAsync([Body] UpdateProfileRequest body, CancellationToken ct = default);

    [Put(ApiRoutes.Users.ChangePassword)]
    Task ChangePasswordAsync([Body] ChangePasswordRequest body, CancellationToken ct = default);
}
