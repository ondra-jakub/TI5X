# Vývojářská dokumentace TI5X (Avalonia)

Tento dokument je určený vývojářům, kteří chtějí emulátor TI5X upravovat nebo rozšiřovat. Popisuje architekturu aplikace, jednotlivé třídy, formáty souborů `.ti5p` a `.ti5l`, testovací projekt a postup přidání nové instrukce nebo Op kódu.

Popis odpovídá stavu kódu v adresářích `Core/`, `ViewModels/`, `Views/`, `Controls/` a v souborech `MainWindow.axaml(.cs)`, `App.axaml.cs` a `TI5X.Avalonia.Tests/Program.cs`. Pokud se kód změní, je potřeba tento dokument aktualizovat.

Související dokumenty:

| Dokument | Obsah |
|---|---|
| [Příručka kalkulačky TI-59](PRIRUCKA_TI59.md) | Chování kalkulačky, tabulka kódů instrukcí, pravidla kódování programu. |
| [Uživatelská příručka emulátoru](EMULATOR_UZIVATEL.md) | Ovládání aplikace, menu, tiskárna, debugger. |
| [Odchylky emulace a známá omezení](ODCHYLKY_A_OMEZENI.md) | Jediný úplný seznam rozdílů oproti originálu a známých chyb. |
| [Specifikace Lunar Lander](LUNAR_LANDER_SPEC.md) | Ukázkový program, na kterém stojí většina testů. |

## Obsah

