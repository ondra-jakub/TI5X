using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Views;

public partial class PrinterView : UserControl
{
    public PrinterView()
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
        if (ViewModel != null && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(ViewModel.FullTapeText);
        }
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.Clear();
    }
}