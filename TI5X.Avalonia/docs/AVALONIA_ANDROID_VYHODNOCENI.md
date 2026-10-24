# Vyhodnocení experimentálního Android headu na Avalonii

Spike `src/TI5X.Avalonia.Android` (ID `nz.gen.geek_central.ti5x.avalonia`) ověřuje, zda lze mobilní varianta TI5X postavit přímo na **Avalonia 12.0.4** bez .NET MAUI. MAUI aplikace (`src/TI5X.Maui`) zůstává zachovaná jako stabilní varianta; obě APK jsou sestavovaná v CI a instalují se vedle sebe.

## 1. Výchozí podmínky a architektura

- Sdílený kód (stránky, ovládací prvky, nápověda, file-přístup) byl vytěžen z desktopu do `Views/CalculatorView|PrinterView|DebuggerView`, `Views/Pages/*`, `Views/HelpSections` a `Services/*` — desktop zůstal po extrakci vizuálně i funkčně beze změny (viz [EMULATOR_VYVOJ, 5.2–5.4](EMULATOR_VYVOJ.md)).
- Head (`src/TI5X.Avalonia.Android`, TFM `net10.0-android`) se skládá pouze ze čtyř souborů: `TI5X.Avalonia.Android.csproj`, `MainApplication.cs` (`AvaloniaAndroidApplication<App>`), `MainActivity.cs` (`AvaloniaMainActivity`), `AndroidFileService.cs` (SAF) + Android resource soubory (AppCompat theme, barvy). Žádné `#if ANDROID` ve sdíleném kódu není potřeba.
- Spuštění: Android `Application.OnCreate` (třída `MainApplication`) inicializuje Avalonia; `App.OnFrameworkInitializationCompleted` dostane `IActivityApplicationLifetime` a nastaví `MainViewFactory` = `PageNavigationHost { Page = NavigationPage { Content = CalculatorPage } }`.
- Sdílená `CalculatorPage` má v `ContentPage.TopCommandBar` tlačítko **Menu** s `MenuFlyout` (stejné položky jako MAUI akční list); tiskárna/debugger/nápověda/About se `PushAsync`-uji, VM tiskárny a debuggeru se uvolní v `NavigationPage.Popped`.
- Soubory: open = SAF `ACTION_OPEN_DOCUMENT` → seekable `MemoryStream` → `LoadProgram(buffer, name)`; save = VM → `MemoryStream` → `OpenOutputStream(uri, "wt")` → flush. Stejný tok jako MAUI, společná abstrakce `IAppFileService` (`StorageFileService` na desktopu).

## 2. Verifikace sestavení (lokální, macOS, SDK 10.0.101)

| Kontrola | Výsledek |
|---|---|
| `dotnet restore TI5X.slnx --locked-mode` | OK (nový `packages.lock.json` v head projektu) |
| `dotnet build TI5X.slnx -c Release -warnaserror` | 0 chyb, 0 upozornění (všechny 3 aplikace) |
| head build net10.0-android `-warnaserror` | 0 chyb, 0 upozornění, bez `NoWarn` |
| `dotnet test --solution TI5X.slnx -c Release` | 64/64 úspěšných |
| `dotnet format TI5X.slnx --verify-no-changes --exclude src/TI5X.Verified/generated` | beze změn |
| `dotnet publish … -p:AndroidPackageFormats=apk` | `nz.gen.geek_central.ti5x.avalonia-Signed.apk` ≈ 31,4 MB |
| `aapt2 dump badging` | `targetSdkVersion:'36'`, minSdk 23, jediné `uses-permission` = `…DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` (podpisové, generované Xamarinem; žádné úložiště/INTERNET) |

## 3. Smoke test na emulátoru (API 33, `Medium_Tablet_API_33`, 1600×2560)

Postup odpovídající ověření MAUI verze: instalace APK, `adb push` testovacích souborů, ovládání přes `input tap`/`input keyevent`/`screencap`, kontrola `logcat`.

