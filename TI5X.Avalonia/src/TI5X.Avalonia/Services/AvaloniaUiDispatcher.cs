using Avalonia.Threading;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Services;

internal sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
