using TI5X.ViewModels;

namespace TI5X.Maui.Pages;

public partial class DebuggerPage : ContentPage
{
    private readonly ProgramDebuggerViewModel _viewModel;

    public DebuggerPage(ProgramDebuggerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnStepClick(object? sender, EventArgs e)
    {
        _viewModel.Step();
        _viewModel.Refresh();
    }

    private void OnRunClick(object? sender, EventArgs e) => _viewModel.Run();

    private void OnResetClick(object? sender, EventArgs e) => _viewModel.Reset();

    private void OnRefreshClick(object? sender, EventArgs e) => _viewModel.Refresh();
}
