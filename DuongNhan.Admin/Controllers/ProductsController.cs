using DuongNhan.Admin.Models;
using DuongNhan.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DuongNhan.Admin.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly ApiService _apiService;
    private readonly ProductExcelParser _excelParser;
    private readonly ILogger<ProductsController> _logger;

    private const long MaxImportFileBytes = 5 * 1024 * 1024;

    public ProductsController(ApiService apiService, ProductExcelParser excelParser, ILogger<ProductsController> logger)
    {
        _apiService = apiService;
        _excelParser = excelParser;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _apiService.GetProductsAsync();
        return View(products ?? new List<ProductViewModel>());
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var product = await _apiService.GetProductAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProductEditModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success = await _apiService.UpdateProductAsync(id, model);
        if (success)
        {
            TempData["Success"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, "Cập nhật thất bại. Vui lòng thử lại.");
        return View(model);
    }

    public IActionResult Create()
    {
        return View(new ProductEditModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductEditModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success = await _apiService.CreateProductAsync(model);
        if (success)
        {
            TempData["Success"] = "Tạo sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, "Tạo sản phẩm thất bại. Vui lòng thử lại.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _apiService.DeleteProductAsync(id);
        if (success)
        {
            TempData["Success"] = "Xóa sản phẩm thành công!";
        }
        else
        {
            TempData["Error"] = "Xóa sản phẩm thất bại.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult DownloadImportTemplate()
    {
        return File(
            _excelParser.BuildTemplate(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "products-import-template.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file Excel (.xlsx).";
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Chỉ hỗ trợ file .xlsx.";
            return RedirectToAction(nameof(Index));
        }

        if (file.Length > MaxImportFileBytes)
        {
            TempData["Error"] = "File quá lớn (tối đa 5MB).";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        var parsed = _excelParser.Parse(stream);

        if (parsed.Rows.Count == 0)
        {
            TempData["Error"] = string.Join(" ", parsed.Errors.Take(5));
            return RedirectToAction(nameof(Index));
        }

        var result = await _apiService.ImportProductsAsync(parsed.Rows);
        if (result == null)
        {
            TempData["Error"] = "Nhập sản phẩm thất bại. Kiểm tra quyền Admin và kết nối API.";
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Imported products: {Created} created, {Updated} updated", result.Created, result.Updated);

        TempData["Success"] = $"Nhập thành công: {result.Created} sản phẩm mới, {result.Updated} cập nhật.";
        if (parsed.Errors.Count > 0 || result.Skipped > 0)
        {
            var skipped = parsed.Errors.Count + result.Skipped;
            TempData["Error"] = $"Bỏ qua {skipped} dòng lỗi. " + string.Join(" ", parsed.Errors.Take(5));
        }

        return RedirectToAction(nameof(Index));
    }
}
