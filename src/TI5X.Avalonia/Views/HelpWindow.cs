using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using TI5X.Avalonia.Services;

namespace TI5X.Avalonia.Views;

public class HelpWindow : Window
{
    public HelpWindow()
    {
        Title = "Nápověda TI5X";
        Width = 640;
        Height = 520;
        Content = new ScrollViewer
        {
            Content = BuildContent()
        };
    }

    private static StackPanel BuildContent()
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Nápověda – emulátor kalkulačky TI-59",
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };

        foreach (var section in HelpSections.Sections)
        {
            panel.Children.Add(new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = section.Heading,
                        FontSize = 14,
                        FontWeight = FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = section.Text,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            });
        }

        return panel;
    }
}