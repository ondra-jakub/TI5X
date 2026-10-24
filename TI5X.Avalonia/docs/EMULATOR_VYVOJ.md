# Vývojářská dokumentace TI5X (Avalonia)

Tento dokument je určený vývojářům, kteří chtějí emulátor TI5X upravovat nebo rozšiřovat. Popisuje architekturu aplikace, jednotlivé třídy, formáty souborů `.ti5p` a `.ti5l`, testovací projekt a postup přidání nové instrukce nebo Op kódu.

Popis odpovídá stavu kódu v adresářích `src/TI5X.Core/`, `src/TI5X.ViewModels/`, `src/TI5X.Avalonia/Views/`, `src/TI5X.Avalonia/Controls/`, `src/TI5X.Maui/`, `src/TI5X.Verified/` a `tests/TI5X.Core.Tests/` a v souborech `MainWindow.axaml(.cs)`, `App.axaml.cs` a `MauiProgram.cs`. Pokud se kód změní, je potřeba tento dokument aktualizovat.

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
   - [5.1 Android aplikace (MAUI)](#51-android-aplikace-maui)
   - [5.2 Sdílené stránky a ovládací prvky (refaktoring pro Android head)](#52-sdílené-stránky-a-ovládací-prvky-refaktoring-pro-android-head)
   - [5.3 Android aplikace (Avalonia, experimentální)](#53-android-aplikace-avalonia-experimentální)
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
| Řešení | `TI5X.slnx` – desktopová aplikace, Android aplikace, sdílené view modely, jádro, testy a verifikované komponenty (centrální správa verzí balíčků v `Directory.Packages.props`, SDK i MTP připnuto v `global.json`). |
| Hlavní projekt | `src/TI5X.Avalonia/TI5X.Avalonia.csproj` – `WinExe`, `net10.0`, `Nullable` a `ImplicitUsings` zapnuté. |
| Android projekt | `src/TI5X.Maui/TI5X.Maui.csproj` – `Exe`, `net10.0-android`, `UseMaui`, `SingleProject`; `ApplicationId` `nz.gen.geek_central.ti5x.maui`, minSdk 21, targetSdk 36. |
| Sdílené view modely | `src/TI5X.ViewModels/TI5X.ViewModels.csproj` – `net10.0` knihovna s `MainViewModel`, `PrinterViewModel`, `ProgramDebuggerViewModel` a rozhraním `IUiDispatcher`; neobsahuje typy Avalonie ani MAUI. |
| Jádro | `src/TI5X.Core/TI5X.Core.csproj` – knihovna bez závislosti na UI (`namespace TI5X.Avalonia.Core`). |
| Verifikace | `src/TI5X.Verified/TI5X.Verified.csproj` – C# kód generovaný z Dafny modelů (`generated/`, zdroje `*.dfy`); regeneruje se přes `dafny translate cs`, CI hlídá shodu. |
| UI framework | Avalonia 12.0.4 pro desktop a .NET MAUI 10.0.110 (Android) pro mobil. V Debug konfiguraci Avalonie navíc `AvaloniaUI.DiagnosticsSupport`. |
| Bindingy | Avalonia: `AvaloniaUseCompiledBindingsByDefault = true` (každé XAML okno musí mít `x:DataType`). MAUI: `x:DataType` u stránek a šablon; `KeyButtonView` používá vlastní `BindingContext`. |
| Prostředky | `src/TI5X.Avalonia/Assets/` je `EmbeddedResource` desktopové aplikace; `src/TI5X.Maui/Resources/Raw/` je `MauiAsset` Android aplikace (`ml.ti5l`, `lunar_lander.ti5p`), ikona a splash jsou v `Resources/AppIcon` a `Resources/Splash`. |
| Testy | `tests/TI5X.Core.Tests/TI5X.Core.Tests.csproj` – TUnit na Microsoft.Testing.Platform; běžící přes `dotnet test` (viz kap. 7). |
| CI | `.github/workflows/ti5x-dotnet.yml` – build, formát, testy s pokrytím, SBOM, Dafny důkazy a Android APK. |

Struktura adresářů:

```
TI5X.Avalonia/
  TI5X.slnx, Directory.*.props,
  global.json, nuget.config       řešení a centrální nastavení
  docs/                          dokumentace
  src/
    TI5X.Avalonia/               desktopová aplikace (Avalonia 12)
      Program.cs, App.axaml(.cs)     start aplikace, vytvoření MainWindow
      MainWindow.axaml(.cs)          hlavní okno, menu, fyzická klávesnice
      Services/AvaloniaUiDispatcher.cs  IUiDispatcher přes Dispatcher.UIThread
      Views/                         vedlejší okna (tiskárna, debugger, nápověda, o aplikaci)
      Controls/                      vlastní ovládací prvky (klávesa, LED displej, karta)
      Assets/                        ml.ti5l, lunar_lander.ti5p
    TI5X.ViewModels/             sdílená MVVM vrstva (INotifyPropertyChanged, IUiDispatcher)
    TI5X.Maui/                   Android aplikace (MAUI 10)
      MauiProgram.cs, App.xaml(.cs)  start aplikace, app-level singleton view modely
      MainPage.xaml(.cs)             hlavní stránka, toolbar Menu, SAF, fyzická klávesnice
      Pages/                         tiskárna, debugger, nápověda
      Controls/                      KeyButtonView, LedDisplayView, CardStripView
      Services/MauiUiDispatcher.cs   IUiDispatcher přes MainThread
      Platforms/Android/             MainActivity, MainApplication, AndroidDocumentSaver, manifest
      Resources/                     Raw assety, ikona, splash
    TI5X.Core/                   emulace bez závislosti na UI
    TI5X.Verified/               Dafny modely a generovaný C# (bounds, aritmetika)
  tests/
    TI5X.Core.Tests/             testy (TUnit)
```

Základní příkazy (spouštějte z adresáře `TI5X.Avalonia/`):

```sh
dotnet build TI5X.slnx -warnaserror
dotnet run --project src/TI5X.Avalonia/TI5X.Avalonia.csproj
dotnet build src/TI5X.Maui/TI5X.Maui.csproj -c Release -f net10.0-android
dotnet publish src/TI5X.Maui/TI5X.Maui.csproj -c Release -f net10.0-android -p:AndroidPackageFormats=apk
dotnet test --solution TI5X.slnx
dotnet format TI5X.slnx --verify-no-changes --exclude src/TI5X.Verified/generated
```

Android build vyžaduje workload `maui-android` (`dotnet workload install maui-android`) a Android SDK platform 36; výsledné APK se podepisuje vývojovým klíčem a je v `src/TI5X.Maui/bin/Release/net10.0-android/`. CI navíc publikuje APK přes `dotnet publish -p:AndroidPackageFormats=apk`.

Jádro ani sdílené view modely nepoužívají typy Avalonie nebo MAUI, takže jsou testovatelné bez grafického rozhraní; testy i Dafny důkazy na jádru stojí.

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
│ TI5X.ViewModels: MainViewModel, PrinterViewModel,                │
│                  ProgramDebuggerViewModel, IUiDispatcher         │
└───────┬───────────────────────────────▲──────────────────────────┘
        │ ProcessKey, Load/Save         │ DisplayChanged, StateChanged,
        │                               │ TapeChanged (+ IUiDispatcher.Post)
┌───────▼───────────────────────────────┴──────────────────────────┐
│ Core: CalculatorEngine ── PrinterEngine                          │
│       Arith, KeyCodes, DisplayCodes, ProgBank/CardDef            │
│       PersistentState (.ti5p), LibraryModule (.ti5l)             │
└──────────────────────────────────────────────────────────────────┘
```

Vazby mezi objekty:

- `MainWindow` v konstruktoru vytvoří `new MainViewModel(new AvaloniaUiDispatcher())` a nastaví ho jako `DataContext`. Vložený prostředek končící na `ml.ti5l` načte ze sestavení Avalonie a předá ho do `MainViewModel.LoadMasterLibrary(Stream)`.
- `App` (MAUI) vytvoří v konstruktoru app-level singleton `MainViewModel`, `PrinterViewModel` a `ProgramDebuggerViewModel` s `MauiUiDispatcher`; stránky dostávají tyto instance konstruktorem. `CreateWindow` vrátí `NavigationPage` s `MainPage`.
- `MainViewModel` vytvoří `CalculatorEngine` a `PrinterEngine` a propojí je přes vlastnost `engine.Printer`. Vestavěnou knihovnu `ml.ti5l` načte platforma (Avalonia z embedded resource, MAUI z `MauiAsset`) a předá ji ve streamu.
- Vedlejší okna/stránky sdílí stejnou instanci jádra: `new PrinterViewModel(ViewModel.Printer, dispatcher)` a `new ProgramDebuggerViewModel(ViewModel.Engine, dispatcher)`.
- `App.OnFrameworkInitializationCompleted` v Avalonii nastaví jen `desktop.MainWindow`. Ostatní okna se otevírají z menu.

### 2.2 Cesta stisku klávesy

1. Klávesa na obrazovce (`KeyButton`) zavolá `MainViewModel.PressKey(KeyCode)` přes zděděný `DataContext`. Fyzická klávesnice (`MainWindow.OnKeyDown`) převede `Key` na kód TI-59 a zavolá totéž.
2. `PressKey` zavolá `CalculatorEngine.ProcessKey(kód)` a hned potom synchronně `UpdateUiState()`.
3. `ProcessKey` rozdělí zpracování:
   - když běží program, reaguje jen na `R/S` (`StopProgram`), ostatní klávesy ignoruje,
   - když se sbírá operand (`DigitsNeeded > 0`), předá klávesu do `CollectOperand`,
   - v režimu `LRN` volá `ProcessLrnKey` (ukládá kódy instrukcí standardu TI-59 včetně sloučených kódů),
   - jinak volá `ExecuteKeyInteractive`.
4. `ExecuteKeyInteractive` zpracuje `2nd` (přepne `SecondState`, další klávesu převede přes `MapSecond`), číslice a zadávání čísla. U instrukcí s operandem (`STO`, `RCL`, `SUM`, `PRD`, `EXC`, `GTO`, `SBR`, `Pgm`, `Fix`, `Op`, `St flg`, `If flg`, `Dsz`) nastaví `CollectingForFunction`, `DigitsNeeded`, `AcceptInd` a `AcceptSymbolic`. Ostatní klávesy provede `ExecuteSingleKey`.
5. Po získání operandu `ExecuteCollectedFunction` zavolá příslušnou metodu `Execute…` (např. `ExecuteStore`, `ExecuteGto`, `ExecuteOp`).

**Běh programu.** `R/S` zavolá `StartProgram()`. Ten na pozadí přes `Task.Run` opakuje `ExecuteSingleProgramStep()`, která dekóduje instrukce strukturálně:

- přečte bajt `prog[PC++]` z aktuální banky (`GetCurProg()`),
- přímo zpracuje `R/S` (stop, předtím dokončí zadávání čísla), `Pause` (prodleva 500 ms), `Lbl` (přeskočí i následující bajt), volání návěští A–E' (bajty 10–19, jako `SBR` z programu), sloučené kódy (`ExecuteMergedCode`: 62–64, 72–74, 83, 84, `92` = RTN) a `HIR` (82, definovaný no-op),
- skoky `GTO`/`SBR` a podmínky `x=t`/`x≥t`/`If flg`/`Dsz` čtou cíl strukturálně přes `ReadBranchTarget` (návěští `1x`, dvoubajtová adresa `0n nn`, `40 nn` pro Ind, jednobajtová `20`–`99`),
- instrukce s registrem (`STO`, `RCL`, `SUM`, `Exc`, `Prd`, `Pgm`, `Op`) a `Fix`/`St flg` čtou operand přes `ReadRegisterOperand` (jeden bajt, `40 nn` = Ind; bajt `21` = chyba),
- ostatní kódy se provedou jako interaktivní klávesy přes `ExecuteKeyInteractive`.

Smyčka skončí po `R/S`, po chybě nebo po `StopProgram()`. Při příznaku 9 se mezi kroky čeká 50 ms.

Diference tohoto modelu oproti originálu (příznak 8, HIR, přepočet Op 16/17 a další) popisuje [Odchylky, kapitola 2](ODCHYLKY_A_OMEZENI.md#2-nejdůležitější-omezení). Sloučené kódy, `RTN` (`92`) a dvoubajtové adresy skoků jsou nyní prováděny podle standardu TI-59.

### 2.3 Události a vlákno UI

`CalculatorEngine` vystavuje dvě události typu `Action`:

| Událost | Kdy se vyvolá | Kdo ji poslouchá |
|---|---|---|
| `DisplayChanged` | `SetX`, `ResetEntry`, `SetErrorState`, `ClearErrorState`, `ShowLrnDisplay`, průběžně při zadávání čísla | `MainViewModel` |
| `StateChanged` | Na konci téměř každé klávesy v `ExecuteKeyInteractive` a `ExecuteCollectedFunction`, při přepnutí `2nd`, v `ResetOperands`, `Reset`, `StartProgram`, `StopProgram` a po skončení běhu | `MainViewModel`, `ProgramDebuggerViewModel` |

`PrinterEngine` má vlastní událost `TapeChanged`, kterou poslouchá `PrinterViewModel`.

Události **nevyvolá**:

- `PersistentState.LoadProgram` ani `LibraryModule.LoadFromFile` / `LoadFromStream`. View model po načtení volá `UpdateUiState()` sám, ale otevřený debugger se neaktualizuje, dokud nepřijde další `StateChanged` nebo uživatel nestiskne `Refresh`.
- Přímé nastavení vlastností `PC`, `CurBank`, `X` a dalších. Například `ProgramDebuggerViewModel.Reset()` nastaví `PC = 0` a obnoví jen vlastní seznam. Stavový řádek hlavního okna ukazuje starou hodnotu až do další změny stavu.

Protože běžící program volá události z vlákna na pozadí, všechny view modely v obsluze volají `IUiDispatcher.Post(...)`. Implementace je `AvaloniaUiDispatcher` (`Dispatcher.UIThread.Post`) v Avalonii a `MauiUiDispatcher` (`MainThread.BeginInvokeOnMainThread`) v MAUI. Aktualizace UI se tedy vždy provede na vlákně UI, ale **asynchronně**. Jádro nemá žádné zámky: během běhu programu vlákno na pozadí mění stav a vlákno UI ho souběžně čte. Cesta přes `ProcessKey` je chráněná tím, že za běhu programu kromě `R/S` nic nepřijímá; tlačítka debuggeru `Step (SST)` i `Reset (PC=0)` mají navíc stráž `IsRunning` a za běhu se ignorují (viz [kapitola 9](#9-technická-úskalí)).

## 3. Jádro – adresář Core

### 3.1 CalculatorEngine

Hlavní třída emulace (`src/TI5X.Core/CalculatorEngine.cs`). Obsahuje stav kalkulačky, zpracování kláves, interpret programu i všechny matematické funkce.

**Konstanty**

| Konstanta | Hodnota | Význam |
|---|---|---|
| `MaxMemories` | 100 | Počet datových registrů `Memories[0..99]`. |
| `MaxProgram` | 960 | Velikost pole `UserProgram` (banka 0). |
| `MaxBanks` | 100 | Velikost pole `Banks` (banka 0 = uživatelský program, 1–99 = knihovna). Logický limit kroků knihovních bank (`Partition.MaxLibProgram` = 1000) žije ve třídě `Partition`. |
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

Statická pomocná třída (`src/TI5X.Core/Arith.cs`):

- konstanty úhlových režimů `ANG_DEG = 0`, `ANG_RAD = 1`, `ANG_GRAD = 2` a formátů `FORMAT_FIXED = 0`, `FORMAT_FLOAT = 1`, `FORMAT_ENG = 2` (`CalculatorEngine` je jen přejímá),
- `MaxExponent = 99`, `MinExponent = -99`,
- `PowerOf10`, `FiguresBeforeDecimal`, `RoundTo` – pomocné funkce formátování,
- převody úhlů `DegToRad`, `RadToDeg`, `GradToRad`, `RadToGrad`, `ToRadians`, `FromRadians`,
- `ToDms`, `FromDms` – převod na stupně, minuty a sekundy a zpět,
- `PolarToRect`, `RectToPolar` – převod polárních a pravoúhlých souřadnic.

### 3.3 KeyCodes

Kódy kláves a mnemoniky tiskárny (`src/TI5X.Core/KeyCodes.cs`):

- **Primární klávesy** mají kód `řádek × 10 + sloupec` (řádky 1–9, sloupce 1–5), např. `KEY_STO = 42`.
- **Funkce 2nd** mají ve stejném řádku sloupec 6–9 a 0 (`KEY_CMS = 47`, `KEY_IND = 40`, `KEY_E_PRIME = 10`). Převod primární klávesy na funkci 2nd dělá `CalculatorEngine.MapSecond`.
- **Číslice** mají kódy svých kláves (`KEY_7 = 62` … `KEY_0 = 92`). V uloženém programu se ale číslice zapisují jako `00`–`09` (viz [Příručka TI-59, 15.2](PRIRUCKA_TI59.md#152-pravidla-kódování-programu)).
- **Sloučené kódy** `OP_*` (`62` PG\*, `63` EX\*, `64` PD\*, `72` ST\*, `73` RC\*, `74` SM\*, `83` GO\*, `84` OP\*, `92` RTN) jsou definované jen jako konstanty; jádro je neprovádí, protože se kryjí s kódy číslic (viz [Odchylky, kapitola 5](ODCHYLKY_A_OMEZENI.md#5-sloučené-kódy-instrukcí)).
- **`PrintMnemonics`** je pole 100 třípísmenných mnemonik. Pořadí je po řádcích klávesnice: v každé desítce jsou nejdřív sloupce 1–9 a jako poslední sloupec 0. `KeyCodeSym(kód)` proto přepočítá index jako `(kód / 10) * 10 + (kód % 10 == 0 ? 9 : kód % 10 − 1)`. Mnemoniky používá debugger a `List`.

Úplná tabulka kódů s mnemonikami je v [Příručce TI-59, 15.1](PRIRUCKA_TI59.md#151-úplná-tabulka-kódů-00-až-99).

### 3.4 DisplayCodes

Sedmisegmentový displej (`src/TI5X.Core/DisplayCodes.cs`):

- bitové masky segmentů `SegA` (0x001), `SegF` (0x002), `SegB` (0x004), `SegG` (0x008), `SegE` (0x010), `SegC` (0x020), `SegD` (0x040) a desetinné tečky `SegDp` (0x100),
- `NrDigits = 12` – počet pozic displeje,
- `SegmentCode(znak)` – maska pro `0`–`9`, `-`, `.`, `C`, `E`, `F`, `H`, `L`, `P`, `r`, `o` a mezeru; ostatní znaky jsou prázdné,
- `StringToSegments(text)` – převede text zprava doleva do 12 masek a tečku připojí k předcházející číslici. Delší text se zleva ořízne.

### 3.5 ProgBank, CardDef a pomocné struktury

Soubor `src/TI5X.Core/ProgBank.cs`:

| Typ | Obsah |
|---|---|
| `ProgBank` | `Program` (pole bajtů), `Card` (`CardDef?`), `HelpHtml` (`string?`). Banka 0 sdílí pole `UserProgram`; knihovní banky mají pole přesně tak dlouhé, kolik kódů obsahoval soubor. |
| `CardDef` | Popisky karty: `Style` (výchozí `"system"`), `TitleLeft`, `TitleRight`, `SubtitleLeft`, `SubtitleRight`, `A`–`E`, `APrime`–`EPrime`, `PrimeCenter`, `BaseLeft`, `BaseRight`. |
| `ReturnStackEntry` | `BankNr`, `Addr`, `FromInteractive` – položka zásobníku návratů. Při `FromInteractive = true` se po návratu program zastaví. |
| `OpStackEntry` | `Operator`, `Operand` – položka zásobníku AOS. |

### 3.6 LibraryModule

Načítání knihoven `.ti5l` (`src/TI5X.Core/LibraryModule.cs`). Třída umí jen číst, zápis knihovny v aplikaci neexistuje.

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

Čtení a zápis programů `.ti5p` (`src/TI5X.Core/PersistentState.cs`):

| Metoda | Popis |
|---|---|
| `SaveProgram(engine, cesta, card, help)` | Otevře soubor a zavolá streamovou variantu. |
| `SaveProgram(engine, stream, card, help)` | Zapíše `mimetype`, `prog00` z **`engine.UserProgram`** (vždy banka 0) a volitelně `card00` a `help00` do předaného streamu. `ZipArchive` je vytvořen s `leaveOpen: true`, stream zůstává volajícímu. Kartu a nápovědu předává volající; `MainViewModel.SaveProgramFile`/`SaveProgram(Stream)` je bere z aktuálně zvolené banky. |
| `LoadProgram(engine, cesta)` | Otevře soubor a zavolá streamovou variantu. |
| `LoadProgram(engine, stream)` | Načte `prog00` do `UserProgram`, k němu `card00` a `help00`, vytvoří novou `Banks[0]` a nastaví `CurBank = 0`, `PC = 0`. Stream se neuzavírá (`leaveOpen: true`). Nevyvolá žádnou událost. |

### 3.8 PrinterEngine

Model tiskárny PC-100A (`src/TI5X.Core/PrinterEngine.cs`):

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
| `IUiDispatcher` | Jediná metoda `Post(Action)`; izoluje view modely od UI frameworku. |
| `MainViewModel` | Konstruktor `MainViewModel(IUiDispatcher)`. Vlastní jádro a tiskárnu (`Engine`, `Printer`). Vlastnosti pro bindingy: `DisplayText`, `IsError`, `IsRunning`, `AngleModeText`, `IsSecondActive`, `IsInvActive`, `IsLrnActive`, `CurrentCard`, `StatusInfo`. Metody: `PressKey`, `UpdateUiState`, `LoadMasterLibrary(Stream)`, `LoadProgram(Stream)`, `SaveProgram(Stream)`, `LoadCustomLibrary(Stream)` a souborové varianty `LoadProgramFile`, `SaveProgramFile`, `LoadCustomLibrary(string)`. Implementuje `IDisposable` a v `Dispose` se odhlásí z událostí jádra. |
| `PrinterViewModel` | Kopíruje `PrinterEngine.TapeLines` do `ObservableCollection<string> TapeLines` a do `FullTapeText` (pro schránku). Změny pásky jsou přes dispatcher koalescované (`Interlocked` příznak) a seznam se aktualizuje přírůstkově, aby výpis `2nd List` nezahlcoval UI. Metody `Advance`, `Clear`, `IDisposable`. |
| `ProgramDebuggerViewModel` | Seznamy `Steps` (`ProgramStepItem`: `Step`, `OpCode`, `Mnemonic`, `IsCurrent`) a `Registers` (`MemoryRegisterItem`: `Index`, `Value`). Poslouchá jen `StateChanged` přes dispatcher. Metody `Refresh`, `Step`, `Run` (přepíná start/stop), `Reset` (jen `PC = 0` a vlastní `Refresh`), `IDisposable`. |

Poznámky k chování:

- `UpdateUiState` skládá `StatusInfo` jako `User Bank 00` / `Library Bank NN`, `PC` a `Regs: 100 − RegOffset / 10`. Hodnota `Regs` neodpovídá skutečnému počtu použitelných registrů (všech 100 je vždy dostupných), viz [Odchylky, kapitola 3.10](ODCHYLKY_A_OMEZENI.md#310-rozdělení-paměti).
- Hlášení `Loaded program…`, `Saved program…` a `Error: …` zůstane ve `StatusInfo` jen do další změny stavu – pak ho `UpdateUiState` přepíše. `LoadCustomLibrary` volá nejdřív `UpdateUiState()` a teprve potom nastaví `StatusInfo`, takže se hlášení `Loaded library …` skutečně zobrazí (dřívější chyba byla opravena).
- Výjimky při načítání a ukládání view model zachytí a zobrazí ve stavovém řádku. Výjimky z jádra při stisku klávesy zachycené nejsou.
- Debugger zobrazuje `100 − RegOffset / 10` registrů (výchozí stav R00–R51). `IsCurrent` se sice nastavuje, ale XAML ho nikde nepoužívá, takže aktuální krok se nezvýrazňuje. Při běhu programu přijde `StateChanged` po většině kroků a `Refresh` pokaždé projde celý seznam kroků.
- `PrinterViewModel` ani `ProgramDebuggerViewModel` se v Avalonii po zavření okna od událostí odhlásí přes `IDisposable` (`OnClosed`). V MAUI jsou oba app-level singletony a žijí po celou dobu běhu aplikace.

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

### 5.1 Android aplikace (MAUI)

| Třída | Typ | Popis |
|---|---|---|
| `MauiProgram` | vstupní bod | `MauiApp.CreateBuilder().UseMauiApp<App>()`; bez vlastních fontů. |
| `App` | `Application` | Vytvoří tři app-level view modely s `MauiUiDispatcher`; `CreateWindow` vrátí `NavigationPage` s `MainPage`. |
| `MainPage` | XAML + code-behind | Stejná mřížka 45 kláves jako desktop, toolbar s položkou **Menu**, indikátory, LED displej a proužek karty. Při prvním zobrazení načte `ml.ti5l` z `MauiAsset` přes `FileSystem.OpenAppPackageFileAsync` a `MemoryStream`. Menu používá `DisplayActionSheetAsync`; otevření/načtení souborů `FilePicker`, uložení `AndroidDocumentSaver`. |
| `PrinterPage` | XAML | `ScrollView` s `BindableLayout` nad `TapeLines`; tlačítka `Advance`, `Copy Tape` (`Clipboard.Default.SetTextAsync`), `Clear Tape`. |
| `DebuggerPage` | XAML | Dva `CollectionView` (kroky, registry) a tlačítka `Step (SST)`, `Run / Halt`, `Reset`, `Refresh`; stejná logika jako desktop. |
| `HelpPage` | XAML + code-behind | Czech sekce nápovědy sestavené do `VerticalStackLayout`; otevírá se jako podstránka. |
| `KeyButtonView` | `ContentView` (XAML) | Bindable vlastnosti `KeyText`, `AltText`, `KeyCode`, `StyleType`; vlastní `BindingContext` a barevné styly. Kliknutí předává `KeyCode` do `MainPage`. |
| `LedDisplayView` | `GraphicsView` + `IDrawable` | Kreslí 12 sedmisegmentových pozic přes `DisplayCodes.StringToSegments`, včetně zkosení maticí `Matrix3x2`; `IDispatcherTimer` 300 ms zajišťuje blikání. |
| `CardStripView` | `GraphicsView` + `IDrawable` | Kreslí kartu `CardDef` stejně jako Avalonia (titulky, pět sloupců A–E a A'–E'). |
| `MainActivity` | Android aktivita | `MauiAppCompatActivity`, portrait, mapuje hardwarové klávesy v `OnKeyDown` na kódy TI-59 a předává výsledek `AndroidDocumentSaver` z `OnActivityResult`. |
| `AndroidDocumentSaver` | Android služba | `Intent.ActionCreateDocument` + `TaskCompletionSource<Stream?>`; výsledek otevře `ContentResolver.OpenOutputStream(uri, "wt")`. Při zrušení vrátí `null`; žádné oprávnění k úložišti. |
| `LedDisplayControl` | `Control` (vlastní vykreslení) | Vlastnosti `Text`, `IsError`, `IsRunning`. Kreslí 12 sedmisegmentových pozic přes `DisplayCodes.StringToSegments`. `DispatcherTimer` 300 ms zajišťuje blikání při chybě a střídání jasu při běhu programu. |
| `CardStripControl` | `Control` (vlastní vykreslení) | Vlastnost `Card`. Kreslí `TitleLeft`, `TitleRight` a pět sloupců s popisky `A`–`E` a `A'`–`E'`. Bez karty zobrazí „USER PROGRAM / CARD SLOT“. Ostatní sloty `CardDef` a `Style` nevykresluje. |

### 5.2 Sdílené stránky a ovládací prvky (refaktoring pro Android head)

Aby head pro Android mohl stavebnice desktopu znovu použít, jsou prezentační bity oddělené od oken:

- `Views/CalculatorView`, `Views/PrinterView`, `Views/DebuggerView` – `UserControl` s tělem (badge, LED, proužek karty, mřížka kláves / páska / kroky+registry). Windows (`MainWindow`, `PrinterWindow`, `ProgramDebuggerWindow`) zůstaly jen jako tenké obaly s lištou menu, resp. s `OnClosed` pro odstranění VM.
- `Views/HelpSections.cs` – jediný zdroj české nápovědy (přepisuje duplicitní texty z desktopu i MAUI). `HelpWindow` a sdílená `Views/Pages/HelpPage` z něj staví UI.
- `Views/Pages/` – `CalculatorPage` (s `ContentPage.TopCommandBar` = tlačítko **Menu** a `MenuFlyout` se stejnými položkami jako MAUI), `PrinterPage`, `DebuggerPage`, `HelpPage`, `AboutPage`. Stránky se na desktopu nikdy nevytvářejí – kompilují se jen do sestavení, takže žádné `#if ANDROID` není potřeba.
- `Services/IAppFileService` + `OpenedFile` – abstrakce výběru souborů. Každý otevřený soubor je bufferovaný do seekable `MemoryStream` (zip kontejner vyžaduje seekable stream). Desktop implementuje `StorageFileService` (StorageProvider), Android `AndroidFileService` (SAF; port MAUI `AndroidDocumentSaver` s `OpenOutputStream(uri, "wt")`; kódy požadavků `0x5449`/`0x544A`).
- `Services/FileActions` – New/Open/Save/LoadLibrary nad `MainViewModel` + `IAppFileService`; volají ho desktop menu i `CalculatorPage`.
- `Services/AppServices` – statická registrace `Main`, `FileService`, `Exit` (desktop: `Close()`, Android: `FinishAffinity`) pro hlavy bez DI.
- `Services/NavigationHelper` – najde nadřazený `NavigationPage` (vizuální/logický strom) pro push z `CalculatorPage`; VM tiskárny a debuggeru se uvolní v obsluze `NavigationPage.Popped` (`DataContext as IDisposable`).
- `Services/EmbeddedAssets` – načtení `ml.ti5l` z embedded resource; volá ho `App` při vytvoření VM (desktop i single-view).

### 5.3 Android aplikace (Avalonia, experimentální)

`src/TI5X.Avalonia.Android` (net10.0-android, ID `nz.gen.geek_central.ti5x.avalonia`) odkazuje přímo na desktopový `TI5X.Avalonia` (WinExe) – žádný samostatný UI projekt neexistuje; proto csproj nastavuje `ValidateExecutableReferencesMatchSelfContained=false` a na `ProjectReference` `GlobalPropertiesToRemove="RuntimeIdentifier;SelfContained"` (jinak obnovovací graf selže – viz [5.4](#54-technická-úskalí-android-head-avalonia)). Struktura:

| Třída | Typ | Popis |
|---|---|---|
| `MainApplication` | `AvaloniaAndroidApplication<App>` | Vstupní bod; `CustomizeAppBuilder` přidá `WithInterFont()` a `LogToTrace()`. Aplikační třída Androidu musí být podtřídou `Android.App.Application` – o to se postará `AvaloniaAndroidApplication<TApp>`, které v `OnCreate` sestaví `AppBuilder` a spustí Avalonia dřív než jakákoli aktivita. |
| `MainActivity` | `AvaloniaMainActivity` | Před `base.OnCreate` nastaví `AppServices.FileService` (AndroidFileService), `AppServices.Exit` a `ActivityResult` (předá výsledky SAF dialogů). `OnKeyDown` mapuje hardwarové klávesy na `KEY_*` (`F1`–`F5` = `A`–`E`). |
| `AndroidFileService` | `IAppFileService` | `ActionOpenDocument`/`ActionCreateDocument` + `TaskCompletionSource` sloty; otevřený proud bufferuje do `MemoryStream`. |
| `MainView` (v `App`) | `PageNavigationHost` | `MainViewFactory` vytváří `PageNavigationHost { Page = new NavigationPage { Content = CalculatorPage } }` – host je nutný, aby systémové tlačítko zpět fungovalo jako navigace a insets šly na stránky. |

Důležitá fakta pro udržování:

- `MinSupportedOSPlatformVersion` je 23 – AndroidX `lifecycle-runtime` (závislost `Avalonia.Android 12.0.4`) nesestaví manifest s minSdk 21; oficiální Avalonia Android šablona také používá 23.
- Aktivita musí mít AppCompat téma (`@style/MyTheme.NoActionBar` odvozené z `Theme.AppCompat.DayNight.NoActionBar`), jinak `AppCompatActivity.SetContentView` spadne při startu.
- `ContentPage.TopCommandBar` se vykresluje v navigační liště `NavigationPage`; výchozí `BarLayoutBehavior` je `Inset`.
- Ovládací prvky `KeyButton`, `LedDisplayControl` a `CardStripControl` se používají beze změn (DataContext teče ze stránky).

### 5.4 Technická úskalí android head (Avalonia)

| Problém | Projev | Řešení |
|---|---|---|
| Odkaz na WinExe z self-contained Androidu | `NETSDK1150` (`ValidateExecutableReferencesMatchSelfContained`) | V head csproj `ValidateExecutableReferencesMatchSelfContained=false`. |
| RID globální vlastnosti tečou do odkazovaného net10.0 projektu | `NETSDK1047`: assets bez cíle `net10.0/android-arm64` | Na `ProjectReference` nastavit `GlobalPropertiesToRemove="RuntimeIdentifier;SelfContained"` – sdílený projekt se pak staví RID-agnosticky. |
| Aktivanta bez AppCompat tématu | `IllegalStateException: You need to use a Theme.AppCompat theme` při startu (AvaloniaActivity dědí `AppCompatActivity`) | `[Activity(Theme = "@style/MyTheme.NoActionBar", ...)]` + `Resources/values/styles.xml` a `colors.xml` v head projektu. |
| minSdk 21 | manifest merger odmítne AndroidX `lifecycle-runtime` (minSdk 23) | `SupportedOSPlatformVersion` 23.0 (oficiální Avalonia šablona také používá 23). |
| Systémové tlačítko zpět ukončí aplikaci místo popnutí stránky | `MainView` bez hostitele → `BackRequested` se nepropadne do navigace | zalomit root do `PageNavigationHost` (podle oficiální Avalonia xplat šablony) a nastavovat přes `IActivityApplicationLifetime.MainViewFactory`. |
| `ISingleViewApplicationLifetime.MainView` na Androidu | Avalonia.Android hlásí přes log, že se místo toho má používat `IActivityApplicationLifetime.MainViewFactory` | větev v `App.OnFrameworkInitializationCompleted` napřed testuje `IActivityApplicationLifetime`. |

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
- Vestavěná `src/TI5X.Avalonia/Assets/ml.ti5l` obsahuje `mimetype`, `help` a jen `prog01` (127 kroků, bez karty).

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
dotnet test --solution TI5X.slnx
dotnet test --solution TI5X.slnx --coverage   # report v tests/TI5X.Core.Tests/bin/.../TestResults
```

- Testy používají **TUnit** na Microsoft.Testing.Platform (`dotnet test` spouští MTP runner díky `global.json`), běží paralelně a aserce se musí `await`ovat.
- Testují se jen třídy z `src/TI5X.Core/`. Sdílené view modely, okna ani Android UI automatické testy nemají.
- Testy nikdy nezapisují do repozitáře: assety se kopírují do výstupního adresáře testů (`Content` s `CopyToOutputDirectory`), dočasné soubory se mažou v `finally`.
- CI (`.github/workflows/ti5x-dotnet.yml`) spouští build, kontrolu formátování, testy s pokrytím, SBOM a Dafny důkazy včetně shody generovaného C# kódu.

### 7.2 Struktura testů

Testy jsou v adresáři `tests/TI5X.Core.Tests/`:

| Soubor | Popis |
|---|---|
| `TestHelpers.cs` | `LunarLanderProgram` (142 bajtů, viz [specifikace](LUNAR_LANDER_SPEC.md)), `SetupEngineWithProgram`, `RunUntilHaltAsync` (čeká na `engine.RunCompletion`), `ExecuteTurnAsync`, `PressKeysAsync`, `NumberKeys`, karta a nápověda assetu. |
| `BasicMathTests.cs` | Aritmetika a priority, režimy úhlů, převody `Arith`. |
| `DisplayCodesTests.cs` | Segmenty LED displeje. |
| `LunarLanderTests.cs` | Kompletní scénáře Lunar Lander (13 testů) z původní konzolové sady. |
| `Ti5pRoundtripTests.cs` | Roundtrip `.ti5p` (soubor i `MemoryStream`), konzistence commitnutého assetu a ověření, že streamové overloady nezavírají předaný stream. |
| `EngineSafetyTests.cs` | Zastavení/restart běhu, meze po `0 Op 17`, krátké knihovní banky, krokování. |
| `MergedCodesTests.cs` | Sloučené kódy, `RTN`, dvoubajtové adresy skoků, číslo před `R/S`, `HIR`. |
| `LrnEntryTests.cs` | Kódování LRN: číslice `00`–`09`, 2nd, `INV SBR` → `92`, sloučené IND kódy, adresy `0n nn`. |
| `ExhaustiveOpcodeTests.cs` | Vyčerpávající sweep: všech 100 opcodů × 100 operandů, krátké banky, všechny klávesy i 2nd funkce, všechny Op kódy; nezávislá tabulka délek instrukcí podle příručky. |
| `PartitionModelTests.cs` | Diferenciální test proti Dafny ověřenému modelu rozdělení paměti. |

Testy přistupují k jádru dvěma způsoby. `ExecuteKeyInteractive` obchází kontrolu `LRN` a běžícího programu v `ProcessKey`. `PressKeysAsync` přes `ProcessKey` napodobuje skutečné stisky z GUI. `Printer` zůstává `null`, takže tisk se v testech netestuje. Testy spouštějící program čekají na `engine.RunCompletion` (hotový běh včetně překreslení displeje), nikoli na polling `IsRunning`.

### 7.3 Přehled testů

Původních 17 scénářů konzolového runneru bylo 1:1 přeneseno (Init, FreeFall, Hover, Walkthrough, FuelClamping, SafeLanding, Rebound, Crash, Telemetry, WinningGame, ResumeAfterQuery, Roundtrip, PhysicalKeys, CrashManualRecall, BasicMath, AngleConversions a konzistence assetu) a doplněno o nové sady: bezpečnost (`EngineSafetyTests`), conformance instrukční sady (`MergedCodesTests`, `LrnEntryTests`), vyčerpávající sweep (`ExhaustiveOpcodeTests`) a shoda s Dafny modelem (`PartitionModelTests`). Celkem 64 testů; přesný stav ukáže `dotnet test --solution TI5X.slnx`.

### 7.4 Jak přidat test

1. Do příslušné třídy (nebo nové třídy v `tests/TI5X.Core.Tests/`) přidejte metodu `[Test] public async Task Neco()`.
2. Vytvořte jádro (`new CalculatorEngine()` nebo `TestHelpers.SetupEngineWithProgram(...)`) a vstup zadejte přes `ExecuteKeyInteractive`, `TestHelpers.PressKeysAsync` nebo přímo `SetX`.
3. Když test spouští program, čekejte přes `await TestHelpers.RunUntilHaltAsync(engine)`.
4. Ověřte stav přes `await Assert.That(...)`: `engine.X` (čísla s tolerancí `.Within(1e-4)`), `engine.DisplayString`, `engine.Memories[n]`, `engine.State`, `engine.Flags[n]`, `engine.PC`. Aserce TUnit musí být vždy `await`ované, jinak se neprovedou.
5. Pokud test zapisuje soubor, použijte `Path.GetTempPath()` a soubor v bloku `finally` smažte.

Kódy kroků programu v testu zapisujte podle [pravidel kódování](PRIRUCKA_TI59.md#152-pravidla-kódování-programu): číslice jako `00`–`09`, operandy jako jeden bajt, skoky s adresou `0n nn`, sloučené kódy jsou podporovány.

Příklad:

```csharp
[Test]
public async Task Op17Partition()
{
    var engine = new CalculatorEngine();
    engine.SetX(30);
    engine.ExecuteKeyInteractive(KeyCodes.KEY_OP);
    engine.ExecuteKeyInteractive(KeyCodes.KEY_1);
    engine.ExecuteKeyInteractive(KeyCodes.KEY_7); // Op 17
    await Assert.That(engine.RegOffset).IsEqualTo(700);
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
- [ ] `dotnet test --solution TI5X.slnx` projde (64 testů) a testy nezmění žádný soubor v repozitáři.
- [ ] [ODCHYLKY_A_OMEZENI.md](ODCHYLKY_A_OMEZENI.md) – upravit matici podpory, kapitolu Op kódů, případně odstranit opravenou odchylku.
- [ ] [PRIRUCKA_TI59.md](PRIRUCKA_TI59.md) – popis funkce a tabulka kódů.
- [ ] [EMULATOR_UZIVATEL.md](EMULATOR_UZIVATEL.md) – pokud se mění ovládání, menu nebo zkratky.
- [ ] Tento dokument – pokud se mění architektura, formát souborů nebo testy.
- [ ] `Views/HelpWindow.cs` – pokud se změna týká obsahu nápovědy v aplikaci.

## 9. Technická úskalí

Přehled technických problémů v kódu, na které při úpravách narazíte. Úplný seznam odchylek pro uživatele vede [ODCHYLKY_A_OMEZENI.md](ODCHYLKY_A_OMEZENI.md).

| Problém | Kde | Důsledek |
|---|---|---|
| ~~Logická délka programu neodpovídá délce pole~~ **opraveno** | `Partition.EffectiveMaxProg` vrací minimum logického rozdělení a skutečné délky pole; vzorec je ověřen Dafny modelem (`src/TI5X.Verified/partition.dfy`) a diferenciálním testem. | `FindLabel`, `List`, `Op 8`, `SST` i běh za koncem banky končí definovanou chybou, ne výjimkou. Přepočet Op 16/17 stále neodpovídá originálu (viz [Odchylky 3.10](ODCHYLKY_A_OMEZENI.md#310-rozdělení-paměti)). |
| ~~Souběh `StartProgram`/`StopProgram`~~ **opraveno** | Běh pracuje s lokálním tokenem a generací běhu; `finally` mění sdílený stav jen vlastnící běh. `StepProgram` i debugger `Step`/`Reset` jsou za běhu ignorovány. | Rychlé zastavení a nové spuštění nemůže ukončit nový běh ani číst zrušený token (testy `EngineSafetyTests`). Stav enginu zůstává sdílený s UI bez zámků – počítat s tím při editaci v debuggeru. |
| Události téměř při každém kroku | `ExecuteKeyInteractive` → `NotifyStateChanged` → `IUiDispatcher.Post` | Při běhu programu se do fronty UI posílá aktualizace po většině kroků. Otevřený debugger pokaždé projde všech 960 kroků. |
| Načítání souborů bez události | `PersistentState.LoadProgram`, `LibraryModule` | Debugger se po načtení sám neobnoví. |
| Neodhlášené události | `PrinterViewModel`, `ProgramDebuggerViewModel` | Po zavření okna zůstává view model přihlášený k událostem jádra. |
| Nepoužité členy | `PrinterEngine.Chars`, `LibraryModule.ModuleHelp`, `ProgBank.HelpHtml` (jen se přenáší), `ProgramStepItem.IsCurrent` (bez bindingu) | Připravené pro budoucí rozšíření; nespoléhejte na ně. |
| Zápis karty bez escapování XML | `PersistentState.SaveProgram` | Popisky s `&` nebo `<` poškodí `card00`; při načtení se karta zahodí. |
| Neúplný zápis karty | `PersistentState.SaveProgram` | Sloty `subtitle*`, `primecenter`, `base*` se při uložení ztratí. |
| Tolerantní čtení kódů | `LibraryModule.ReadProgramEntry` | Neplatné tokeny se tiše změní na `0`, hodnoty nad 99 se nekontrolují. |
| Neznámé Op kódy bez chyby | `ExecuteOp` (`default`) | `Op 9`, `10`, `14`, `15`, `18`, `19` a `40`+ nic neudělají, ani nehlásí chybu. |
| Neověřená číselná pravidla | aritmetika jádra stále v `double` | Rozsah (1E-99…9.99E99), přesnost 13 číslic a režim zaokrouhlení/podtečení nejsou doloženy z primárního zdroje; migrace na Bcd13 je podmíněna uzavřením číselného kontraktu (viz plán `.kilo/plans/`). |

### 9.1 Regenerace verifikovaných komponent

Nové Dafny komponenty patří do `src/TI5X.Verified/`:

1. Přidejte `*.dfy` zdroj a ověřte ho: `dafny verify src/TI5X.Verified/<model>.dfy`.
2. Vygenerujte C# **kanonickým příkazem CI** (verifikace proběhne znovu automaticky; jiný příkaz by změnil hlavičku generovaného souboru a rozbil CI diff):
   ```sh
   cd TI5X.Avalonia
   dafny translate cs --include-runtime src/TI5X.Verified/<model>.dfy --output regen/<model>
   cp regen/<model>.cs src/TI5X.Verified/generated/<model>.cs
   cp regen/<model>-cs.dtr src/TI5X.Verified/generated/<model>-cs.dtr
   ```
3. Zařaďte generovaný kód do `TI5X.Verified.csproj` a přidejte diferenciální test proti produkční implementaci.
4. CI hlídá `dafny verify`, audit předpokladů i shodu regenerovaného kódu s commitnutým (porovnání ignoruje vložený řádek `Command-line arguments`); generovaný kód nikdy neupravujte ručně. |
