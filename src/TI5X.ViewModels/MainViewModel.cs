using TI5X.Avalonia.Core;

namespace TI5X.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly CalculatorEngine _engine;
    private readonly PrinterEngine _printer;
    private readonly IUiDispatcher _dispatcher;

    private string _displayText = "0.";
    private bool _isError;
    private bool _isRunning;
    private string _angleModeText = "DEG";
    private bool _isSecondActive;
    private bool _isInvActive;
    private bool _isLrnActive;
    private CardDef? _currentCard;
    private string _statusInfo = "Ready";
    private bool _disposed;

    public CalculatorEngine Engine => _engine;
    public PrinterEngine Printer => _printer;

    public string DisplayText
    {
        get => _displayText;
        set => SetProperty(ref _displayText, value);
    }

    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public string AngleModeText
    {
        get => _angleModeText;
        set => SetProperty(ref _angleModeText, value);
    }

    public bool IsSecondActive
    {
        get => _isSecondActive;
        set => SetProperty(ref _isSecondActive, value);
    }

    public bool IsInvActive
    {
        get => _isInvActive;
        set => SetProperty(ref _isInvActive, value);
    }

    public bool IsLrnActive
    {
        get => _isLrnActive;
        set => SetProperty(ref _isLrnActive, value);
    }

    public CardDef? CurrentCard
    {
        get => _currentCard;
        set => SetProperty(ref _currentCard, value);
    }

    public string StatusInfo
    {
        get => _statusInfo;
        set => SetProperty(ref _statusInfo, value);
    }

    public MainViewModel(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _engine = new CalculatorEngine();
        _printer = new PrinterEngine();
        _engine.Printer = _printer;

        _engine.DisplayChanged += OnEngineDisplayChanged;
        _engine.StateChanged += OnEngineStateChanged;

        UpdateUiState();
    }

    public void LoadMasterLibrary(Stream stream)
    {
        try
        {
            var library = LibraryModule.LoadFromStream(stream);
            foreach (var kv in library.Banks)
            {
                _engine.Banks[kv.Key] = kv.Value;
            }
            UpdateUiState();
        }
        catch (Exception ex)
        {
            StatusInfo = $"Error loading Master Library: {ex.Message}";
        }
    }

    public void PressKey(int keyCode)
    {
        _engine.ProcessKey(keyCode);
        UpdateUiState();
    }

    private void OnEngineDisplayChanged()
    {
        _dispatcher.Post(UpdateUiState);
    }

    private void OnEngineStateChanged()
    {
        _dispatcher.Post(UpdateUiState);
    }

    public void UpdateUiState()
    {
        DisplayText = _engine.DisplayString;
        IsError = _engine.State == CalculatorEngine.EngineState.Error;
        IsRunning = _engine.IsRunning;

        AngleModeText = _engine.CurAngleMode switch
        {
            Arith.ANG_DEG => "DEG",
            Arith.ANG_RAD => "RAD",
            Arith.ANG_GRAD => "GRAD",
            _ => "DEG"
        };

        IsSecondActive = _engine.SecondState;
        IsInvActive = _engine.InvState;
        IsLrnActive = _engine.LrnMode;

        var curBank = _engine.Banks[_engine.CurBank];
        CurrentCard = curBank?.Card;

        string bankLabel = _engine.CurBank == 0 ? "User Bank 00" : $"Library Bank {_engine.CurBank:D2}";
        StatusInfo = $"{bankLabel} | PC: {_engine.PC:D3} | Regs: {100 - _engine.RegOffset / 10}";
    }

    public void LoadProgram(Stream stream, string? displayName = null)
    {
        try
        {
            PersistentState.LoadProgram(_engine, stream);
            UpdateUiState();
            StatusInfo = displayName == null
                ? "Loaded program"
                : $"Loaded program from {displayName}";
        }
        catch (Exception ex)
        {
            StatusInfo = $"Error: {ex.Message}";
        }
    }

    public void LoadProgramFile(string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            LoadProgram(fs, Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            StatusInfo = $"Error: {ex.Message}";
        }
    }

    public void SaveProgram(Stream stream, string? displayName = null)
    {
        try
        {
            var curBank = _engine.Banks[_engine.CurBank];
            PersistentState.SaveProgram(_engine, stream, curBank?.Card, curBank?.HelpHtml);
            StatusInfo = displayName == null
                ? "Saved program"
                : $"Saved program to {displayName}";
        }
        catch (Exception ex)
        {
            StatusInfo = $"Error: {ex.Message}";
        }
    }

    public void SaveProgramFile(string filePath)
    {
        try
        {
            using var fs = File.Create(filePath);
            SaveProgram(fs, Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            StatusInfo = $"Error: {ex.Message}";
        }
    }

    public void LoadCustomLibrary(Stream stream, string? displayName = null)
    {
        try
        {
            var library = LibraryModule.LoadFromStream(stream);
            foreach (var kv in library.Banks)
            {
                _engine.Banks[kv.Key] = kv.Value;
            }
            UpdateUiState();
            StatusInfo = displayName == null
                ? $"Loaded library ({library.Banks.Count} programs)"
                : $"Loaded library {displayName} ({library.Banks.Count} programs)";
        }
        catch (Exception ex)
        {
            StatusInfo = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Loads one of the built-in Solid State Software modules (transcribed from the
    /// original TMC0540 CROM dumps). Plugging in a module replaces any previously
    /// loaded library banks, like swapping the physical module in the TI-59.
    /// </summary>
    public void LoadBuiltInLibrary(Stream stream, string displayName)
    {
        try
        {
            var library = LibraryModule.LoadFromStream(stream);
            for (int i = 1; i < CalculatorEngine.MaxBanks; ++i)
            {
                _engine.Banks[i] = null;
            }
            foreach (var kv in library.Banks)
            {
                _engine.Banks[kv.Key] = kv.Value;
            }
            UpdateUiState();
            StatusInfo = $"{displayName}: {library.Banks.Count} programs loaded";
        }
        catch (Exception ex)
        {
            StatusInfo = $"Error: {ex.Message}";
        }
    }

    public void LoadCustomLibrary(string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            LoadCustomLibrary(fs, Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            StatusInfo = $"Error: {ex.Message}";
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _engine.DisplayChanged -= OnEngineDisplayChanged;
        _engine.StateChanged -= OnEngineStateChanged;
        GC.SuppressFinalize(this);
    }
}
