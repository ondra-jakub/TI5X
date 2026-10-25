using Avalonia.Controls;
using TI5X.Avalonia.Services;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Views;

/// <summary>
/// Responsive calculator host. Narrow windows show the calculator alone (the printer
/// stays reachable from the menu); once the window is wide enough the embedded virtual
/// printer pane appears on the left, mirroring the TI-59 docked in its PC-100 cradle.
/// Used by both the desktop window and the Android root page.
/// </summary>
public partial class CalculatorShell : UserControl
{
    /// <summary>Minimum width (DIP) at which the printer pane is shown beside the calculator.</summary>
    private const double WideThreshold = 900.0;

    /// <summary>Fixed calculator column width in the wide layout.</summary>
    private const double CalculatorColumnWidth = 460.0;

    private PrinterViewModel? _printerViewModel;
    private bool _isWide;

    public CalculatorShell()
    {
        InitializeComponent();
        SizeChanged += (_, _) => ApplyResponsiveLayout();
        Loaded += (_, _) => ApplyResponsiveLayout();
        AttachedToVisualTree += (_, _) => EnsurePrinterViewModel();
        DataContextChanged += (_, _) => EnsurePrinterViewModel();
        DetachedFromVisualTree += (_, _) => DisposePrinterViewModel();
    }

    private void EnsurePrinterViewModel()
    {
        if (_printerViewModel != null || DataContext is not MainViewModel main) return;
        _printerViewModel = new PrinterViewModel(main.Printer, new AvaloniaUiDispatcher());
        PrinterViewControl.DataContext = _printerViewModel;
    }

    private void DisposePrinterViewModel()
    {
        _printerViewModel?.Dispose();
        _printerViewModel = null;
    }

    private void ApplyResponsiveLayout()
    {
        bool wide = Bounds.Width >= WideThreshold;
        if (wide == _isWide) return;
        _isWide = wide;

        RootGrid.ColumnDefinitions.Clear();
        Grid.SetColumn(PrinterPane, 0);
        Grid.SetColumn(Calculator, 0);

        if (wide)
        {
            // printer takes the remaining space (capped by its MaxWidth), calculator fixed
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CalculatorColumnWidth, GridUnitType.Pixel) });
            Grid.SetColumn(Calculator, 1);
            PrinterPane.IsVisible = true;
        }
        else
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            PrinterPane.IsVisible = false;
        }
    }
}