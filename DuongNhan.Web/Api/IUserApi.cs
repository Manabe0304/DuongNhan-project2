using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Users;
using Refit;

namespace DuongNhan.Web.Api;

public interface IUserApi
{
    [Get(ApiRoutes.Users.Me)]
    Task<UserDto> GetMeAsync(CancellationToken ct = default);
}