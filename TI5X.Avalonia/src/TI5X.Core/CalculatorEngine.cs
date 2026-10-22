using System.Globalization;

namespace TI5X.Avalonia.Core;

public class CalculatorEngine
{
    // Constants
    public const int MaxMemories = 100;
    public const int MaxProgram = 960; // TI-59 standard max steps
    public const int MaxBanks = 100;   // library bank numbers live in Partition.MaxLibProgram
    public const int MaxOpStack = 10;
    public const int MaxReturnStack = 8;
    public const int MaxFlags = 10;

    // Angle modes
    public const int ANG_DEG = Arith.ANG_DEG;
    public const int ANG_RAD = Arith.ANG_RAD;
    public const int ANG_GRAD = Arith.ANG_GRAD;

    // Display formats
    public const int FORMAT_FIXED = Arith.FORMAT_FIXED;
    public const int FORMAT_FLOAT = Arith.FORMAT_FLOAT;
    public const int FORMAT_ENG = Arith.FORMAT_ENG;

    // Flags
    public const int FLAG_TRACE_PRINT = 7;
    public const int FLAG_STOP_ON_ERROR = 8;
    public const int FLAG_SLOW_EXECUTION = 9;

    // Stack Operators
    public const int STACKOP_NONE = 0;
    public const int STACKOP_LPAREN = 1;
    public const int STACKOP_ADD = 2;
    public const int STACKOP_SUB = 3;
    public const int STACKOP_MUL = 4;
    public const int STACKOP_DIV = 5;
    public const int STACKOP_MOD = 6;
    public const int STACKOP_EXP = 7;
    public const int STACKOP_ROOT = 8;

    // Engine States
    public enum EngineState
    {
        Entry,
        DecimalEntry,
        ExponentEntry,
        Result,
        Error
    }

    // State Variables
    public double X { get; set; }
    public double T { get; set; }
    public int PC { get; set; }
    public int CurBank { get; set; }
    public int RegOffset { get; set; } = 480; // Op 17 default: 479.59 partition (480 steps, 60 memories 0..59)

    public int CurAngleMode { get; set; } = ANG_DEG;
    public int CurFormat { get; set; } = FORMAT_FIXED;
    public int CurDecimals { get; set; } = -1; // -1 = standard variable

    public bool SecondState { get; set; }
    public bool InvState { get; set; }
    public bool LrnMode { get; set; }

    public EngineState State { get; private set; } = EngineState.Entry;
    public string DisplayString { get; private set; } = "0.";
    public bool ExponentEntered { get; private set; }

    // Memories & Program Banks
    public double[] Memories { get; } = new double[MaxMemories];
    public byte[] UserProgram { get; } = new byte[MaxProgram];
    public ProgBank?[] Banks { get; } = new ProgBank?[MaxBanks];
    public bool[] Flags { get; } = new bool[MaxFlags];

    // Execution & Stacks
    public OpStackEntry[] OpStack { get; } = new OpStackEntry[MaxOpStack];
    public int OpStackCount { get; private set; }

    public ReturnStackEntry[] ReturnStack { get; } = new ReturnStackEntry[MaxReturnStack];
    public int ReturnStackCount { get; private set; }

    public byte[] PrintRegister { get; } = new byte[20];

    // Execution control
    public bool IsRunning { get; private set; }
    public bool PauseRequested { get; private set; }
    private CancellationTokenSource? _runCts;
    private Task _runTask = Task.CompletedTask;
    private int _runGeneration;

    /// <summary>
    /// Completes when the current (or last) background program run has fully finished,
    /// including the final display refresh and state notification.
    /// </summary>
    public Task RunCompletion => _runTask;

    // Events
    public event Action? DisplayChanged;
    public event Action? StateChanged;

    // Key Entry multi-step sequence
    public int DigitsNeeded { get; private set; }
    public int AccumDigits { get; private set; }
    public int CollectingForFunction { get; private set; }
    public bool GotInd { get; private set; }
    public bool AcceptSymbolic { get; private set; }
    public bool AcceptInd { get; private set; }

    public PrinterEngine? Printer { get; set; }

    public CalculatorEngine()
    {
        Banks[0] = new ProgBank(UserProgram);
        Reset(clearLibs: true);
    }

    public void Reset(bool clearLibs = true)
    {
        StopProgram();
        CurAngleMode = ANG_DEG;
        CurFormat = FORMAT_FIXED;
        CurDecimals = -1;
        OpStackCount = 0;
        ReturnStackCount = 0;
        X = 0.0;
        T = 0.0;
        PC = 0;
        CurBank = 0;
        RegOffset = 480;
        SecondState = false;
        InvState = false;
        LrnMode = false;
        ResetLrnEntry();
        ResetOperands();

        Array.Clear(Flags, 0, Flags.Length);
        Array.Clear(Memories, 0, Memories.Length);
        Array.Clear(UserProgram, 0, UserProgram.Length);
        Array.Clear(PrintRegister, 0, PrintRegister.Length);

        if (clearLibs)
        {
            for (int i = 1; i < MaxBanks; ++i)
                Banks[i] = null;
        }

        ResetEntry();
        NotifyStateChanged();
    }

    public void ResetEntry()
    {
        State = EngineState.Entry;
        DisplayString = CurFormat == FORMAT_FIXED ? "0." : "0. 00";
        ExponentEntered = CurFormat != FORMAT_FIXED;
        NotifyDisplayChanged();
    }

    public void SetErrorState()
    {
        State = EngineState.Error;
        if (Flags[FLAG_STOP_ON_ERROR])
        {
            StopProgram();
        }
        NotifyDisplayChanged();
    }

    public void ClearErrorState()
    {
        if (State == EngineState.Error)
        {
            State = EngineState.Result;
            NotifyDisplayChanged();
        }
    }

