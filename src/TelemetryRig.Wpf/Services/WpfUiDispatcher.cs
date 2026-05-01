using System.Windows.Threading;

namespace TelemetryRig.Wpf.Services;

/// <summary>
/// Small wrapper around WPF Dispatcher.
///
/// Background threads must not modify ObservableCollection or UI controls directly.
/// They ask the Dispatcher to run the UI work on the UI thread.
/// </summary>
public sealed class WpfUiDispatcher
{
    private readonly Dispatcher _dispatcher;

    public WpfUiDispatcher(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task InvokeAsync(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return _dispatcher.InvokeAsync(action, DispatcherPriority.Background).Task;
    }
}
