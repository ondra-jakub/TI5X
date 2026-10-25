namespace TI5X.Avalonia.Core;

// Hand port of the verified TI-59 printer model TMC0251
// (ti59-dafny/dfy/TI59/Print.dfy, which is a faithful port of the C oracle
// ti59-dafny/oracle/TMS0500-simulator/print.c). This file must not use any
// Dafny types; TI5X.Core does not reference TI5X.Verified (per the Partition
// integration pattern). Consistency with the generated translation is proven
// by Ti59PrintModelTests in tests/TI5X.Core.Tests (which reference both
// projects); the oracle tape fixture tests/traces/ml01print.tape.txt is the
// external reference.

public enum Ti59PrintEventKind
{
    Char,       // print.c:175 LOG("PRINT_CHAR[%d]='%c' ")
    Func,       // print.c:191,201 LOG("PRINT_FUNC[%d]='%.3s' ")
    NotFound,   // print.c:196 LOG("PRINT[%d]='%d not fund' ")
    Clear,      // print.c:209 LOG("PRINT_CLEAR[%d]='%.20s' ")
    Print       // print.c:232 LOG("PRINT[%d]='%.20s' ")
}

// One log() of the C simulator; Head is the head value shown in the log
// (Char/NotFound log the old head before the step, Func logs the new head
// after 3 steps, Clear logs the pre-clear buffer, Print logs the tape row).
public sealed record Ti59PrintEvent(
    Ti59PrintEventKind Kind,
    int Head,
    string Str,
    int Code)
{
    public static Ti59PrintEvent Char(int head, char ch) =>
        new(Ti59PrintEventKind.Char, head, ch.ToString(), ch);
    public static Ti59PrintEvent Func(int head, string str) =>
        new(Ti59PrintEventKind.Func, head, str, 0);
    public static Ti59PrintEvent NotFound(int head, int code) =>
        new(Ti59PrintEventKind.NotFound, head, string.Empty, code);
    public static Ti59PrintEvent Clear(int head, string buf) =>
        new(Ti59PrintEventKind.Clear, head, buf, 0);
    public static Ti59PrintEvent Print(int head, string buf) =>
        new(Ti59PrintEventKind.Print, head, buf, 0);
}

public sealed class Ti59PrintMachine
{
    // print.c:18 BUFFER_SIZE (20 znaku, buffer[20] = NUL v C se neresi)
    public const int BufferSize = 20;
    public const int HeadInit = 5;   // print.c:252 head = 5

    // Sbernicove op kody print chipu (print.c: maska (irg & 0xFF0F) == 0x0A08)
    public const ushort OpLoadChar = 0x0A68;  // print.c:169-178
    public const ushort OpLoadFunc = 0x0A78;  // print.c:179-205
    public const ushort OpClear = 0x0A88;     // print.c:206-212
    public const ushort OpStep = 0x0A98;      // print.c:213-224 (busy mrtva)
    public const ushort OpPrint = 0x0AA8;     // print.c:225-233
    public const ushort OpAdvance = 0x0AB8;   // print.c:234-238

    private char[] _buffer = CreateInitBuffer();
    private int _head = HeadInit;
    private readonly List<string> _tape = new();
    private readonly List<Ti59PrintEvent> _events = new();

    public IReadOnlyList<string> Tape => _tape;
    public IReadOnlyList<Ti59PrintEvent> Events => _events;
    public int Head => _head;
    public string BufferString => new string(_buffer);

    private static char[] CreateInitBuffer()
    {
        // print.c:251-254 memset 'X' (pri bootu tiskarny)
        var buf = new char[BufferSize];
        Array.Fill(buf, 'X');
        return buf;
    }

    // print.c:155-160 print_step: head 0 wrapuje na 19, jinak -- (head jevi
    // zprava doleva; ROM proto naplni radek obracenym poradem znaku)
    public static int HeadStep(int head) => head == 0 ? BufferSize - 1 : head - 1;

    // print.c:36-45 print_font - TMC0251 (TI-59): 64 znaku po 8, index =
    // 6bitovy kod (ext[3-8]); font[0]=' ', vsechny znaky unikatni.
    public static readonly char[] FontTable =
    [
        ' ', '0', '1', '2', '3', '4', '5', '6',
        '7', '8', '9', 'A', 'B', 'C', 'D', 'E',
        '-', 'F', 'G', 'H', 'I', 'J', 'K', 'L',
        'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
        '.', 'U', 'V', 'W', 'X', 'Y', 'Z', '+',
        'x', '*', 's', 'p', 'e', '(', ')', ',',
        '^', '%', '|', '/', '=', '\'', '#', '~',
        'z', '?', ':', '!', ']', '"', '[', '$'
    ];

