using System.Reflection;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Services;

/// <summary>
/// One of the standard TI Solid State Software modules transcribed from the original
/// TMC0540 CROM ROM dumps (see http://www.datamath.org/Chips/TMC0540.htm) and built
/// into a .ti5l by util/mklibfromdump. Module 1 (Master Library) is loaded at startup;
/// the others are available from the Library menu.
/// </summary>
public sealed record BuiltInLibrary(int ModuleNr, string ResourceName, string DisplayName);

/// <summary>
/// Loads assets embedded into the shared TI5X.Avalonia assembly (the built-in library
/// modules) so both the desktop and Android heads get the same startup data.
/// </summary>
public static class EmbeddedAssets
{
    /// <summary>The standard Solid State Software modules built into the app.</summary>
    public static readonly IReadOnlyList<BuiltInLibrary> BuiltInLibraries =
    [
        new(1, "ml.ti5l", "1 Master Library"),
        new(2, "mod02.ti5l", "2 Applied Statistics"),
        new(3, "mod03.ti5l", "3 Real Estate/Investment"),
        new(4, "mod04.ti5l", "4 Surveying"),
        new(5, "mod05.ti5l", "5 Marine Navigation"),
        new(6, "mod06.ti5l", "6 Aviation"),
        new(7, "mod07.ti5l", "7 Leisure Library"),
        new(8, "mod08.ti5l", "8 Securities Analysis"),
        new(9, "mod09.ti5l", "9 Business Decisions"),
        new(10, "mod10.ti5l", "10 Math/Utilities"),
        new(11, "mod11.ti5l", "11 Electrical Engineering"),
        new(12, "mod12.ti5l", "12 Agriculture"),
        new(13, "mod13.ti5l", "13 RPN Simulator"),
    ];

    public static void LoadMasterLibrary(MainViewModel viewModel)
    {
        var stream = OpenResource("ml.ti5l");
        if (stream != null)
        {
            using (stream)
            {
                viewModel.LoadMasterLibrary(stream);
            }
        }
    }

    /// <summary>Opens the embedded .ti5l of a built-in module for on-demand loading.</summary>
    public static Stream? OpenBuiltInLibrary(BuiltInLibrary library)
        => OpenResource(library.ResourceName);

    private static Stream? OpenResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        return resName == null ? null : assembly.GetManifestResourceStream(resName);
    }
}