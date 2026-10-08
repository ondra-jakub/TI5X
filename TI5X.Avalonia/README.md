# TI5X pro .NET 10 a Avalonia 12

Desktopový emulátor programovatelné kalkulačky **TI-59** (a TI-58) pro .NET 10 a Avalonia 12.0.4, portovaný z Android projektu TI5X v tomto repozitáři. Původní Android zdroje zůstávají zachované.

## Spuštění

Vyžaduje .NET 10 SDK a grafické prostředí macOS, Windows nebo Linux.
Z kořenového adresáře repozitáře:

```sh
dotnet run --project TI5X.Avalonia/TI5X.Avalonia.csproj
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
| [Vývojářská dokumentace](docs/EMULATOR_VYVOJ.md) | Architektura MVVM, třídy `Core`, formáty souborů, testy, postup rozšiřování. |
| [Odchylky emulace a známá omezení](docs/ODCHYLKY_A_OMEZENI.md) | Jediný úplný seznam rozdílů oproti originálu TI-59 a chybějících funkcí. |
| [Specifikace Lunar Lander](docs/LUNAR_LANDER_SPEC.md) | Ukázkový program: pravidla hry, fyzikální model, kompletní výpis 142 kroků. |

> **Důležité:** program zapsaný z klávesnice v režimu `LRN` se v emulátoru uloží chybně a nefunguje. Funkční program vytvořte jako soubor `.ti5p` (postup v [příručce, kapitola 15](docs/PRIRUCKA_TI59.md#152-pravidla-kódování-programu)) a načtěte ho přes `File > Open Program (.ti5p)...`.

## Testy

Samostatný konzolový testovací projekt nevyžaduje grafické prostředí ani testovací balíčky (xUnit/NUnit nepoužívá, `dotnet test` nic nespustí):

```sh
dotnet run --project TI5X.Avalonia/TI5X.Avalonia.Tests/TI5X.Avalonia.Tests.csproj
```

Testů je 17: základní aritmetika a priorita operací, úhlové režimy a převody, a dále 12 testů hry Lunar Lander (inicializace, volný pád, vznášení, průlet, omezení paliva, bezpečné přistání i havárie, telemetrie, vítězná hra), test ukládání a načtení `.ti5p`, generování souboru `Assets/lunar_lander.ti5p` a dva testy přes cestu GUI (`ProcessKey`). Program vypisuje `[PASS]`/`[FAIL]` u každého testu a vrací návratový kód 0 (úspěch) nebo 1 (selhání).

Přehled jednotlivých testů, jejich závislosti a postup přidání nového testu popisuje [vývojářská dokumentace, kapitola 7](docs/EMULATOR_VYVOJ.md#7-testy).

> ⚠️ Test `Generate Assets/lunar_lander.ti5p` přepíše soubor `Assets/lunar_lander.ti5p` v adresáři projektu. Po spuštění testů proto zkontrolujte `git status`.

## Omezení

Emulátor není ověřenou emulací všech detailů TI-59. Úplný seznam rozdílů oproti originálu (např. chování režimu `LRN`, sloučené kódy instrukcí, příznaky, statistika, tiskárna) a známých omezení (neúplná Master Library, `.ti5p` neukládá registry, omezená fyzická klávesnice, GUI bez automatizovaných testů) vede [Odchylky emulace a známá omezení](docs/ODCHYLKY_A_OMEZENI.md).

Licenční podmínky původního projektu jsou v souboru `COPYING` v kořeni repozitáře.