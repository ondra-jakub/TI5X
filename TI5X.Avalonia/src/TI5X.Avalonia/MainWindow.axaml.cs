using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
        var viewModel = new MainViewModel(new AvaloniaUiDispatcher());
        DataContext = viewModel;
        LoadEmbeddedMasterLibrary(viewModel);
    }

    private static void LoadEmbeddedMasterLibrary(MainViewModel viewModel)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith("ml.ti5l", StringComparison.OrdinalIgnoreCase));

        if (resName == null) return;

        using var stream = assembly.GetManifestResourceStream(resName);
        if (stream != null)
        {
            viewModel.LoadMasterLibrary(stream);
        }
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
        ViewModel?.Engine.ClearProgram();
        ViewModel?.Engine.Reset();
        ViewModel?.UpdateUiState();
    }

    private async void OnMenuOpenProgram(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open TI-5X Program",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("TI-5X Program (*.ti5p)") { Patterns = new[] { "*.ti5p" } },
                new FilePickerFileType("All files (*.*)") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            string path = files[0].Path.LocalPath;
            ViewModel.LoadProgramFile(path);
        }
    }

    private async void OnMenuSaveProgram(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save TI-5X Program",
            DefaultExtension = "ti5p",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("TI-5X Program (*.ti5p)") { Patterns = new[] { "*.ti5p" } }
            }
        });

        if (file != null)
        {
            string path = file.Path.LocalPath;
            ViewModel.SaveProgramFile(path);
        }
    }

    private async void OnMenuLoadLibrary(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load TI-5X Library Module",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("TI-5X Library (*.ti5l)") { Patterns = new[] { "*.ti5l" } },
                new FilePickerFileType("All files (*.*)") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            string path = files[0].Path.LocalPath;
            ViewModel.LoadCustomLibrary(path);
        }
    }

    private void OnMenuExit(object? sender, RoutedEventArgs e)
    {
        Close();
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
