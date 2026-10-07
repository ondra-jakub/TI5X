using Avalonia.Controls;
using Avalonia.Interactivity;
using TI5X.Avalonia.ViewModels;

namespace TI5X.Avalonia.Views;

public partial class ProgramDebuggerWindow : Window
{
    public ProgramDebuggerWindow()
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