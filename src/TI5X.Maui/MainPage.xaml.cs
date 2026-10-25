using TI5X.Avalonia.Core;
using TI5X.Maui.Controls;
using TI5X.Maui.Pages;
using TI5X.ViewModels;

namespace TI5X.Maui;

/// <summary>
/// One of the standard TI Solid State Software modules shipped in Resources/Raw
/// (mirrors EmbeddedAssets of the Avalonia head). Module 1 (Master Library) is loaded
/// at startup; the others are available from the "Built-in Libraries" menu entry.
/// </summary>
public sealed record BuiltInLibrary(string FileName, string DisplayName);

public partial class MainPage : ContentPage
{
    private const string MenuNewProgram = "New Program";
    private const string MenuOpenProgram = "Open Program (.ti5p)";
    private const string MenuSaveProgram = "Save Program (.ti5p)";
    private const string MenuLoadLibrary = "Load Library Module (.ti5l)";
    private const string MenuBuiltInLibraries = "Built-in Libraries";
    private const string MenuPrinter = "Virtual Printer";
    private const string MenuDebugger = "Program Debugger";
    private const string MenuHelp = "Help";
    private const string MenuAbout = "About";
    private const string MenuExit = "Exit";

    /// <summary>The standard Solid State Software modules built into the app.</summary>
    private static readonly BuiltInLibrary[] LibraryCatalog =
    [
        new("ml.ti5l", "1 Master Library"),
        new("mod02.ti5l", "2 Applied Statistics"),
        new("mod03.ti5l", "3 Real Estate/Investment"),
        new("mod04.ti5l", "4 Surveying"),
        new("mod05.ti5l", "5 Marine Navigation"),
        new("mod06.ti5l", "6 Aviation"),
        new("mod07.ti5l", "7 Leisure Library"),
        new("mod08.ti5l", "8 Securities Analysis"),
        new("mod09.ti5l", "9 Business Decisions"),
        new("mod10.ti5l", "10 Math/Utilities"),
        new("mod11.ti5l", "11 Electrical Engineering"),
        new("mod12.ti5l", "12 Agriculture"),
        new("mod13.ti5l", "13 RPN Simulator"),
    ];

    private readonly MainViewModel _viewModel;
    private bool _masterLibraryLoaded;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_masterLibraryLoaded) return;
        _masterLibraryLoaded = true;
        await LoadMasterLibraryAsync();
    }

    private async Task LoadMasterLibraryAsync()
    {
        try
        {
            using var raw = await FileSystem.OpenAppPackageFileAsync("ml.ti5l");
            using var buffer = new MemoryStream();
            await raw.CopyToAsync(buffer);
            buffer.Position = 0;
            _viewModel.LoadMasterLibrary(buffer);
        }
        catch (Exception ex)
        {
            _viewModel.StatusInfo = $"Error loading Master Library: {ex.Message}";
        }
    }

    private void OnKeyClicked(object? sender, EventArgs e)
    {
        if (sender is KeyButtonView key)
        {
            _viewModel.PressKey(key.KeyCode);
        }
    }

    private async void OnMenuClicked(object? sender, EventArgs e)
    {
        string? action = await DisplayActionSheetAsync("TI5X", "Cancel", null,
            MenuNewProgram, MenuOpenProgram, MenuSaveProgram, MenuLoadLibrary,
            MenuBuiltInLibraries, MenuPrinter, MenuDebugger, MenuHelp, MenuAbout, MenuExit);

        switch (action)
        {
            case MenuNewProgram:
                NewProgram();
                break;
            case MenuOpenProgram:
                await OpenProgramAsync();
                break;
            case MenuSaveProgram:
                await SaveProgramAsync();
                break;
            case MenuLoadLibrary:
                await LoadLibraryAsync();
                break;
            case MenuBuiltInLibraries:
                await SelectBuiltInLibraryAsync();
                break;
            case MenuPrinter:
                await Navigation.PushAsync(new PrinterPage(App.PrinterViewModel));
                break;
            case MenuDebugger:
                await Navigation.PushAsync(new DebuggerPage(App.DebuggerViewModel));
                break;
            case MenuHelp:
                await Navigation.PushAsync(new HelpPage());
                break;
            case MenuAbout:
                await DisplayAlertAsync("About TI5X",
                    "TI5X — TI-59 calculator\n\nAndroid port of the TI5X project.\n.NET 10 / MAUI\n\nSee the original project COPYING file for license terms.",
                    "OK");
                break;
            case MenuExit:
                Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.FinishAffinity();
                break;
        }
    }

    private async Task SelectBuiltInLibraryAsync()
    {
        string? cancel = "Cancel";
        string[] choices = [.. LibraryCatalog.Select(l => l.DisplayName)];
        string? picked = await DisplayActionSheetAsync("Built-in Libraries", cancel, null, choices);
        if (picked == null || picked == cancel || choices.Contains(picked) == false)
        {
            return;
        }

        var library = LibraryCatalog.First(l => l.DisplayName == picked);
        using Stream? raw = await FileSystem.OpenAppPackageFileAsync(library.FileName);
        if (raw == null)
        {
            _viewModel.StatusInfo = $"Module {library.DisplayName} is not built into the app";
            return;
        }

        using var buffer = new MemoryStream();
        await raw.CopyToAsync(buffer);
        buffer.Position = 0;
        _viewModel.LoadBuiltInLibrary(buffer, library.DisplayName);
    }

    private void NewProgram()
    {
        _viewModel.Engine.ClearProgram();
        _viewModel.Engine.Reset();
        _viewModel.UpdateUiState();
    }

    private async Task OpenProgramAsync()
    {
        try
        {
            var file = await PickFileAsync("Open TI-5X Program");
            if (file == null) return;

            using var buffer = await ReadFileAsync(file);
            _viewModel.LoadProgram(buffer, file.FileName);
        }
        catch (Exception ex)
        {
            _viewModel.StatusInfo = $"Error: {ex.Message}";
        }
    }

    private async Task SaveProgramAsync()
    {
        try
        {
            await using var output = await AndroidDocumentSaver.CreateAsync("program.ti5p", PersistentState.CalcMimeType);
            if (output == null) return;

            using var buffer = new MemoryStream();
            _viewModel.SaveProgram(buffer, "program.ti5p");
            buffer.Position = 0;
            await buffer.CopyToAsync(output);
            await output.FlushAsync();
        }
        catch (Exception ex)
        {
            _viewModel.StatusInfo = $"Error: {ex.Message}";
        }
    }

    private async Task LoadLibraryAsync()
    {
        try
        {
            var file = await PickFileAsync("Load TI-5X Library Module");
            if (file == null) return;

            using var buffer = await ReadFileAsync(file);
            _viewModel.LoadCustomLibrary(buffer, file.FileName);
        }
        catch (Exception ex)
        {
            _viewModel.StatusInfo = $"Error: {ex.Message}";
        }
    }

    private static async Task<Stream> ReadFileAsync(FileResult file)
    {
        using var source = await file.OpenReadAsync();
        var buffer = new MemoryStream();
        await source.CopyToAsync(buffer);
        buffer.Position = 0;
        return buffer;
    }

    private static async Task<FileResult?> PickFileAsync(string title)
    {
        var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.Android] = new[] { "*/*" }
        });

        return await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = title,
            FileTypes = fileTypes
        });
    }
}
