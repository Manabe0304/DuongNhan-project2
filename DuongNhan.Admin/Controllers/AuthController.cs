using DuongNhan.Admin.Models;
using DuongNhan.Admin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DuongNhan.Admin.Controllers;

public class AuthController : Controller
{
    private readonly AdminAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AdminAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, token, error) = await _authService.LoginAsync(model.Email, model.Password);

        if (success && token != null)
        {
            // Decode JWT to get role
            var role = "User";
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(token);
                role = jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value ?? "User";
            }
            catch
            {
                // Fallback to default role
            }

            await _authService.SignInHttpContextAsync(HttpContext, token, model.Email, role, model.RememberMe);

            _logger.LogInformation("Admin {Email} logged in", model.Email);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError(string.Empty, error ?? "Đăng nhập thất bại. Kiểm tra lại email và mật khẩu.");
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutHttpContextAsync(HttpContext);
        _logger.LogInformation("Admin logged out");
        return RedirectToAction("Login");
    }
}
