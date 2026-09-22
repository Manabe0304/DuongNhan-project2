using System.Security.Cryptography;
using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Skin;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Skin.Upload;

internal sealed class UploadSkinEndpoint(
    AppDbContext db,
    IFileStorageService fileStorage,
    ILogger<UploadSkinEndpoint> logger) : EndpointWithoutRequest<UploadSkinResponse>
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public override void Configure()
    {
        Post(ApiRoutes.Skin.Upload);
        AllowAnonymous();
        AllowFileUploads();

        Summary(s =>
        {
            s.Summary = "Upload a skin image for analysis";
            s.Description = "Accepts multipart/form-data with a single image file, saves it to storage and creates a SkinImage record.";
            s.Responses[200] = "Image uploaded successfully.";
            s.Responses[400] = "Invalid image file.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        IFormFile? file = null;
        for (var i = 0; i < Files.Count; i++)
        {
            if (Files[i].Name == "file")
            {
                file = Files[i];
                break;
            }
        }
        file ??= Files.Count > 0 ? Files[0] : null;

        if (file is null || file.Length == 0)
        {
            AddError("file", "No image file was provided.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellation: ct);
            return;
        }

        if (file.Length > MaxFileSize)
        {
            AddError("file", "Image file exceeds maximum allowed size of 10MB.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellation: ct);
            return;
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            AddError("file", "Only JPG, PNG, and WebP images are supported.");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellation: ct);
            return;
        }

        // Determine user identity or fallback to guest user
        var userId = AppDbSeeder.GuestUserId;
        var claimId = User.FindFirst(AppClaimTypes.UserId)?.Value;
        if (Guid.TryParse(claimId, out var parsedId))
        {
            userId = parsedId;
        }

        // Ensure user exists in database to prevent FK constraint failure
        var userExists = await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
        {
            userId = AppDbSeeder.GuestUserId;
            var guestExists = await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == AppDbSeeder.GuestUserId, ct);
            if (!guestExists)
            {
                db.Users.Add(new User
                {
                    Id = AppDbSeeder.GuestUserId,
                    Email = "guest@duongnhan.ai",
                    PasswordHash = "AQAAAAIAAYagAAAAEPlaceholderHashForGuestDemoUser1234567890=",
                    DisplayName = "Khách dùng thử",
                    Status = "active"
                });
                await db.SaveChangesAsync(ct);
            }
        }

        using var memoryStream = new MemoryStream();
        await using (var readStream = file.OpenReadStream())
        {
            await readStream.CopyToAsync(memoryStream, ct);
        }

        memoryStream.Position = 0;
        var checksumBytes = SHA256.HashData(memoryStream.ToArray());
        var checksum = Convert.ToHexString(checksumBytes).ToLowerInvariant();

        memoryStream.Position = 0;
        var storageKey = await fileStorage.SaveFileAsync(memoryStream, file.FileName, file.ContentType, ct);

        var skinImage = new SkinImage
        {
            UserId = userId,
            StorageKey = storageKey,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            ChecksumSha256 = checksum,
            Status = "uploaded"
        };

        db.SkinImages.Add(skinImage);
        await db.SaveChangesAsync(ct);

        UploadSkinEndpointLogs.ImageSaved(logger, skinImage.Id, storageKey);

        await Send.OkAsync(new UploadSkinResponse(
            SkinImageId: skinImage.Id,
            OriginalFileName: skinImage.OriginalFileName,
            FileSizeBytes: skinImage.FileSizeBytes,
            Status: skinImage.Status,
            PreviewUrl: $"/api/skin/{skinImage.Id}"), ct);
    }
}

internal static partial class UploadSkinEndpointLogs
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "SkinImage {ImageId} saved with key {StorageKey}")]
    public static partial void ImageSaved(ILogger logger, Guid imageId, string storageKey);
}