    public void FinishNumberEntry()
    {
        if (State != EngineState.Result && State != EngineState.Error)
        {
            try
            {
                if (ExponentEntered && DisplayString.Length >= 4)
                {
                    string mantissaStr = DisplayString.Substring(0, DisplayString.Length - 3).Trim();
                    string expSign = DisplayString.Substring(DisplayString.Length - 3, 1);
                    string expDigits = DisplayString.Substring(DisplayString.Length - 2);

                    double mant = double.Parse(mantissaStr, CultureInfo.InvariantCulture);
                    int exp = int.Parse(expDigits, CultureInfo.InvariantCulture);
                    if (expSign == "-") exp = -exp;

                    X = mant * Math.Pow(10.0, exp);
                }
                else
                {
                    X = double.Parse(DisplayString.TrimEnd('.').Length == 0 ? "0" : DisplayString, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                X = 0.0;
            }
            State = EngineState.Result;
        }
    }

    public void SetX(double newX)
    {
        if (double.IsNaN(newX) || double.IsInfinity(newX))
        {
            SetErrorState();
            return;
        }

        X = newX;
        State = EngineState.Result;
        DisplayString = FormatNumber(X, CurFormat, CurDecimals);
        NotifyDisplayChanged();

        if (Flags[FLAG_TRACE_PRINT] && Printer != null)
        {
            PrintDisplay();
        }
    }

    public static string FormatNumber(double val, int format, int decimals)
    {
        if (double.IsNaN(val) || double.IsInfinity(val))
            return "       Error";

        int exp = Arith.PowerOf10(val);
        int useFormat = format;

        // Auto float if out of range for fixed
        if (useFormat == FORMAT_FIXED && (Math.Abs(val) >= 1e10 || (val != 0.0 && Math.Abs(val) < 1e-10)))
        {
            useFormat = FORMAT_FLOAT;
        }

        if (useFormat == FORMAT_FLOAT || useFormat == FORMAT_ENG)
        {
            int scaleExp = exp;
            if (useFormat == FORMAT_ENG)
            {
                scaleExp = exp >= 0 ? (exp / 3) * 3 : ((exp - 2) / 3) * 3;
            }

            double mantissa = val / Math.Pow(10.0, scaleExp);
            int beforeDecimal = Arith.FiguresBeforeDecimal(val, scaleExp);
            int decPlaces = Math.Max(0, 8 - beforeDecimal);

            string mantStr = mantissa.ToString($"F{decPlaces}", CultureInfo.InvariantCulture);
            string expStr = $"{(scaleExp < 0 ? "-" : " ")}{Math.Abs(scaleExp):D2}";
            return $"{mantStr}{expStr}";
        }
        else
        {
            if (decimals >= 0)
            {
                int beforeDecimal = Arith.FiguresBeforeDecimal(val, 0);
                int decPlaces = Math.Max(0, Math.Min(decimals, 10 - beforeDecimal));
                double rounded = Math.Round(val, decPlaces, MidpointRounding.AwayFromZero);
                return rounded.ToString($"F{decPlaces}", CultureInfo.InvariantCulture);
            }
            else
            {
                int beforeDecimal = Arith.FiguresBeforeDecimal(val, 0);
                int decPlaces = Math.Max(0, 10 - beforeDecimal);
                string res = val.ToString($"F{decPlaces}", CultureInfo.InvariantCulture);
                if (decPlaces > 0)
                {
                    res = res.TrimEnd('0');
                    if (res.EndsWith('.'))
                        res += "";
                    else if (!res.Contains('.'))
                        res += ".";
                }
                else
                {
                    res += ".";
                }
                return string.IsNullOrEmpty(res) ? "0." : res;
            }
        }
    }

    public void ProcessKey(int keyCode)
    {
        if (IsRunning && keyCode == KeyCodes.KEY_R_S)
        {
            StopProgram();
            return;
        }

        if (IsRunning)
            return;

        // Multi-step operand collection (e.g., STO 0 1, GTO A, Fix 2)
        if (DigitsNeeded > 0)
        {
            CollectOperand(keyCode);
            return;
        }

        if (LrnMode)
        {
            ProcessLrnKey(keyCode);
            return;
        }

        ExecuteKeyInteractive(keyCode);
    }

    private void CollectOperand(int keyCode)
    {
        // Check for 2nd modifier during operand
        if (keyCode == KeyCodes.KEY_2ND)
        {
            SecondState = !SecondState;
            NotifyStateChanged();
            return;
        }

        int effectiveKey = keyCode;
        if (SecondState)
        {
            effectiveKey = MapSecond(keyCode);
            SecondState = false;
        }

        // Check for Indirection
        if (AcceptInd && !GotInd && effectiveKey == KeyCodes.KEY_IND)
        {
            GotInd = true;
            NotifyStateChanged();
            return;
        }

        // Symbolic keys A..E, A'..E'
        if (AcceptSymbolic && (effectiveKey is >= KeyCodes.KEY_A and <= KeyCodes.KEY_E or >= KeyCodes.KEY_E_PRIME and <= KeyCodes.KEY_D_PRIME))
        {
            ExecuteCollectedFunction(effectiveKey, isSymbolic: true);
            return;
        }

        // Digit input 0..9
        int digit = KeyToDigit(effectiveKey);
        if (digit >= 0)
        {
            AccumDigits = AccumDigits * 10 + digit;
            DigitsNeeded--;
            if (DigitsNeeded == 0)
            {
                ExecuteCollectedFunction(AccumDigits, isSymbolic: false);
            }
            return;
        }

        // Invalid key pressed during operand collection -> cancel or error
        ResetOperands();
        SetErrorState();
    }

    private void ExecuteCollectedFunction(int operand, bool isSymbolic)
    {
        int func = CollectingForFunction;
        bool ind = GotInd;
        bool inv = InvState;
        ResetOperands();
        InvState = false;

        switch (func)
        {
            case KeyCodes.KEY_STO:
                ExecuteStore(operand, ind);
                break;
            case KeyCodes.KEY_RCL:
                ExecuteRecall(operand, ind);
                break;
            case KeyCodes.KEY_SUM:
                ExecuteSum(operand, ind, inv);
                break;
            case KeyCodes.KEY_PRD:
                ExecutePrd(operand, ind, inv);
                break;
            case KeyCodes.KEY_EXC:
                ExecuteExchange(operand, ind);
                break;
            case KeyCodes.KEY_GTO:
                ExecuteGto(operand, isSymbolic, ind);
                break;
            case KeyCodes.KEY_SBR:
                // Only reachable from the keyboard: program SBR is decoded
                // structurally in ExecuteSingleProgramStep.
                ExecuteSbr(operand, isSymbolic, ind, fromInteractive: true);
                break;
            case KeyCodes.KEY_PGM:
                ExecutePgm(operand, ind);
                break;
            case KeyCodes.KEY_FIX:
                ExecuteFix(operand);
                break;
            case KeyCodes.KEY_OP:
                ExecuteOp(operand, ind);
                break;
            case KeyCodes.KEY_ST_FLG:
                ExecuteStFlg(operand, inv);
                break;
            case KeyCodes.KEY_IF_FLG:
                // If flag requires target jump
                ExecuteIfFlg(operand, inv);
                break;
            case KeyCodes.KEY_DSZ:
                ExecuteDsz(operand, inv);
                break;
        }
        NotifyStateChanged();
    }

    private void ResetOperands()
    {
        DigitsNeeded = 0;
        AccumDigits = 0;
        CollectingForFunction = 0;
        GotInd = false;
        AcceptSymbolic = false;
        AcceptInd = false;
        NotifyStateChanged();
    }

    public void ExecuteKeyInteractive(int keyCode)
    {
        if (keyCode == KeyCodes.KEY_2ND)
        {
            SecondState = !SecondState;
            NotifyStateChanged();
            return;
        }

        int effKey = keyCode;
        if (SecondState)
        {
            effKey = MapSecond(keyCode);
            SecondState = false;
            NotifyStateChanged();
        }

        // If collecting operands for function (STO, RCL, GTO, FIX, etc.)
        if (CollectingForFunction != 0)
        {
            CollectOperand(effKey);
            return;
        }

        bool inv = InvState;

        // Number entry keys
        int digit = KeyToDigit(effKey);
        if (digit >= 0)
        {
            ProcessDigit(digit);
            return;
        }

        if (effKey == KeyCodes.KEY_DOT)
        {
            ProcessDot();
            return;
        }

        if (effKey == KeyCodes.KEY_PLUS_MINUS)
        {
            ProcessPlusMinus();
            return;
        }

        if (effKey == KeyCodes.KEY_EE)
        {
            ProcessEE();
            return;
        }

        // Functions that require arguments
        if (effKey is KeyCodes.KEY_STO or KeyCodes.KEY_RCL or KeyCodes.KEY_SUM or KeyCodes.KEY_PRD or KeyCodes.KEY_EXC)
        {
            FinishNumberEntry();
            CollectingForFunction = effKey;
            DigitsNeeded = 2;
            AccumDigits = 0;
            AcceptInd = true;
            AcceptSymbolic = false;
            NotifyStateChanged();
            return;
        }

        if (effKey == KeyCodes.KEY_GTO)
        {
            FinishNumberEntry();
            CollectingForFunction = effKey;
            DigitsNeeded = 3;
            AccumDigits = 0;
            AcceptInd = true;
            AcceptSymbolic = true;
            NotifyStateChanged();
            return;
        }

        if (effKey == KeyCodes.KEY_SBR)
        {
            FinishNumberEntry();
            if (inv)
            {
                InvState = false;
                ExecuteReturn();
                return;
            }
            CollectingForFunction = effKey;
            DigitsNeeded = 3;
            AccumDigits = 0;
            AcceptInd = true;
            AcceptSymbolic = true;
            NotifyStateChanged();
            return;
        }

        if (effKey == KeyCodes.KEY_PGM)
        {
            FinishNumberEntry();
            CollectingForFunction = effKey;
            DigitsNeeded = 2;
            AccumDigits = 0;
            AcceptInd = true;
            AcceptSymbolic = false;
            NotifyStateChanged();
            return;
        }

        if (effKey == KeyCodes.KEY_FIX)
        {
            FinishNumberEntry();
            if (inv)
            {
                InvState = false;
                CurDecimals = -1;
                SetX(X);
                return;
            }
            CollectingForFunction = effKey;
            DigitsNeeded = 1;
            AccumDigits = 0;
            AcceptInd = false;
            AcceptSymbolic = false;
            NotifyStateChanged();
            return;
        }

        if (effKey == KeyCodes.KEY_OP)
        {
            FinishNumberEntry();
            CollectingForFunction = effKey;
            DigitsNeeded = 2;
            AccumDigits = 0;
            AcceptInd = true;
            AcceptSymbolic = false;
            NotifyStateChanged();
            return;
        }

        if (effKey == KeyCodes.KEY_ST_FLG)
        {
            FinishNumberEntry();
            CollectingForFunction = effKey;
            DigitsNeeded = 1;
            AccumDigits = 0;
            AcceptInd = false;
            AcceptSymbolic = false;
            NotifyStateChanged();
            return;
        }

        if (effKey == KeyCodes.KEY_IF_FLG)
        {
            FinishNumberEntry();
            CollectingForFunction = effKey;
            DigitsNeeded = 1;
            AccumDigits = 0;
            AcceptInd = false;
            AcceptSymbolic = false;
            NotifyStateChanged();
            return;
        }

        if (effKey == KeyCodes.KEY_DSZ)
        {
            FinishNumberEntry();
            CollectingForFunction = effKey;
            DigitsNeeded = 1;
            AccumDigits = 0;
            AcceptInd = false;
            AcceptSymbolic = false;
            NotifyStateChanged();
            return;
        }

        // Direct Execution keys
        InvState = false; // Reset INV after standard command
        ExecuteSingleKey(effKey, inv);
        NotifyStateChanged();
    }

    private void ExecuteSingleKey(int effKey, bool inv)
    {
        switch (effKey)
        {
            case KeyCodes.KEY_CLR:
                ClearAll();
                break;
            case KeyCodes.KEY_CE:
                ClearEntry();
                break;
            case KeyCodes.KEY_CP:
                ClearProgram();
                break;
            case KeyCodes.KEY_CMS:
                ClearMemories();
                break;
            case KeyCodes.KEY_LRN:
                FinishNumberEntry();
                LrnMode = !LrnMode;
                if (LrnMode)
                    ShowLrnDisplay();
                else
                    SetX(X);
                break;
            case KeyCodes.KEY_RST:
                FinishNumberEntry();
                PC = 0;
                CurBank = 0;
                break;
            case KeyCodes.KEY_R_S:
                FinishNumberEntry();
                StartProgram();
                break;
            case KeyCodes.KEY_SST:
                FinishNumberEntry();
                StepProgram();
                break;
            case KeyCodes.KEY_BST:
                FinishNumberEntry();
                if (PC > 0) PC--;
                if (LrnMode) ShowLrnDisplay();
                break;
            case KeyCodes.KEY_X_SWAP_T:
                FinishNumberEntry();
                (X, T) = (T, X);
                SetX(X);
                break;
            case KeyCodes.KEY_ADD:
                ProcessBinaryOp(STACKOP_ADD);
                break;
            case KeyCodes.KEY_SUB:
                ProcessBinaryOp(STACKOP_SUB);
                break;
            case KeyCodes.KEY_MUL:
                ProcessBinaryOp(STACKOP_MUL);
                break;
            case KeyCodes.KEY_DIV:
                ProcessBinaryOp(STACKOP_DIV);
                break;
            case KeyCodes.KEY_Y_TO_X:
                ProcessBinaryOp(inv ? STACKOP_ROOT : STACKOP_EXP);
                break;
            case KeyCodes.KEY_LPAREN:
                ProcessLParen();
                break;
            case KeyCodes.KEY_RPAREN:
                ProcessRParen();
                break;
            case KeyCodes.KEY_EQUALS:
                ProcessEquals();
                break;
            case KeyCodes.KEY_RECIPROCAL:
                FinishNumberEntry();
                if (X == 0.0) SetErrorState();
                else SetX(1.0 / X);
                break;
            case KeyCodes.KEY_X_SQUARED:
                FinishNumberEntry();
                if (inv)
                {
                    if (X < 0.0) SetErrorState();
                    else SetX(Math.Sqrt(X));
                }
                else
                {
                    SetX(X * X);
                }
                break;
            case KeyCodes.KEY_SQRT_X:
                FinishNumberEntry();
                if (inv)
                {
                    SetX(X * X);
                }
                else
                {
                    if (X < 0.0) SetErrorState();
                    else SetX(Math.Sqrt(X));
                }
                break;
            case KeyCodes.KEY_LNX:
                FinishNumberEntry();
                if (inv)
                {
                    SetX(Math.Exp(X));
                }
                else
                {
                    if (X <= 0.0) SetErrorState();
                    else SetX(Math.Log(X));
                }
                break;
            case KeyCodes.KEY_LOG:
                FinishNumberEntry();
                if (inv)
                {
                    SetX(Math.Pow(10.0, X));
                }
                else
                {
                    if (X <= 0.0) SetErrorState();
                    else SetX(Math.Log10(X));
                }
                break;
            case KeyCodes.KEY_SIN:
                FinishNumberEntry();
                if (inv)
                {
                    if (X < -1.0 || X > 1.0) SetErrorState();
                    else SetX(Arith.FromRadians(Math.Asin(X), CurAngleMode));
                }
                else
                {
                    SetX(Math.Sin(Arith.ToRadians(X, CurAngleMode)));
                }
                break;
            case KeyCodes.KEY_COS:
                FinishNumberEntry();
                if (inv)
                {
                    if (X < -1.0 || X > 1.0) SetErrorState();
                    else SetX(Arith.FromRadians(Math.Acos(X), CurAngleMode));
                }
                else
                {
                    SetX(Math.Cos(Arith.ToRadians(X, CurAngleMode)));
                }
                break;
            case KeyCodes.KEY_TAN:
                FinishNumberEntry();
                if (inv)
                {
                    SetX(Arith.FromRadians(Math.Atan(X), CurAngleMode));
                }
                else
                {
                    SetX(Math.Tan(Arith.ToRadians(X, CurAngleMode)));
                }
                break;
            case KeyCodes.KEY_P_TO_R:
                FinishNumberEntry();
                if (inv)
                {
                    // R->P
                    var (r, th) = Arith.RectToPolar(X, T, CurAngleMode);
                    X = r;
                    T = th;
                }
                else
                {
                    // P->R
                    var (xVal, yVal) = Arith.PolarToRect(X, T, CurAngleMode);
                    X = xVal;
                    T = yVal;
                }
                SetX(X);
                break;
            case KeyCodes.KEY_D_MS:
                FinishNumberEntry();
                if (inv)
                    SetX(Arith.FromDms(X));
                else
                    SetX(Arith.ToDms(X));
                break;
            case KeyCodes.KEY_PI:
                FinishNumberEntry();
                SetX(Math.PI);
                break;
            case KeyCodes.KEY_INT:
                FinishNumberEntry();
                if (inv)
                    SetX(X - Math.Truncate(X)); // Fractional part
                else
                    SetX(Math.Truncate(X)); // Integer part
                break;
            case KeyCodes.KEY_ABS:
                FinishNumberEntry();
                SetX(Math.Abs(X));
                break;
            case KeyCodes.KEY_DEG:
                CurAngleMode = ANG_DEG;
                break;
            case KeyCodes.KEY_RAD:
                CurAngleMode = ANG_RAD;
                break;
            case KeyCodes.KEY_GRAD:
                CurAngleMode = ANG_GRAD;
                break;
            case KeyCodes.KEY_ENG:
                CurFormat = inv ? FORMAT_FIXED : FORMAT_ENG;
                SetX(X);
                break;
            case KeyCodes.KEY_INV:
                InvState = !inv;
                break;
            case KeyCodes.KEY_X_GE_T:
                FinishNumberEntry();
                bool geCond = (X >= T) ^ inv;
                ExecuteConditionalJump(geCond);
                break;
            case KeyCodes.KEY_X_EQ_T:
                FinishNumberEntry();
                bool eqCond = (X == T) ^ inv;
                ExecuteConditionalJump(eqCond);
                break;
            case KeyCodes.KEY_NOP:
                break;
            case KeyCodes.KEY_SIGMA_PLUS:
                FinishNumberEntry();
                ExecuteSigmaPlus(inv);
                break;
            case KeyCodes.KEY_MEAN_X:
                FinishNumberEntry();
                ExecuteMean(inv);
                break;
            case KeyCodes.KEY_PRT:
                FinishNumberEntry();
                PrintDisplay();
                break;
            case KeyCodes.KEY_ADV:
                Printer?.Advance();
                break;
            case KeyCodes.KEY_LIST:
                FinishNumberEntry();
                ExecuteList();
                break;
            case KeyCodes.KEY_A:
            case KeyCodes.KEY_B:
            case KeyCodes.KEY_C:
            case KeyCodes.KEY_D:
            case KeyCodes.KEY_E:
            case KeyCodes.KEY_A_PRIME:
            case KeyCodes.KEY_B_PRIME:
            case KeyCodes.KEY_C_PRIME:
            case KeyCodes.KEY_D_PRIME:
            case KeyCodes.KEY_E_PRIME:
                FinishNumberEntry();
                ExecuteSymbolicJump(effKey);
                break;
        }
    }

    private void ProcessDigit(int digit)
    {
        ClearErrorState();
        if (State == EngineState.Result || State == EngineState.Error)
        {
            State = EngineState.Entry;
            DisplayString = digit.ToString() + ".";
            ExponentEntered = false;
        }
        else if (State == EngineState.Entry)
        {
            if (DisplayString == "0." || DisplayString == "-0.")
            {
                DisplayString = (DisplayString.StartsWith("-") ? "-" : "") + digit.ToString() + ".";
            }
            else
            {
                // Mantissa max 10 digits
                int digitsCount = CountMantissaDigits(DisplayString);
                if (digitsCount < 10)
                {
                    int dotIdx = DisplayString.IndexOf('.');
                    DisplayString = DisplayString.Substring(0, dotIdx) + digit.ToString() + ".";
                }
            }
        }
        else if (State == EngineState.DecimalEntry)
        {
            int digitsCount = CountMantissaDigits(DisplayString);
            if (digitsCount < 10)
            {
                DisplayString += digit.ToString();
            }
        }
        else if (State == EngineState.ExponentEntry)
        {
            // Shift exponent digits
            string mantPart = DisplayString.Substring(0, DisplayString.Length - 2);
            char oldLast = DisplayString[DisplayString.Length - 1];
            DisplayString = mantPart + oldLast + digit.ToString();
        }
        NotifyDisplayChanged();
    }

    private void ProcessDot()
    {
        ClearErrorState();
        if (State == EngineState.Result || State == EngineState.Error)
        {
            State = EngineState.DecimalEntry;
            DisplayString = "0.";
            ExponentEntered = false;
        }
        else if (State == EngineState.Entry)
        {
            State = EngineState.DecimalEntry;
        }
        NotifyDisplayChanged();
    }

    private void ProcessPlusMinus()
    {
        if (State == EngineState.Result)
        {
            SetX(-X);
        }
        else if (State == EngineState.ExponentEntry)
        {
            int expSignIdx = DisplayString.Length - 3;
            char sign = DisplayString[expSignIdx] == '-' ? ' ' : '-';
            DisplayString = DisplayString.Substring(0, expSignIdx) + sign + DisplayString.Substring(expSignIdx + 1);
            NotifyDisplayChanged();
        }
        else
        {
            if (DisplayString.StartsWith("-"))
                DisplayString = DisplayString.Substring(1);
            else
                DisplayString = "-" + DisplayString;
            NotifyDisplayChanged();
        }
    }

    private void ProcessEE()
    {
        if (State == EngineState.Result)
        {
            CurFormat = FORMAT_FLOAT;
            SetX(X);
        }
        else if (State is EngineState.Entry or EngineState.DecimalEntry)
        {
            if (!ExponentEntered)
            {
                DisplayString += " 00";
                ExponentEntered = true;
            }
            State = EngineState.ExponentEntry;
            NotifyDisplayChanged();
        }
    }

    private int CountMantissaDigits(string s)
    {
        int count = 0;
        foreach (char c in s)
        {
            if (c is >= '0' and <= '9') count++;
            else if (c == ' ') break;
        }
        return count;
    }

    // Binary Operators & AOS Stack
    private void ProcessBinaryOp(int op)
    {
        FinishNumberEntry();
        while (OpStackCount > 0 && Precedence(OpStack[OpStackCount - 1].Operator) >= Precedence(op))
        {
            DoStackTop();
            if (State == EngineState.Error) return;
        }

        if (OpStackCount < MaxOpStack)
        {
            OpStack[OpStackCount++] = new OpStackEntry(op, X);
            ResetEntry();
        }
        else
        {
            SetErrorState();
        }
    }

    private void ProcessLParen()
    {
        FinishNumberEntry();
        if (OpStackCount < MaxOpStack)
        {
            OpStack[OpStackCount++] = new OpStackEntry(STACKOP_LPAREN, 0.0);
            ResetEntry();
        }
        else
        {
            SetErrorState();
        }
    }

    private void ProcessRParen()
    {
        FinishNumberEntry();
        while (OpStackCount > 0 && OpStack[OpStackCount - 1].Operator != STACKOP_LPAREN)
        {
            DoStackTop();
            if (State == EngineState.Error) return;
        }

        if (OpStackCount > 0 && OpStack[OpStackCount - 1].Operator == STACKOP_LPAREN)
        {
            OpStackCount--;
            SetX(X);
        }
        else
        {
            SetErrorState();
        }
    }

    private void ProcessEquals()
    {
        FinishNumberEntry();
        while (OpStackCount > 0)
        {
            if (OpStack[OpStackCount - 1].Operator == STACKOP_LPAREN)
            {
                OpStackCount--; // Close paren automatically
            }
            else
            {
                DoStackTop();
                if (State == EngineState.Error) return;
            }
        }
        SetX(X);
    }

    private void DoStackTop()
    {
        if (OpStackCount == 0) return;
        var top = OpStack[--OpStackCount];
        switch (top.Operator)
        {
            case STACKOP_ADD:
                X = top.Operand + X;
                break;
            case STACKOP_SUB:
                X = top.Operand - X;
                break;
            case STACKOP_MUL:
                X = top.Operand * X;
                break;
            case STACKOP_DIV:
                if (X == 0.0) SetErrorState();
                else X = top.Operand / X;
                break;
            case STACKOP_MOD:
                if (X == 0.0) SetErrorState();
                else X = Math.IEEERemainder(top.Operand, X);
                break;
            case STACKOP_EXP:
                X = Math.Pow(top.Operand, X);
                break;
            case STACKOP_ROOT:
                if (X == 0.0) SetErrorState();
                else X = Math.Pow(top.Operand, 1.0 / X);
                break;
        }
    }

    private static int Precedence(int op) => op switch
    {
        STACKOP_LPAREN => 0,
        STACKOP_ADD or STACKOP_SUB => 1,
        STACKOP_MUL or STACKOP_DIV or STACKOP_MOD => 2,
        STACKOP_EXP or STACKOP_ROOT => 3,
        _ => -1
    };

    // Memory operations
    private int ResolveReg(int reg, bool ind)
    {
        if (ind)
        {
            if (reg < 0 || reg >= MaxMemories) return -1;
            reg = (int)Math.Truncate(Memories[reg]);
        }
        return reg >= 0 && reg < MaxMemories ? reg : -1;
    }

    public void ExecuteStore(int reg, bool ind)
    {
        int r = ResolveReg(reg, ind);
        if (r >= 0) Memories[r] = X;
        else SetErrorState();
    }

    public void ExecuteRecall(int reg, bool ind)
    {
        int r = ResolveReg(reg, ind);
        if (r >= 0) SetX(Memories[r]);
        else SetErrorState();
    }

    public void ExecuteSum(int reg, bool ind, bool inv)
    {
        int r = ResolveReg(reg, ind);
        if (r >= 0)
        {
            if (inv) Memories[r] -= X;
            else Memories[r] += X;
        }
        else SetErrorState();
    }

    public void ExecutePrd(int reg, bool ind, bool inv)
    {
        int r = ResolveReg(reg, ind);
        if (r >= 0)
        {
            if (inv)
            {
                if (X == 0.0) SetErrorState();
                else Memories[r] /= X;
            }
            else
            {
                Memories[r] *= X;
            }
        }
        else SetErrorState();
    }

    public void ExecuteExchange(int reg, bool ind)
    {
        int r = ResolveReg(reg, ind);
        if (r >= 0)
        {
            (X, Memories[r]) = (Memories[r], X);
            SetX(X);
        }
        else SetErrorState();
    }

    public void ClearMemories()
    {
        Array.Clear(Memories, 0, Memories.Length);
    }

    public void ClearProgram()
    {
        Array.Clear(UserProgram, 0, UserProgram.Length);
        PC = 0;
    }

    public void ClearAll()
    {
        OpStackCount = 0;
        ResetEntry();
    }

    public void ClearEntry()
    {
        ResetEntry();
    }

    // Program Flow & Subroutines
    public void ExecuteGto(int target, bool isSymbolic, bool ind)
    {
        if (ind)
        {
            int r = ResolveReg(target, false);
            if (r >= 0) target = (int)Math.Truncate(Memories[r]);
            else { SetErrorState(); return; }
        }

        if (isSymbolic)
        {
            int addr = FindLabel(target);
            if (addr >= 0) PC = addr;
            else SetErrorState();
        }
        else
        {
            if (target >= 0 && target < GetMaxProg()) PC = target;
            else SetErrorState();
        }
    }

    public void ExecuteSbr(int target, bool isSymbolic, bool ind, bool fromInteractive)
    {
        if (ReturnStackCount < MaxReturnStack)
        {
            ReturnStack[ReturnStackCount++] = new ReturnStackEntry(CurBank, PC, fromInteractive);
            ExecuteGto(target, isSymbolic, ind);
            if (fromInteractive && !IsRunning)
            {
                StartProgram();
            }
        }
        else
        {
            SetErrorState();
        }
    }

    public void ExecuteReturn()
    {
        if (ReturnStackCount > 0)
        {
            var ret = ReturnStack[--ReturnStackCount];
            CurBank = ret.BankNr;
            PC = ret.Addr;
            if (ret.FromInteractive)
            {
                StopProgram();
            }
        }
        else
        {
            StopProgram();
        }
    }

    public void ExecutePgm(int bankNr, bool ind)
    {
        if (ind)
        {
            int r = ResolveReg(bankNr, false);
            if (r >= 0) bankNr = (int)Math.Truncate(Memories[r]);
            else { SetErrorState(); return; }
        }

        if (bankNr >= 0 && bankNr < MaxBanks && Banks[bankNr] != null)
        {
            CurBank = bankNr;
            PC = 0;
        }
        else
        {
            SetErrorState();
        }
    }

    public void ExecuteFix(int decimals)
    {
        CurDecimals = decimals >= 0 && decimals <= 8 ? decimals : -1;
        SetX(X);
    }

    public void ExecuteStFlg(int flag, bool inv)
    {
        if (flag >= 0 && flag < MaxFlags)
            Flags[flag] = !inv;
        else
            SetErrorState();
    }

    public void ExecuteIfFlg(int flag, bool inv)
    {
        if (flag >= 0 && flag < MaxFlags)
        {
            bool cond = Flags[flag] ^ inv;
            ExecuteConditionalJump(cond);
        }
        else
        {
            SetErrorState();
        }
    }

    public void ExecuteDsz(int reg, bool inv)
    {
        if (reg >= 0 && reg < MaxFlags)
        {
            Memories[reg] -= 1.0;
            bool jump = inv ? (Memories[reg] == 0.0) : (Memories[reg] != 0.0);
            ExecuteConditionalJump(jump);
        }
        else
        {
            SetErrorState();
        }
    }

    private void ExecuteConditionalJump(bool cond)
    {
        if (cond)
        {
            DigitsNeeded = 3;
            AccumDigits = 0;
            CollectingForFunction = KeyCodes.KEY_GTO;
            AcceptSymbolic = true;
            AcceptInd = true;
        }
        else
        {
            // Keyboard-only path (program conditionals are decoded inline in
            // ExecuteSingleProgramStep): a false conditional pressed on the
            // keyboard skips the stored target bytes when the pointer is
            // inside a program, mirroring single-step debugging.
            if (PC > 0)
            {
                SkipProgramBranchTarget();
            }
        }
    }

    private void SkipProgramBranchTarget()
    {
        byte[] prog = GetCurProg();
        int max = GetMaxProg();
        if (PC >= max) return;

        byte t1 = prog[PC];
        if (t1 == KeyCodes.KEY_IND || t1 <= 9) // "40 nn" or two-byte "0n nn" target
        {
            PC += Math.Min(2, max - PC);
        }
        else
        {
            PC++;
        }
    }

    public void ExecuteSymbolicJump(int labelKey)
    {
        int addr = FindLabel(labelKey);
        if (addr >= 0)
        {
            ExecuteSbr(labelKey, isSymbolic: true, ind: false, fromInteractive: true);
        }
        else
        {
            SetErrorState();
        }
    }

    private int FindLabel(int labelKey)
    {
        byte[] prog = GetCurProg();
        int max = GetMaxProg();
        for (int i = 0; i < max - 1; ++i)
        {
            if (prog[i] == KeyCodes.KEY_LBL && prog[i + 1] == labelKey)
            {
                return i + 2;
            }
        }
        return -1;
    }

    public byte[] GetCurProg()
    {
        return Banks[CurBank]?.Program ?? UserProgram;
    }

    public int GetMaxProg()
    {
        // The usable range is bounded by the actual bank array: Op 17 can claim more
        // logical steps than the 960-byte bank 0 holds, and library banks are loaded
        // with arrays as short as their file content. Reads must never exceed it.
        // The bound itself is verified in src/TI5X.Verified/partition.dfy.
        return Partition.EffectiveMaxProg(RegOffset, GetCurProg().Length, CurBank);
    }

    // Statistics functions (Σ+, mean, etc.)
    private void ExecuteSigmaPlus(bool inv)
    {
        // Registers used for statistics:
        // R01: Σ y
        // R02: Σ y²
        // R03: n
        // R04: Σ x
        // R05: Σ x²
        // R06: Σ xy
        double sign = inv ? -1.0 : 1.0;
        double xVal = X;
        double yVal = T;

        Memories[1] += sign * yVal;
        Memories[2] += sign * yVal * yVal;
        Memories[3] += sign * 1.0;
        Memories[4] += sign * xVal;
        Memories[5] += sign * xVal * xVal;
        Memories[6] += sign * xVal * yVal;

        SetX(Memories[3]); // Display count n
    }

    private void ExecuteMean(bool inv)
    {
        double n = Memories[3];
        if (n <= 0.0)
        {
            SetErrorState();
            return;
        }

        if (inv)
        {
            // Standard deviation
            if (n <= 1.0)
            {
                SetErrorState();
                return;
            }
            double sx = Math.Sqrt(Math.Max(0, (Memories[5] - (Memories[4] * Memories[4]) / n) / (n - 1.0)));
            double sy = Math.Sqrt(Math.Max(0, (Memories[2] - (Memories[1] * Memories[1]) / n) / (n - 1.0)));
            T = sy;
            SetX(sx);
        }
        else
        {
            // Mean
            double xBar = Memories[4] / n;
            double yBar = Memories[1] / n;
            T = yBar;
            SetX(xBar);
        }
    }

    // Special Op Codes
    public void ExecuteOp(int opCode, bool ind)
    {
        if (ind)
        {
            int r = ResolveReg(opCode, false);
            if (r >= 0) opCode = (int)Math.Truncate(Memories[r]);
            else { SetErrorState(); return; }
        }

        switch (opCode)
        {
            case 0: // Set printer trace mode
                Flags[FLAG_TRACE_PRINT] = true;
                break;
            case 1: // Clear printer register
            case 2:
            case 3:
            case 4:
            case 5:
                ExecutePrintOp(opCode);
                break;
            case 6: // Plot current point
                Printer?.Advance();
                break;
            case 7: // Plot
                Printer?.Advance();
                break;
            case 8: // List labels
                ExecuteListLabels();
                break;
            case 11: // Linear regression
                ExecuteLinReg();
                break;
            case 12: // Trend prediction (x -> y')
                ExecuteTrend();
                break;
            case 13: // Correlation coefficient
                ExecuteCorrelation();
                break;
            case 16: // Display partition
                SetX(RegOffset / 10 * 100.0 + (MaxMemories - RegOffset / 10));
                break;
            case 17: // Repartition program / memories
                ExecuteRepartition();
                break;
            case >= 20 and <= 29: // Increment memory 0..9 and test
                ExecuteIncrementMemory(opCode - 20);
                break;
            case >= 30 and <= 39: // Decrement memory 0..9 and test
                ExecuteDecrementMemory(opCode - 30);
                break;
            default:
                break;
        }
    }

    private void ExecutePrintOp(int op)
    {
        if (op == 0)
        {
            Array.Clear(PrintRegister, 0, PrintRegister.Length);
        }
        else if (op is >= 1 and <= 4)
        {
            // Load 5 characters from X into PrintRegister section (op-1)*5
            long encoded = (long)Math.Truncate(Math.Abs(X));
            int offset = (op - 1) * 5;
            for (int i = 4; i >= 0; --i)
            {
                int code = (int)(encoded % 100);
                encoded /= 100;
                if (offset + i < PrintRegister.Length)
                {
                    PrintRegister[offset + i] = (byte)code;
                }
            }
        }
        else if (op == 5)
        {
            Printer?.Render(PrintRegister);
            Array.Clear(PrintRegister, 0, PrintRegister.Length);
        }
    }

    private void ExecuteListLabels()
    {
        byte[] prog = GetCurProg();
        int max = GetMaxProg();
        for (int i = 0; i < max - 1; ++i)
        {
            if (prog[i] == KeyCodes.KEY_LBL)
            {
                int lbl = prog[i + 1];
                Printer?.PrintLine($"{i:D3}  LBL {KeyCodes.KeyCodeSym(lbl)}");
            }
        }
    }

    private void ExecuteLinReg()
    {
        double n = Memories[3];
        if (n <= 1.0) { SetErrorState(); return; }
        double sumX = Memories[4], sumY = Memories[1];
        double sumX2 = Memories[5], sumXY = Memories[6];
        double denom = n * sumX2 - sumX * sumX;
        if (denom == 0.0) { SetErrorState(); return; }

        double slope = (n * sumXY - sumX * sumY) / denom;
        double intercept = (sumY - slope * sumX) / n;
        T = intercept;
        SetX(slope);
    }

    private void ExecuteTrend()
    {
        double n = Memories[3];
        if (n <= 1.0) { SetErrorState(); return; }
        double sumX = Memories[4], sumY = Memories[1];
        double sumX2 = Memories[5], sumXY = Memories[6];
        double denom = n * sumX2 - sumX * sumX;
        if (denom == 0.0) { SetErrorState(); return; }

        double slope = (n * sumXY - sumX * sumY) / denom;
        double intercept = (sumY - slope * sumX) / n;
        double yHat = slope * X + intercept;
        SetX(yHat);
    }

    private void ExecuteCorrelation()
    {
        double n = Memories[3];
        if (n <= 1.0) { SetErrorState(); return; }
        double sumX = Memories[4], sumY = Memories[1];
        double sumX2 = Memories[5], sumY2 = Memories[2], sumXY = Memories[6];
        double num = n * sumXY - sumX * sumY;
        double den = Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));
        if (den == 0.0) { SetErrorState(); return; }
        SetX(num / den);
    }

