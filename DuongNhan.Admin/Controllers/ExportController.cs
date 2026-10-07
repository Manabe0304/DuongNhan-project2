using System.Text;
using DuongNhan.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DuongNhan.Admin.Controllers;

[Authorize]
public class ExportController : Controller
{
    private readonly ApiService _apiService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(ApiService apiService, ILogger<ExportController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<IActionResult> AffiliateLinks()
    {
        var products = await _apiService.GetProductsAsync();
        if (products == null)
        {
            TempData["Error"] = "Không thể tải dữ liệu sản phẩm.";
            return RedirectToAction("Index", "Products");
        }

        var affiliateProducts = products.Where(p => !string.IsNullOrEmpty(p.AffiliateUrl)).ToList();

        var csv = new StringBuilder();
        csv.AppendLine("STT,Tên sản phẩm,Thương hiệu,Danh mục,Giá,Link Affiliate,Ngày tạo");

        int stt = 1;
        foreach (var p in affiliateProducts)
        {
            csv.AppendLine($"{stt},\"{p.Name}\",\"{p.Brand}\",\"{p.Category}\",{p.Price},\"{p.AffiliateUrl}\",{p.CreatedAt:yyyy-MM-dd}");
            stt++;
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"affiliate-links-{DateTime.Now:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> AllProducts()
    {
        var products = await _apiService.GetProductsAsync();
        if (products == null)
        {
            TempData["Error"] = "Không thể tải dữ liệu sản phẩm.";
            return RedirectToAction("Index", "Products");
        }

        var csv = new StringBuilder();
        csv.AppendLine("STT,Tên sản phẩm,Thương hiệu,Danh mục,Giá,Link Affiliate,Trạng thái,Ngày tạo");

        int stt = 1;
        foreach (var p in products)
        {
            var status = p.IsActive ? "Hoạt động" : "Ẩn";
            var affiliate = p.AffiliateUrl ?? "";
            csv.AppendLine($"{stt},\"{p.Name}\",\"{p.Brand}\",\"{p.Category}\",{p.Price},\"{affiliate}\",{status},{p.CreatedAt:yyyy-MM-dd}");
            stt++;
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"products-{DateTime.Now:yyyyMMdd}.csv");
    }
}
