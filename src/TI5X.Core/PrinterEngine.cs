using System.Text;

namespace TI5X.Avalonia.Core;

// Virtual TI-59 printer (PC-100A/TMC0251). All print paths go through the
// verified model port Ti59PrintMachine (font TMC0251, print_func mnemonics,
// 20-character buffer, head mechanics identical to the C oracle print.c).
// The public API and the UI surface (TapeLines, TapeChanged, MaxLines) are
// unchanged; the tape row content is now the authentic 20-column line coming
// out of the model (including unprinted columns as spaces).
//
// Documentation of limits: print paths driven by TI-59 ROM programs (e.g. the
// ML-01 "MASTER" diagnostic tape) are NOT reproduced by the app - the app
// does not execute the real ROM (verified model integration of the printer
// is the source for the app paths only; see docs/ODCHYLKY_A_OMEZENI.md).
public class PrinterEngine
{
    public const int CharColumns = 20;
    public const int CharWidth = 5;
    public const int CharHeight = 7;
    // 1000 lines: a full List of the default 960-step user partition fits on
    // the tape without trimming; the model keeps the whole tape regardless.
    public const int MaxLines = 1000;

    // 5x7 dot matrix font for the 64 PC-100A characters
    public static readonly byte[][] Chars =
    [
        [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00], /*00 space*/
        [0x0e, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0e], /*01 0*/
        [0x04, 0x06, 0x04, 0x04, 0x04, 0x04, 0x0e], /*02 1*/
        [0x0e, 0x11, 0x10, 0x0c, 0x02, 0x01, 0x1f], /*03 2*/
        [0x0e, 0x11, 0x10, 0x0c, 0x10, 0x11, 0x0e], /*04 3*/
        [0x08, 0x0c, 0x0a, 0x09, 0x1f, 0x08, 0x08], /*05 4*/
        [0x1f, 0x01, 0x0f, 0x10, 0x10, 0x11, 0x0e], /*06 5*/
        [0x0c, 0x02, 0x01, 0x0f, 0x11, 0x11, 0x0e], /*07 6*/
        [0x1f, 0x10, 0x08, 0x04, 0x02, 0x02, 0x02], /*10 7*/
        [0x0e, 0x11, 0x11, 0x0e, 0x11, 0x11, 0x0e], /*11 8*/
        [0x0e, 0x11, 0x11, 0x1e, 0x10, 0x08, 0x06], /*12 9*/
        [0x0e, 0x11, 0x11, 0x1f, 0x11, 0x11, 0x11], /*13 A*/
        [0x0f, 0x11, 0x11, 0x0f, 0x11, 0x11, 0x0f], /*14 B*/
        [0x0e, 0x11, 0x01, 0x01, 0x01, 0x11, 0x0e], /*15 C*/
        [0x0f, 0x12, 0x12, 0x12, 0x12, 0x12, 0x0f], /*16 D*/
        [0x1f, 0x01, 0x01, 0x0f, 0x01, 0x01, 0x1f], /*17 E*/
        [0x00, 0x00, 0x00, 0x1f, 0x00, 0x00, 0x00], /*20 -*/
        [0x1f, 0x01, 0x01, 0x0f, 0x01, 0x01, 0x01], /*21 F*/
        [0x0e, 0x11, 0x01, 0x01, 0x19, 0x11, 0x1e], /*22 G*/
        [0x11, 0x11, 0x11, 0x1f, 0x11, 0x11, 0x11], /*23 H*/
        [0x0e, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0e], /*24 I*/
        [0x10, 0x10, 0x10, 0x10, 0x10, 0x11, 0x0e], /*25 J*/
        [0x11, 0x09, 0x05, 0x03, 0x05, 0x09, 0x11], /*26 K*/
        [0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x1f], /*27 L*/
        [0x11, 0x1b, 0x15, 0x15, 0x11, 0x11, 0x11], /*30 M*/
        [0x11, 0x11, 0x13, 0x15, 0x19, 0x11, 0x11], /*31 N*/
        [0x1f, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1f], /*32 O*/
        [0x0f, 0x11, 0x11, 0x0f, 0x01, 0x01, 0x01], /*33 P*/
        [0x0e, 0x11, 0x11, 0x11, 0x15, 0x19, 0x1e], /*34 Q*/
        [0x0f, 0x11, 0x11, 0x0f, 0x05, 0x09, 0x11], /*35 R*/
        [0x0e, 0x11, 0x01, 0x0e, 0x10, 0x11, 0x0e], /*36 S*/
        [0x1f, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04], /*37 T*/
        [0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0x06], /*40 .*/
        [0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0e], /*41 U*/
        [0x11, 0x11, 0x11, 0x0a, 0x0a, 0x04, 0x04], /*42 V*/
        [0x11, 0x11, 0x11, 0x15, 0x15, 0x15, 0x0a], /*43 W*/
        [0x11, 0x11, 0x0a, 0x04, 0x0a, 0x11, 0x11], /*44 X*/
        [0x11, 0x11, 0x0a, 0x04, 0x04, 0x04, 0x04], /*45 Y*/
        [0x1f, 0x10, 0x08, 0x04, 0x02, 0x01, 0x1f], /*46 Z*/
        [0x00, 0x04, 0x04, 0x1f, 0x04, 0x04, 0x00], /*47 +*/
        [0x00, 0x11, 0x0a, 0x04, 0x0a, 0x11, 0x00], /*50 **/
        [0x00, 0x0a, 0x04, 0x1f, 0x04, 0x0a, 0x00], /*51 /*/
        [0x1e, 0x02, 0x02, 0x02, 0x02, 0x03, 0x02], /*52 ?*/
        [0x00, 0x10, 0x0e, 0x0b, 0x0a, 0x0a, 0x0a], /*53 %*/
        [0x00, 0x00, 0x0e, 0x11, 0x1f, 0x01, 0x0e], /*54 $*/
        [0x10, 0x08, 0x04, 0x04, 0x04, 0x08, 0x10], /*55 (*/
        [0x01, 0x02, 0x04, 0x04, 0x04, 0x02, 0x01], /*56 )*/
        [0x00, 0x00, 0x00, 0x03, 0x03, 0x02, 0x01], /*57 ,*/
        [0x04, 0x0e, 0x15, 0x04, 0x04, 0x04, 0x04], /*60 ↑*/
        [0x03, 0x13, 0x08, 0x04, 0x02, 0x19, 0x18], /*61 √*/
        [0x04, 0x0c, 0x04, 0x00, 0x04, 0x06, 0x04], /*62 :*/
        [0x00, 0x10, 0x08, 0x04, 0x02, 0x01, 0x00], /*63 /*/
        [0x00, 0x00, 0x1f, 0x00, 0x1f, 0x00, 0x00], /*64 =*/
        [0x0c, 0x0c, 0x0c, 0x00, 0x00, 0x00, 0x00], /*65 '*/
        [0x11, 0x0a, 0x04, 0x0a, 0x11, 0x00, 0x00], /*66 "*/
        [0x1f, 0x00, 0x11, 0x0a, 0x04, 0x0a, 0x11], /*67 #*/
        [0x07, 0x08, 0x06, 0x01, 0x0f, 0x00, 0x00], /*70 &*/
        [0x0e, 0x11, 0x11, 0x08, 0x04, 0x00, 0x04], /*71 ?*/
        [0x00, 0x04, 0x00, 0x1f, 0x00, 0x04, 0x00], /*72 +*/
        [0x04, 0x0a, 0x0a, 0x0a, 0x04, 0x00, 0x04], /*73 !*/
        [0x1f, 0x0a, 0x0a, 0x0a, 0x0a, 0x0a, 0x1f], /*74 []*/
        [0x00, 0x00, 0x00, 0x00, 0x04, 0x0a, 0x15], /*75 →*/
        [0x1f, 0x0a, 0x0a, 0x0a, 0x0a, 0x0a, 0x0a], /*76 π*/
        [0x1f, 0x02, 0x04, 0x08, 0x04, 0x02, 0x1f]  /*77 Σ*/
    ];

