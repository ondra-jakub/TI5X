using Avalonia.Controls;

namespace TI5X.Avalonia.Views;

public partial class ProgramDebuggerWindow : Window
{
    public ProgramDebuggerWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        (DataContext as IDisposable)?.Dispose();
    }
}