    private void ExecuteRepartition()
    {
        int numMemories = (int)Math.Truncate(X);
        if (numMemories is >= 0 and <= 100)
        {
            RegOffset = (100 - numMemories) * 10;
            SetX(X);
        }
        else
        {
            SetErrorState();
        }
    }

    private void ExecuteIncrementMemory(int reg)
    {
        Memories[reg] += 1.0;
        SetX(Memories[reg]);
    }

    private void ExecuteDecrementMemory(int reg)
    {
        Memories[reg] -= 1.0;
        SetX(Memories[reg]);
    }

    public void PrintDisplay()
    {
        Printer?.PrintLine($"{DisplayString,20}");
    }

    public void ExecuteList()
    {
        byte[] prog = GetCurProg();
        int max = GetMaxProg();
        for (int i = 0; i < max; ++i)
        {
            byte code = prog[i];
            Printer?.PrintLine($"{i:D3}  {code:D2} {KeyCodes.KeyCodeSym(code)}");
        }
    }

    // LRN Mode — pending-entry state (see ProcessLrnKey)
    private int _lrnOpcode;       // 0 = no operand pending
    private int _lrnOpcodePos;    // position of the already stored opcode byte
    private int _lrnDigitsNeeded;
    private int _lrnAccum;
    private bool _lrnGotInd;
    private bool _lrnAcceptsInd;
    private int _lrnPhase;        // 0 = first operand, 1 = branch target
    private bool _lrnInvPending;