1. [Projekt a sestavení](#1-projekt-a-sestavení)
2. [Architektura](#2-architektura)
   - [2.1 Vrstvy](#21-vrstvy)
   - [2.2 Cesta stisku klávesy](#22-cesta-stisku-klávesy)
   - [2.3 Události a vlákno UI](#23-události-a-vlákno-ui)
3. [Jádro – adresář Core](#3-jádro--adresář-core)
   - [3.1 CalculatorEngine](#31-calculatorengine)
   - [3.2 Arith](#32-arith)
   - [3.3 KeyCodes](#33-keycodes)
   - [3.4 DisplayCodes](#34-displaycodes)
   - [3.5 ProgBank, CardDef a pomocné struktury](#35-progbank-carddef-a-pomocné-struktury)
   - [3.6 LibraryModule](#36-librarymodule)
   - [3.7 PersistentState](#37-persistentstate)
   - [3.8 PrinterEngine](#38-printerengine)
4. [View modely](#4-view-modely)
5. [Okna a ovládací prvky](#5-okna-a-ovládací-prvky)
6. [Formáty souborů](#6-formáty-souborů)
   - [6.1 Společný základ](#61-společný-základ)
   - [6.2 Program .ti5p](#62-program-ti5p)
   - [6.3 Položka prog – kódy kroků](#63-položka-prog--kódy-kroků)
   - [6.4 Položka card – popis karty](#64-položka-card--popis-karty)
   - [6.5 Položka help – nápověda](#65-položka-help--nápověda)
   - [6.6 Knihovna .ti5l](#66-knihovna-ti5l)
   - [6.7 Co se neukládá](#67-co-se-neukládá)
7. [Testy](#7-testy)
   - [7.1 Spuštění](#71-spuštění)
   - [7.2 Struktura testovacího programu](#72-struktura-testovacího-programu)
   - [7.3 Přehled testů](#73-přehled-testů)
   - [7.4 Jak přidat test](#74-jak-přidat-test)
8. [Rozšiřování emulátoru](#8-rozšiřování-emulátoru)
   - [8.1 Přidání Op kódu](#81-přidání-op-kódu)
   - [8.2 Přidání nebo změna instrukce](#82-přidání-nebo-změna-instrukce)
   - [8.3 Kontrolní seznam](#83-kontrolní-seznam)
9. [Technická úskalí](#9-technická-úskalí)

---

## 1. Projekt a sestavení

| Položka | Hodnota |
|---|---|
| Hlavní projekt | `TI5X.Avalonia.csproj` – `WinExe`, `net10.0`, `Nullable` a `ImplicitUsings` zapnuté. |
| UI framework | Avalonia 12.0.4 (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`), v konfiguraci Debug navíc `AvaloniaUI.DiagnosticsSupport`. |
| Bindingy | `AvaloniaUseCompiledBindingsByDefault = true` – každé XAML okno musí mít `x:DataType`. |
| Prostředky | Vše v `Assets/` je zároveň `EmbeddedResource` i `AvaloniaResource`. Za běhu se používá jen vložený `ml.ti5l`. |
| Testy | `TI5X.Avalonia.Tests/` – samostatná konzolová aplikace s odkazem na hlavní projekt. Hlavní projekt její zdrojové soubory vylučuje (`Compile Remove="TI5X.Avalonia.Tests\**"`). |

Struktura adresářů:

```
TI5X.Avalonia/
  Program.cs, App.axaml(.cs)     start aplikace, vytvoření MainWindow
  MainWindow.axaml(.cs)          hlavní okno, menu, fyzická klávesnice
  Core/                          emulace bez závislosti na Avalonii
  ViewModels/                    MVVM vrstva (INotifyPropertyChanged)
  Views/                         vedlejší okna (tiskárna, debugger, nápověda, o aplikaci)
  Controls/                      vlastní ovládací prvky (klávesa, LED displej, karta)
  Assets/                        ml.ti5l, lunar_lander.ti5p
  TI5X.Avalonia.Tests/           konzolový testovací program
  docs/                          dokumentace
```

Základní příkazy (spouštějte z adresáře projektu `TI5X.Avalonia/`):

```sh
dotnet build TI5X.Avalonia.csproj
dotnet run --project TI5X.Avalonia.csproj
dotnet run --project TI5X.Avalonia.Tests/TI5X.Avalonia.Tests.csproj
```

Adresář `Core/` nepoužívá žádné typy z Avalonie, takže jádro lze testovat i používat bez grafického rozhraní. Testovací projekt toho využívá.

## 2. Architektura

### 2.1 Vrstvy

Aplikace používá vzor MVVM bez dalšího frameworku. `ViewModelBase` implementuje `INotifyPropertyChanged` a metodu `SetProperty`.

```
┌──────────────────────── Views / Controls ────────────────────────┐
│ MainWindow  PrinterWindow  ProgramDebuggerWindow  Help/About     │
│ KeyButton   LedDisplayControl   CardStripControl                 │
└───────┬───────────────────────────────▲──────────────────────────┘
        │ PressKey(kód), menu           │ bindingy (PropertyChanged)
┌───────▼───────────────────────────────┴──────────────────────────┐
│ ViewModels: MainViewModel, PrinterViewModel,                     │
│             ProgramDebuggerViewModel                             │
└───────┬───────────────────────────────▲──────────────────────────┘
        │ ProcessKey, Load/Save         │ DisplayChanged, StateChanged,
        │                               │ TapeChanged (+ Dispatcher.UIThread.Post)
┌───────▼───────────────────────────────┴──────────────────────────┐
│ Core: CalculatorEngine ── PrinterEngine                          │
│       Arith, KeyCodes, DisplayCodes, ProgBank/CardDef            │
│       PersistentState (.ti5p), LibraryModule (.ti5l)             │
└──────────────────────────────────────────────────────────────────┘
```

Vazby mezi objekty:

- `MainWindow` v konstruktoru nastaví `DataContext = new MainViewModel()`.
- `MainViewModel` vytvoří `CalculatorEngine` a `PrinterEngine` a propojí je přes vlastnost `engine.Printer`. Pak načte vestavěnou knihovnu `ml.ti5l`.
- Vedlejší okna dostávají vlastní view model, který sdílí stejnou instanci jádra: `new PrinterViewModel(ViewModel.Printer)` a `new ProgramDebuggerViewModel(ViewModel.Engine)`.
- `App.OnFrameworkInitializationCompleted` nastaví jen `desktop.MainWindow`. Ostatní okna se otevírají z menu.

### 2.2 Cesta stisku klávesy

1. Klávesa na obrazovce (`KeyButton`) zavolá `MainViewModel.PressKey(KeyCode)` přes zděděný `DataContext`. Fyzická klávesnice (`MainWindow.OnKeyDown`) převede `Key` na kód TI-59 a zavolá totéž.
2. `PressKey` zavolá `CalculatorEngine.ProcessKey(kód)` a hned potom synchronně `UpdateUiState()`.
3. `ProcessKey` rozdělí zpracování:
   - když běží program, reaguje jen na `R/S` (`StopProgram`), ostatní klávesy ignoruje,
   - když se sbírá operand (`DigitsNeeded > 0`), předá klávesu do `CollectOperand`,
   - v režimu `LRN` volá `ProcessLrnKey` (uloží kód do `UserProgram[PC]`),
   - jinak volá `ExecuteKeyInteractive`.
4. `ExecuteKeyInteractive` zpracuje `2nd` (přepne `SecondState`, další klávesu převede přes `MapSecond`), číslice a zadávání čísla. U instrukcí s operandem (`STO`, `RCL`, `SUM`, `PRD`, `EXC`, `GTO`, `SBR`, `Pgm`, `Fix`, `Op`, `St flg`, `If flg`, `Dsz`) nastaví `CollectingForFunction`, `DigitsNeeded`, `AcceptInd` a `AcceptSymbolic`. Ostatní klávesy provede `ExecuteSingleKey`.
5. Po získání operandu `ExecuteCollectedFunction` zavolá příslušnou metodu `Execute…` (např. `ExecuteStore`, `ExecuteGto`, `ExecuteOp`).

**Běh programu.** `R/S` zavolá `StartProgram()`. Ten na pozadí přes `Task.Run` opakuje `ExecuteSingleProgramStep()`:

- přečte bajt `prog[PC++]` z aktuální banky (`GetCurProg()`),
- přímo zpracuje `R/S` (stop), `Pause` (prodleva 500 ms) a `Lbl` (přeskočí i následující bajt),
- ostatní kódy předá do `ExecuteKeyInteractive` s nastaveným příznakem `_executingProgramStep`.

V programu se operand nesbírá po číslicích: **celý následující bajt programu** je operandem (např. `42 05` = `STO 05`, `69 17` = `Op 17`). Bajty 10–19 se u `GTO`/`SBR` berou jako symbolická návěští A–E'. Při příznaku 9 se mezi kroky čeká 50 ms. Smyčka skončí po `R/S`, po chybě nebo po `StopProgram()`.

Rozdíly tohoto modelu oproti originálu (sloučené kódy, jednobajtová adresa skoku, `LRN`) popisuje [Odchylky, kapitola 2](ODCHYLKY_A_OMEZENI.md#2-nejdůležitější-omezení).

### 2.3 Události a vlákno UI

`CalculatorEngine` vystavuje tři události typu `Action`:

| Událost | Kdy se vyvolá | Kdo ji poslouchá |
|---|---|---|
| `DisplayChanged` | `SetX`, `ResetEntry`, `SetErrorState`, `ClearErrorState`, `ShowLrnDisplay`, průběžně při zadávání čísla | `MainViewModel` |
| `StateChanged` | Na konci téměř každé klávesy v `ExecuteKeyInteractive` a `ExecuteCollectedFunction`, při přepnutí `2nd`, v `ResetOperands`, `Reset`, `StartProgram`, `StopProgram` a po skončení běhu | `MainViewModel`, `ProgramDebuggerViewModel` |
| `PrintOutputRequested` | Deklarovaná, ale nikde se nevyvolává | nikdo |

`PrinterEngine` má vlastní událost `TapeChanged`, kterou poslouchá `PrinterViewModel`.

Události **nevyvolá**:

- `PersistentState.LoadProgram` ani `LibraryModule.LoadFromFile` / `LoadFromStream`. View model po načtení volá `UpdateUiState()` sám, ale otevřený debugger se neaktualizuje, dokud nepřijde další `StateChanged` nebo uživatel nestiskne `Refresh`.
- Přímé nastavení vlastností `PC`, `CurBank`, `X` a dalších. Například `ProgramDebuggerViewModel.Reset()` nastaví `PC = 0` a obnoví jen vlastní seznam. Stavový řádek hlavního okna ukazuje starou hodnotu až do další změny stavu.

Protože běžící program volá události z vlákna na pozadí, všechny view modely v obsluze volají `Dispatcher.UIThread.Post(...)`. Aktualizace UI se tedy vždy provede na vlákně UI, ale **asynchronně**. Jádro nemá žádné zámky: během běhu programu vlákno na pozadí mění stav a vlákno UI ho souběžně čte. Tento návrh je bezpečný jen na cestě přes `ProcessKey`, která za běhu programu kromě `R/S` nic nepřijímá. ⚠️ Tlačítka debuggeru už tuto pojistku **obcházejí**: `Step (SST)` volá přímo `CalculatorEngine.StepProgram()` a `Reset (PC=0)` nastavuje `engine.PC = 0`, aniž kterákoli z nich kontrolovala `IsRunning`. Stisk `Step (SST)` za běhu programu proto spustí `ExecuteSingleProgramStep` na vlákně UI souběžně se smyčkou v `Task.Run` a `Reset (PC=0)` mění PC uprostřed běhu – jde o existující souběžný přístup (race condition), ne jen o hypotetickou vstupní cestu (viz [kapitola 9](#9-technická-úskalí)).

## 3. Jádro – adresář Core

### 3.1 CalculatorEngine

Hlavní třída emulace (`Core/CalculatorEngine.cs`). Obsahuje stav kalkulačky, zpracování kláves, interpret programu i všechny matematické funkce.

**Konstanty**

| Konstanta | Hodnota | Význam |
|---|---|---|
| `MaxMemories` | 100 | Počet datových registrů `Memories[0..99]`. |
| `MaxProgram` | 960 | Velikost pole `UserProgram` (banka 0). |
| `MaxLibProgram` | 1000 | Hodnota `GetMaxProg()` pro knihovní banky. |
| `MaxBanks` | 100 | Velikost pole `Banks` (banka 0 = uživatelský program, 1–99 = knihovna). |
| `MaxOpStack` | 10 | Hloubka zásobníku operací AOS (`OpStack`). |
| `MaxReturnStack` | 8 | Hloubka zásobníku návratů `SBR` (`ReturnStack`). |
| `MaxFlags` | 10 | Počet příznaků `Flags[0..9]`. |
| `FLAG_TRACE_PRINT` / `FLAG_STOP_ON_ERROR` / `FLAG_SLOW_EXECUTION` | 7 / 8 / 9 | Příznaky se zvláštním významem. |
| `STACKOP_*` | 0–8 | Kódy operací na zásobníku AOS (`ADD`, `SUB`, `MUL`, `DIV`, `MOD`, `EXP`, `ROOT`, `LPAREN`). |

**Stav**

| Vlastnost | Význam |
|---|---|
| `X`, `T` | Displejový registr a registr T. |
| `DisplayString`, `State`, `ExponentEntered` | Text displeje a stav (výčet `EngineState`): `Entry` (celá část čísla), `DecimalEntry` (za desetinnou tečkou), `ExponentEntry` (mantičsa exponentu po `EE`), `Result` (hotový výsledek), `Error` (chybový stav). |
| `PC`, `CurBank` | Čítač kroků a aktuální banka. |
| `RegOffset` | Rozdělení paměti. Výchozí hodnota 480; `GetMaxProg()` vrací pro banku 0 hodnotu `RegOffset * 2`, tedy 960. `Op 17` nastaví `RegOffset = (100 − n) * 10`. |
| `CurAngleMode`, `CurFormat`, `CurDecimals` | Úhlový režim (`Arith.ANG_*`), formát (`Arith.FORMAT_*`), počet desetinných míst (−1 = proměnný). |
| `SecondState`, `InvState`, `LrnMode` | Indikátory `2nd`, `INV`, `LRN`. |
| `Memories`, `Flags`, `UserProgram`, `Banks` | Registry, příznaky, program banky 0 a pole bank. |
| `OpStack`/`OpStackCount`, `ReturnStack`/`ReturnStackCount` | Zásobníky AOS a podprogramů. |
| `PrintRegister` | 20 bajtů tiskového registru pro `Op 1`–`Op 5`. |
| `IsRunning`, `PauseRequested` | Stav běhu programu. |
| `DigitsNeeded`, `AccumDigits`, `CollectingForFunction`, `GotInd`, `AcceptSymbolic`, `AcceptInd` | Stav sbírání operandu. |
| `Printer` | Volitelná instance `PrinterEngine`. Když je `null` (např. v testech), tisk se tiše ignoruje. |

**Hlavní veřejné metody**

| Metoda | Použití |
|---|---|
| `ProcessKey(kód)` | Vstupní bod z UI (respektuje `LRN`, sbírání operandu a běžící program). |
| `ExecuteKeyInteractive(kód)` | Provedení klávesy mimo `LRN`; používají ho testy a interpret programu. |
| `SetX(hodnota)` | Nastaví `X`, naformátuje displej, vyvolá `DisplayChanged` a při příznaku 7 vytiskne displej. NaN a ∞ vedou na chybový stav. |
| `SetErrorState()` / `ClearErrorState()` | Chybový stav (blikající displej). |
| `Reset(clearLibs)` | Úplný reset; s `clearLibs: true` (výchozí) smaže i banky 1–99. |
| `StartProgram()`, `StopProgram()`, `StepProgram()` | Řízení běhu programu. |
| `ExecuteSymbolicJump(návěští)` | Ekvivalent stisku `A`–`E'`: najde `Lbl`, uloží návrat a spustí program. |
| `ExecuteStore`, `ExecuteRecall`, `ExecuteSum`, `ExecutePrd`, `ExecuteExchange` | Operace s registry (parametr `ind` = nepřímé adresování). |
| `ExecuteGto`, `ExecuteSbr`, `ExecuteReturn`, `ExecutePgm` | Skoky, podprogramy a přepnutí banky. |
| `ExecuteFix`, `ExecuteStFlg`, `ExecuteIfFlg`, `ExecuteDsz`, `ExecuteOp` | Formát, příznaky, smyčky a Op kódy. |
| `ClearAll`, `ClearEntry`, `ClearProgram`, `ClearMemories` | `CLR`, `CE`, `CP`, `CMS`. |
| `PrintDisplay`, `ExecuteList` | `Prt` a `List`. |
| `GetCurProg()`, `GetMaxProg()` | Pole programu aktuální banky (`Banks[CurBank]?.Program ?? UserProgram`) a jeho logická délka (banka 0: `RegOffset * 2`, ostatní: 1000). |
| `FormatNumber(hodnota, formát, místa)` | Statická formátovací funkce displeje. |

Interní metody jsou pojmenované podle funkce: `ProcessDigit`, `ProcessDot`, `ProcessEE`, `ProcessBinaryOp`, `ProcessLParen`, `ProcessRParen`, `ProcessEquals`, `DoStackTop` (AOS), `FindLabel`, `ExecuteConditionalJump`, `ExecuteSigmaPlus`, `ExecuteMean`, `ExecuteLinReg`, `ExecuteTrend`, `ExecuteCorrelation`, `ExecuteRepartition`, `ExecutePrintOp`, `ExecuteListLabels`, `ProcessLrnKey`, `InsertInstruction`, `DeleteInstruction`, `MapSecond`, `KeyToDigit`.

`ExecuteOp` je jeden `switch` s větvemi 0–8, 11–13, 16, 17, 20–29 a 30–39. Ostatní čísla spadnou do `default` a neudělají nic (bez chyby). Co jednotlivé větve dělají a čím se liší od originálu, popisuje [Odchylky, kapitola 4](ODCHYLKY_A_OMEZENI.md#4-op-kódy).

### 3.2 Arith

Statická pomocná třída (`Core/Arith.cs`):

- konstanty úhlových režimů `ANG_DEG = 0`, `ANG_RAD = 1`, `ANG_GRAD = 2` a formátů `FORMAT_FIXED = 0`, `FORMAT_FLOAT = 1`, `FORMAT_ENG = 2` (`CalculatorEngine` je jen přejímá),
- `MaxExponent = 99`, `MinExponent = -99`,
- `PowerOf10`, `FiguresBeforeDecimal`, `RoundTo` – pomocné funkce formátování,
- převody úhlů `DegToRad`, `RadToDeg`, `GradToRad`, `RadToGrad`, `ToRadians`, `FromRadians`,
- `ToDms`, `FromDms` – převod na stupně, minuty a sekundy a zpět,
- `PolarToRect`, `RectToPolar` – převod polárních a pravoúhlých souřadnic.

### 3.3 KeyCodes

Kódy kláves a mnemoniky tiskárny (`Core/KeyCodes.cs`):

- **Primární klávesy** mají kód `řádek × 10 + sloupec` (řádky 1–9, sloupce 1–5), např. `KEY_STO = 42`.
- **Funkce 2nd** mají ve stejném řádku sloupec 6–9 a 0 (`KEY_CMS = 47`, `KEY_IND = 40`, `KEY_E_PRIME = 10`). Převod primární klávesy na funkci 2nd dělá `CalculatorEngine.MapSecond`.
- **Číslice** mají kódy svých kláves (`KEY_7 = 62` … `KEY_0 = 92`). V uloženém programu se ale číslice zapisují jako `00`–`09` (viz [Příručka TI-59, 15.2](PRIRUCKA_TI59.md#152-pravidla-kódování-programu)).
- **Sloučené kódy** `OP_*` (`62` PG\*, `63` EX\*, `64` PD\*, `72` ST\*, `73` RC\*, `74` SM\*, `83` GO\*, `84` OP\*, `92` RTN) jsou definované jen jako konstanty; jádro je neprovádí, protože se kryjí s kódy číslic (viz [Odchylky, kapitola 5](ODCHYLKY_A_OMEZENI.md#5-sloučené-kódy-instrukcí)).
- **`PrintMnemonics`** je pole 100 třípísmenných mnemonik. Pořadí je po řádcích klávesnice: v každé desítce jsou nejdřív sloupce 1–9 a jako poslední sloupec 0. `KeyCodeSym(kód)` proto přepočítá index jako `(kód / 10) * 10 + (kód % 10 == 0 ? 9 : kód % 10 − 1)`. Mnemoniky používá debugger a `List`.

Úplná tabulka kódů s mnemonikami je v [Příručce TI-59, 15.1](PRIRUCKA_TI59.md#151-úplná-tabulka-kódů-00-až-99).

### 3.4 DisplayCodes

Sedmisegmentový displej (`Core/DisplayCodes.cs`):

- bitové masky segmentů `SegA` (0x001), `SegF` (0x002), `SegB` (0x004), `SegG` (0x008), `SegE` (0x010), `SegC` (0x020), `SegD` (0x040) a desetinné tečky `SegDp` (0x100),
- `NrDigits = 12` – počet pozic displeje,
- `SegmentCode(znak)` – maska pro `0`–`9`, `-`, `.`, `C`, `E`, `F`, `H`, `L`, `P`, `r`, `o` a mezeru; ostatní znaky jsou prázdné,
- `StringToSegments(text)` – převede text zprava doleva do 12 masek a tečku připojí k předcházející číslici. Delší text se zleva ořízne.

### 3.5 ProgBank, CardDef a pomocné struktury

Soubor `Core/ProgBank.cs`:

| Typ | Obsah |
|---|---|
| `ProgBank` | `Program` (pole bajtů), `Card` (`CardDef?`), `HelpHtml` (`string?`). Banka 0 sdílí pole `UserProgram`; knihovní banky mají pole přesně tak dlouhé, kolik kódů obsahoval soubor. |
| `CardDef` | Popisky karty: `Style` (výchozí `"system"`), `TitleLeft`, `TitleRight`, `SubtitleLeft`, `SubtitleRight`, `A`–`E`, `APrime`–`EPrime`, `PrimeCenter`, `BaseLeft`, `BaseRight`. |
| `ReturnStackEntry` | `BankNr`, `Addr`, `FromInteractive` – položka zásobníku návratů. Při `FromInteractive = true` se po návratu program zastaví. |
| `OpStackEntry` | `Operator`, `Operand` – položka zásobníku AOS. |

### 3.6 LibraryModule

Načítání knihoven `.ti5l` (`Core/LibraryModule.cs`). Třída umí jen číst, zápis knihovny v aplikaci neexistuje.

| Člen | Popis |
|---|---|
| `LoadFromStream(stream)` / `LoadFromFile(cesta)` | Načte ZIP a vrátí `LibraryModule`. Stream zůstává otevřený (`leaveOpen: true`). |
| `Banks` | `Dictionary<int, ProgBank>` – banky podle čísla z názvu položky `progNN`. |
| `ModuleHelp` | Obsah položky `help`. Načte se, ale aplikace ho nikde nezobrazuje. |
| `ReadProgramEntry(entry)` | Statický parser XML s kódy kroků; používá ho i `PersistentState`. |
| `ReadCardEntry(entry)` | Statický parser `carddef`; při jakékoli chybě vrátí `null`. |
| `CalcMimeType` | `application/vnd.nz.gen.geek_central.ti5x` (konstanta stejná jako v `PersistentState`). |

Podrobnosti formátu jsou v [kapitole 6](#6-formáty-souborů).

### 3.7 PersistentState

Čtení a zápis programů `.ti5p` (`Core/PersistentState.cs`):

| Metoda | Popis |
|---|---|
| `SaveProgram(engine, cesta, card, help)` | Zapíše `mimetype`, `prog00` z **`engine.UserProgram`** (vždy banka 0) a volitelně `card00` a `help00`. Kartu a nápovědu předává volající. `MainViewModel.SaveProgramFile` je bere z aktuálně zvolené banky. |
| `LoadProgram(engine, cesta)` | Načte `prog00` do `UserProgram`, k němu `card00` a `help00`, vytvoří novou `Banks[0]` a nastaví `CurBank = 0`, `PC = 0`. Nevyvolá žádnou událost. |

### 3.8 PrinterEngine

Model tiskárny PC-100A (`Core/PrinterEngine.cs`):

| Člen | Popis |
|---|---|
| `CharColumns = 20`, `CharWidth = 5`, `CharHeight = 7` | Rozměry řádku a znaku. |
| `MaxLines = 500` | Po překročení se nejstarší řádky mažou. |
| `Chars` | Bitmapový font 5×7 pro 64 znaků. **Zatím se nepoužívá** – páska se ukládá a zobrazuje jako text. |
| `TapeLines`, `TapeChanged` | Obsah pásky (seznam řetězců) a událost po každé změně. |
| `PrintLine(text)`, `Advance()`, `Clear()` | Přidání řádku, prázdný řádek, smazání pásky. |
| `Render(printRegister)` | Převede až 20 kódů tiskového registru na znaky přes `CodeToChar` a přidá řádek (`Op 5`). |
| `CodeToChar(kód)` | Převod dvoumístného kódu znaku tiskárny na znak; neznámé kódy dávají mezeru. |

Jádro tiskne přes `Printer?.…`: `Prt` a trasovací tisk (`PrintDisplay`, displej zarovnaný doprava na 20 znaků), `Adv`, `List` (`ExecuteList`), `Op 5` (`Render`), `Op 6`/`Op 7` (`Advance`) a `Op 8` (`ExecuteListLabels`).

## 4. View modely

| Třída | Odpovědnost |
|---|---|
| `ViewModelBase` | `INotifyPropertyChanged`, `OnPropertyChanged`, `SetProperty`. |
| `MainViewModel` | Vlastní jádro a tiskárnu (`Engine`, `Printer`). Vlastnosti pro bindingy: `DisplayText`, `IsError`, `IsRunning`, `AngleModeText`, `IsSecondActive`, `IsInvActive`, `IsLrnActive`, `CurrentCard`, `StatusInfo`. Metody: `PressKey`, `UpdateUiState`, `LoadProgramFile`, `SaveProgramFile`, `LoadCustomLibrary`. Při startu načte vložený prostředek končící na `ml.ti5l` a jeho banky zapíše do `engine.Banks`. |
| `PrinterViewModel` | Kopíruje `PrinterEngine.TapeLines` do `ObservableCollection<string> TapeLines` a do `FullTapeText` (pro schránku). Při každé změně pásky seznam celý přestaví. Metody `Advance`, `Clear`. |
| `ProgramDebuggerViewModel` | Seznamy `Steps` (`ProgramStepItem`: `Step`, `OpCode`, `Mnemonic`, `IsCurrent`) a `Registers` (`MemoryRegisterItem`: `Index`, `Value`). Poslouchá jen `StateChanged`. Metody `Refresh`, `Step`, `Run` (přepíná start/stop), `Reset` (jen `PC = 0` a vlastní `Refresh`). |

Poznámky k chování:

- `UpdateUiState` skládá `StatusInfo` jako `User Bank 00` / `Library Bank NN`, `PC` a `Regs: 100 − RegOffset / 10`. Hodnota `Regs` neodpovídá skutečnému počtu použitelných registrů (všech 100 je vždy dostupných), viz [Odchylky, kapitola 3.10](ODCHYLKY_A_OMEZENI.md#310-rozdělení-paměti).
- Hlášení `Loaded program…`, `Saved program…` a `Error: …` zůstane ve `StatusInfo` jen do další změny stavu – pak ho `UpdateUiState` přepíše. ⚠️ Hlášení `Loaded library …` se na stavovém řádku nikdy neukáže: v `LoadCustomLibrary` se po něm hned volá `UpdateUiState()`, které ho okamžitě přepíše (`MainViewModel.cs`, `StatusInfo = "Loaded library …"` → `UpdateUiState()`).
- Výjimky při načítání a ukládání view model zachytí a zobrazí ve stavovém řádku. Výjimky z jádra při stisku klávesy zachycené nejsou.
- Debugger zobrazuje `100 − RegOffset / 10` registrů (výchozí stav R00–R51). `IsCurrent` se sice nastavuje, ale XAML ho nikde nepoužívá, takže aktuální krok se nezvýrazňuje. Při běhu programu přijde `StateChanged` po většině kroků a `Refresh` pokaždé projde celý seznam kroků.
- `PrinterViewModel` ani `ProgramDebuggerViewModel` se po zavření okna z událostí neodhlásí. Každé otevření okna vytvoří nový view model.

## 5. Okna a ovládací prvky

| Třída | Typ | Popis |
|---|---|---|
| `Program` | vstupní bod | `BuildAvaloniaApp()` s `UsePlatformDetect`, `WithInterFont`, `LogToTrace`; v konfiguraci Debug `WithDeveloperTools`. |
| `App` | `Application` | Načte `App.axaml` a vytvoří `MainWindow`. |
| `MainWindow` | XAML + code-behind | Menu (`OnMenuNewProgram`, `OnMenuOpenProgram`, `OnMenuSaveProgram`, `OnMenuLoadLibrary`, `OnMenuExit`, `OnMenuShowPrinter`, `OnMenuShowDebugger`, `OnMenuShowHelp`, `OnMenuShowAbout`), mapování fyzické klávesnice v `OnKeyDown`, mřížka 45 kláves `KeyButton`. Okna tiskárny a debuggeru drží v polích, takže je otevře jen jednou (další volba menu je aktivuje). `New Program` volá `ClearProgram()`, `Reset()` (smaže i knihovny) a `UpdateUiState()`. |
| `PrinterWindow` | XAML | `ItemsControl` s páskou a tlačítka `Advance`, `Copy Tape` (`Clipboard.SetTextAsync(FullTapeText)`), `Clear Tape`. |
| `ProgramDebuggerWindow` | XAML | Dva `ListBox` (kroky, registry) a tlačítka `Step (SST)`, `Run / Halt (R/S)`, `Reset (PC=0)`, `Refresh`. Tlačítko `Step` po kroku volá ještě `Refresh`. |
| `HelpWindow` | jen C# | `ScrollViewer` s jedním `TextBlock`; otevírá se nemodálně. |
| `AboutWindow` | jen C# | Titulek „About TI5X“; otevírá se modálně přes `ShowDialog(this)`. |
| `KeyButton` | `UserControl` (XAML) | Vlastnosti `KeyText`, `AltText` (popisek 2nd nad klávesou), `KeyCode` (primární kód), `StyleType` (`Standard`, `Number`, `Yellow`, `TopRow`). Kliknutí volá `MainViewModel.PressKey(KeyCode)`. Funkce 2nd řeší jádro, klávesa posílá vždy primární kód. |
| `LedDisplayControl` | `Control` (vlastní vykreslení) | Vlastnosti `Text`, `IsError`, `IsRunning`. Kreslí 12 sedmisegmentových pozic přes `DisplayCodes.StringToSegments`. `DispatcherTimer` 300 ms zajišťuje blikání při chybě a střídání jasu při běhu programu. |
| `CardStripControl` | `Control` (vlastní vykreslení) | Vlastnost `Card`. Kreslí `TitleLeft`, `TitleRight` a pět sloupců s popisky `A`–`E` a `A'`–`E'`. Bez karty zobrazí „USER PROGRAM / CARD SLOT“. Ostatní sloty `CardDef` a `Style` nevykresluje. |

## 6. Formáty souborů

Formáty vycházejí z původního emulátoru ti5x pro Android (odtud MIME typ `application/vnd.nz.gen.geek_central.ti5x`). Tato kapitola popisuje, **co skutečně čte a zapisuje kód této aplikace**, nikoli úplnou specifikaci původního formátu.

### 6.1 Společný základ

- Oba formáty jsou archivy ZIP (`System.IO.Compression.ZipArchive`).
- Položka `mimetype` obsahuje text `application/vnd.nz.gen.geek_central.ti5x` (ASCII, bez konce řádku). Zapisovač ji ukládá jako první a bez komprese.
- **Čtení obsah `mimetype` nekontroluje** a nevadí ani, když položka chybí.
- Textové položky se čtou jako UTF-8. Zapisovač používá `StreamWriter` s `Encoding.UTF8`, takže `prog00`, `card00` a `help00` začínají znakem BOM (`EF BB BF`).

### 6.2 Program .ti5p

| Položka | Zapisuje `SaveProgram` | Čte `LoadProgram` |
|---|---|---|
| `mimetype` | vždy | ne |
| `prog00` | vždy, všech 960 bajtů `UserProgram` | ano; když chybí, `LoadProgram` neudělá **nic** (bez chyby) |
| `card00` | jen když volající předá kartu | ano, jen pokud existuje `prog00` |
| `help00` | jen když je nápověda neprázdná | ano, jen pokud existuje `prog00` |

Po načtení:

- `UserProgram` se vynuluje a zkopíruje se do něj nejvýše 960 načtených kódů,
- `Banks[0]` se nahradí novou `ProgBank(UserProgram, card, help)`,
- `CurBank = 0` a `PC = 0`,
- registry, příznaky, rozdělení paměti, `X`, `T`, formát ani úhlový režim se nemění.

Ostatní položky archivu (`prog01`, `help` apod.) `LoadProgram` ignoruje.

### 6.3 Položka prog – kódy kroků

Zapisovač vytváří tento dokument (zkráceno):

```xml
<?xml version="1.0" encoding="utf-8"?>
<state>
  <calc>
    <prog>76 11 06 00 00 42 00 … 00 00</prog>
  </calc>
</state>
```

- Kódy jsou desítková čísla formátovaná `D2`, oddělená jednou mezerou, všechna na jednom řádku.
- Ukládá se vždy celé pole `UserProgram`, tedy 960 hodnot včetně koncových nul, bez ohledu na rozdělení paměti.

Čtení (`LibraryModule.ReadProgramEntry`):

- hledá element `calc/prog` pod kořenem dokumentu; **název kořene se nekontroluje**,
- obsah rozdělí podle mezer, tabulátorů a konců řádků (knihovna `ml.ti5l` má kódy na více řádcích),
- každý token převede přes `byte.TryParse`. Neplatný token nebo hodnota mimo 0–255 se uloží jako `0`. Hodnoty 100–255 se přijmou bez kontroly, i když neodpovídají žádné instrukci,
- délka výsledného pole se rovná počtu tokenů,
- chybí-li `calc/prog`, výsledkem je prázdné pole,
- chybné XML vyvolá výjimku. `MainViewModel` ji zachytí a zobrazí `Error: …` ve stavovém řádku.

Pravidla, jak kódovat instrukce a operandy, aby program v emulátoru fungoval, jsou v [Příručce TI-59, 15.2](PRIRUCKA_TI59.md#152-pravidla-kódování-programu). Ruční vytvoření souboru ukazuje [Příručka TI-59, 15.3](PRIRUCKA_TI59.md#153-vytvoření-souboru-ti5p).

### 6.4 Položka card – popis karty

```xml
<carddef style="system">
<label slot="titleleft">LUNAR LANDER</label>
<label slot="titleright">TI-58 / TI-59</label>
<label slot="a">INIT</label>
<label slot="aprime">PHYS</label>
</carddef>
```

Kořen `carddef` má atribut `style` (při čtení výchozí `system`). Každý popisek je element `label` s atributem `slot`; čte se jeho textový obsah (`XElement.Value`). Ostatní elementy a neznámé sloty se ignorují. Jakákoli chyba při čtení karty vede na `null` (program se načte bez karty).

| Slot | Vlastnost `CardDef` | Čte se | Zapisuje se | Vykresluje se |
|---|---|---|---|---|
| `titleleft`, `titleright` | `TitleLeft`, `TitleRight` | ano | ano | ano |
| `a`, `b`, `c`, `d`, `e` | `A`–`E` | ano | ano | ano |
| `aprime` … `eprime` | `APrime`–`EPrime` | ano | ano | ano |
| `subtitleleft`, `subtitleright` | `SubtitleLeft`, `SubtitleRight` | ano | **ne** | ne |
| `primecenter` | `PrimeCenter` | ano | **ne** | ne |
| `baseleft`, `baseright` | `BaseLeft`, `BaseRight` | ano | **ne** | ne |
| atribut `style` | `Style` | ano | ano | ne |

Poznámky:

- Názvy slotů se při čtení porovnávají bez ohledu na velikost písmen. Zapisovač používá malá písmena.
- Zapisovač vynechá prázdné sloty a **neescapuje XML**. Popisek se znakem `&` nebo `<` vytvoří neplatné XML a při dalším načtení se karta tiše zahodí.
- Při `File > Save Program (.ti5p)...` se sloty `subtitle*`, `primecenter` a `base*` ztratí.

### 6.5 Položka help – nápověda

- `help00` (v `.ti5p`) a `helpNN` (v `.ti5l`) se čtou jako text UTF-8 do `ProgBank.HelpHtml`. Formát se nekontroluje; v původním formátu jde o HTML.
- `help` (v `.ti5l`) je nápověda celého modulu a čte se do `LibraryModule.ModuleHelp`.
- **Žádný z těchto textů aplikace nezobrazuje.** `HelpHtml` se jen přenese do nového souboru při `Save Program`. `ModuleHelp` se nepoužívá vůbec.

### 6.6 Knihovna .ti5l

| Položka | Význam |
|---|---|
| `mimetype` | Jako u `.ti5p` (nečte se). |
| `help` | Nápověda modulu → `ModuleHelp`. |
| `progNN` | Program banky `NN`. Rozpozná se položka, jejíž název (`ZipArchiveEntry.Name`, bez cesty) začíná `prog`, má přesně 6 znaků a končí číslem. |
| `cardNN` | Karta banky `NN` (volitelná). |
| `helpNN` | Nápověda banky `NN` (volitelná). |

Chování při načtení:

- Pole programu banky má přesně tolik bajtů, kolik obsahoval soubor. Nedoplňuje se na 1000, i když `GetMaxProg()` pro knihovní banky vrací 1000 (viz [kapitola 9](#9-technická-úskalí)).
- `MainViewModel.LoadCustomLibrary` zapíše banky do `engine.Banks[NN]` a přepíše stávající banky se stejným číslem. Ostatní banky zůstanou.
- Položka `prog00` v knihovně by přepsala `Banks[0]` polem, které není `UserProgram`. Banka 0 by se tím odpojila od pole, které používá `LRN` a `Save Program`. Knihovny proto mají používat banky 01–99.
- Vestavěná `Assets/ml.ti5l` obsahuje `mimetype`, `help` a jen `prog01` (127 kroků, bez karty).

### 6.7 Co se neukládá

`.ti5p` uchovává jen program banky 0, kartu a nápovědu. Neukládá:

- datové registry,
- příznaky,
- `X` a `T`,
- rozdělení paměti,
- formát a úhlový režim,
- zásobníky,
- obsah tiskové pásky,
- načtené knihovny.

Serializace celého stavu kalkulačky v aplikaci neexistuje. `PersistentState` přes svůj název ukládá jen program. Viz též [Odchylky, kapitola 6](ODCHYLKY_A_OMEZENI.md#6-obecná-omezení-aplikace).

## 7. Testy

### 7.1 Spuštění

```sh
dotnet run --project TI5X.Avalonia.Tests/TI5X.Avalonia.Tests.csproj
```

- Testovací projekt je obyčejná konzolová aplikace. Nepoužívá xUnit ani NUnit, takže `dotnet test` nic nespustí.
- Program vypíše `[PASS]` nebo `[FAIL]` u každého testu a souhrn. Vrací návratový kód 0 (vše prošlo) nebo 1.
- Testují se jen třídy z `Core/`. View modely a okna automatické testy nemají.

> ⚠️ **Test `Generate Assets/lunar_lander.ti5p` přepíše soubor `Assets/lunar_lander.ti5p` v repozitáři.** Cestu odvozuje z `AppContext.BaseDirectory` o čtyři úrovně výš (`TI5X.Avalonia.Tests/bin/<konfigurace>/net10.0/` → kořen projektu). Obsah je deterministický, ale ZIP obsahuje časová razítka, takže soubor bude v gitu vypadat změněný. Po spuštění testů zkontrolujte `git status`.

### 7.2 Struktura testovacího programu

Vše je v souboru `TI5X.Avalonia.Tests/Program.cs`:

| Prvek | Popis |
|---|---|
| `LunarLanderProgram` | Pole 142 bajtů s programem Lunar Lander (viz [specifikace](LUNAR_LANDER_SPEC.md)). Komentáře uvádějí adresy kroků. |
| `Main` | Postupně volá `RunTest(název, metoda)` a sčítá neúspěchy. |
| `RunTest` | Spustí metodu; výjimka znamená neúspěch. |
| `AssertEqual(očekávaná, skutečná, tolerance, zpráva)` | Porovnání čísel s tolerancí (výchozí 1e-4). |
| `AssertStringEqual`, `AssertTrue` | Porovnání řetězců a podmínka. |
| `SetupEngineWithProgram(program)` | Nové jádro s programem v bance 0, `PC = 0`. |
| `RunUntilHalt(engine, timeoutMs)` | Protože program běží na pozadí, čeká (polling po 2 ms) na `IsRunning == false`; po 5 s vyhodí výjimku. |
| `ExecuteTurn(engine, burn)` | `SetX(burn)`, `R/S` a čekání na zastavení. |
| `PressKeys(engine, kódy…)` | Simulace cesty z GUI přes `ProcessKey`; po každé klávese čeká na zastavení programu. |
| `NumberKeys(hodnota)` | Převede celé číslo na kódy číselných kláves. |
| `LanderAssetPath()` | Cesta k `Assets/lunar_lander.ti5p`. |

Testy přistupují k jádru dvěma způsoby. `ExecuteKeyInteractive` obchází kontrolu `LRN` a běžícího programu v `ProcessKey`. `PressKeys` přes `ProcessKey` napodobuje skutečné stisky z GUI. `Printer` zůstává `null`, takže tisk se v testech netestuje.

### 7.3 Přehled testů

Testy se spouštějí v tomto pořadí (17 testů):

| # | Název v Main | Metoda | Co ověřuje |
|---|---|---|---|
| 1 | Basic Calculator Arithmetic & Priorities | `TestBasicMath` | `2 + 3 × 4 = 14` (AOS) a chyba při dělení nulou. |
| 2 | Angle Modes & Conversions | `TestAngleConversions` | `sin 30° = 0,5`. |
| 3 | Lunar Lander Initialization (Lbl A) | `TestLunarLanderInit` | Počáteční hodnoty R00–R02 a displej `-500.2500`. |
| 4 | Lunar Lander Free Fall Trajectory (B=0) | `TestLunarLanderFreeFall` | Dva kroky volného pádu. |
| 5 | Lunar Lander Equilibrium Hover (B=5) | `TestLunarLanderHover` | Vznášení (zrychlení 0). |
| 6 | Lunar Lander Walkthrough Descent (Turns 1..6) | `TestLunarLanderWalkthrough` | Šest kol se známými hodnotami. |
| 7 | Lunar Lander Fuel Clamping (B > Fuel) | `TestLunarLanderFuelClamping` | Omezení spotřeby na zbývající palivo. |
| 8 | Lunar Lander Safe Touchdown Boundary (v >= -5.0) | `TestLunarLanderSafeLanding` | Bezpečné přistání bez chyby. |
| 9 | Lunar Lander Rebound Landing (v > 0) | `TestLunarLanderReboundLanding` | Dotyk s kladnou rychlostí. |
| 10 | Lunar Lander Fatal Crash Boundary (v < -5.0) | `TestLunarLanderCrash` | Havárie končí chybovým stavem. |
| 11 | Lunar Lander Telemetry Keys (Lbl C, D, E) | `TestLunarLanderTelemetry` | Dotazy na palivo, výšku a rychlost. |
| 12 | Lunar Lander Complete Winning Game (64,0,0,0,0,466) | `TestLunarLanderWinningGame` | Celá vítězná hra. |
| 13 | Lunar Lander Resume With B After Query | `TestLunarLanderResumeAfterQuery` | Pokračování klávesou `B` po dotazu. |
| 14 | Lunar Lander .ti5p Save, Load and Run Roundtrip | `TestLunarLanderTi5pRoundtrip` | `SaveProgram` → `LoadProgram` do dočasného souboru, shoda bajtů, karta a běh. |
| 15 | Generate Assets/lunar_lander.ti5p | `GenerateLanderAsset` | Vytvoří (přepíše) ukázkový soubor s kartou a nápovědou. |
| 16 | Lunar Lander Asset Played With Physical Keys (GUI path) | `TestLunarLanderPhysicalKeys` | Načte asset a odehraje hru přes `ProcessKey`. |
| 17 | Lunar Lander Crash Then Manual RCL (GUI path) | `TestLunarLanderCrashManualRecall` | Havárie, `CLR`, `E` a ruční `RCL`. |

Testy 16 a 17 čtou soubor vytvořený testem 15, takže na pořadí záleží.

### 7.4 Jak přidat test

1. Do `Program.cs` přidejte statickou metodu `private static void TestNeco()`.
2. Vytvořte jádro (`new CalculatorEngine()` nebo `SetupEngineWithProgram(...)`) a vstup zadejte přes `ExecuteKeyInteractive`, `PressKeys` nebo přímo `SetX`.
3. Když test spouští program (`R/S`, `ExecuteSymbolicJump`, `SBR`), počkejte na zastavení přes `RunUntilHalt` nebo `PressKeys`.
4. Ověřte stav přes `AssertEqual` / `AssertStringEqual` / `AssertTrue` (`engine.X`, `engine.DisplayString`, `engine.Memories[n]`, `engine.State`, `engine.Flags[n]`, `engine.PC`).
5. Zaregistrujte test v `Main`: `failures += RunTest("Popis testu", TestNeco);`.
6. Pokud test zapisuje soubor, použijte `Path.GetTempPath()` a soubor v bloku `finally` smažte (vzorem je `TestLunarLanderTi5pRoundtrip`).

Kódy kroků programu v testu zapisujte podle [pravidel kódování](PRIRUCKA_TI59.md#152-pravidla-kódování-programu): číslice jako `00`–`09`, operandy jako jeden bajt, místo sloučených kódů zápis s `IND`.

Příklad:

```csharp
private static void TestOp17Partition()
{
    var engine = new CalculatorEngine();
    engine.SetX(30);
    engine.ExecuteKeyInteractive(KeyCodes.KEY_2ND);
    engine.ExecuteKeyInteractive(KeyCodes.KEY_9);   // 2nd 9 = Op
    engine.ExecuteKeyInteractive(KeyCodes.KEY_1);
    engine.ExecuteKeyInteractive(KeyCodes.KEY_7);   // Op 17
    AssertEqual(700, engine.RegOffset, 0, "30 Op 17 -> RegOffset 700");
}
```

## 8. Rozšiřování emulátoru

### 8.1 Přidání Op kódu

Op kódy jsou nejjednodušší místo pro rozšíření, protože sbírání operandu (dvě číslice z klávesnice, jeden bajt v programu, `Op IND nn`) už zajišťuje jádro.

1. V `CalculatorEngine.ExecuteOp` přidejte do `switch` novou větev `case nn:` s komentářem a voláním privátní metody, např. `ExecuteMyOp()`. Číslo nesmí kolidovat se stávajícími větvemi (0–8, 11–13, 16, 17, 20–39).
2. Metodu umístěte k ostatním implementacím Op kódů (za `ExecuteOp`) a dodržujte zvyklosti:
   - výsledek nastavujte přes `SetX(...)` (formátuje displej, vyvolá `DisplayChanged` a při příznaku 7 tiskne),
   - chybu signalizujte `SetErrorState()` a metodu ukončete,
   - registr T měňte přes vlastnost `T`, registry přes `Memories[...]`,
   - tiskárnu volejte vždy jako `Printer?.…` (v testech je `null`),
   - pro práci s programem používejte `GetCurProg()` a `GetMaxProg()` a hlídejte skutečnou délku pole (`prog.Length`), viz [kapitola 9](#9-technická-úskalí).
3. `StateChanged` vyvolávat nemusíte – `ExecuteCollectedFunction` ho volá po každé instrukci s operandem.
4. Přidejte test podle [7.4](#74-jak-přidat-test). Zkuste zadání z klávesnice (`2nd`, `9`, dvě číslice) i v programu (`69 nn`).
5. Aktualizujte dokumentaci (viz [8.3](#83-kontrolní-seznam)).

### 8.2 Přidání nebo změna instrukce

Kódy instrukcí jsou dané klávesnicí TI-59 a všechny pozice už mají konstantu v `KeyCodes`. „Přidání instrukce“ proto obvykle znamená doplnit nebo opravit obsluhu existujícího kódu.

1. **Konstanta a mnemonika.** Ověřte konstantu `KEY_…` v `KeyCodes.cs` a mnemoniku v `PrintMnemonics` na indexu podle `KeyCodeSym` (sloupec 0 je v desítce poslední).
2. **Funkce 2nd.** Pokud jde o funkci 2nd, musí ji obsahovat `MapSecond` (primární kód → kód 2nd).
3. **Instrukce bez operandu.** Přidejte `case KeyCodes.KEY_…:` do `ExecuteSingleKey`. `INV` je dostupný v parametru `inv`. Na začátku obvykle volejte `FinishNumberEntry()`, aby se dokončilo zadávané číslo.
4. **Instrukce s operandem.** V `ExecuteKeyInteractive` přidejte větev, která zavolá `FinishNumberEntry()` a nastaví:
   - `CollectingForFunction = effKey`,
   - `DigitsNeeded` (počet číslic z klávesnice),
   - `AccumDigits = 0`,
   - `AcceptInd` (povolí `IND`),
   - `AcceptSymbolic` (povolí návěští A–E'),

   a pak zavolá `NotifyStateChanged()`. Do `ExecuteCollectedFunction` doplňte `case`, který zavolá implementaci s parametry `operand`, `ind` a `inv`.
5. **Pozor na `KeyToDigit`.** `ExecuteKeyInteractive` testuje číslice **před** všemi ostatními větvemi. Kódy `00`–`09` a kódy číselných kláves (`62`–`64`, `72`–`74`, `82`–`84`, `92`) se proto vždy zpracují jako číslice. Obsluha sloučených kódů (např. `72` = `STO IND`) by vyžadovala rozlišit tyto kódy podle kontextu v `ExecuteSingleProgramStep` ještě před voláním `ExecuteKeyInteractive` (viz [Odchylky, kapitola 5](ODCHYLKY_A_OMEZENI.md#5-sloučené-kódy-instrukcí)).
6. **Zvláštní chování v programu.** Instrukce, které ovlivňují tok programu bez provedení (`R/S`, `Pause`, `Lbl`), se obsluhují přímo v `ExecuteSingleProgramStep`.
7. **Režim LRN.** `ProcessLrnKey` ukládá kód klávesy beze změny. Editační klávesy (`SST`, `BST`, `Ins`, `Del`, `LRN`) se zpracují zvlášť.
8. **UI.** Klávesa na obrazovce posílá primární kód (`KeyCode` v `MainWindow.axaml`); popisek 2nd je jen `AltText`. Novou zkratku fyzické klávesnice přidejte do `switch` v `MainWindow.OnKeyDown`.
9. **Test a dokumentace.** Přidejte test a projděte [kontrolní seznam](#83-kontrolní-seznam).

### 8.3 Kontrolní seznam

Po změně chování jádra:

- [ ] `dotnet build TI5X.Avalonia.csproj` projde bez chyb.
- [ ] `dotnet run --project TI5X.Avalonia.Tests/TI5X.Avalonia.Tests.csproj` vrátí 0 (a vraťte nebo potvrďte změnu `Assets/lunar_lander.ti5p`).
- [ ] [ODCHYLKY_A_OMEZENI.md](ODCHYLKY_A_OMEZENI.md) – upravit matici podpory, kapitolu Op kódů, případně odstranit opravenou odchylku.
- [ ] [PRIRUCKA_TI59.md](PRIRUCKA_TI59.md) – popis funkce a tabulka kódů.
- [ ] [EMULATOR_UZIVATEL.md](EMULATOR_UZIVATEL.md) – pokud se mění ovládání, menu nebo zkratky.
- [ ] Tento dokument – pokud se mění architektura, formát souborů nebo testy.
- [ ] `Views/HelpWindow.cs` – pokud se změna týká obsahu nápovědy v aplikaci.

## 9. Technická úskalí

Přehled technických problémů v kódu, na které při úpravách narazíte. Úplný seznam odchylek pro uživatele vede [ODCHYLKY_A_OMEZENI.md](ODCHYLKY_A_OMEZENI.md).

| Problém | Kde | Důsledek |
|---|---|---|
| Logická délka programu neodpovídá délce pole | `GetMaxProg()` vrací 1000 pro knihovní banky (pole má délku podle souboru, `ml.ti5l` banka 01 jen 127) a `RegOffset * 2` pro banku 0 (po `0 Op 17` až 2000, pole má 960). | `FindLabel` (hledání chybějícího návěští), `ExecuteList` (`List`), `ExecuteListLabels` (`Op 8`), `SST` i běh programu za koncem pole přistoupí mimo pole a vyhodí `IndexOutOfRangeException`. Na vlákně UI to aplikaci shodí. V běžícím programu výjimka ukončí smyčku v `Task.Run` bez hlášení. Viz [Odchylky, bod 7](ODCHYLKY_A_OMEZENI.md#2-nejdůležitější-omezení). |
| Souběžný přístup bez zámků | `StartProgram` (`Task.Run`) vs. čtení stavu v `UpdateUiState` a `Refresh`; navíc debugger (`Step`, `Reset`) volá jádro přímo z vlákna UI bez kontroly `IsRunning` | Cesta přes `ProcessKey` je chráněná tím, že za běhu programu přijímá jen `R/S`. Ale tlačítka debuggeru `Step (SST)` a `Reset (PC=0)` tuto pojistku obcházejí: za běhu programu spouštějí `ExecuteSingleProgramStep` na vlákně UI souběžně se smyčkou na pozadí, resp. mění PC uprostřed běhu. Jde o existující závadu, dokud se nepřidá kontrola `IsRunning` nebo zámek. |
| Události téměř při každém kroku | `ExecuteKeyInteractive` → `NotifyStateChanged` → `Dispatcher.UIThread.Post` | Při běhu programu se do fronty UI posílá aktualizace po většině kroků. Otevřený debugger pokaždé projde všech 960 kroků. |
| Načítání souborů bez události | `PersistentState.LoadProgram`, `LibraryModule` | Debugger se po načtení sám neobnoví. |
| Neodhlášené události | `PrinterViewModel`, `ProgramDebuggerViewModel` | Po zavření okna zůstává view model přihlášený k událostem jádra. |
| Nepoužité členy | `PrintOutputRequested`, `PrinterEngine.Chars`, `LibraryModule.ModuleHelp`, `ProgBank.HelpHtml` (jen se přenáší), `ProgramStepItem.IsCurrent` (bez bindingu), `KeyCodes.OP_*` | Připravené pro budoucí rozšíření; nespoléhejte na ně. |
| Zápis karty bez escapování XML | `PersistentState.SaveProgram` | Popisky s `&` nebo `<` poškodí `card00`; při načtení se karta zahodí. |
| Neúplný zápis karty | `PersistentState.SaveProgram` | Sloty `subtitle*`, `primecenter`, `base*` se při uložení ztratí. |
| Tolerantní čtení kódů | `LibraryModule.ReadProgramEntry` | Neplatné tokeny se tiše změní na `0`, hodnoty nad 99 se nekontrolují. |
| Neznámé Op kódy bez chyby | `ExecuteOp` (`default`) | `Op 9`, `10`, `14`, `15`, `18`, `19` a `40`+ nic neudělají, ani nehlásí chybu. |
