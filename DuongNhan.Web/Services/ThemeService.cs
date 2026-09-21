namespace DuongNhan.Web.Services;

public sealed class ThemeService
{
    public string Current { get; private set; } = "light";
    public event Action? OnChange;

    public void SetTheme(string theme)
    {
        Current = theme;
        OnChange?.Invoke();
    }

    public void Toggle()
    {
        SetTheme(Current == "light" ? "dark" : "light");
    }
}