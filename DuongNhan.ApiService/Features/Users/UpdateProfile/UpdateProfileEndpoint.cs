using System.Text.RegularExpressions;
using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Mappers;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Users;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Users.UpdateProfile;

internal sealed partial class UpdateProfileEndpoint(AppDbContext db, UserMapper mapper)
    : Endpoint<UpdateProfileRequest, UserDto>
{
    public override void Configure()
    {
        Put(ApiRoutes.Users.UpdateProfile);
        Policies(AuthPolicies.RequireUser);

        Summary(s =>
        {
            s.Summary = "Update the authenticated user's profile";
            s.Description = "Updates the display name and phone number. The email address cannot be changed here.";
            s.Responses[200] = "The updated profile.";
            s.Responses[400] = "Invalid name or phone number.";
            s.Responses[404] = "The user no longer exists.";
        });
    }

    public override async Task HandleAsync(UpdateProfileRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(AppClaimTypes.UserId)?.Value, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var name = req.DisplayName?.Trim();
        var phone = req.PhoneNumber?.Trim();

        if (string.IsNullOrEmpty(name) || name.Length > 100)
        {
            AddError("Họ và tên không được để trống và tối đa 100 ký tự.");
        }

        if (!string.IsNullOrEmpty(phone) && !PhonePattern().IsMatch(phone))
        {
            AddError("Số điện thoại không hợp lệ.");
        }

        ThrowIfAnyErrors();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        user.DisplayName = name;
        user.PhoneNumber = string.IsNullOrEmpty(phone) ? null : phone;
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(mapper.ToDto(user), ct);
    }

    [GeneratedRegex(@"^[0-9+()\-.\s]{6,30}$")]
    private static partial Regex PhonePattern();
}