    private void ResetLrnEntry()
    {
        _lrnOpcode = 0;
        _lrnDigitsNeeded = 0;
        _lrnAccum = 0;
        _lrnGotInd = false;
        _lrnAcceptsInd = false;
        _lrnPhase = 0;
        _lrnInvPending = false;
    }

    private void ProcessLrnKey(int keyCode)
    {
        if (keyCode == KeyCodes.KEY_LRN)
        {
            LrnMode = false;
            ResetLrnEntry();
            SetX(X);
            NotifyStateChanged();
            return;
        }

        if (keyCode == KeyCodes.KEY_SST)
        {
            ResetLrnEntry();
            if (PC < GetMaxProg() - 1) PC++;
            ShowLrnDisplay();
            return;
        }

        if (keyCode == KeyCodes.KEY_BST)
        {
            ResetLrnEntry();
            if (PC > 0) PC--;
            ShowLrnDisplay();
            return;
        }

        if (keyCode == KeyCodes.KEY_INS)
        {
            ResetLrnEntry();
            InsertInstruction();
            ShowLrnDisplay();
            return;
        }

        if (keyCode == KeyCodes.KEY_DEL)
        {
            ResetLrnEntry();
            DeleteInstruction();
            ShowLrnDisplay();
            return;
        }

        if (keyCode == KeyCodes.KEY_INV)
        {
            if (_lrnOpcode != 0) { SetErrorState(); return; } // INV inside an operand is invalid
            _lrnInvPending = !_lrnInvPending; // INV INV cancels
            ShowLrnDisplay();
            return;
        }

        if (keyCode == KeyCodes.KEY_2ND)
        {
            SecondState = !SecondState;
            ShowLrnDisplay();
            return;
        }

        int effKey = keyCode;
        if (SecondState)
        {
            effKey = MapSecond(keyCode);
            SecondState = false;
        }

        bool inv = _lrnInvPending;
        _lrnInvPending = false;

        // A pending INV merges with the next key where a merged code exists;
        // otherwise INV is stored as its own step 22.
        if (inv)
        {
            if (effKey == KeyCodes.KEY_SBR)
            {
                StoreLrnByte(KeyCodes.OP_INV_SBR); // single step 92 = RTN
                ShowLrnDisplay();
                return;
            }
            StoreLrnByte(KeyCodes.KEY_INV);
            if (effKey == KeyCodes.KEY_FIX)
            {
                StoreLrnByte(effKey); // INV Fix has no operand
                ShowLrnDisplay();
                return;
            }
        }

        if (_lrnOpcode != 0)
        {
            ProcessLrnOperandKey(effKey);
            return;
        }

        int digit = KeyToDigit(effKey);
        if (digit >= 0)
        {
            StoreLrnByte(digit); // digits are stored as 00-09
            ShowLrnDisplay();
            return;
        }

        switch (effKey)
        {
            case KeyCodes.KEY_STO or KeyCodes.KEY_RCL or KeyCodes.KEY_SUM or KeyCodes.KEY_EXC
                or KeyCodes.KEY_PRD or KeyCodes.KEY_PGM or KeyCodes.KEY_OP:
                BeginLrnOperand(effKey, 2, acceptsInd: true);
                break;
            case KeyCodes.KEY_FIX:
            case KeyCodes.KEY_ST_FLG:
                BeginLrnOperand(effKey, 1, acceptsInd: false);
                break;
            case KeyCodes.KEY_IF_FLG:
            case KeyCodes.KEY_DSZ:
                // flag/register digit first, then a branch target
                BeginLrnOperand(effKey, 1, acceptsInd: false);
                _lrnPhase = 0; // target phase entered after the digit completes
                break;
            case KeyCodes.KEY_GTO or KeyCodes.KEY_SBR or KeyCodes.KEY_X_EQ_T or KeyCodes.KEY_X_GE_T:
                BeginLrnTarget(effKey);
                break;
            default:
                StoreLrnByte(effKey);
                break;
        }
        ShowLrnDisplay();
    }

