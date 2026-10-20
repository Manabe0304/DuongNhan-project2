namespace DuongNhan.Web.Services;

/// <summary>
/// Single source of truth for page names sent to GA4 (page_view.page_title) and shown in the app topbar.
/// Matches on the URL path only (query string ignored), so "/checkout/success?x=1" still resolves.
/// Keep these names in sync with the <PageTitle> tags on each page (they use the form "{name} — Dưỡng Nhan").
/// </summary>
public static class PageTitles
{
    public const string Fallback = "Dưỡng Nhan AI";

    public static string For(string urlOrPath)
    {
        var path = ToPath(urlOrPath);

        return path switch
        {
            "/" => "Trang chủ",

            // Phân tích da
            "/scan" or "/upload" => "AI Scan Da",
            var p when p.StartsWith("/diagnosis", StringComparison.Ordinal) => "Kết quả phân tích da",
            "/history" => "Lịch sử phân tích",
            "/dashboard" => "Bảng điều khiển",

            // Sản phẩm & gợi ý
            "/recommendations" => "Gợi ý chăm sóc",
            "/products" => "Sản phẩm & Shopee Mall",
            "/doctors" => "Bác sĩ da liễu",

            // Gói & thanh toán
            "/pricing" => "Bảng giá gói quét",
            "/plans" or "/subscription/plans" => "Các gói dịch vụ",
            "/subscription/current" => "Gói hiện tại",
            "/subscription/usage" => "Lượt sử dụng",
            "/billing" or "/subscription/billing" => "Thanh toán",
            "/checkout" => "Thanh toán gói dịch vụ",
            "/checkout/success" => "Thanh toán thành công",
            "/checkout/cancel" or "/checkout-cancel" => "Đã hủy thanh toán",

            // Tài khoản
            "/profile" => "Hồ sơ cá nhân & Cài đặt",
            "/change-password" => "Đổi mật khẩu",
            "/delete-account" => "Xóa tài khoản",
            "/login" or "/auth/login" => "Đăng nhập",
            "/register" or "/auth/register" => "Đăng ký",
            "/forgot-password" or "/auth/forgot-password" => "Quên mật khẩu",
            "/reset-password" => "Đặt lại mật khẩu",
            "/verify-email" => "Xác minh email",
            "/confirm-email" => "Xác nhận email",

            // Giới thiệu & hỗ trợ
            "/welcome" or "/onboarding/welcome" => "Chào mừng",
            "/how-it-works" or "/onboarding/how-it-works" => "Cách hoạt động",
            "/first-upload" or "/onboarding/first-upload" => "Tải ảnh đầu tiên",
            "/contact" => "Liên hệ",

            // Pháp lý
            "/privacy" or "/privacy-policy" => "Chính sách bảo mật",
            "/terms" or "/terms-of-service" => "Điều khoản sử dụng",
            "/cookies" or "/cookie-policy" => "Chính sách cookie",
            "/data-deletion" or "/delete-data" => "Yêu cầu xóa dữ liệu",

            // Hệ thống
            "/404" => "Không tìm thấy trang",
            "/error" => "Lỗi",

            _ => Fallback,
        };
    }

    private static string ToPath(string urlOrPath)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath)) return "/";

        var path = Uri.TryCreate(urlOrPath, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath
            : urlOrPath.Split('?', '#')[0];

        path = path.TrimEnd('/').ToLowerInvariant();
        return path.Length == 0 ? "/" : path;
    }
}
