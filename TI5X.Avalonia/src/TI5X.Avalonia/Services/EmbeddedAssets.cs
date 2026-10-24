using System.Reflection;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Services;

/// <summary>
/// Loads assets embedded into the shared TI5X.Avalonia assembly (e.g. the built-in
/// master library <c>ml.ti5l</c>) so both the desktop and Android heads get the same
/// startup data.
/// </summary>
public static class EmbeddedAssets
{
    public static void LoadMasterLibrary(MainViewModel viewModel)
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
}