using DuongNhan.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DuongNhan.Admin.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApiService _apiService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApiService apiService, ILogger<DashboardController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _apiService.GetDashboardStatsAsync();
        if (stats == null)
        {
            // Return empty stats if API is not available
            stats = new Models.DashboardStats();
        }
        return View(stats);
    }
}
