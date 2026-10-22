namespace TI5X.Maui.Pages;

public partial class HelpPage : ContentPage
{
    private static readonly (string Heading, string Text)[] Sections =
    [
        ("Základní ovládání",
            "Čísla a aritmetiku zadávejte klávesnicí kalkulačky na obrazovce nebo klávesnicí počítače: číslice (i numerická), . + - * /, Enter = „=“, Backspace = CE, Escape = CLR a F1–F5 = A–E.\n\n" +
            "Klávesa CE maže jen zadávané číslo a ruší chybový stav, CLR navíc všechny odložené operace. Chybový stav poznáte podle plně blikajícího displeje; zruší ho CE nebo CLR.\n\n" +
            "Režimy DEG, RAD a GRAD (jednotka úhlu) se přepínají klávesami 2nd Deg, 2nd Rad a 2nd Grad; aktuální režim svítí v indikátoru nad displejem."),

        ("Klávesy 2nd a INV",
            "Klávesa 2nd vybere žlutou druhou funkci následující klávesy (na displeji svítí indikátor 2nd), INV inverzní funkci (indikátor INV). Opakovaným stiskem se stav zruší. Zadání se vždy vztahuje jen k jedné další klávese.\n\n" +
            "Poznámka: INV EE nevrací z vědeckého formátu; použijte INV 2nd Eng."),

        ("Formáty zobrazení",
            "2nd Eng přepne do inženýrského formátu, 2nd Fix n nastaví pevný počet desetinných míst (n = 0–8). INV 2nd Eng nebo INV 2nd Fix vrátí standardní formát. EE zadává exponent desetinné mocniny. Mantisa má vždy 8 platných číslic."),

        ("Paměti",
            "STO uloží displej do registru, RCL registr vyvolá, SUM přičte, Prd vynásobí a Exc vymění. Všechny zadávají dvoumístné číslo registru 00–99, např. 5 STO 01. 2nd CMs vynuluje všechny registry.\n\n" +
            "2nd Ind vybere registr nepřímo: STO Ind nn použije číslo v registru nn jako adresu."),

        ("Programování",
            "Upozornění: program zapsaný z klávesnice v režimu LRN se v tomto emulátoru uloží chybně a nefunguje. LRN proto používejte jen k prohlížení programu (SST a BST posunují po jednom kroku); displej ukazuje číslo kroku a kód (např. 012 76).\n\n" +
            "Funkční program vytvořte jako soubor .ti5p (kódy instrukcí podle dokumentace) a načtěte ho přes Menu > Open Program (.ti5p). Program spusťte klávesou A–E (nebo R/S od kroku 000), běh zastavíte R/S, čítač kroků vrátí RST.\n\n" +
            "Detaily: sledování běhu, návěští, skoky a podprogramy popisuje příručka v adresáři docs."),

        ("Soubory",
            "Menu > Open Program (.ti5p) načte program do banky 0 (registry zůstávají beze změny), Save Program (.ti5p) uloží program banky 0 přes systémový výběr umístění. Load Library Module (.ti5l) načte knihovní modul do bank 01–99; vestavěná knihovna (jen banka 01, neúplná) se načte při startu.\n\n" +
            "New Program smaže program i všechny načtené knihovny včetně vestavěné."),

        ("Tiskárna a debugger",
            "Menu > Virtual Printer otevře virtuální tiskárnu: Advance posune papír, Copy Tape zkopíruje pásku do schránky, Clear Tape ji smaže. Tisknou klávesy 2nd Prt, 2nd List a 2nd Adv.\n\n" +
            "Menu > Program Debugger otevře debugger: Program Steps ukazuje kroky aktuální banky (číslo, kód, mnemonika), Data Registers obsah registrů. Tlačítka Step (SST), Run / Halt (R/S), Reset (PC=0) a Refresh. Po načtení souboru použijte Refresh."),

        ("Odkaz na dokumentaci",
            "Úplná česká dokumentace je v adresáři docs v kořeni projektu:\n" +
            "• PRIRUCKA_TI59.md – příručka kalkulačky (počítání, programování, tabulka kódů)\n" +
            "• EMULATOR_UZIVATEL.md – ovládání aplikace (menu, soubory, tiskárna, debugger)\n" +
            "• EMULATOR_VYVOJ.md – vývojářská dokumentace\n" +
            "• ODCHYLKY_A_OMEZENI.md – seznam odchylek od originálu TI-59\n" +
            "• LUNAR_LANDER_SPEC.md – ukázkový program Lunar Lander")
    ];

    public HelpPage()
    {
        InitializeComponent();

        ContentStack.Children.Add(new Label
        {
            Text = "Nápověda – emulátor kalkulačky TI-59",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromRgb(224, 216, 208)
        });

        foreach (var (heading, text) in Sections)
        {
            ContentStack.Children.Add(new Label
            {
                Text = heading,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromRgb(230, 180, 50),
                Margin = new Thickness(0, 8, 0, 0)
            });
            ContentStack.Children.Add(new Label
            {
                Text = text,
                TextColor = Color.FromRgb(224, 216, 208)
            });
        }
    }
}
