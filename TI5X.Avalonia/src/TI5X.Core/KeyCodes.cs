namespace TI5X.Avalonia.Core;

public static class KeyCodes
{
    // Primary key codes (Row 1..9, Col 1..5 -> Row*10 + Col)
    public const int KEY_A = 11;
    public const int KEY_B = 12;
    public const int KEY_C = 13;
    public const int KEY_D = 14;
    public const int KEY_E = 15;

    public const int KEY_2ND = 21;
    public const int KEY_INV = 22;
    public const int KEY_LNX = 23;
    public const int KEY_CE = 24;
    public const int KEY_CLR = 25;

    public const int KEY_LRN = 31;
    public const int KEY_X_SWAP_T = 32;
    public const int KEY_X_SQUARED = 33;
    public const int KEY_SQRT_X = 34;
    public const int KEY_RECIPROCAL = 35; // 1/x
    public const int KEY_1_X = KEY_RECIPROCAL;

    public const int KEY_SST = 41;
    public const int KEY_STO = 42;
    public const int KEY_RCL = 43;
    public const int KEY_SUM = 44;
    public const int KEY_Y_TO_X = 45;

    public const int KEY_BST = 51;
    public const int KEY_EE = 52;
    public const int KEY_LPAREN = 53;
    public const int KEY_RPAREN = 54;
    public const int KEY_DIV = 55;

    public const int KEY_GTO = 61;
    public const int KEY_7 = 62;
    public const int KEY_8 = 63;
    public const int KEY_9 = 64;
    public const int KEY_MUL = 65;

    public const int KEY_SBR = 71;
    public const int KEY_4 = 72;
    public const int KEY_5 = 73;
    public const int KEY_6 = 74;
    public const int KEY_SUB = 75;

    public const int KEY_RST = 81;
    public const int KEY_1 = 82;
    public const int KEY_2 = 83;
    public const int KEY_3 = 84;
    public const int KEY_ADD = 85;

    public const int KEY_R_S = 91;
    public const int KEY_0 = 92;
    public const int KEY_DOT = 93;
    public const int KEY_PLUS_MINUS = 94;
    public const int KEY_EQUALS = 95;

    // 2nd modifier key codes (mapped to opcode / display representations)
    public const int KEY_A_PRIME = 16;
    public const int KEY_B_PRIME = 17;
    public const int KEY_C_PRIME = 18;
    public const int KEY_D_PRIME = 19;
    public const int KEY_E_PRIME = 10;

    public const int KEY_LOG = 28;
    public const int KEY_CP = 29;

    public const int KEY_PGM = 36;
    public const int KEY_P_TO_R = 37;
    public const int KEY_SIN = 38;
    public const int KEY_COS = 39;
    public const int KEY_TAN = 30;

    public const int KEY_INS = 46;
    public const int KEY_CMS = 47;
    public const int KEY_EXC = 48;
    public const int KEY_PRD = 49;
    public const int KEY_IND = 40;

    public const int KEY_DEL = 56;
    public const int KEY_ENG = 57;
    public const int KEY_FIX = 58;
    public const int KEY_INT = 59;
    public const int KEY_ABS = 50; // |x|

    public const int KEY_PAUSE = 66;
    public const int KEY_X_EQ_T = 67;
    public const int KEY_NOP = 68;
    public const int KEY_OP = 69;
    public const int KEY_DEG = 60;

    public const int KEY_LBL = 76;
    public const int KEY_X_GE_T = 77;
    public const int KEY_SIGMA_PLUS = 78;
    public const int KEY_MEAN_X = 79;
    public const int KEY_RAD = 70;

    public const int KEY_ST_FLG = 86;
    public const int KEY_IF_FLG = 87;
    public const int KEY_D_MS = 88;
    public const int KEY_PI = 89;
    public const int KEY_GRAD = 80;

    public const int KEY_DSZ = 97;
    public const int KEY_ADV = 98;
    public const int KEY_PRT = 99;
    public const int KEY_LIST = 90;

