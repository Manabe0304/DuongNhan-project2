namespace DuongNhan.Web.Services;

/// <summary>
/// The server runs in UTC (Railway), so ToLocalTime() would still show UTC. Dates shown to users are
/// converted to Vietnam time (GMT+7, no daylight saving) with a fixed offset.
/// </summary>
public static class VietnamTime
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public static DateTimeOffset ToVn(this DateTimeOffset value) => value.ToOffset(Offset);
}
