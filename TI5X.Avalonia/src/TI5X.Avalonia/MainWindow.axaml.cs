using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TI5X.Avalonia.Core;
using TI5X.Avalonia.Services;
using TI5X.Avalonia.Views;
using TI5X.ViewModels;

namespace TI5X.Avalonia;

public partial class MainWindow : Window
{
    private PrinterWindow? _printerWindow;
    private ProgramDebuggerWindow? _debuggerWindow;

    public MainWindow()
    {
        InitializeComponent();
        // The view model is created and registered by App (AppServices) so that both the
        // desktop window and the single-view (Android) head share the same instance.
        DataContext = AppServices.RequireMain();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (ViewModel == null) return;

        // Map physical PC keyboard keys to TI-59 keys
        int? keyCode = e.Key switch
        {
            Key.D0 or Key.NumPad0 => KeyCodes.KEY_0,
            Key.D1 or Key.NumPad1 => KeyCodes.KEY_1,
            Key.D2 or Key.NumPad2 => KeyCodes.KEY_2,
            Key.D3 or Key.NumPad3 => KeyCodes.KEY_3,
            Key.D4 or Key.NumPad4 => KeyCodes.KEY_4,
            Key.D5 or Key.NumPad5 => KeyCodes.KEY_5,
            Key.D6 or Key.NumPad6 => KeyCodes.KEY_6,
            Key.D7 or Key.NumPad7 => KeyCodes.KEY_7,
            Key.D8 or Key.NumPad8 => KeyCodes.KEY_8,
            Key.D9 or Key.NumPad9 => KeyCodes.KEY_9,
            Key.OemPeriod or Key.Decimal => KeyCodes.KEY_DOT,
            Key.OemPlus or Key.Add => KeyCodes.KEY_ADD,
            Key.OemMinus or Key.Subtract => KeyCodes.KEY_SUB,
            Key.Multiply => KeyCodes.KEY_MUL,
            Key.Divide => KeyCodes.KEY_DIV,
            Key.Enter or Key.Return => KeyCodes.KEY_EQUALS,
            Key.Back => KeyCodes.KEY_CE,
            Key.Escape => KeyCodes.KEY_CLR,
            Key.F1 => KeyCodes.KEY_A,
            Key.F2 => KeyCodes.KEY_B,
            Key.F3 => KeyCodes.KEY_C,
            Key.F4 => KeyCodes.KEY_D,
            Key.F5 => KeyCodes.KEY_E,
            _ => null
        };

        if (keyCode.HasValue)
        {
            ViewModel.PressKey(keyCode.Value);
            e.Handled = true;
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

    private void OnMenuExit(object? sender, RoutedEventArgs e)
    {
        AppServices.Exit?.Invoke();
    }

    private void OnMenuShowPrinter(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        if (_printerWindow == null || !_printerWindow.IsVisible)
        {
            _printerWindow = new PrinterWindow
            {
                DataContext = new PrinterViewModel(ViewModel.Printer, new AvaloniaUiDispatcher())
            };
            _printerWindow.Closed += (s, args) => _printerWindow = null;
            _printerWindow.Show();
        }
        else
        {
            _printerWindow.Activate();
        }
    }

    private void OnMenuShowDebugger(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        if (_debuggerWindow == null || !_debuggerWindow.IsVisible)
        {
            _debuggerWindow = new ProgramDebuggerWindow
            {
                DataContext = new ProgramDebuggerViewModel(ViewModel.Engine, new AvaloniaUiDispatcher())
            };
            _debuggerWindow.Closed += (s, args) => _debuggerWindow = null;
            _debuggerWindow.Show();
        }
        else
        {
            _debuggerWindow.Activate();
        }
    }

    private void OnMenuShowHelp(object? sender, RoutedEventArgs e)
    {
        var helpWin = new HelpWindow();
        helpWin.Show();
    }

    private void OnMenuShowAbout(object? sender, RoutedEventArgs e)
    {
        var aboutWin = new AboutWindow();
        aboutWin.ShowDialog(this);
    }
}