using TI5X.Avalonia.Core;

namespace TI5X.Avalonia.Services;

/// <summary>
/// Platform abstraction over user file dialogs (desktop StorageProvider / Android SAF).
/// Opened streams are always buffered into a seekable in-memory stream, because the
/// program formats (zip container) require a seekable stream and SAF input streams are not.
/// </summary>
public interface IAppFileService
{
    /// <summary>Opens an existing file picked by the user.</summary>
    Task<OpenedFile?> OpenAsync(string title, IEnumerable<string> patterns, CancellationToken cancellationToken = default);

    /// <summary>Creates a new output file selected by the user.</summary>
    Task<Stream?> CreateAsync(string fileName, string mimeType, CancellationToken cancellationToken = default);
}

/// <summary>Result of <see cref="IAppFileService.OpenAsync"/>.</summary>
public sealed record OpenedFile(Stream Stream, string? Name);