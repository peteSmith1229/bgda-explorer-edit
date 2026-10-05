using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace WorldExplorer.Infrastructure;

public enum ToastKind
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>A transient, non-blocking notification shown in the corner of the main window.</summary>
public sealed class Toast
{
    public Toast(string title, string? message, ToastKind kind, string? actionText, ICommand? action)
    {
        Title = title;
        Message = message;
        Kind = kind;
        ActionText = actionText;
        Action = action;
    }

    public string Title { get; }
    public string? Message { get; }
    public ToastKind Kind { get; }
    public string? ActionText { get; }
    public ICommand? Action { get; }

    public string IconKey => Kind switch
    {
        ToastKind.Success => "Icon.Success",
        ToastKind.Warning => "Icon.Warning",
        ToastKind.Error => "Icon.Error",
        _ => "Icon.Info"
    };

    public string BrushKey => Kind switch
    {
        ToastKind.Success => "Brush.Success",
        ToastKind.Warning => "Brush.Warning",
        ToastKind.Error => "Brush.Danger",
        _ => "Brush.Info"
    };

    internal DispatcherTimer? Timer { get; set; }
}

/// <summary>
/// Queue of toasts bound by MainWindow. Replaces the informational message
/// boxes ("Entry queued", "Saved to …") that used to interrupt the user;
/// genuine questions (confirm delete, discard changes) still use dialogs.
/// </summary>
public sealed class NotificationService
{
    private const int MaxVisible = 4;

    public ObservableCollection<Toast> Items { get; } = new();

    public ICommand DismissCommand { get; }

    public NotificationService()
    {
        DismissCommand = new RelayCommand(p => { if (p is Toast t) Dismiss(t); });
    }

    public void Info(string title, string? message = null) => Show(title, message, ToastKind.Info);
    public void Success(string title, string? message = null) => Show(title, message, ToastKind.Success);
    public void Warning(string title, string? message = null) => Show(title, message, ToastKind.Warning);
    public void Error(string title, string? message = null) => Show(title, message, ToastKind.Error);

    public void Show(string title, string? message, ToastKind kind,
                     string? actionText = null, Action? action = null)
    {
        Toast? toast = null;
        ICommand? command = action == null
            ? null
            : new RelayCommand(() =>
            {
                action();
                if (toast != null) Dismiss(toast);
            });
        toast = new Toast(title, message, kind, actionText, command);

        // Errors and toasts with an action stay up a little longer.
        var seconds = kind is ToastKind.Error or ToastKind.Warning || action != null ? 8 : 4;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        timer.Tick += (_, _) => Dismiss(toast);
        toast.Timer = timer;

        Items.Add(toast);
        while (Items.Count > MaxVisible)
        {
            Dismiss(Items[0]);
        }

        timer.Start();
    }

    public void Dismiss(Toast toast)
    {
        toast.Timer?.Stop();
        Items.Remove(toast);
    }
}
