namespace DuongNhan.Shared.Dtos.Skin;

public sealed record UploadSkinResponse(
    Guid SkinImageId,
    string OriginalFileName,
    long FileSizeBytes,
    string Status,
    string? PreviewUrl = null
);
