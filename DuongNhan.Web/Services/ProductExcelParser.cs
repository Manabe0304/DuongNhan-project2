using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using DuongNhan.Shared.Dtos.Products;

namespace DuongNhan.Web.Services;

/// <summary>
/// Reads products + affiliate links from an .xlsx file. The first row must be a header row.
/// Header names are matched case/accent-insensitively; English and Vietnamese names are accepted.
/// Required columns: Name, Affiliate link. Optional: Brand, Category, Price, Image URL, Description,
/// Target conditions, Usage instructions, Skin type (e.g. "Oily,Combination" or "All"), Step (1-6 or a
/// name such as "Cleanse", "Serum", "Moisturize").
/// </summary>
public sealed class ProductExcelParser
{
    private static readonly Dictionary<string, string[]> Aliases = new()
    {
        ["name"] = ["name", "productname", "product", "tensanpham", "ten"],
        ["brand"] = ["brand", "thuonghieu"],
        ["category"] = ["category", "danhmuc", "loai"],
        ["price"] = ["price", "gia", "dongia"],
        ["image"] = ["imageurl", "image", "anh", "hinhanh", "linkanh"],
        ["description"] = ["description", "mota"],
        ["conditions"] = ["targetconditions", "conditions", "tinhtrangda"],
        ["usage"] = ["usageinstructions", "usage", "huongdansudung", "cachdung"],
        ["skintype"] = ["skintype", "skin", "loaida", "dacuaban"],
        ["step"] = ["step", "buoc", "routinestep", "buocskincare"],
        ["affiliate"] = ["affiliateurl", "affiliatelink", "affiliate", "linkaffiliate", "link", "url"],
    };

    public const int MaxRows = 2000;

    public ExcelParseResult Parse(Stream stream)
    {
        var result = new ExcelParseResult();

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception)
        {
            result.Errors.Add("Không đọc được file. Vui lòng dùng file Excel .xlsx hợp lệ.");
            return result;
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault();
            var used = sheet?.RangeUsed();
            if (sheet is null || used is null)
            {
                result.Errors.Add("File Excel không có dữ liệu.");
                return result;
            }

            var firstRow = used.FirstRow().RowNumber();
            var lastRow = used.LastRow().RowNumber();
            var firstCol = used.FirstColumn().ColumnNumber();
            var lastCol = used.LastColumn().ColumnNumber();

            var columns = new Dictionary<string, int>();
            for (var c = firstCol; c <= lastCol; c++)
            {
                var header = Normalize(sheet.Cell(firstRow, c).GetString());
                if (header.Length == 0) continue;
                foreach (var (key, names) in Aliases)
                {
                    if (!columns.ContainsKey(key) && names.Contains(header))
                    {
                        columns[key] = c;
                        break;
                    }
                }
            }

            if (!columns.ContainsKey("name") || !columns.ContainsKey("affiliate"))
            {
                result.Errors.Add("Thiếu cột bắt buộc: 'Name' (Tên sản phẩm) và 'Affiliate Link'. Hãy tải file mẫu.");
                return result;
            }

            string Cell(int row, string key) =>
                columns.TryGetValue(key, out var col) ? sheet.Cell(row, col).GetString().Trim() : string.Empty;

            for (var r = firstRow + 1; r <= lastRow; r++)
            {
                var name = Cell(r, "name");
                var url = Cell(r, "affiliate");

                if (name.Length == 0 && url.Length == 0) continue; // blank line

                if (result.Rows.Count >= MaxRows)
                {
                    result.Errors.Add($"Chỉ hỗ trợ tối đa {MaxRows} sản phẩm mỗi lần nhập.");
                    break;
                }

                if (name.Length == 0)
                {
                    result.Errors.Add($"Dòng {r}: thiếu tên sản phẩm.");
                    continue;
                }

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    result.Errors.Add($"Dòng {r}: link affiliate không hợp lệ.");
                    continue;
                }

                decimal price = 0;
                if (columns.TryGetValue("price", out var priceCol))
                {
                    var priceCell = sheet.Cell(r, priceCol);
                    if (priceCell.DataType == XLDataType.Number)
                        price = (decimal)priceCell.GetDouble();
                    else if (!TryParsePrice(priceCell.GetString(), out price))
                    {
                        result.Errors.Add($"Dòng {r}: giá không hợp lệ.");
                        continue;
                    }
                }

                result.Rows.Add(new ImportProductRow(
                    Name: name,
                    Brand: Cell(r, "brand"),
                    Category: Cell(r, "category"),
                    Price: price,
                    ImageUrl: NullIfEmpty(Cell(r, "image")),
                    Description: NullIfEmpty(Cell(r, "description")),
                    TargetConditions: NullIfEmpty(Cell(r, "conditions")),
                    UsageInstructions: NullIfEmpty(Cell(r, "usage")),
                    AffiliateUrl: url,
                    SkinType: NullIfEmpty(Cell(r, "skintype")),
                    Step: NullIfEmpty(Cell(r, "step"))));
            }

            if (result.Rows.Count == 0 && result.Errors.Count == 0)
                result.Errors.Add("Không tìm thấy dòng sản phẩm nào trong file.");
        }

        return result;
    }

    public byte[] BuildTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Products");
        string[] headers =
        [
            "Name", "Brand", "Category", "Price", "Affiliate Link",
            "Image URL", "Description", "Target Conditions", "Usage Instructions", "Skin Type", "Step"

        ];
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
        }

        sheet.Cell(2, 1).Value = "CeraVe Foaming Facial Cleanser";
        sheet.Cell(2, 2).Value = "CeraVe";
        sheet.Cell(2, 3).Value = "Cleanser";
        sheet.Cell(2, 4).Value = 340000;
        sheet.Cell(2, 5).Value = "https://shopee.vn/product/123456789?affiliate_id=duongnhan";
        sheet.Cell(2, 6).Value = "https://example.com/image.jpg";
        sheet.Cell(2, 7).Value = "Sữa rửa mặt dịu nhẹ";
        sheet.Cell(2, 8).Value = "Acne,EnlargedPores";
        sheet.Cell(2, 9).Value = "Dùng sáng và tối";
        sheet.Cell(2, 10).Value = "Oily,Combination";
        sheet.Cell(2, 11).Value = "Cleanse";

        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static bool TryParsePrice(string raw, out decimal price)
    {
        var digits = new string(raw.Where(ch => char.IsDigit(ch) || ch == ',' || ch == '.').ToArray());
        if (digits.Length == 0) { price = 0; return raw.Trim().Length == 0; }

        // Vietnamese style "340.000" / "340,000" means thousands separators, not decimals.
        var onlyDigits = digits.Replace(".", "").Replace(",", "");
        return decimal.TryParse(onlyDigits, NumberStyles.None, CultureInfo.InvariantCulture, out price);
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        }
        return sb.ToString();
    }
}

public sealed class ExcelParseResult
{
    public List<ImportProductRow> Rows { get; } = [];
    public List<string> Errors { get; } = [];
}
