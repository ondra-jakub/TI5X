using TI5X.Avalonia.Core;
using TI5X.Maui.Controls;
using TI5X.Maui.Pages;
using TI5X.ViewModels;

namespace TI5X.Maui;

public partial class MainPage : ContentPage
{
    private const string MenuNewProgram = "New Program";
    private const string MenuOpenProgram = "Open Program (.ti5p)";
    private const string MenuSaveProgram = "Save Program (.ti5p)";
    private const string MenuLoadLibrary = "Load Library Module (.ti5l)";
    private const string MenuPrinter = "Virtual Printer";
    private const string MenuDebugger = "Program Debugger";
    private const string MenuHelp = "Help";
    private const string MenuAbout = "About";
    private const string MenuExit = "Exit";

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
            MenuPrinter, MenuDebugger, MenuHelp, MenuAbout, MenuExit);

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
