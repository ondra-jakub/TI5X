using System.Collections.ObjectModel;
using Avalonia.Threading;
using TI5X.Avalonia.Core;

namespace TI5X.Avalonia.ViewModels;

public class PrinterViewModel : ViewModelBase
{
    private readonly PrinterEngine _printer;
    private string _fullTapeText = string.Empty;

    public ObservableCollection<string> TapeLines { get; } = new();

    public string FullTapeText
    {
        get => _fullTapeText;
        set => SetProperty(ref _fullTapeText, value);
    }

    public PrinterViewModel(PrinterEngine printer)
    {
        _printer = printer;
        _printer.TapeChanged += OnTapeChanged;
        RefreshTape();
    }

    private void OnTapeChanged()
    {
        Dispatcher.UIThread.Post(RefreshTape);
    }

    public void RefreshTape()
    {
        TapeLines.Clear();
        foreach (var line in _printer.TapeLines)
        {
            TapeLines.Add(line);
        }
        FullTapeText = string.Join(Environment.NewLine, _printer.TapeLines);
    }

    public void Advance()
    {
        _printer.Advance();
    }

    public void Clear()
    {
        _printer.Clear();
    }
}
