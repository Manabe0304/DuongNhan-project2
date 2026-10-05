using DuongNhan.Admin.Models;
using DuongNhan.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DuongNhan.Admin.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly ApiService _apiService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ApiService apiService, ILogger<ProductsController> logger)
    {
        _apiService = apiService;
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
}
