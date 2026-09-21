using Microsoft.AspNetCore.Components;

namespace DuongNhan.Web.Services;

public sealed class NavigationService(NavigationManager nav)
{
    public string CurrentUrl => nav.Uri;
    public void NavigateTo(string url) => nav.NavigateTo(url);
    public void NavigateToLogin() => nav.NavigateTo("/login");
    public void NavigateToHome() => nav.NavigateTo("/");
}