    private void BeginLrnOperand(int op, int digits, bool acceptsInd)
    {
        int pos = PC;
        if (!StoreLrnByte(op))
        {
            return; // no room (or read-only bank): no pending operand state
        }
        _lrnOpcode = op;
        _lrnOpcodePos = pos;
        _lrnDigitsNeeded = digits;
        _lrnAccum = 0;
        _lrnGotInd = false;
        _lrnAcceptsInd = acceptsInd;
        _lrnPhase = 0;
    }

    private void BeginLrnTarget(int op)
    {
        int pos = PC;
        if (!StoreLrnByte(op))
        {
            return; // no room (or read-only bank): no pending target state
        }
        _lrnOpcode = op;
        _lrnOpcodePos = pos;
        _lrnDigitsNeeded = 3;    // three-digit absolute address
        _lrnAccum = 0;
        _lrnGotInd = false;
        _lrnAcceptsInd = true;
        _lrnPhase = 1;
    }

    private void ProcessLrnOperandKey(int effKey)
    {
        // Symbolic label completes a branch target.
        if (_lrnPhase == 1 && effKey is >= KeyCodes.KEY_A and <= KeyCodes.KEY_E
                                     or >= KeyCodes.KEY_E_PRIME and <= KeyCodes.KEY_D_PRIME)
        {
            StoreLrnByte(effKey);
            ResetLrnEntry();
            ShowLrnDisplay();
            return;
        }

        if (effKey == KeyCodes.KEY_IND && _lrnAcceptsInd && !_lrnGotInd)
        {
            _lrnGotInd = true;
            _lrnAccum = 0;
            _lrnDigitsNeeded = 2; // indirect register takes two digits
            ShowLrnDisplay();
            return;
        }

        int digit = KeyToDigit(effKey);
        if (digit < 0)
        {
            SetErrorState();
            ResetLrnEntry();
            return;
        }

        _lrnAccum = _lrnAccum * 10 + digit;
        _lrnDigitsNeeded--;
        if (_lrnDigitsNeeded > 0)
        {
            ShowLrnDisplay();
            return;
        }
        CompleteLrnOperand();
    }