| # | Krok | Výsledek |
|---|---|---|
| 1 | Chladný start `am start -W` | `Status: ok`, **2026 ms** první start, 1229–1311 ms další spuštění (software render SwiftShader, ARM64 emulace) |
| 2 | Startovní stav | displej `0.`, DEG, `User Bank 00 | PC: 000 | Regs: 52` ⇒ vestavěná `ml.ti5l` se načetla (z embedded resource desktopového sestavení) |
| 3 | Dotyková klávesnice | stisk číslic, 2nd→Prt; `2nd Prt` vytiskne hodnotu displeje na pásku |
| 4 | Hardware klávesy | `KEYCODE_9` → displej `9.`; `KEYCODE_F1` spustí program A (PC 095, HUD Lunar Lander) — mapování shodné s MAUI |
| 5 | Menu pod tlačítkem **Menu** | `MenuFlyout`: New/Open/Save Program, Load Library, Virtual Printer, Program Debugger, Help, About, Exit |
| 6 | Open Program (.ti5p) přes SAF | DocumentsUI → `lunar_lander.ti5p` → stav `Loaded program from lunar_lander.ti5p`, karta LUNAR LANDER na proužku |
| 7 | Běh programu + debugger | kroky 000–028 s kódy/mnemonikami, registry R00: 600, R01: −500, R02: 2500 |
| 8 | Virtual Printer | nav bar + páska s vytisknutou hodnotou, Advance, Copy Tape (schránka), Clear Tape |
| 9 | Save Program přes SAF | vytvoření `program.ti5p` v Downloads; `adb pull` + `unzip -l`: `mimetype` (40 B, správný MIME), `prog00` (XML s BOM), `card00`, `help00` |
| 10 | Load Library přes SAF | načtení knihovny z pickeru (viz poznámka o MIME níže) |
| 11 | Nápověda a About | `HelpPage` obsahuje všech 8 českých sekcí (jednotný zdroj `HelpSections`), `AboutPage` vypíše text s odstavci |
| 12 | Tlačítko zpět systému | popne `NavigationPage` (tiskárna→kalkulačka); na kořenové stránce aplikaci ukončí |
| 13 | Exit v menu | `FinishAffinity`; `dumpsys` potvrdí zánik aktivity (launcher má focus) |
| 14 | `logcat` přes celé sezení | žádné `FATAL EXCEPTION`, žádné Avalonia chyby; hlášení jen Info/GC |

Poznámky k pozorovanému chování:

- **Zpět vs. navigace:** bez `PageNavigationHost` kolem kořenu systémové zpět **ukončilo aplikaci** (bod v plánu označený jako riziko se potvrdil a je vyřešen obalením do `PageNavigationHost` podle oficiální Avalonia xplat šablony). Po opravě je chování identické s MAUI.
- **Schránka (rizikový bod z plánu):** `Copy Tape` používá `TopLevel.GetTopLevel(this)?.Clipboard` a `ClipboardExtensions.SetTextAsync` (v 12.0.4 je `SetTextAsync` rozšiřující metoda, přímo na `IClipboard` neexistuje). Kopírování proběhlo bez chyby; obsah schránky se na emulátoru přímo neověřoval (pouze absence výjimky a správného toku), plná verifikace by vyžadovala aplikaci/IME pro čtení schránky.
- **SAF bez filtru přípon:** výběr s MIME `*/*` je shodný s MAUI chováním (FilePicker `Android = ["*/*"]`); lze vybrat libovolný soubor, špatný formát se projeví jen hláškou ve stavovém řádku.
- **Pozice prvků:** navigační lišta (`NavigationPage`) má systémově světlé pozadí, obsah stránky je pod ní (`BarLayoutBehavior.Inset`). Vzhled nezůstává v tmavé barevné schématu aplikace (rozdíl oproti MAUI, kde menu/action sheet řeší platforma) — kosmetika, viz odstavec omezení.

## 4. Odchylky a úskalí oproti plánu

