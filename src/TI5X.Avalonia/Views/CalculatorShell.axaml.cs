using Avalonia.Controls;
using Avalonia.VisualTree;
using TI5X.Avalonia.Services;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Views;

/// <summary>
/// Responsive calculator host. Narrow surfaces show only the calculator, which fills
/// the window up to <see cref="CalculatorMaxWidth"/> and is centered beyond it — on
/// foldables neither the folded nor the unfolded state stretches the keypad. Once the
/// surface is wide enough the embedded virtual printer pane appears on the left,
/// mirroring the TI-59 docked in its PC-100 cradle. Used by both the desktop window
/// and the Android root page.
/// </summary>
public partial class CalculatorShell : UserControl
{
    /// <summary>Minimum width (DIP) at which the printer pane is shown beside the calculator.
    /// 680 DIP covers foldable inner displays (≈ 673–906 DIP wide when unfolded) and
    /// small tablets, while ordinary phones (≤ ~500 DIP) stay in the narrow layout.</summary>
    private const double WideThreshold = 680.0;

    /// <summary>Fixed calculator column width in the wide layout.</summary>
    private const double CalculatorColumnWidth = 460.0;

    /// <summary>Maximum width of the calculator body in every layout, so that wide
    /// windows (foldables, tablets, maximized desktop windows) never stretch the
    /// keypad and display across the whole surface.</summary>
    private const double CalculatorMaxWidth = 520.0;

    private PrinterViewModel? _printerViewModel;
    private bool _isWide;

    public CalculatorShell()
    {
        InitializeComponent();
        Calculator.MaxWidth = CalculatorMaxWidth;
        SizeChanged += (_, e) => AppServices.Log($"TI5X Shell: SizeChanged {e.NewSize.Width:F1}x{e.NewSize.Height:F1} wide={e.NewSize.Width >= WideThreshold}");
        SizeChanged += (_, _) => ApplyResponsiveLayout();
        Loaded += (_, _) => { AppServices.Log($"TI5X Shell: Loaded Bounds={Bounds.Width:F1}x{Bounds.Height:F1} wide={Bounds.Width >= WideThreshold}"); ApplyResponsiveLayout(); };
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
            // narrow layout: the body fills the window up to CalculatorMaxWidth
            // (Avalonia centers a Stretch child capped by MaxWidth, so wide surfaces
            // like foldables never show a stretched keypad)
        }
    }
}