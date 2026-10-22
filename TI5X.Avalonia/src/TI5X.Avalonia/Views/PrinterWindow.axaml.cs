using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using TI5X.Avalonia.ViewModels;

namespace TI5X.Avalonia.Views;

public partial class PrinterWindow : Window
{
    public PrinterWindow()
    {
        InitializeComponent();
    }

    private PrinterViewModel? ViewModel => DataContext as PrinterViewModel;

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
