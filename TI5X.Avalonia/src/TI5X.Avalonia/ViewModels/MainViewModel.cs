using System.Reflection;
using System.Windows.Input;
using Avalonia.Threading;
using TI5X.Avalonia.Core;

namespace TI5X.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly CalculatorEngine _engine;
    private readonly PrinterEngine _printer;
    private LibraryModule? _masterLibrary;

    private string _displayText = "0.";
    private bool _isError;
    private bool _isRunning;
    private string _angleModeText = "DEG";
    private bool _isSecondActive;
    private bool _isInvActive;
    private bool _isLrnActive;
    private CardDef? _currentCard;
    private string _statusInfo = "Ready";

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

    public MainViewModel()
    {
        _engine = new CalculatorEngine();
        _printer = new PrinterEngine();
        _engine.Printer = _printer;

        _engine.DisplayChanged += OnEngineDisplayChanged;
        _engine.StateChanged += OnEngineStateChanged;

        LoadEmbeddedMasterLibrary();
        UpdateUiState();
    }

    private void LoadEmbeddedMasterLibrary()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Look for resource containing ml.ti5l
            var resName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith("ml.ti5l", StringComparison.OrdinalIgnoreCase));

            if (resName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resName);
                if (stream != null)
                {
                    _masterLibrary = LibraryModule.LoadFromStream(stream);
                    foreach (var kv in _masterLibrary.Banks)
                    {
                        _engine.Banks[kv.Key] = kv.Value;
                    }
                }
            }
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
        Dispatcher.UIThread.Post(UpdateUiState);
    }

    private void OnEngineStateChanged()
    {
        Dispatcher.UIThread.Post(UpdateUiState);
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

    public void LoadProgramFile(string filePath)
    {
        try
        {
            PersistentState.LoadProgram(_engine, filePath);
            UpdateUiState();
            StatusInfo = $"Loaded program from {Path.GetFileName(filePath)}";
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
            var curBank = _engine.Banks[_engine.CurBank];
            PersistentState.SaveProgram(_engine, filePath, curBank?.Card, curBank?.HelpHtml);
            StatusInfo = $"Saved program to {Path.GetFileName(filePath)}";
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
            var lib = LibraryModule.LoadFromFile(filePath);
            foreach (var kv in lib.Banks)
            {
                _engine.Banks[kv.Key] = kv.Value;
            }
            StatusInfo = $"Loaded library {Path.GetFileName(filePath)} ({lib.Banks.Count} programs)";
            UpdateUiState();
        }
        catch (Exception ex)
        {
            StatusInfo = $"Error: {ex.Message}";
        }
    }
}
