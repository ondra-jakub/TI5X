using Avalonia.Controls;
using Avalonia.Interactivity;
using TI5X.Avalonia.Services;
using TI5X.Avalonia.Views.Pages;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Views;

/// <summary>
/// Shared calculator body. In the Android single-view head (hosted in a NavigationPage)
/// a hamburger button above the display — left of the "TEXAS INSTRUMENTS" label — opens
/// the app menu; on desktop the window menu bar is used instead and the button stays
/// hidden.
/// </summary>
public partial class CalculatorView : UserControl
{
    public CalculatorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        bool isSingleView = NavigationHelper.FindNavigationPage(this) != null;
        MenuButton.IsVisible = isSingleView;
        if (isSingleView && BuiltInLibrariesItem.ItemCount == 0)
        {
            PopulateBuiltInLibraries();
        }
    }

    private void PopulateBuiltInLibraries()
    {
        foreach (var library in EmbeddedAssets.BuiltInLibraries)
        {
            var item = new MenuItem
            {
                Header = library.DisplayName,
                Tag = library
            };
            item.Click += OnLoadBuiltInLibrary;
            BuiltInLibrariesItem.Items.Add(item);
        }
    }

    private void OnLoadBuiltInLibrary(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: BuiltInLibrary library }
            && EmbeddedAssets.OpenBuiltInLibrary(library) is { } stream)
        {
            using (stream)
            {
                AppServices.RequireMain().LoadBuiltInLibrary(stream, library.DisplayName);
            }
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
        await PushPageAsync(new PrinterPage
        {
            DataContext = new PrinterViewModel(main.Printer, new AvaloniaUiDispatcher())
        });
    }

    private async void OnMenuShowDebugger(object? sender, RoutedEventArgs e)
    {
        var main = AppServices.RequireMain();
        await PushPageAsync(new DebuggerPage
        {
            DataContext = new ProgramDebuggerViewModel(main.Engine, new AvaloniaUiDispatcher())
        });
    }

    private async void OnMenuShowHelp(object? sender, RoutedEventArgs e)
    {
        await PushPageAsync(new HelpPage());
    }

    private async void OnMenuShowAbout(object? sender, RoutedEventArgs e)
    {
        await PushPageAsync(new AboutPage());
    }

    private void OnMenuExit(object? sender, RoutedEventArgs e)
    {
        AppServices.Exit?.Invoke();
    }

    private async Task PushPageAsync(Page page)
    {
        if (NavigationHelper.FindNavigationPage(this) is not { } navigation) return;
        await navigation.PushAsync(page);
    }
}