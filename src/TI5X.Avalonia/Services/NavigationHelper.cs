using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace TI5X.Avalonia.Services;

/// <summary>
/// Looks up the hosting <see cref="NavigationPage"/> from inside a page by walking
/// the visual and logical ancestor chains. Used by shared pages (compiled into the
/// desktop assembly) to request navigation without Android-specific references.
/// </summary>
public static class NavigationHelper
{
    public static NavigationPage? FindNavigationPage(Control start)
    {
        return start.GetVisualAncestors().OfType<NavigationPage>().FirstOrDefault()
            ?? start.GetLogicalAncestors().OfType<NavigationPage>().FirstOrDefault();
    }
}