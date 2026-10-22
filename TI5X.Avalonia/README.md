# TI5X pro .NET 10 a Avalonia 12

Desktopový emulátor programovatelné kalkulačky **TI-59** (a TI-58) pro .NET 10 a Avalonia 12.0.4, portovaný z Android projektu TI5X v tomto repozitáři. Původní Android zdroje zůstávají zachované.

## Spuštění

Vyžaduje .NET 10 SDK a grafické prostředí macOS, Windows nebo Linux.
Z kořenového adresáře repozitáře:

```sh
dotnet run --project TI5X.Avalonia/src/TI5X.Avalonia/TI5X.Avalonia.csproj
```

## Ovládání

- Klávesnice kalkulačky na obrazovce: číselný vstup, aritmetika, vědecké funkce, paměti, režimy DEG/RAD/GRAD a programování.
- Fyzická klávesnice: číslice (včetně numerické), `.` `+` `-` `*` `/`, `Enter` = `=`, `Backspace` = `CE`, `Escape` = `CLR`, `F1`–`F5` = `A`–`E`.
- Menu **File**: `New Program`, `Open Program (.ti5p)...`, `Save Program (.ti5p)...`, `Load Library Module (.ti5l)...`, `Exit`.
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

> **Důležité:** programy zapsané v režimu `LRN` se ukládají ve standardním kódování TI-59 a lze je spouštět; programy ze **starší verze emulátoru** (surové kódy kláves) je potřeba přepsat. Postup psaní programů popisuje [příručka, kapitola 15](docs/PRIRUCKA_TI59.md#152-pravidla-kódování-programu).

## Testy

Testy (TUnit na Microsoft.Testing.Platform) pokrývají celé jádro bez grafického prostředí – 56 testů: migrovaná sada Lunar Lander, základní aritmetika, displej, bezpečnost mezí a souběhu, conformance instrukční sady (sloučené kódy, LRN kódování), vyčerpávající sweep všech opcodů × operandů a diferenciální test proti Dafny ověřenému modelu:

```sh
dotnet test --solution TI5X.Avalonia/TI5X.slnx
```

Testy nic nezapisují do repozitáře. Přehled testů a postup přidání nového popisuje [vývojářská dokumentace, kapitola 7](docs/EMULATOR_VYVOJ.md#7-testy). CI navíc spouští kontrolu formátování, pokrytí, SBOM a Dafny důkazy.

## Omezení

Emulátor není ověřenou emulací všech detailů TI-59. Úplný seznam rozdílů oproti originálu (např. chování příznaku 8, `HIR`, přepočet Op 16/17, statistika, tiskárna) a známých omezení (neúplná Master Library, `.ti5p` neukládá registry, omezená fyzická klávesnice, GUI bez automatizovaných testů) vede [Odchylky emulace a známá omezení](docs/ODCHYLKY_A_OMEZENI.md).

Licenční podmínky původního projektu jsou v souboru `COPYING` v kořeni repozitáře.