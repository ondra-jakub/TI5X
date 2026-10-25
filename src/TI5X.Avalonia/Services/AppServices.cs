using TI5X.Avalonia.Views;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Services;

/// <summary>
/// Static service registry so heads without a DI container (Android) can share the
/// view model and file service with the desktop head. The Android activity must set
/// <see cref="FileService"/> and <see cref="Exit"/> before <c>base.OnCreate</c>,
/// because the Avalonia application class is initialized there.
/// </summary>
public static class AppServices
{
    public static MainViewModel? Main { get; set; }

    public static IAppFileService? FileService { get; set; }

    /// <summary>Platform-specific application exit (desktop: close main window, Android: FinishAffinity).</summary>
    public static Action? Exit { get; set; }

    /// <summary>
    /// Optional diagnostic logger routed to the platform log (Android: logcat via the
    /// head; desktop: Debug.WriteLine). Used for layout diagnostics on foldables.
    /// </summary>
    public static Action<string>? DiagnosticLog { get; set; }

    /// <summary>Logs through <see cref="DiagnosticLog"/> when a head provides it.</summary>
    public static void Log(string message) => DiagnosticLog?.Invoke(message);

    /// <summary>
    /// Optional tactile feedback invoked when an on-screen key is pressed
    /// (Android head: short haptic tick via the Vibrator service; null on desktop).
    /// </summary>
    public static Action? KeyPressFeedback { get; set; }

    public static MainViewModel RequireMain()
        => Main ?? throw new InvalidOperationException("AppServices.Main was not initialized by the platform head.");

    public static IAppFileService RequireFileService()
        => FileService ?? throw new InvalidOperationException("AppServices.FileService was not initialized by the platform head.");
}