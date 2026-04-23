using Family.Vault.UI.Components.Shared;

namespace Family.Vault.UI.Services;

/// <summary>
/// Scoped service that broadcasts toast notifications to the <c>ToastContainer</c> component.
/// </summary>
public sealed class ToastService
{
    public event Action<ToastMessage>? OnShow;

    public void ShowSuccess(string message) => Raise(new ToastMessage(message, ToastLevel.Success));
    public void ShowError(string message) => Raise(new ToastMessage(message, ToastLevel.Error));
    public void ShowInfo(string message) => Raise(new ToastMessage(message, ToastLevel.Info));
    public void ShowWarning(string message) => Raise(new ToastMessage(message, ToastLevel.Warning));

    private void Raise(ToastMessage toast) => OnShow?.Invoke(toast);
}

public sealed record ToastMessage(string Text, ToastLevel Level, int DurationMs = 4000);

public enum ToastLevel { Success, Error, Info, Warning }
