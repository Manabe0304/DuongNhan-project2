using DuongNhan.ApiService.Data;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Admin;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Admin.Users;

public sealed record ListUsersRequest
{
    [QueryParam]
    public string? Search { get; init; }

    [QueryParam]
    public string? Status { get; init; }

    [QueryParam]
    public int Page { get; init; } = 1;

    [QueryParam]
    public int PageSize { get; init; } = 20;
}

internal sealed class ListUsersEndpoint(AppDbContext db) : Endpoint<ListUsersRequest, AdminPagedResult<AdminUserDto>>
{
    private const int MaxPageSize = 100;

    public override void Configure()
    {
        Get(ApiRoutes.Admin.Users);
        Policies(AuthPolicies.RequireAdmin);

        Summary(s =>
        {
            s.Summary = "List user accounts";
            s.Description = "Paged, searchable list of user accounts with role, status and scan count.";
            s.Responses[200] = "A page of users, newest first.";
            s.Responses[403] = "Caller is not an admin.";
        });
    }

    public override async Task HandleAsync(ListUsersRequest req, CancellationToken ct)
    {
        var page = Math.Max(1, req.Page);
        var pageSize = Math.Clamp(req.PageSize, 1, MaxPageSize);

        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = req.Search.Trim().ToLowerInvariant();
            var likePattern = $"%{term}%";

            query = query.Where(u =>
                EF.Functions.Like(u.Email.ToLower(), likePattern)
                || (u.DisplayName != null && EF.Functions.Like(u.DisplayName.ToLower(), likePattern)));
        }

        if (!string.IsNullOrWhiteSpace(req.Status))
        {
            var status = req.Status.Trim().ToLowerInvariant();
            query = query.Where(u => u.Status == status);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(
                u.Id,
                u.Email,
                u.DisplayName,
                u.UserRoles.Select(r => r.Role.Name).FirstOrDefault() ?? "User",
                u.Status,
                u.EmailVerifiedAt != null,
                u.SkinImages.Count(),
                u.CreatedAt))
            .ToListAsync(ct);

        await Send.OkAsync(new AdminPagedResult<AdminUserDto>(items, total, page, pageSize), ct);
    }
}
