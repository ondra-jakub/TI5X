using TI5X.ViewModels;

namespace TI5X.Maui;

public partial class App : Application
{
    public static MainViewModel ViewModel { get; private set; } = null!;
    public static PrinterViewModel PrinterViewModel { get; private set; } = null!;
    public static ProgramDebuggerViewModel DebuggerViewModel { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;

        var dispatcher = new MauiUiDispatcher();
        ViewModel = new MainViewModel(dispatcher);
        PrinterViewModel = new PrinterViewModel(ViewModel.Printer, dispatcher);
        DebuggerViewModel = new ProgramDebuggerViewModel(ViewModel.Engine, dispatcher);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = new MainPage(ViewModel);
        return new Window(new NavigationPage(page));
    }
}
