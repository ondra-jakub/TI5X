# TI5X pro .NET 10 a Avalonia 12

Desktopový port kalkulačky TI-59 z tohoto Android projektu. Používá .NET 10 a Avalonia 12.0.4; původní Android zdroje zůstávají zachované.

## Spuštění

Vyžaduje .NET 10 SDK a grafické prostředí macOS, Windows nebo Linux.
Z kořenového adresáře repozitáře:

```sh
dotnet run --project TI5X.Avalonia/TI5X.Avalonia.csproj
```

## Ovládání

- Klávesnice kalkulačky: číselný vstup, aritmetika, vědecké funkce, paměti, režimy DEG/RAD/GRAD a programování.
- Fyzická klávesnice: číslice, numerické operátory, Enter pro výsledek, Escape pro CLR, Backspace pro CE, F1–F5 pro A–E.
- Menu File: otevření a uložení programů `.ti5p`, načtení knihoven `.ti5l`.
- Menu View: virtuální tiskárna PC-100A a debugger s programovými kroky a registry.
- Menu Help: stručná nápověda a informace o aplikaci.

## Ověření

Samostatný konzolový testovací projekt nevyžaduje grafické prostředí ani další testovací balíčky:

```sh
dotnet run --project TI5X.Avalonia.Tests/TI5X.Avalonia.Tests.csproj
```

Ověřuje LED desetinné tečky, plnou šířku displeje, prioritu operací, dělení nulou a převody úhlů. Při selhání vrací nenulový návratový kód.

## Omezení

Jde o počáteční desktopový port, nikoli o ověřenou emulaci všech detailů TI-59 nebo plnou funkční shodu s Android verzí. Dodaná knihovna obsahuje pouze dostupnou banku z `assets/ml.ti5l-new`, nikoli kompletní Master Library. Úplná kompatibilita programových instrukcí, souborů a periferií vyžaduje další porovnávací testy. Grafické rozhraní zatím není automatizovaně otestované.

Licenční podmínky původního projektu jsou v souboru `COPYING` v kořeni repozitáře.