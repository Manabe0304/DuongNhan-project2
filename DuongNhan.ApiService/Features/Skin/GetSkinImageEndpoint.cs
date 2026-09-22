using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Skin;

public sealed record GetSkinImageRequest(Guid Id);

internal sealed class GetSkinImageEndpoint(
    AppDbContext db,
    IFileStorageService fileStorage) : Endpoint<GetSkinImageRequest>
{
    public override void Configure()
    {
        Get(ApiRoutes.Skin.Image);
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "Get skin image file";
            s.Description = "Streams the raw image file for the given SkinImage ID.";
            s.Responses[200] = "Image stream.";
            s.Responses[404] = "Image not found.";
        });
    }

    public override async Task HandleAsync(GetSkinImageRequest req, CancellationToken ct)
    {
        var skinImage = await db.SkinImages
            .AsNoTracking()
            .FirstOrDefaultAsync(si => si.Id == req.Id, ct);

        if (skinImage is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var fileResult = await fileStorage.GetFileAsync(skinImage.StorageKey, ct);
        if (fileResult is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.StreamAsync(
            stream: fileResult.Value.Stream,
            contentType: fileResult.Value.ContentType,
            cancellation: ct);
    }
}
