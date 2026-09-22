namespace DuongNhan.ApiService.Data;

/// <summary>
/// Names for the global query filters registered on the model. Named filters
/// (EF Core 10) can be disabled selectively, e.g.
/// <c>IgnoreQueryFilters([AppQueryFilters.SoftDelete])</c>.
/// </summary>
internal static class AppQueryFilters
{
    /// <summary>
    /// Excludes soft-deleted rows, and rows whose owning aggregate has been
    /// soft-deleted (for example a refresh token belonging to a deleted user).
    /// </summary>
    public const string SoftDelete = "SoftDelete";
}
