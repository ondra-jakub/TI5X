using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace TI5X.Avalonia.Services;

/// <summary>
/// Desktop implementation of <see cref="IAppFileService"/> based on
/// <see cref="TopLevel.StorageProvider"/>. Opened files are buffered into a seekable
/// <see cref="MemoryStream"/> and saved streams are copied into the picked file —
/// the same flow as on Android (SAF), so both heads behave identically.
/// </summary>
public sealed class StorageFileService(Func<TopLevel?> topLevelProvider) : IAppFileService
{
    public async Task<OpenedFile?> OpenAsync(string title, IEnumerable<string> patterns, CancellationToken cancellationToken = default)
    {
        var storage = GetStorageProvider();
        if (storage == null) return null;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = BuildFilter(patterns)
        });

        if (files.Count == 0) return null;

        var file = files[0];
        await using var input = await file.OpenReadAsync();
        var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return new OpenedFile(buffer, file.Name);
    }

    public async Task<Stream?> CreateAsync(string fileName, string mimeType, CancellationToken cancellationToken = default)
    {
        var storage = GetStorageProvider();
        if (storage == null) return null;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save TI-5X Program",
            SuggestedFileName = fileName,
            DefaultExtension = Path.GetExtension(fileName).TrimStart('.'),
            FileTypeChoices =
            [
                new FilePickerFileType("TI-5X Program (*.ti5p)") { Patterns = ["*.ti5p"] }
            ]
        });

        if (file == null) return null;

        return await file.OpenWriteAsync();
    }

    private IStorageProvider? GetStorageProvider() => topLevelProvider()?.StorageProvider;

    private static IReadOnlyList<FilePickerFileType> BuildFilter(IEnumerable<string> patterns)
    {
        var patternList = patterns.ToList();
        var label = $"TI-5X files ({string.Join(", ", patternList)})";
        return
        [
            new FilePickerFileType(label) { Patterns = patternList },
            new FilePickerFileType("All files (*.*)") { Patterns = ["*.*"] }
        ];
    }
}