using System.Collections.ObjectModel;
using TI5X.Avalonia.Core;

namespace TI5X.ViewModels;

public class ProgramStepItem : ViewModelBase
{
    private int _step;
    private byte _opCode;
    private string _mnemonic = "";
    private bool _isCurrent;

    public int Step
    {
        get => _step;
        set => SetProperty(ref _step, value);
    }

    public byte OpCode
    {
        get => _opCode;
        set => SetProperty(ref _opCode, value);
    }

    public string Mnemonic
    {
        get => _mnemonic;
        set => SetProperty(ref _mnemonic, value);
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set => SetProperty(ref _isCurrent, value);
    }
}

public class MemoryRegisterItem : ViewModelBase
{
    private int _index;
    private double _value;

    public int Index
    {
        get => _index;
        set => SetProperty(ref _index, value);
    }

    public double Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

public class ProgramDebuggerViewModel : ViewModelBase, IDisposable
{
    private readonly CalculatorEngine _engine;
    private readonly IUiDispatcher _dispatcher;
    private bool _disposed;
    public ObservableCollection<ProgramStepItem> Steps { get; } = new();
    public ObservableCollection<MemoryRegisterItem> Registers { get; } = new();

    public ProgramDebuggerViewModel(CalculatorEngine engine, IUiDispatcher dispatcher)
    {
        _engine = engine;
        _dispatcher = dispatcher;
        _engine.StateChanged += OnEngineChanged;
        Refresh();
    }

    private void OnEngineChanged()
    {
        _dispatcher.Post(Refresh);
    }

    public void Refresh()
    {
        byte[] prog = _engine.GetCurProg();
        int max = _engine.GetMaxProg();

        // Steps
        if (Steps.Count != max)
        {
            Steps.Clear();
            for (int i = 0; i < max; ++i)
            {
                byte code = i < prog.Length ? prog[i] : (byte)0;
                Steps.Add(new ProgramStepItem
                {
                    Step = i,
                    OpCode = code,
                    Mnemonic = KeyCodes.KeyCodeSym(code),
                    IsCurrent = i == _engine.PC
                });
            }
        }
        else
        {
            for (int i = 0; i < max; ++i)
            {
                byte code = i < prog.Length ? prog[i] : (byte)0;
                Steps[i].OpCode = code;
                Steps[i].Mnemonic = KeyCodes.KeyCodeSym(code);
                Steps[i].IsCurrent = (i == _engine.PC);
            }
        }

        // Registers (R00..R99)
        int numRegs = 100 - _engine.RegOffset / 10;
        if (Registers.Count != numRegs)
        {
            Registers.Clear();
            for (int i = 0; i < numRegs; ++i)
            {
                Registers.Add(new MemoryRegisterItem
                {
                    Index = i,
                    Value = _engine.Memories[i]
                });
            }
        }
        else
        {
            for (int i = 0; i < numRegs; ++i)
            {
                Registers[i].Value = _engine.Memories[i];
            }
        }
    }

    public void Step()
    {
        if (_engine.IsRunning) return; // engine guard; do not race the background loop
        _engine.StepProgram();
    }

    public void Run()
    {
        if (_engine.IsRunning)
            _engine.StopProgram();
        else
            _engine.StartProgram();
    }

    public void Reset()
    {
        if (_engine.IsRunning) return; // moving PC mid-run corrupts the background loop
        _engine.PC = 0;
        Refresh();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _engine.StateChanged -= OnEngineChanged;
        GC.SuppressFinalize(this);
    }
}
