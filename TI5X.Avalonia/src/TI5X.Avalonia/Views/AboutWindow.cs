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
        Height = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = "TI5X — TI-59 calculator\n\nDesktop port of the TI5X Android project.\n.NET 10 / Avalonia 12\n\nSee the original project COPYING file for license terms."
                },
                new TextBlock { Text = "Source codes:" },
                new HyperlinkButton
                {
                    Content = "github.com/ondra-jakub/TI5X",
                    NavigateUri = new System.Uri("https://github.com/ondra-jakub/TI5X")
                }
            }
        };
    }
}