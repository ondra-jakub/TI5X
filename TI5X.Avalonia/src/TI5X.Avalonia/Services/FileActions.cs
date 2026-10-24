using TI5X.Avalonia.Core;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Services;

/// <summary>
/// Shared user-facing file actions (New / Open / Save program, Load library) built on
/// <see cref="MainViewModel"/> stream APIs and <see cref="IAppFileService"/>. Used by the
/// desktop menu bar and by the Android command-bar menu so both heads share one flow.
/// </summary>
public static class FileActions
{
    public static void NewProgram(MainViewModel viewModel)
    {
        viewModel.Engine.ClearProgram();
        viewModel.Engine.Reset();
        viewModel.UpdateUiState();
    }

    public static async Task OpenProgramAsync(MainViewModel viewModel, IAppFileService fileService, CancellationToken cancellationToken = default)
    {
        var file = await fileService.OpenAsync("Open TI-5X Program", ["*.ti5p"], cancellationToken);
        if (file == null) return;

        using var buffer = file.Stream;
        viewModel.LoadProgram(buffer, file.Name ?? "selected file");
    }

    public static async Task SaveProgramAsync(MainViewModel viewModel, IAppFileService fileService, CancellationToken cancellationToken = default)
    {
        await using var output = await fileService.CreateAsync("program.ti5p", PersistentState.CalcMimeType, cancellationToken);
        if (output == null) return;

        using var buffer = new MemoryStream();
        viewModel.SaveProgram(buffer, "program.ti5p");
        buffer.Position = 0;
        await buffer.CopyToAsync(output, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }

    public static async Task LoadLibraryAsync(MainViewModel viewModel, IAppFileService fileService, CancellationToken cancellationToken = default)
    {
        var file = await fileService.OpenAsync("Load TI-5X Library Module", ["*.ti5l"], cancellationToken);
        if (file == null) return;

        using var buffer = file.Stream;
        viewModel.LoadCustomLibrary(buffer, file.Name ?? "selected file");
    }
}