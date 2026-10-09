using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Products;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Products.Import;

internal sealed class ImportProductsEndpoint(AppDbContext db)
    : Endpoint<ImportProductsRequest, ImportProductsResponse>
{
    private const int MaxRows = 2000;

    public override void Configure()
    {
        Post(ApiRoutes.Products.Import);
        Policies(AuthPolicies.RequireAdmin);

        Summary(s =>
        {
            s.Summary = "Bulk import products with affiliate links";
            s.Description = "Adds new products and updates existing ones (matched by affiliate URL, then by name + brand). Products that are not in the import are left untouched.";
            s.Responses[200] = "Import result counts.";
            s.Responses[400] = "Invalid payload.";
        });
    }

    public override async Task HandleAsync(ImportProductsRequest req, CancellationToken ct)
    {
        if (req.Products is null || req.Products.Count == 0)
        {
            AddError("No products to import.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (req.Products.Count > MaxRows)
        {
            AddError($"A maximum of {MaxRows} products can be imported at once.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        // Load every product (soft-delete filter on) so existing rows are updated rather than duplicated.
        var existing = await db.Products.ToListAsync(ct);

        var byUrl = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        var byNameBrand = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in existing)
        {
            if (!string.IsNullOrWhiteSpace(p.AffiliateUrl))
                byUrl.TryAdd(p.AffiliateUrl.Trim(), p);
            byNameBrand.TryAdd(Key(p.Name, p.Brand), p);
        }

        int created = 0, updated = 0, skipped = 0;

        foreach (var row in req.Products)
        {
            var name = row.Name?.Trim();
            var url = row.AffiliateUrl?.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url) ||
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                row.Price < 0)
            {
                skipped++;
                continue;
            }

            var brand = Truncate(row.Brand?.Trim(), 150) ?? string.Empty;
            var category = Truncate(row.Category?.Trim(), 100);
            var skinType = string.IsNullOrWhiteSpace(row.SkinType) ? null : SkincareCatalog.NormalizeSkinTypes(row.SkinType);
            var step = SkincareCatalog.ParseStep(row.Step);

            if (!byUrl.TryGetValue(url, out var product))
                byNameBrand.TryGetValue(Key(name, brand), out product);

            if (product is null)
            {
                product = new Product
                {
                    Name = Truncate(name, 255)!,
                    Brand = brand,
                    Category = string.IsNullOrEmpty(category) ? "Unknown" : category,
                    Price = row.Price,
                    ImageUrl = Truncate(row.ImageUrl?.Trim(), 1000),
                    Description = Truncate(row.Description?.Trim(), 2000),
                    TargetConditions = Truncate(row.TargetConditions?.Trim(), 500),
                    UsageInstructions = Truncate(row.UsageInstructions?.Trim(), 1000),
                    AffiliateUrl = Truncate(url, 2000),
                    SkinType = skinType ?? SkincareCatalog.AllSkinTypes,
                    Step = step ?? SkincareCatalog.StepForCategory(category),
                    IsActive = true
                };
                db.Products.Add(product);
                created++;
            }
            else
            {
                product.Name = Truncate(name, 255)!;
                if (brand.Length > 0) product.Brand = brand;
                if (!string.IsNullOrEmpty(category)) product.Category = category;
                product.Price = row.Price;
                product.AffiliateUrl = Truncate(url, 2000);
                if (!string.IsNullOrWhiteSpace(row.ImageUrl)) product.ImageUrl = Truncate(row.ImageUrl.Trim(), 1000);
                if (!string.IsNullOrWhiteSpace(row.Description)) product.Description = Truncate(row.Description.Trim(), 2000);
                if (!string.IsNullOrWhiteSpace(row.TargetConditions)) product.TargetConditions = Truncate(row.TargetConditions.Trim(), 500);
                if (!string.IsNullOrWhiteSpace(row.UsageInstructions)) product.UsageInstructions = Truncate(row.UsageInstructions.Trim(), 1000);
                if (skinType is not null) product.SkinType = skinType;
                    if (step is not null) product.Step = step;
                    else if (product.Step is null) product.Step = SkincareCatalog.StepForCategory(product.Category);
                product.IsActive = true;
                updated++;
            }

            // Keep lookups current so duplicate rows inside the same file merge instead of double-inserting.
            byUrl[url] = product;
            byNameBrand[Key(product.Name, product.Brand)] = product;
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ImportProductsResponse(created, updated, skipped), ct);
    }

    private static string Key(string name, string brand) => $"{name.Trim()}|{brand.Trim()}";

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
