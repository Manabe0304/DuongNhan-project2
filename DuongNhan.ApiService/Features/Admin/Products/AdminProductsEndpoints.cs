using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Admin;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Admin.Products;

internal static class AdminProductMapping
{
    public static AdminProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.Brand, p.Category, p.Price, p.ImageUrl, p.AffiliateUrl,
        p.SkinType, SkincareCatalog.ResolveStep(p.Step, p.Category), p.IsActive, p.CreatedAt,
        p.Description, p.TargetConditions, p.UsageInstructions);

    public static string? Clip(string? value, int max)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    public static bool IsHttpUrl(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

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
        var rows = await db.Products.AsNoTracking().ToListAsync(ct);

        var result = rows
            .Select(AdminProductMapping.ToDto)
            .OrderBy(p => p.Step)
            .ThenBy(p => p.Name)
            .ToList();

        await Send.OkAsync(result, ct);
    }
}

internal sealed class CreateAdminProductEndpoint(AppDbContext db) : Endpoint<SaveAdminProductRequest, AdminProductDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.Admin.Products);
        Policies(AuthPolicies.RequireAdmin);

        Summary(s =>
        {
            s.Summary = "Create a product";
            s.Description = "Name and a valid http(s) affiliate link are required. Step defaults from the category, SkinType to \"All\".";
            s.Responses[200] = "The created product.";
            s.Responses[400] = "Missing or invalid fields.";
        });
    }

    public override async Task HandleAsync(SaveAdminProductRequest req, CancellationToken ct)
    {
        var name = AdminProductMapping.Clip(req.Name, 255);
        var url = req.AffiliateUrl?.Trim();

        if (name is null) AddError("Tên sản phẩm là bắt buộc.");
        if (!AdminProductMapping.IsHttpUrl(url)) AddError("Link affiliate phải là URL http(s) hợp lệ.");
        if (req.Price is < 0) AddError("Giá không được âm.");
        if (req.Step is { } s && (s < SkincareCatalog.MinStep || s > SkincareCatalog.MaxStep))
            AddError($"Step phải nằm trong khoảng {SkincareCatalog.MinStep}-{SkincareCatalog.MaxStep}.");
        ThrowIfAnyErrors();

        var category = AdminProductMapping.Clip(req.Category, 100) ?? "Unknown";

        var product = new Product
        {
            Name = name!,
            Brand = AdminProductMapping.Clip(req.Brand, 150) ?? string.Empty,
            Category = category,
            Price = req.Price ?? 0,
            ImageUrl = AdminProductMapping.Clip(req.ImageUrl, 1000),
            Description = AdminProductMapping.Clip(req.Description, 2000),
            TargetConditions = AdminProductMapping.Clip(req.TargetConditions, 500),
            UsageInstructions = AdminProductMapping.Clip(req.UsageInstructions, 1000),
            AffiliateUrl = AdminProductMapping.Clip(url, 2000),
            SkinType = SkincareCatalog.NormalizeSkinTypes(req.SkinType),
            Step = req.Step ?? SkincareCatalog.StepForCategory(category),
            IsActive = req.IsActive ?? true
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(AdminProductMapping.ToDto(product), ct);
    }
}

public sealed record UpdateAdminProductCommand
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Brand { get; init; }
    public string? Category { get; init; }
    public decimal? Price { get; init; }
    public string? ImageUrl { get; init; }
    public string? Description { get; init; }
    public string? TargetConditions { get; init; }
    public string? UsageInstructions { get; init; }
    public string? AffiliateUrl { get; init; }
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
            s.Summary = "Update a product";
            s.Description = "Only the supplied (non-null) fields change. SkinType is a comma-separated list (e.g. \"Oily,Combination\") or \"All\".";
            s.Responses[200] = "The updated product.";
            s.Responses[400] = "Invalid field.";
            s.Responses[404] = "No such product.";
        });
    }

    public override async Task HandleAsync(UpdateAdminProductCommand req, CancellationToken ct)
    {
        if (req.Step is { } step && (step < SkincareCatalog.MinStep || step > SkincareCatalog.MaxStep))
            AddError($"Step phải nằm trong khoảng {SkincareCatalog.MinStep}-{SkincareCatalog.MaxStep}.");
        if (req.Price is < 0) AddError("Giá không được âm.");
        if (req.AffiliateUrl is not null && !AdminProductMapping.IsHttpUrl(req.AffiliateUrl.Trim()))
            AddError("Link affiliate phải là URL http(s) hợp lệ.");
        if (req.Name is not null && string.IsNullOrWhiteSpace(req.Name))
            AddError("Tên sản phẩm không được để trống.");
        ThrowIfAnyErrors();

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (product is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (req.Name is not null) product.Name = AdminProductMapping.Clip(req.Name, 255)!;
        if (req.Brand is not null) product.Brand = AdminProductMapping.Clip(req.Brand, 150) ?? string.Empty;
        if (req.Category is not null) product.Category = AdminProductMapping.Clip(req.Category, 100) ?? product.Category;
        if (req.Price is not null) product.Price = req.Price.Value;
        if (req.ImageUrl is not null) product.ImageUrl = AdminProductMapping.Clip(req.ImageUrl, 1000);
        if (req.Description is not null) product.Description = AdminProductMapping.Clip(req.Description, 2000);
        if (req.TargetConditions is not null) product.TargetConditions = AdminProductMapping.Clip(req.TargetConditions, 500);
        if (req.UsageInstructions is not null) product.UsageInstructions = AdminProductMapping.Clip(req.UsageInstructions, 1000);
        if (req.AffiliateUrl is not null) product.AffiliateUrl = AdminProductMapping.Clip(req.AffiliateUrl, 2000);
        if (req.SkinType is not null) product.SkinType = SkincareCatalog.NormalizeSkinTypes(req.SkinType);
        if (req.Step is not null) product.Step = req.Step;
        if (req.IsActive is not null) product.IsActive = req.IsActive.Value;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(AdminProductMapping.ToDto(product), ct);
    }
}

public sealed record DeleteAdminProductCommand
{
    public Guid Id { get; init; }
}

internal sealed class DeleteAdminProductEndpoint(AppDbContext db, TimeProvider clock) : Endpoint<DeleteAdminProductCommand>
{
    public override void Configure()
    {
        Delete(ApiRoutes.Admin.Product);
        Policies(AuthPolicies.RequireAdmin);

        Summary(s =>
        {
            s.Summary = "Delete a product";
            s.Description = "Soft delete: the product is hidden everywhere but its row is kept.";
            s.Responses[204] = "Deleted.";
            s.Responses[404] = "No such product.";
        });
    }

    public override async Task HandleAsync(DeleteAdminProductCommand req, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (product is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        product.IsActive = false;
        product.DeletedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