    // Merged opcodes
    public const int OP_INV_SBR = 92; // RTN
    public const int OP_PGM_IND = 62;
    public const int OP_EXC_IND = 63;
    public const int OP_PRD_IND = 64;
    public const int OP_STO_IND = 72;
    public const int OP_RCL_IND = 73;
    public const int OP_SUM_IND = 74;
    public const int OP_GTO_IND = 83;
    public const int OP_OP_IND = 84;
    public const int OP_HIR = 82; // hidden-register operations (semantics undocumented)

    /// <summary>
    /// Single source of truth for merged program opcodes (IND combinations and
    /// RTN). In a program these bytes have their own meaning; on the keyboard
    /// the same values are digit keys.
    /// </summary>
    public static bool IsMergedCode(int op) => op is OP_PGM_IND or OP_EXC_IND or OP_PRD_IND
        or OP_STO_IND or OP_RCL_IND or OP_SUM_IND or OP_GTO_IND or OP_OP_IND or OP_INV_SBR;

    public static readonly string[] PrintMnemonics =
    {
        /*01*/ " 01", /*02*/ " 02", /*03*/ " 03", /*04*/ " 04", /*05*/ " 05",
        /*06*/ " 06", /*07*/ " 07", /*08*/ " 08", /*09*/ " 09", /*00*/ " 00",
        /*11*/ " A ", /*12*/ " B ", /*13*/ " C ", /*14*/ " D ", /*15*/ " E ",
        /*16*/ "A' ", /*17*/ "B' ", /*18*/ "C' ", /*19*/ "D' ", /*10*/ "E' ",
        /*21*/ "2ND", /*22*/ "INV", /*23*/ "LNX", /*24*/ "CE ", /*25*/ "CLR",
        /*26*/ "2ND", /*27*/ "INV", /*28*/ "LOG", /*29*/ "CP ", /*20*/ "CLR",
        /*31*/ "LRN", /*32*/ "X⇌T", /*33*/ "X² ", /*34*/ "√X ", /*35*/ "1/X",
        /*36*/ "PGM", /*37*/ "P/R", /*38*/ "SIN", /*39*/ "COS", /*30*/ "TAN",
        /*41*/ "SST", /*42*/ "STO", /*43*/ "RCL", /*44*/ "SUM", /*45*/ "Yⓧ ",
        /*46*/ "INS", /*47*/ "CMS", /*48*/ "EXC", /*49*/ "PRD", /*40*/ "IND",
        /*51*/ "BST", /*52*/ "EE ", /*53*/ " ( ", /*54*/ " ) ", /*55*/ " ÷ ",
        /*56*/ "DEL", /*57*/ "ENG", /*58*/ "FIX", /*59*/ "INT", /*50*/ "I×I",
        /*61*/ "GTO", /*62*/ "PG*", /*63*/ "EX*", /*64*/ "PD*", /*65*/ " × ",
        /*66*/ "PAU", /*67*/ " EQ", /*68*/ "NOP", /*69*/ "OP ", /*60*/ "DEG",
        /*71*/ "SBR", /*72*/ "ST*", /*73*/ "RC*", /*74*/ "SM*", /*75*/ " - ",
        /*76*/ "LBL", /*77*/ " GE", /*78*/ "∑+ ", /*79*/ " ẍ ", /*70*/ "RAD",
        /*81*/ "RST", /*82*/ "HIR", /*83*/ "GO*", /*84*/ "OP*", /*85*/ " + ",
        /*86*/ "STF", /*87*/ "IFF", /*88*/ "DMS", /*89*/ " π ", /*80*/ "GRD",
        /*91*/ "R/S", /*92*/ "RTN", /*93*/ " . ", /*94*/ "+/-", /*95*/ " = ",
        /*96*/ "WRT", /*97*/ "DSZ", /*98*/ "ADV", /*99*/ "PRT", /*90*/ "LST"
    };

    public static string KeyCodeSym(int keyCode)
    {
        if (keyCode < 0 || keyCode > 99)
            return "???";
        int codeIndex = (keyCode / 10) * 10 + (keyCode % 10 == 0 ? 9 : (keyCode % 10 - 1));
        if (codeIndex >= 0 && codeIndex < PrintMnemonics.Length)
            return PrintMnemonics[codeIndex];
        return $"{keyCode:D2}";
    }
}
