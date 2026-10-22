using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Views;

public partial class PrinterWindow : Window
{
    public PrinterWindow()
    {
        InitializeComponent();
    }

    private PrinterViewModel? ViewModel => DataContext as PrinterViewModel;

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        (DataContext as IDisposable)?.Dispose();
    }

    private void OnAdvanceClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.Advance();
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null && Clipboard != null)
        {
            await Clipboard.SetTextAsync(ViewModel.FullTapeText);
        }
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.Clear();
    }
}