    private void CompleteLrnOperand()
    {
        int op = _lrnOpcode;
        int val = _lrnAccum;

        if (_lrnPhase == 0)
        {
            if (op == KeyCodes.KEY_IF_FLG || op == KeyCodes.KEY_DSZ)
            {
                // first operand written; the branch target follows
                if (!StoreLrnByte(val))
                {
                    ResetLrnEntry(); // boundary error already signaled
                    ShowLrnDisplay();
                    return;
                }
                _lrnPhase = 1;
                _lrnDigitsNeeded = 3;
                _lrnAccum = 0;
                _lrnGotInd = false;
                _lrnAcceptsInd = true;
                ShowLrnDisplay();
                return;
            }
            if (_lrnGotInd)
            {
                // replace the stored opcode with the merged IND code
                StoreLrnByteAt(_lrnOpcodePos, MergedCodeFor(op));
                StoreLrnByte(val);
            }
            else
            {
                StoreLrnByte(val);
            }
        }
        else
        {
            if (op == KeyCodes.KEY_GTO && _lrnGotInd)
            {
                StoreLrnByteAt(_lrnOpcodePos, KeyCodes.OP_GTO_IND); // merged "83 nn"
                StoreLrnByte(val);
            }
            else if (_lrnGotInd)
            {
                // SBR / x=t / x>=t / If flg / Dsz targets use the expanded "40 nn"
                if (StoreLrnByte(KeyCodes.KEY_IND))
                {
                    StoreLrnByte(val);
                }
            }
            else
            {
                // three-digit absolute address as "0n nn"
                if (StoreLrnByte(val / 100))
                {
                    StoreLrnByte(val % 100);
                }
            }
        }
        ResetLrnEntry();
        ShowLrnDisplay();
    }