| Plán | Skutečnost | Důsledek |
|---|---|---|
| minSdk 21 | AndroidX `lifecycle-runtime` (závislost `Avalonia.Android 12.0.4`) vyžaduje minSdk **23** | `SupportedOSPlatformVersion` = 23.0; oficiální Avalonia šablona používá také 23 |
| `AvaloniaMainActivity<TApp>` (generický) | v 12.0.4 **nengenerický** `AvaloniaMainActivity`; aplikaci spouští `AvaloniaAndroidApplication<TApp>` jako podtřída `Android.App.Application` | head má `MainApplication` + `MainActivity` podle oficiální šablony |
| `sv.MainView` přes `ISingleViewApplicationLifetime` | funguje, ale Avalonia.Android doporučuje (a bez varování vyžaduje) `IActivityApplicationLifetime.MainViewFactory`; bez `PageNavigationHost` navíc nefunguje systémové zpět | větev v `App` napřed použije `MainViewFactory`, root je `PageNavigationHost` |
| odkaz head → desktop WinExe | `NETSDK1150` (executable reference) a `NETSDK1047` (RID-specific obnova odkazovaného projektu) | `ValidateExecutableReferencesMatchSelfContained=false` + `GlobalPropertiesToRemove="RuntimeIdentifier;SelfContained"` na `ProjectReference` (detaily v [EMULATOR_VYVOJ, 5.4](EMULATOR_VYVOJ.md#54-technická-úskalí-android-head-avalonia)) |
| aktivita z plánu `Theme = Maui.SplashTheme` | nepřenosné; AvaloniaActivity = `AppCompatActivity` | vlastní `MyTheme.NoActionBar` (Theme.AppCompat.DayNight.NoActionBar) + barva pozadí, žádná splash obrazovka |
| ikona aplikace „volitelně mipmap z legacy res“ | realizováno (výchozí robot Androidu) | kosmetické omezení, pro produkční verzi doplnit mipmap sety |

## 5. Srovnání s MAUI variantou

| Hledisko | MAUI (`TI5X.Maui`) | Avalonia (`TI5X.Avalonia.Android`) |
|---|---|---|
| Sdílení kódu | jádro + VM, UI duplicitní (XAML MAUI) | jádro + VM + **sdílené UI** (stránky i ovládací prvky); desktop bez duplikace |
| Minimální Android | API 21 | API 23 (dané Avalonií) |
| Velikost APK | ≈ 22 MB | ≈ 31 MB (Skia + vlastní pipeline) |
| Stabilita runtime | 0 chyb | 0 chyb; 0 FATÁL i v celém sezení |
| Start na API 33 (SoftShader, arm64) | ~1,5–2 s | ~1,2–2 s (porovnatelné) |
| Vzhled nav lišty/dialogů | plná kontrola přes .NET UI | systémová `NavigationPage` lišta (světlá), `MenuFlyout` Avalonie |
| Vývojářské nároky | MAUI workload + MAUI specifika u každé stránky | čistý Avalonia; Android head ~120 řádků aplikace; řeší se jen bootstrap a SAF |
| Rizika | MAUI na .NET 10 stabilní, ale platforma vázaná na Microsoft tempo | Avalonia.Android je v 12.x „experimental“ (podle dokumentace balíčku), navigation stack je nový v 12.x |

## 6. Doporučení

1. **MAUI zůstává hlavní Android variantou** pro šíření (menší APK, minSdk 21, jednodušší integrace platformy).
2. **Avalonia head si dále vyplatí rozvíjet** jako experiment: sdílení UI s desktopem znamená, že každá nová funkce desktopu na Android skoro zadarmo dorazí. Klíčové bloker riziko (zpět/navigace) se podařilo vyřešit; zbývající známé nedostatky jsou kosmetické (ikona, tmavá nav lišta).
3. Případné pokračování (out of scope tohoto spikey): ikona/splash (mipmap + drawable), tmavá `NavigationPage` lišta (`RequestedThemeVariant` / styly), ověřit chování na reálném zařízení a na gestech (predictive back, Android 13+), zvážit `BarLayoutBehavior.Overlay` + safe-area padding pro fullscreen displej.

## 7. Reprodukce smoke testu

```sh
cd TI5X.Avalonia
dotnet publish src/TI5X.Avalonia.Android/TI5X.Avalonia.Android.csproj -c Release -f net10.0-android -p:AndroidPackageFormats=apk
~/Library/Android/sdk/platform-tools/adb install -r src/TI5X.Avalonia.Android/bin/Release/net10.0-android/publish/nz.gen.geek_central.ti5x.avalonia-Signed.apk
~/Library/Android/sdk/platform-tools/adb push src/TI5X.Avalonia/Assets/lunar_lander.ti5p /sdcard/Download/
~/Library/Android/sdk/platform-tools/adb push src/TI5X.Avalonia/Assets/ml.ti5l /sdcard/Download/
~/Library/Android/sdk/platform-tools/adb shell am start -W -n nz.gen.geek_central.ti5x.avalonia/crc64e31de4a059778544.MainActivity
```

V emulátoru Medium_Tablet_API_33 (1600×2560) pak: dotyk klávesnice, tlačítko **Menu** vlevo nahoře, položky open/save/load, podstránky Printer/Debugger/Help/About, zpět systémovým tlačítkem, ukončení přes **Exit**. Kontrola: `adb exec-out screencap -p > shot.png`, `adb shell uiautomator` nelze použít (Jedno nativní okno Avalonia), `logcat -d | grep -c "FATAL EXCEPTION"` = 0.