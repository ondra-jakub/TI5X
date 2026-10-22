using TI5X.ViewModels;

namespace TI5X.Maui.Pages;

public partial class PrinterPage : ContentPage
{
    private readonly PrinterViewModel _viewModel;

    public PrinterPage(PrinterViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnAdvanceClick(object? sender, EventArgs e) => _viewModel.Advance();

    private async void OnCopyClick(object? sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(_viewModel.FullTapeText);
    }

    private void OnClearClick(object? sender, EventArgs e) => _viewModel.Clear();
}