    // print.c:74-83 print_font_sr60 - TMC0254 (SR-60); na TI-59 maskou
    // odmitnuto (0x0A06 != 0x0A08), portovano pro uplnost.
    public static readonly char[] FontSr60Table =
    [
        ' ', '0', '1', '2', '3', '4', '5', '6',
        '7', '8', '9', '.', 'z', '#', '!', '"',
        '=', '-', '+', ':', 'x', '\'', '~', ')',
        's', '^', '$', '[', '/', '|', 'e', ']',
        'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
        'Y', 'Z', '*', 'p', '?', ',', '%', '(',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
        'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P'
    ];

    // print.c:86-130 print_func: 39 polozek; v C na konec skenu terminator
    // {0, {0}} (str[0] == NUL) - tady vzdy presne 39 (zadna za nim).
    public static readonly (byte Code, string Str)[] FuncTable =
    [
        (0x00, "   "), (0x11, " = "), (0x12, " - "), (0x13, " + "),
        (0x16, " / "), (0x17, " x "), (0x1A, "xsY"), (0x1B, "Y^x"),
        (0x21, "CLR"), (0x22, "INV"), (0x23, "DPT"), (0x26, "CE "),
        (0x27, "+/-"), (0x2D, "EE "), (0x31, "e^x"), (0x33, "x^2"),
        (0x36, "1/x"), (0x3C, "sX "), (0x3D, "X|Y"), (0x51, "LNX"),
        (0x53, "PRM"), (0x54, " % "), (0x56, "COS"), (0x57, "SIN"),
        (0x5D, "TAN"), (0x61, "SUM"), (0x66, "STO"), (0x67, "pi "),
        (0x68, "RCL"), (0x69, "$+ "), (0x70, "ERR"), (0x71, " ( "),
        (0x72, " ) "), (0x73, "LRN"), (0x74, "RUN"), (0x76, "HLT"),
        (0x78, "STP"), (0x7A, "GTO"), (0x7C, "IF ")
    ];

    // print.c:185-194 first-match sken (C smycka po terminator)
    public static bool FuncLookup(byte code, out string mnemonic)
    {
        foreach (var (funcCode, str) in FuncTable)
        {
            if (funcCode == code)
            {
                mnemonic = str;
                return true;
            }
        }
        mnemonic = string.Empty;
        return false;
    }

    // %.20s na buffer (char[21], buffer[20] vzdy NUL) = vzdy 20 znaku
    public static string BufStr20(ReadOnlySpan<char> buffer) =>
        new string(buffer[..BufferSize]);

    // [print.c:169-178] 0x0A68: buffer[head] = font[ext[3-8]], LOG stareho
    // head, print_step. (code 0..63; jine C zahodit nemuze - 6 bitu)
    public void LoadChar(byte code)
    {
        var ch = FontTable[code];
        _events.Add(Ti59PrintEvent.Char(_head, ch));
        _buffer[_head] = ch;
        _head = HeadStep(_head);
    }

    // [print.c:179-205] 0x0A78: 3 znaky str[2..0] (zprava doleva), LOG po
    // 3 krocich (novy head); nenalezeno: LOG "not fund" (stary head), 3x '?',
    // LOG "???"
    public void LoadFunc(byte code)
    {
        if (FuncLookup(code, out var mnem))
        {
            for (int j = 2; j >= 0; --j)
            {
                _buffer[_head] = mnem[j];
                _head = HeadStep(_head);
            }
            _events.Add(Ti59PrintEvent.Func(_head, mnem));
        }
        else
        {
            _events.Add(Ti59PrintEvent.NotFound(_head, code));
            for (int j = 0; j < 3; ++j)
            {
                _buffer[_head] = '?';
                _head = HeadStep(_head);
            }
            _events.Add(Ti59PrintEvent.Func(_head, "???"));
        }
    }

    // [print.c:206-212] 0x0A88: LOG pred memset; buffer = mezery, head = 19
    public void Clear()
    {
        _events.Add(Ti59PrintEvent.Clear(_head, BufStr20(_buffer)));
        Array.Fill(_buffer, ' ');
        _head = BufferSize - 1;
    }

    // [print.c:213-224] 0x0A98: busy vetev mrtva (busy v C nikdy neni
    // nastaveno) -> vzdy print_step, bez udalosti
    public void Step() => _head = HeadStep(_head);

    // [print.c:225-233] 0x0AA8: radek pasky = buffer, bez zmeny stavu
    public void PrintLine()
    {
        var line = BufStr20(_buffer);
        _tape.Add(line);
        _events.Add(Ti59PrintEvent.Print(_head, line));
    }

    // [print.c:234-238] 0x0AB8: display_print("") = prazdny radek pasky,
    // bez udalosti (koment C: "we advance one line instead of half")
    public void Advance() => _tape.Add(string.Empty);

    // print.c:251-254 print_init: buffer 'X' 20 znaku, head = 5 (+ paska/
    // udalosti v runneru vznikaji az behem behu)
    public void Reset()
    {
        _buffer = CreateInitBuffer();
        _head = HeadInit;
        _tape.Clear();
        _events.Clear();
    }
}