    /// <summary>
    /// Stores one instruction byte in LRN mode. Requires room to advance the
    /// step pointer: a byte that would be written without advancing would be
    /// clobbered by the next byte of the same instruction, so the boundary
    /// signals a defined error instead of silently corrupting the program.
    /// </summary>
    private bool StoreLrnByte(int code)
    {
        if (CurBank != 0) return false; // library banks are read-only (documented)
        if (PC >= GetMaxProg() - 1)
        {
            SetErrorState();
            return false;
        }
        UserProgram[PC] = (byte)code;
        PC++;
        NotifyStateChanged();
        return true;
    }

    private void StoreLrnByteAt(int pos, int code)
    {
        // Retroactive merged-code rewrite: only for the editable bank 0.
        if (CurBank == 0 && pos >= 0 && pos < GetMaxProg())
        {
            UserProgram[pos] = (byte)code;
        }
    }

    private static int MergedCodeFor(int op) => op switch
    {
        KeyCodes.KEY_STO => KeyCodes.OP_STO_IND,
        KeyCodes.KEY_RCL => KeyCodes.OP_RCL_IND,
        KeyCodes.KEY_SUM => KeyCodes.OP_SUM_IND,
        KeyCodes.KEY_EXC => KeyCodes.OP_EXC_IND,
        KeyCodes.KEY_PRD => KeyCodes.OP_PRD_IND,
        KeyCodes.KEY_PGM => KeyCodes.OP_PGM_IND,
        KeyCodes.KEY_OP => KeyCodes.OP_OP_IND,
        _ => op
    };

    private void InsertInstruction()
    {
        if (CurBank == 0 && PC < MaxProgram)
        {
            for (int i = MaxProgram - 1; i > PC; --i)
                UserProgram[i] = UserProgram[i - 1];
            UserProgram[PC] = (byte)KeyCodes.KEY_NOP;
        }
    }

    private void DeleteInstruction()
    {
        if (CurBank == 0 && PC < MaxProgram)
        {
            for (int i = PC; i < MaxProgram - 1; ++i)
                UserProgram[i] = UserProgram[i + 1];
            UserProgram[MaxProgram - 1] = 0;
        }
    }

    public void ShowLrnDisplay()
    {
        byte[] prog = GetCurProg();
        byte opCode = PC < prog.Length ? prog[PC] : (byte)0;
        DisplayString = $"{PC:D3} {opCode:D2}";
        NotifyDisplayChanged();
    }

