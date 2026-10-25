using Avalonia.Controls;
using Avalonia.Interactivity;
using TI5X.Avalonia.Services;

namespace TI5X.Avalonia.Views.Pages;

public partial class CalculatorPage : ContentPage
{
    private bool _popDisposalAttached;

    public CalculatorPage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, RoutedEventArgs e)
    {
        if (_popDisposalAttached) return;
        if (NavigationHelper.FindNavigationPage(this) is { } navigation)
        {
            // Printer/debugger view models are disposable; dispose them when their
            // page is popped from the navigation stack (parity with desktop windows).
            navigation.Popped += (_, args) => (args.Page.DataContext as IDisposable)?.Dispose();
            _popDisposalAttached = true;
        }
    }
}