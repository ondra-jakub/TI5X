using System.Collections.ObjectModel;
using TI5X.Avalonia.Core;

namespace TI5X.ViewModels;

public class PrinterViewModel : ViewModelBase, IDisposable
{
    private readonly PrinterEngine _printer;
    private readonly IUiDispatcher _dispatcher;
    private string _fullTapeText = string.Empty;
    private bool _disposed;
    private int _refreshPending;

    public ObservableCollection<string> TapeLines { get; } = new();

    public string FullTapeText
    {
        get => _fullTapeText;
        set => SetProperty(ref _fullTapeText, value);
    }

    public PrinterViewModel(PrinterEngine printer, IUiDispatcher dispatcher)
    {
        _printer = printer;
        _dispatcher = dispatcher;
        _printer.TapeChanged += OnTapeChanged;
        RefreshTape();
    }

    private void OnTapeChanged()
    {
        if (Interlocked.Exchange(ref _refreshPending, 1) == 1) return;

        _dispatcher.Post(() =>
        {
            Interlocked.Exchange(ref _refreshPending, 0);
            RefreshTape();
        });
    }

    public void RefreshTape()
    {
        string[] source;
        try
        {
            source = _printer.TapeLines.ToArray();
        }
        catch (InvalidOperationException)
        {
            OnTapeChanged();
            return;
        }

        bool rebuild = TapeLines.Count > source.Length;

        if (!rebuild)
        {
            for (int i = 0; i < TapeLines.Count; ++i)
            {
                if (!string.Equals(TapeLines[i], source[i], StringComparison.Ordinal))
                {
                    rebuild = true;
                    break;
                }
            }
        }

        if (rebuild)
        {
            TapeLines.Clear();
        }

        for (int i = TapeLines.Count; i < source.Length; ++i)
        {
            TapeLines.Add(source[i]);
        }

        FullTapeText = string.Join(Environment.NewLine, source);
    }

    public void Advance()
    {
        _printer.Advance();
    }

    public void Clear()
    {
        _printer.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _printer.TapeChanged -= OnTapeChanged;
        GC.SuppressFinalize(this);
    }
}
