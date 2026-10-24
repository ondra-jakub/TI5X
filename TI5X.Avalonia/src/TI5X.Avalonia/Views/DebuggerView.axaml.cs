using Avalonia.Controls;
using Avalonia.Interactivity;
using TI5X.ViewModels;

namespace TI5X.Avalonia.Views;

public partial class DebuggerView : UserControl
{
    public DebuggerView()
    {
        InitializeComponent();
    }

    private ProgramDebuggerViewModel? ViewModel => DataContext as ProgramDebuggerViewModel;

    private void OnStepClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.Step();
        ViewModel?.Refresh();
    }

    private void OnRunClick(object? sender, RoutedEventArgs e) => ViewModel?.Run();
    private void OnResetClick(object? sender, RoutedEventArgs e) => ViewModel?.Reset();
    private void OnRefreshClick(object? sender, RoutedEventArgs e) => ViewModel?.Refresh();
}