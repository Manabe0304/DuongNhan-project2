namespace DuongNhan.ApiService.Data;

internal interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
}