    // Program Execution Loop
    public void StartProgram()
    {
        if (IsRunning) return;
        var cts = new CancellationTokenSource();
        int generation = ++_runGeneration;
        _runCts = cts;
        IsRunning = true;
        NotifyStateChanged();

        _runTask = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    // Ownership check: a newer run or an explicit stop must end this loop.
                    if (!IsRunning || generation != _runGeneration || cts.IsCancellationRequested)
                        break;

                    bool stepOk = ExecuteSingleProgramStep();
                    if (!stepOk)
                        break;

                    if (PauseRequested)
                    {
                        PauseRequested = false;
                        await Task.Delay(500, cts.Token);
                    }
                    else if (Flags[FLAG_SLOW_EXECUTION])
                    {
                        await Task.Delay(50, cts.Token);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Only the owning run may touch the shared state, otherwise a stale
                // task could cancel a run that was started right after a stop.
                if (generation == _runGeneration)
                {
                    IsRunning = false;
                    NotifyStateChanged();
                    if (State != EngineState.Error)
                    {
                        SetX(X);
                    }
                }
            }
        });
    }

    public void StopProgram()
    {
        IsRunning = false;
        _runCts?.Cancel();
        NotifyStateChanged();
    }

    public void StepProgram()
    {
        if (IsRunning) return; // SST from the debugger must not race the background loop
        ExecuteSingleProgramStep();
    }

    private readonly record struct BranchTarget(int Value, bool Symbolic, bool Indirect);

    /// <summary>
    /// Reads a branch target (GTO/SBR/conditionals) from the program bytes.
    /// Forms: "1x" symbolic label, "0n nn" three-digit address, "40 nn" indirect,
    /// or a single-byte absolute address (emulator convenience form 20-99).
    /// </summary>
    private BranchTarget? ReadBranchTarget(byte[] prog, int max)
    {
        if (PC >= max) return null;
        int t1 = prog[PC++];
        if (t1 == KeyCodes.KEY_IND)
        {
            if (PC >= max) return null;
            return new BranchTarget(prog[PC++], Symbolic: false, Indirect: true);
        }
        if (t1 is >= 10 and <= 19)
        {
            return new BranchTarget(t1, Symbolic: true, Indirect: false);
        }
        if (t1 <= 9)
        {
            if (PC >= max) return null;
            return new BranchTarget(t1 * 100 + prog[PC++], Symbolic: false, Indirect: false);
        }
        return new BranchTarget(t1, Symbolic: false, Indirect: false);
    }

    /// <summary>
    /// Reads a register operand byte; the expanded "40 nn" form marks indirect
    /// addressing. Byte 21 (the 2nd key code) is not a valid operand.
    /// </summary>
    private (int Value, bool Indirect)? ReadRegisterOperand(byte[] prog, int max)
    {
        if (PC >= max) return null;
        int b = prog[PC++];
        if (b == KeyCodes.KEY_IND)
        {
            if (PC >= max) return null;
            return (prog[PC++], true);
        }
        if (b == KeyCodes.KEY_2ND)
        {
            return null;
        }
        return (b, false);
    }

    /// <summary>
    /// Executes a merged program opcode (62-64, 72-74, 83, 84 = operation with IND,
    /// 92 = RTN). On the keyboard these codes are digit keys; in a program they have
    /// their own meaning, which is why they are intercepted before key dispatch.
    /// </summary>
    private bool ExecuteMergedCode(byte op, byte[] prog, int max)
    {
        if (op == KeyCodes.OP_INV_SBR) // 92 = RTN
        {
            ExecuteReturn();
            return State != EngineState.Error;
        }

        if (PC >= max)
        {
            SetErrorState();
            return false;
        }
        int nn = prog[PC++];
        switch (op)
        {
            case KeyCodes.OP_PGM_IND: ExecutePgm(nn, ind: true); break;
            case KeyCodes.OP_EXC_IND: ExecuteExchange(nn, ind: true); break;
            case KeyCodes.OP_PRD_IND: ExecutePrd(nn, ind: true, inv: false); break;
            case KeyCodes.OP_STO_IND: ExecuteStore(nn, ind: true); break;
            case KeyCodes.OP_RCL_IND: ExecuteRecall(nn, ind: true); break;
            case KeyCodes.OP_SUM_IND: ExecuteSum(nn, ind: true, inv: false); break;
            case KeyCodes.OP_GTO_IND: ExecuteGto(nn, isSymbolic: false, ind: true); break;
            case KeyCodes.OP_OP_IND: ExecuteOp(nn, ind: true); break;
        }
        return State != EngineState.Error;
    }

    private bool ExecuteSingleProgramStep()
    {
        byte[] prog = GetCurProg();
        int max = GetMaxProg();
        if (PC < 0 || PC >= max)
        {
            SetErrorState();
            return false;
        }

        byte op = prog[PC++];

        // R/S stops the program; a number entered just before it is kept.
        if (op == KeyCodes.KEY_R_S)
        {
            FinishNumberEntry();
            return false;
        }

        if (op == KeyCodes.KEY_PAUSE)
        {
            PauseRequested = true;
            return true;
        }

        if (op == KeyCodes.KEY_LBL)
        {
            if (PC < max) PC++; // skip label identifier
            return true;
        }

        if (KeyCodes.IsMergedCode(op))
        {
            return ExecuteMergedCode(op, prog, max);
        }

        // HIR (hidden registers): the original semantics are not documented;
        // a defined no-op instead of a misinterpreted digit.
        if (op == KeyCodes.OP_HIR)
        {
            return true;
        }

        switch (op)
        {
            case >= 10 and <= 19: // label byte A-E' in a program is a subroutine call
                {
                    // Program-context call: the return must continue the run
                    // instead of stopping it (keyboard A-E' starts and stops).
                    ExecuteSbr(op, isSymbolic: true, ind: false, fromInteractive: false);
                    return State != EngineState.Error;
                }
            case KeyCodes.KEY_GTO:
                {
                    var target = ReadBranchTarget(prog, max);
                    if (target == null) { SetErrorState(); return false; }
                    ExecuteGto(target.Value.Value, target.Value.Symbolic, target.Value.Indirect);
                    return State != EngineState.Error;
                }
            case KeyCodes.KEY_SBR:
                {
                    var target = ReadBranchTarget(prog, max);
                    if (target == null) { SetErrorState(); return false; }
                    ExecuteSbr(target.Value.Value, target.Value.Symbolic, target.Value.Indirect, fromInteractive: false);
                    return State != EngineState.Error;
                }
            case KeyCodes.KEY_X_GE_T:
            case KeyCodes.KEY_X_EQ_T:
                {
                    var target = ReadBranchTarget(prog, max);
                    if (target == null) { SetErrorState(); return false; }
                    FinishNumberEntry();
                    bool inv = InvState;
                    InvState = false;
                    bool cond = op == KeyCodes.KEY_X_GE_T ? (X >= T) : (X == T);
                    if (cond ^ inv)
                    {
                        ExecuteGto(target.Value.Value, target.Value.Symbolic, target.Value.Indirect);
                    }
                    return State != EngineState.Error;
                }
            case KeyCodes.KEY_ST_FLG:
                {
                    if (PC >= max) { SetErrorState(); return false; }
                    int flag = prog[PC++];
                    bool inv = InvState;
                    InvState = false;
                    ExecuteStFlg(flag, inv);
                    return State != EngineState.Error;
                }
            case KeyCodes.KEY_IF_FLG:
                {
                    if (PC >= max) { SetErrorState(); return false; }
                    int flag = prog[PC++];
                    var target = ReadBranchTarget(prog, max);
                    if (target == null || flag > 9) { SetErrorState(); return false; }
                    bool inv = InvState;
                    InvState = false;
                    if (Flags[flag] ^ inv)
                    {
                        ExecuteGto(target.Value.Value, target.Value.Symbolic, target.Value.Indirect);
                    }
                    return State != EngineState.Error;
                }
            case KeyCodes.KEY_DSZ:
                {
                    if (PC >= max) { SetErrorState(); return false; }
                    int reg = prog[PC++];
                    var target = ReadBranchTarget(prog, max);
                    if (target == null || reg > 9) { SetErrorState(); return false; }
                    bool inv = InvState;
                    InvState = false;
                    Memories[reg] -= 1.0;
                    bool jump = inv ? (Memories[reg] == 0.0) : (Memories[reg] != 0.0);
                    if (jump)
                    {
                        ExecuteGto(target.Value.Value, target.Value.Symbolic, target.Value.Indirect);
                    }
                    return State != EngineState.Error;
                }
            case KeyCodes.KEY_STO or KeyCodes.KEY_RCL or KeyCodes.KEY_SUM or KeyCodes.KEY_EXC
                or KeyCodes.KEY_PRD or KeyCodes.KEY_PGM or KeyCodes.KEY_OP:
                {
                    var operand = ReadRegisterOperand(prog, max);
                    if (operand == null) { SetErrorState(); return false; }
                    bool inv = InvState;
                    InvState = false;
                    int val = operand.Value.Value;
                    bool ind = operand.Value.Indirect;
                    FinishNumberEntry();
                    switch (op)
                    {
                        case KeyCodes.KEY_STO: ExecuteStore(val, ind); break;
                        case KeyCodes.KEY_RCL: ExecuteRecall(val, ind); break;
                        case KeyCodes.KEY_SUM: ExecuteSum(val, ind, inv); break;
                        case KeyCodes.KEY_EXC: ExecuteExchange(val, ind); break;
                        case KeyCodes.KEY_PRD: ExecutePrd(val, ind, inv); break;
                        case KeyCodes.KEY_PGM: ExecutePgm(val, ind); break;
                        case KeyCodes.KEY_OP: ExecuteOp(val, ind); break;
                    }
                    return State != EngineState.Error;
                }
            case KeyCodes.KEY_FIX:
                {
                    bool invFix = InvState;
                    InvState = false;
                    if (invFix)
                    {
                        // INV Fix clears the decimal setting and takes no operand
                        // byte; the LRN writer stores it as "22 58" (see 15.2).
                        CurDecimals = -1;
                        SetX(X);
                        return State != EngineState.Error;
                    }
                    if (PC >= max) { SetErrorState(); return false; }
                    int decimals = prog[PC++];
                    ExecuteFix(decimals);
                    return State != EngineState.Error;
                }
        }

        // Remaining opcodes behave like interactive keys (operators, functions,
        // digits, ...).
        ExecuteKeyInteractive(op);
        return State != EngineState.Error;
    }

    private static int MapSecond(int key) => key switch
    {
        KeyCodes.KEY_A => KeyCodes.KEY_A_PRIME,
        KeyCodes.KEY_B => KeyCodes.KEY_B_PRIME,
        KeyCodes.KEY_C => KeyCodes.KEY_C_PRIME,
        KeyCodes.KEY_D => KeyCodes.KEY_D_PRIME,
        KeyCodes.KEY_E => KeyCodes.KEY_E_PRIME,
        KeyCodes.KEY_LNX => KeyCodes.KEY_LOG,
        KeyCodes.KEY_CE => KeyCodes.KEY_CP,
        KeyCodes.KEY_LRN => KeyCodes.KEY_PGM,
        KeyCodes.KEY_X_SWAP_T => KeyCodes.KEY_P_TO_R,
        KeyCodes.KEY_X_SQUARED => KeyCodes.KEY_SIN,
        KeyCodes.KEY_SQRT_X => KeyCodes.KEY_COS,
        KeyCodes.KEY_RECIPROCAL => KeyCodes.KEY_TAN,
        KeyCodes.KEY_SST => KeyCodes.KEY_INS,
        KeyCodes.KEY_STO => KeyCodes.KEY_CMS,
        KeyCodes.KEY_RCL => KeyCodes.KEY_EXC,
        KeyCodes.KEY_SUM => KeyCodes.KEY_PRD,
        KeyCodes.KEY_Y_TO_X => KeyCodes.KEY_IND,
        KeyCodes.KEY_BST => KeyCodes.KEY_DEL,
        KeyCodes.KEY_EE => KeyCodes.KEY_ENG,
        KeyCodes.KEY_LPAREN => KeyCodes.KEY_FIX,
        KeyCodes.KEY_RPAREN => KeyCodes.KEY_INT,
        KeyCodes.KEY_DIV => KeyCodes.KEY_ABS,
        KeyCodes.KEY_GTO => KeyCodes.KEY_PAUSE,
        KeyCodes.KEY_7 => KeyCodes.KEY_X_EQ_T,
        KeyCodes.KEY_8 => KeyCodes.KEY_NOP,
        KeyCodes.KEY_9 => KeyCodes.KEY_OP,
        KeyCodes.KEY_MUL => KeyCodes.KEY_DEG,
        KeyCodes.KEY_SBR => KeyCodes.KEY_LBL,
        KeyCodes.KEY_4 => KeyCodes.KEY_X_GE_T,
        KeyCodes.KEY_5 => KeyCodes.KEY_SIGMA_PLUS,
        KeyCodes.KEY_6 => KeyCodes.KEY_MEAN_X,
        KeyCodes.KEY_SUB => KeyCodes.KEY_RAD,
        KeyCodes.KEY_RST => KeyCodes.KEY_ST_FLG,
        KeyCodes.KEY_1 => KeyCodes.KEY_IF_FLG,
        KeyCodes.KEY_2 => KeyCodes.KEY_D_MS,
        KeyCodes.KEY_3 => KeyCodes.KEY_PI,
        KeyCodes.KEY_ADD => KeyCodes.KEY_GRAD,
        KeyCodes.KEY_0 => KeyCodes.KEY_DSZ,
        KeyCodes.KEY_DOT => KeyCodes.KEY_ADV,
        KeyCodes.KEY_PLUS_MINUS => KeyCodes.KEY_PRT,
        KeyCodes.KEY_EQUALS => KeyCodes.KEY_LIST,
        _ => key
    };

    private static int KeyToDigit(int key) => key switch
    {
        >= 0 and <= 9 => key,
        KeyCodes.KEY_0 => 0,
        KeyCodes.KEY_1 => 1,
        KeyCodes.KEY_2 => 2,
        KeyCodes.KEY_3 => 3,
        KeyCodes.KEY_4 => 4,
        KeyCodes.KEY_5 => 5,
        KeyCodes.KEY_6 => 6,
        KeyCodes.KEY_7 => 7,
        KeyCodes.KEY_8 => 8,
        KeyCodes.KEY_9 => 9,
        _ => -1
    };

    private void NotifyDisplayChanged() => DisplayChanged?.Invoke();
    private void NotifyStateChanged() => StateChanged?.Invoke();
}
