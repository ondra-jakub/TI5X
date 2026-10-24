using Avalonia.Controls;
using Avalonia.Interactivity;
using TI5X.Avalonia.Services;
using TI5X.ViewModels;

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

    private void OnMenuNewProgram(object? sender, RoutedEventArgs e)
    {
        FileActions.NewProgram(AppServices.RequireMain());
    }

    private async void OnMenuOpenProgram(object? sender, RoutedEventArgs e)
    {
        await FileActions.OpenProgramAsync(AppServices.RequireMain(), AppServices.RequireFileService());
    }

    private async void OnMenuSaveProgram(object? sender, RoutedEventArgs e)
    {
        await FileActions.SaveProgramAsync(AppServices.RequireMain(), AppServices.RequireFileService());
    }

    private async void OnMenuLoadLibrary(object? sender, RoutedEventArgs e)
    {
        await FileActions.LoadLibraryAsync(AppServices.RequireMain(), AppServices.RequireFileService());
    }

    private async void OnMenuShowPrinter(object? sender, RoutedEventArgs e)
    {
        var main = AppServices.RequireMain();
        await PushPageAsync(() => new PrinterPage
        {
            DataContext = new PrinterViewModel(main.Printer, new AvaloniaUiDispatcher())
        });
    }

    private async void OnMenuShowDebugger(object? sender, RoutedEventArgs e)
    {
        var main = AppServices.RequireMain();
        await PushPageAsync(() => new DebuggerPage
        {
            DataContext = new ProgramDebuggerViewModel(main.Engine, new AvaloniaUiDispatcher())
        });
    }

    private async void OnMenuShowHelp(object? sender, RoutedEventArgs e)
    {
        await PushPageAsync(() => new HelpPage());
    }

    private async void OnMenuShowAbout(object? sender, RoutedEventArgs e)
    {
        await PushPageAsync(() => new AboutPage());
    }

    private void OnMenuExit(object? sender, RoutedEventArgs e)
    {
        AppServices.Exit?.Invoke();
    }

    private async Task PushPageAsync(Func<Page> pageFactory)
    {
        if (NavigationHelper.FindNavigationPage(this) is not { } navigation) return;
        await navigation.PushAsync(pageFactory());
    }
}