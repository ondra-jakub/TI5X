namespace TI5X.Avalonia.Core;

public class ProgBank
{
    public byte[] Program { get; set; }
    public CardDef? Card { get; set; }
    public string? HelpHtml { get; set; }

    public ProgBank(byte[] program, CardDef? card = null, string? helpHtml = null)
    {
        Program = program;
        Card = card;
        HelpHtml = helpHtml;
    }
}

public class CardDef
{
    public string Style { get; set; } = "system";
    public string? TitleLeft { get; set; }
    public string? TitleRight { get; set; }
    public string? SubtitleLeft { get; set; }
    public string? SubtitleRight { get; set; }

    public string? A { get; set; }
    public string? B { get; set; }
    public string? C { get; set; }
    public string? D { get; set; }
    public string? E { get; set; }

    public string? APrime { get; set; }
    public string? BPrime { get; set; }
    public string? CPrime { get; set; }
    public string? DPrime { get; set; }
    public string? EPrime { get; set; }

    public string? PrimeCenter { get; set; }
    public string? BaseLeft { get; set; }
    public string? BaseRight { get; set; }
}

public struct ReturnStackEntry
{
    public int BankNr;
    public int Addr;
    public bool FromInteractive;

    public ReturnStackEntry(int bankNr, int addr, bool fromInteractive)
    {
        BankNr = bankNr;
        Addr = addr;
        FromInteractive = fromInteractive;
    }
}

public struct OpStackEntry
{
    public int Operator;
    public double Operand;

    public OpStackEntry(int op, double operand)
    {
        Operator = op;
        Operand = operand;
    }
}
