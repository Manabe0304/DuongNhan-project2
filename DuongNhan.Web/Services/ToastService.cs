namespace DuongNhan.Web.Services;

public sealed class ToastService
{
    private readonly List<ToastMessage> _toasts = [];
    public IReadOnlyList<ToastMessage> Toasts => _toasts;
    public event Action? OnChange;

    public void ShowSuccess(string message) => Add(message, "text-bg-success");
    public void ShowError(string message) => Add(message, "text-bg-danger");
    public void ShowInfo(string message) => Add(message, "text-bg-info");

    public void Remove(Guid id)
    {
        _toasts.RemoveAll(t => t.Id == id);
        OnChange?.Invoke();
    }

    private void Add(string message, string cssClass)
    {
        var toast = new ToastMessage(Guid.NewGuid(), message, cssClass);
        _toasts.Add(toast);
        OnChange?.Invoke();

        _ = Task.Delay(TimeSpan.FromSeconds(4)).ContinueWith(_ =>
        {
            Remove(toast.Id);
        });
    }
}

public sealed record ToastMessage(Guid Id, string Message, string CssClass);