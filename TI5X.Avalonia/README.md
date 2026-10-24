# TI5X pro .NET 10, Avalonia 12 a Android (MAUI i Avalonia)

Emulátor programovatelné kalkulačky **TI-59** (a TI-58) pro .NET 10: desktopová aplikace a dvě mobilní aplikace pro Android – stabilní varianta v .NET MAUI a experimentální varianta přímo v Avalonia 12.0.4. Projekt je portovaný z původního Android projektu TI5X v tomto repozitáři; původní Android zdroje zůstávají zachované. Emulační jádro, view modely i sdílené UI komponenty jsou společné pro všechny tři aplikace.

## Spuštění

Vyžaduje .NET 10 SDK a grafické prostředí macOS, Windows nebo Linux.
Z adresáře `TI5X.Avalonia` (tj. adresáře s tímto souborem `README.md`; pozor — ne z kořene repozitáře, v něm leží původní Android projekt):

```sh
dotnet run --project src/TI5X.Avalonia/TI5X.Avalonia.csproj
```

## Android (MAUI)

Mobilní aplikace cílí na `net10.0-android` a vyžaduje workload `maui-android` a Android SDK platform 36 (compile/target), minimální verze je Android 5.0 (API 21). Aplikace je ověřená na Androidu 13 (API 33).

```sh
dotnet workload install maui-android
dotnet build src/TI5X.Maui/TI5X.Maui.csproj -c Release -f net10.0-android
# nasazení na běžící emulátor nebo zařízení:
dotnet build src/TI5X.Maui/TI5X.Maui.csproj -t:Run -f net10.0-android
# distribuovatelné APK:
dotnet publish src/TI5X.Maui/TI5X.Maui.csproj -c Release -f net10.0-android -p:AndroidPackageFormats=apk
```

APK najdete v `src/TI5X.Maui/bin/Release/net10.0-android/publish/`. Aplikace nevyžaduje oprávnění k úložišti; soubory `.ti5p`/`.ti5l` otevírá a ukládá přes systémový výběr souborů (SAF). Bližší popis je v [uživatelské příručce](docs/EMULATOR_UZIVATEL.md#12-android-aplikace-maui).

## Android (Avalonia, experimentální)

Spike aplikace postavená přímo na **Avalonia 12** bez MAUI (`src/TI5X.Avalonia.Android`): používá tytéž sdílené stránky jako desktop, navigaci `NavigationPage` a SAF pro soubory. Vyžaduje Android 6.0 (API 23), sestavení cílí na API 36 a je ověřené na Androidu 13 (API 33). Instaluje se vedle MAUI varianty (odlišné ID aplikace).

```sh
dotnet publish src/TI5X.Avalonia.Android/TI5X.Avalonia.Android.csproj -c Release -f net10.0-android -p:AndroidPackageFormats=apk
adb install -r src/TI5X.Avalonia.Android/bin/Release/net10.0-android/publish/nz.gen.geek_central.ti5x.avalonia-Signed.apk
```

Popis a vyhodnocení spikeu (ověřené funkce, načtená data, doporučení) jsou v [docs/AVALONIA_ANDROID_VYHODNOCENI.md](docs/AVALONIA_ANDROID_VYHODNOCENI.md); ovládání je popsáno v [uživatelské příručce, kap. 13](docs/EMULATOR_UZIVATEL.md#13-android-aplikace-avalonia-experimentální).

## Ovládání

- Klávesnice kalkulačky na obrazovce: číselný vstup, aritmetika, vědecké funkce, paměti, režimy DEG/RAD/GRAD a programování.
- Fyzická klávesnice: číslice (včetně numerické), `.` `+` `-` `*` `/`, `Enter` = `=`, `Backspace` = `CE`, `Escape` = `CLR`, `F1`–`F5` = `A`–`E`.
- Menu **File**: `New Program`, `Open Program (.ti5p)...`, `Save Program (.ti5p)...`, `Load Library Module (.ti5l)...`, `Exit`. V Android aplikaci je celé menu pod tlačítkem **Menu** v horní liště.
- Menu **View**: `Virtual Printer (PC-100A)` a `Program Debugger / Steps`.
- Menu **Help**: `TI-59 / Master Library Guide` a `About TI5X Avalonia`.

## Dokumentace

Veškerá dokumentace je v češtině v adresáři [`docs/`](docs/):

| Dokument | Obsah |
|---|---|
| [Příručka kalkulačky TI-59](docs/PRIRUCKA_TI59.md) | Plná příručka: klávesnice, počítání, AOS, paměti, statistika, programování, tabulka všech 100 kódů instrukcí, ukázkové programy. |
| [Uživatelská příručka emulátoru](docs/EMULATOR_UZIVATEL.md) | Spuštění aplikace, okno, menu, klávesové zkratky, soubory `.ti5p`/`.ti5l`, virtuální tiskárna, debugger, Lunar Lander. |
| [Vývojářská dokumentace](docs/EMULATOR_VYVOJ.md) | Architektura MVVM, projekt `TI5X.Core`, formáty souborů, testy, Dafny verifikace, postup rozšiřování. |
| [Odchylky emulace a známá omezení](docs/ODCHYLKY_A_OMEZENI.md) | Jediný úplný seznam rozdílů oproti originálu TI-59 a chybějících funkcí. |
| [Specifikace Lunar Lander](docs/LUNAR_LANDER_SPEC.md) | Ukázkový program: pravidla hry, fyzikální model, kompletní výpis 142 kroků. |
| [Vyhodnocení Android spikey na Avalonii](docs/AVALONIA_ANDROID_VYHODNOCENI.md) | Experimentální head `TI5X.Avalonia.Android`: funkční protokol smoke testu, naměřená data, výhody/nevýhody oproti MAUI a doporučení. |

> **Důležité:** programy zapsané v režimu `LRN` se ukládají ve standardním kódování TI-59 a lze je spouštět; programy ze **starší verze emulátoru** (surové kódy kláves) je potřeba přepsat. Postup psaní programů popisuje [příručka, kapitola 15](docs/PRIRUCKA_TI59.md#152-pravidla-kódování-programu).

## Testy

Testy (TUnit na Microsoft.Testing.Platform) pokrývají celé jádro bez grafického prostředí – 64 testů: migrovaná sada Lunar Lander, základní aritmetika, displej, bezpečnost mezí a souběhu, conformance instrukční sady (sloučené kódy, LRN kódování), vyčerpávající sweep všech opcodů × operandů a diferenciální test proti Dafny ověřenému modelu:

```sh
dotnet test --solution TI5X.slnx
```

Testy nic nezapisují do repozitáře. Přehled testů a postup přidání nového popisuje [vývojářská dokumentace, kapitola 7](docs/EMULATOR_VYVOJ.md#7-testy). CI navíc spouští kontrolu formátování, pokrytí, SBOM, Dafny důkazy a sestavení Android APK.

## Omezení

Emulátor není ověřenou emulací všech detailů TI-59. Úplný seznam rozdílů oproti originálu (např. chování příznaku 8, `HIR`, přepočet Op 16/17, statistika, tiskárna) a známých omezení (neúplná Master Library, `.ti5p` neukládá registry, omezená fyzická klávesnice, GUI bez automatizovaných testů, Android ověřený ručním smoke testem) vede [Odchylky emulace a známá omezení](docs/ODCHYLKY_A_OMEZENI.md).

Licenční podmínky původního projektu jsou v souboru `COPYING` v kořeni repozitáře.