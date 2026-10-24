using Avalonia.Controls;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Views;

public partial class PrinterWindow : Window
{
    public PrinterWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        (DataContext as IDisposable)?.Dispose();
    }
}