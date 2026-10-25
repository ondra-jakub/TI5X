using TI5X.ViewModels;

namespace TI5X.Maui;

internal sealed class MauiUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => MainThread.BeginInvokeOnMainThread(action);
}