    private readonly Ti59PrintMachine _model = new();

    private readonly List<string> _tapeLines = new();

    public event Action? TapeChanged;

    public IReadOnlyList<string> TapeLines => _tapeLines;

    // Obsah bufferu tiskarny (20 sloupcu) - pro diagnostiku a testy.
    public string PrinterBuffer => _model.BufferString;

    // Pozice tiskove hlavy (0..19, sweep zprava doleva jako v print.c).
    public int PrinterHead => _model.Head;

    public void Clear()
    {
        _tapeLines.Clear();
        TapeChanged?.Invoke();
    }

    public void Advance()
    {
        _model.Advance();
        TakeModelTape();
        TapeChanged?.Invoke();
    }

    // Tisk textoveho radku: nasimuluje autenticky beh (clear bufferu + znaky
    // pres font do bufferu + PRINT op). Radek se paduje/meze na 20 sloupcu
    // (fyzicka paska ma vzdy 20 sloupcu; mezera = netisknuty sloupec - stejná
    // semantika jako v korpusové pasence ml01print.tape.txt).
    public void PrintLine(string text)
    {
        PrintLineToBuffer(text);
        TakeModelTape();
        TapeChanged?.Invoke();
    }

    // Tisk obsahuje print registru (Op 1-5, Master Library ML-01): tiskne se
    // 20sloupcovy radek z PrintRegister (viz Render) pres model (clear + znaky
    // + PRINT op) - stejna cesta jako PrintLine.
    public void Render(byte[] printRegister)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Math.Min(printRegister.Length, CharColumns); ++i)
        {
            sb.Append(PrintRegisterChar(printRegister[i]));
        }
        PrintLineToBuffer(sb.ToString());
        TakeModelTape();
        TapeChanged?.Invoke();
    }

    // Kod XY: vyssi pole = X mod 8 (v 6bitovem poli nezbyva misto pro bity
    // >= 8; "8"/"9" v desitkach se maskuji), nizsi pole = jednotky cela
    // (0-9; jejich BCD bit 3 se ORuje do LSB vyssiho pole). Kod je vzdy
    // 0-63; kod 00 = mezera.
    //
    // Overeno proti orakulu (realna ROM, eventy PRINT_CHAR; Master Library
    // ML-01 = print register):
    //   00->0  12->10 '9'  16->14 'D'  17->15 'E'  18->8 '7'   20->16 '-'
    //   54->44 'e'         56->46 ')'  89->9 '8'   99->9 '8'   19->9 '8'
    //   88->8 '7'          98->8 '7'   91->9 '8'
    // Vzorec sedi na vsech 14 bodech; alternativa "druha cislice take mod 8"
    // by dala pro 88/89 mezeru/jinou císlici, coz orakulum vyvraci.
    public static char PrintRegisterChar(byte code)
    {
        int row = code / 10;
        int col = code % 10;
        int idx = (((row & 7) << 3) | col) & 0x3F;
        return Ti59PrintMachine.FontTable[idx];
    }

    public static char CodeToChar(byte code) => PrintRegisterChar(code);

    // UI (2-cifrne) kody programovych kroku, pro nez ma tiskarna TMC0251
    // vlastni mnemoniku v print_func (print.c:86-130; tabulka = jediny
    // zdroj pravdy v TI59PrintMachine.FuncTable). Ostatni kroky (cisla,
    // LBL, HIR, D.MS, ...) tiskarna nema - fallback KeyCodes.KeyCodeSym
    // (kvuli znakum mimo sadu TMC0251 upravi CharToFontIndex aliasy).
    // Pozn.: na realnem TI-59 je 2ND OP 01-05 (print register) a listing
    // soucasti Master Library (TMC0541); klavesa 93 "." nema v print_func
    // protikus (0x23 "DPT" = nastava desetinne carky, ne znak tecky).
    private static readonly (int UiCode, byte FuncCode)[] ListFuncMap =
    [
        (22, 0x22), // INV
        (24, 0x26), // CE
        (25, 0x21), // CLR
        (30, 0x5D), // TAN
        (31, 0x73), // LRN
        (33, 0x33), // x^2
        (34, 0x3C), // sX (sqrt)
        (35, 0x36), // 1/x
        (38, 0x57), // SIN
        (39, 0x56), // COS
        (42, 0x66), // STO
        (43, 0x68), // RCL
        (44, 0x61), // SUM
        (45, 0x1B), // Y^x
        (48, 0x3D), // X|Y (EXC)
        (52, 0x2D), // EE
        (53, 0x71), // (
        (54, 0x72), // )
        (55, 0x16), // /
        (61, 0x7A), // GTO
        (65, 0x17), // x
        (75, 0x12), // -
        (78, 0x69), // $+ (SUM+)
        (85, 0x13), // +
        (87, 0x7C), // IF (2nd IFF)
        (89, 0x67), // pi
        (94, 0x27), // +/-
        (95, 0x11), // =
    ];

    // Mnemonika pro listing programu: autenticka TMC0251 mnemonika z
    // print_func tam, kde existuje, jinak klavesni zkratka KeyCodeSym.
    public static string ListMnemonic(int uiCode)
    {
        foreach (var (ui, funcCode) in ListFuncMap)
        {
            if (ui == uiCode && Ti59PrintMachine.FuncLookup(funcCode, out var mnemonic))
            {
                return mnemonic;
            }
        }
        return KeyCodes.KeyCodeSym(uiCode);
    }

    private void PrintLineToBuffer(string text)
    {
        var line = ToLine(text);
        _model.Clear();                          // 0x0A88: buffer = mezery, head = 19
        for (int i = CharColumns - 1; i >= 0; --i)  // ROM naplni radek zprava doleva
        {
            _model.LoadChar(CharToFontIndex(line[i]));
        }
        _model.PrintLine();                      // 0x0AA8: radek pasky = buffer
    }

    // Radek pasky: max 20 znaku, kratsi se paduje do prava mezerami
    // (netisknute sloupce); delsi se oreze (fyzicka paska ma 20 sloupcu).
    private static string ToLine(string text)
    {
        var sb = new StringBuilder(text);
        if (sb.Length > CharColumns)
        {
            sb.Length = CharColumns;
        }
        while (sb.Length < CharColumns)
        {
            sb.Append(' ');
        }
        return sb.ToString();
    }

    public static bool TryFontIndex(char ch, out byte index)
    {
        for (byte i = 0; i < Ti59PrintMachine.FontTable.Length; ++i)
        {
            if (Ti59PrintMachine.FontTable[i] == ch)
            {
                index = i;
                return true;
            }
        }
        index = 0;
        return false;
    }

    public static byte CharToFontIndex(char ch)
    {
        if (TryFontIndex(ch, out var idx))
        {
            return idx;
        }
        // Fallback pro znaky mimo 64znakovou sadu TMC0251 (UI zkratky
        // z KeyCodes.KeyCodeSym/PrintMnemonics maji pro nektere klavesy
        // graficke znaky, ktere tiskarna nema - nahradi se nejblizsim
        // tisknutelnym ekvivalentem; cizi znak se netiskne = mezera).
        return ch switch
        {
            '÷' => CharToFontIndex('/'),   // 55 " ÷ " - tiskarna ma '/'
            '×' => CharToFontIndex('x'),   // 65 " × "
            '√' => CharToFontIndex('s'),   // "s" = sqrt glyf TMC0251
            '∑' => CharToFontIndex('$'),   // 78 "$+ " - '$' = suma
            '⇌' => CharToFontIndex('|'),   // 32 'X⇌T' - '|' = exchange glyf
            'ⓧ' => CharToFontIndex('x'),   // 45 'Yx'
            'π' => CharToFontIndex('p'),   // 'p' = pi glyf TMC0251
            'ẍ' => CharToFontIndex('x'),   // 79
            _ => 0,
        };
    }

    private void TakeModelTape()
    {
        // Model si drzi celu pasku; UI vezme nove radky (max MaxLines).
        foreach (var line in _model.Tape.Skip(_lastTaken))
        {
            _tapeLines.Add(line);
        }
        _lastTaken = _model.Tape.Count;
        TrimTape();
    }

    private int _lastTaken;

    private void TrimTape()
    {
        while (_tapeLines.Count > MaxLines)
        {
            _tapeLines.RemoveAt(0);
        }
    }
}