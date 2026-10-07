using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TI5X.Avalonia.Views;

public class AboutWindow : Window
{
    public AboutWindow()
    {
        Title = "About TI5X";
        Width = 420;
        Height = 240;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = new TextBlock
        {
            Margin = new Thickness(24),
            TextWrapping = TextWrapping.Wrap,
            Text = "TI5X — TI-59 calculator\n\nDesktop port of the TI5X Android project.\n.NET 10 / Avalonia 12\n\nSee the original project COPYING file for license terms."
        };
    }
}