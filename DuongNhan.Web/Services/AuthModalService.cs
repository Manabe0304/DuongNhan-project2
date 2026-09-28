namespace DuongNhan.Web.Services;

/// <summary>
/// Holds whether the shared login/register modal (see AuthModal.razor) is open and in
/// which mode, so the header, the sidebar, or any page can trigger it without a page
/// navigation — mirroring how AuthModal.tsx is summoned from anywhere in the React app.
/// </summary>
public sealed class AuthModalService
{
    public bool IsOpen { get; private set; }
    public bool IsRegisterMode { get; private set; }

    /// <summary>Raised whenever open state or mode changes; subscribers should re-render.</summary>
    public event Action? Changed;

    public void OpenLogin()
    {
        IsRegisterMode = false;
        IsOpen = true;
        Changed?.Invoke();
    }

    public void OpenRegister()
    {
        IsRegisterMode = true;
        IsOpen = true;
        Changed?.Invoke();
    }

    public void Close()
    {
        IsOpen = false;
        Changed?.Invoke();
    }
}
