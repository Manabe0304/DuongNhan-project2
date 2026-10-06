namespace DuongNhan.Admin.Models;

public class ImportProductRow
{
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? TargetConditions { get; set; }
    public string? UsageInstructions { get; set; }
    public string AffiliateUrl { get; set; } = string.Empty;
}

public class ImportProductsResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
}

public class ExcelParseResult
{
    public List<ImportProductRow> Rows { get; } = new();
    public List<string> Errors { get; } = new();
}
