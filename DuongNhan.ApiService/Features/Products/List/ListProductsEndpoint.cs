using DuongNhan.ApiService.Data;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Products;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Products.List;

internal sealed class ListProductsEndpoint(AppDbContext db) : EndpointWithoutRequest<List<ProductDto>>
{
    public override void Configure()
    {
        Get(ApiRoutes.Products.List);
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "List all active skincare products";
            s.Description = "Returns the catalog of active skincare products.";
            s.Responses[200] = "List of products.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var products = await db.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .AsNoTracking()
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Brand,
                p.Category,
                p.Price,
                p.ImageUrl,
                p.Description,
                p.TargetConditions,
                p.UsageInstructions))
            .ToListAsync(ct);

        await Send.OkAsync(products, ct);
    }
}
