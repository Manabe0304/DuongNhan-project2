using DuongNhan.ApiService.Data;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Admin;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Admin.Products;

internal sealed class ListAdminProductsEndpoint(AppDbContext db) : EndpointWithoutRequest<List<AdminProductDto>>
{
    public override void Configure()
    {
        Get(ApiRoutes.Admin.Products);
        Policies(AuthPolicies.RequireAdmin);

        Summary(s =>
        {
            s.Summary = "List every product (including hidden ones)";
            s.Description = "Admin catalogue view with routine step, skin type and visibility.";
            s.Responses[200] = "All non-deleted products.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var rows = await db.Products
            .AsNoTracking()
            .ToListAsync(ct);

        var result = rows
            .Select(p => new AdminProductDto(
                p.Id, p.Name, p.Brand, p.Category, p.Price, p.ImageUrl, p.AffiliateUrl,
                p.SkinType, p.Step ?? SkincareCatalog.StepForCategory(p.Category), p.IsActive, p.CreatedAt))
            .OrderBy(p => p.Step)
            .ThenBy(p => p.Name)
            .ToList();

        await Send.OkAsync(result, ct);
    }
}

public sealed record UpdateAdminProductCommand
{
    public Guid Id { get; init; }
    public string? SkinType { get; init; }
    public int? Step { get; init; }
    public bool? IsActive { get; init; }
}

internal sealed class UpdateAdminProductEndpoint(AppDbContext db) : Endpoint<UpdateAdminProductCommand, AdminProductDto>
{
    public override void Configure()
    {
        Put(ApiRoutes.Admin.Product);
        Policies(AuthPolicies.RequireAdmin);

        Summary(s =>
        {
            s.Summary = "Update a product's skin type, routine step or visibility";
            s.Description = "Only the supplied fields change. SkinType is a comma-separated list (e.g. \"Oily,Combination\") or \"All\".";
            s.Responses[200] = "The updated product.";
            s.Responses[400] = "Invalid step.";
            s.Responses[404] = "No such product.";
        });
    }

    public override async Task HandleAsync(UpdateAdminProductCommand req, CancellationToken ct)
    {
        if (req.Step is { } step && (step < SkincareCatalog.MinStep || step > SkincareCatalog.MaxStep))
        {
            AddError($"Step must be between {SkincareCatalog.MinStep} and {SkincareCatalog.MaxStep}.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (product is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (req.SkinType is not null) product.SkinType = SkincareCatalog.NormalizeSkinTypes(req.SkinType);
        if (req.Step is not null) product.Step = req.Step;
        if (req.IsActive is not null) product.IsActive = req.IsActive.Value;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new AdminProductDto(
            product.Id, product.Name, product.Brand, product.Category, product.Price, product.ImageUrl,
            product.AffiliateUrl, product.SkinType,
            product.Step ?? SkincareCatalog.StepForCategory(product.Category),
            product.IsActive, product.CreatedAt), ct);
    }
}
