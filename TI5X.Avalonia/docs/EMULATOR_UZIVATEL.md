# Uživatelská příručka emulátoru TI5X (Avalonia a Android)

Tato příručka popisuje ovládání emulátoru kalkulačky TI-59 pro .NET 10: desktopové aplikace v Avalonia 12 a mobilní aplikace pro Android v .NET MAUI. Vysvětluje okno aplikace, menu, klávesové zkratky, práci se soubory, virtuální tiskárnu a debugger. Androidová verze se ovládá stejně; její odlišnosti shrnuje [kapitola 12](#12-android-aplikace-maui) a experimentální varianta přímo na Avalonii [kapitola 13](#13-android-aplikace-avalonia-experimentální).

Související dokumenty:

| Dokument | Obsah |
|---|---|
| [Příručka kalkulačky TI-59](PRIRUCKA_TI59.md) | Počítání a programování na TI-59, tabulka kódů instrukcí, ukázkové programy. |
| [Odchylky emulace a známá omezení](ODCHYLKY_A_OMEZENI.md) | Úplný seznam rozdílů oproti originálu a chybějících funkcí. |
| [Specifikace Lunar Lander](LUNAR_LANDER_SPEC.md) | Ukázkový program – pravidla hry a numerický model. |
| [Vývojářská dokumentace](EMULATOR_VYVOJ.md) | Architektura, formáty souborů `.ti5p`/`.ti5l`, testy. |

Názvy položek menu, tlačítek a oken jsou v aplikaci anglicky a v této příručce se uvádějí v původním znění.

> **Nejdůležitější upozornění.** Programy zapsané v režimu `LRN` se ukládají ve standardním kódování TI-59 a lze je spouštět. Programy zapsané **starší verzí emulátoru** (surové kódy kláves) je potřeba přepsat. Detaily viz [Odchylky, kapitola 2](ODCHYLKY_A_OMEZENI.md#2-nejdůležitější-omezení) a [Pravidla kódování programu](PRIRUCKA_TI59.md#152-pravidla-kódování-programu).

## Obsah

1. [Požadavky a spuštění](#1-požadavky-a-spuštění)
2. [Hlavní okno](#2-hlavní-okno)
   - [2.1 Horní lišta a indikátory](#21-horní-lišta-a-indikátory)
   - [2.2 LED displej](#22-led-displej)
   - [2.3 Proužek karty](#23-proužek-karty)
   - [2.4 Klávesnice](#24-klávesnice)
   - [2.5 Stavový řádek](#25-stavový-řádek)
3. [Menu](#3-menu)
4. [Fyzická klávesnice počítače](#4-fyzická-klávesnice-počítače)
5. [Programy .ti5p](#5-programy-ti5p)
6. [Knihovny .ti5l a vestavěná Master Library](#6-knihovny-ti5l-a-vestavěná-master-library)
7. [Virtuální tiskárna PC-100A](#7-virtuální-tiskárna-pc-100a)
8. [Debugger programu](#8-debugger-programu)
9. [Ukázka: Lunar Lander](#9-ukázka-lunar-lander)
10. [Nápověda a informace o aplikaci](#10-nápověda-a-informace-o-aplikaci)
11. [Časté problémy](#11-časté-problémy)
12. [Android aplikace (MAUI)](#12-android-aplikace-maui)
13. [Android aplikace (Avalonia, experimentální)](#13-android-aplikace-avalonia-experimentální)

---

## 1. Požadavky a spuštění

**Požadavky:**

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (projekt cílí na `net10.0`),
- grafické prostředí macOS, Windows nebo Linux (Avalonia 12.0.4 se stáhne automaticky jako balíček NuGet při prvním sestavení).

**Spuštění** z kořenového adresáře repozitáře (adresář nad `TI5X.Avalonia`):

```sh
dotnet run --project TI5X.Avalonia/src/TI5X.Avalonia/TI5X.Avalonia.csproj
```

nebo přímo z adresáře projektu `TI5X.Avalonia/src/TI5X.Avalonia`:

```sh
dotnet run
```

První spuštění trvá déle, protože se obnovují balíčky a sestavuje projekt. Při startu se automaticky načte vestavěná knihovna `src/TI5X.Avalonia/Assets/ml.ti5l` (viz [kapitola 6](#6-knihovny-ti5l-a-vestavěná-master-library)).

Po spuštění je kalkulačka ve výchozím stavu: displej `0.`, úhlový režim `DEG`, prázdný program v bance 0, vynulované registry a příznaky.

---

## 2. Hlavní okno

Okno má titulek `TI-59 / TI5X Calculator (.NET 10 Avalonia)`, výchozí velikost 440 × 820 bodů a nejmenší velikost 380 × 700 bodů. Shora dolů obsahuje:

1. menu `File`, `View`, `Help` (viz [kapitola 3](#3-menu)),
2. horní lištu s nápisem `TEXAS INSTRUMENTS TI-59` a indikátory,
3. LED displej,
4. proužek karty se štítky kláves A–E',
5. klávesnici 9 × 5 kláves,
6. stavový řádek.

### 2.1 Horní lišta a indikátory

Vpravo na horní liště se zobrazují malé indikátory:

| Indikátor | Kdy se zobrazuje |
|---|---|
| `DEG` / `RAD` / `GRAD` | Vždy. Aktuální úhlový režim (přepíná se `2nd Deg`, `2nd Rad`, `2nd Grad`). |
| `2nd` | Po stisku `2nd` – další klávesa provede svou druhou funkci. |
| `INV` | Po stisku `INV` – další funkce se provede inverzně. |
| `LRN` | V režimu zápisu programu `LRN`. |

### 2.2 LED displej

Displej napodobuje červený sedmisegmentový LED displej TI-59 s 12 pozicemi (mantisa se znaménkem a dvoumístný exponent) a desetinnými tečkami.

| Stav | Vzhled |
|---|---|
| Běžný stav | Svítí výsledek nebo zadávané číslo. |
| Chyba | Číslice blikají (interval 300 ms). Bliká naposledy zobrazená hodnota. Chybu zruší `CE`, `CLR` nebo zadání nového čísla. |
| Běh programu | Displej střídavě mění jas (ztlumeně bliká). Během běhu reaguje kalkulačka jen na `R/S`, které program zastaví. |
| Režim `LRN` | Ukazuje `PPP KK` – číslo kroku a kód instrukce v něm. |

Formáty zobrazení (`Fix`, `EE`, `Eng`) popisuje [Příručka TI-59, kapitola 3](PRIRUCKA_TI59.md#3-zadávání-čísel-a-formáty-zobrazení).

### 2.3 Proužek karty

Pod displejem je proužek, který napodobuje papírovou kartu vkládanou nad klávesy A–E originálu. Zobrazuje popis karty **aktuální banky**:

- nahoře levý a pravý titulek (`titleleft`, `titleright`),
- v pěti sloupcích nad sebou štítek druhé funkce (`A'`–`E'`, červeně) a štítek primární klávesy (`A`–`E`, tučně).

Pokud aktuální banka žádnou kartu nemá, proužek ukazuje šedý text `USER PROGRAM / CARD SLOT`. Kartu dodává soubor `.ti5p` nebo `.ti5l` (položka `card00`, resp. `cardNN`). Proužek je jen popisný – magnetické karty emulátor nepodporuje ([Odchylky 3.11](ODCHYLKY_A_OMEZENI.md#311-knihovní-programy-karty-a-soubory)).

### 2.4 Klávesnice

Klávesnice má 45 kláves ve stejném rozložení jako TI-59. Na každé klávese je primární funkce, nad klávesou žlutě druhá funkce (vyvolá se `2nd` a poté klávesou). Klávesy se ovládají kliknutím myší.

| Řádek | Klávesy (primární / druhá funkce) |
|---|---|
| 1 | `A`/`A´` · `B`/`B´` · `C`/`C´` · `D`/`D´` · `E`/`E´` |
| 2 | `2nd` · `INV` · `lnx`/`log` · `CE`/`CP` · `CLR` |
| 3 | `LRN`/`Pgm` · `x⇌t`/`P→R` · `x²`/`sin` · `√x`/`cos` · `1/x`/`tan` |
| 4 | `SST`/`Ins` · `STO`/`CMs` · `RCL`/`Exc` · `SUM`/`Prd` · `yˣ`/`Ind` |
| 5 | `BST`/`Del` · `EE`/`Eng` · `(`/`Fix` · `)`/`Int` · `÷`/`\|x\|` |
| 6 | `GTO`/`Pause` · `7`/`x=t` · `8`/`Nop` · `9`/`Op` · `×`/`Deg` |
| 7 | `SBR`/`Lbl` · `4`/`x≥t` · `5`/`∑+` · `6`/`x̄` · `-`/`Rad` |
| 8 | `RST`/`St flg` · `1`/`If flg` · `2`/`D.MS` · `3`/`π` · `+`/`Grad` |
| 9 | `R/S` · `0`/`Dsz` · `.`/`Adv` · `+/-`/`Prt` · `=`/`List` |

Klávesa `R/S` nemá v emulátoru druhou funkci (originální `2nd Write` chybí). Význam jednotlivých funkcí a kódy kláves popisuje [Příručka TI-59, kapitola 2](PRIRUCKA_TI59.md#2-klávesnice).

### 2.5 Stavový řádek

Pod klávesnicí je šedý stavový řádek. Ve výchozím stavu má tvar:

```
User Bank 00 | PC: 000 | Regs: 52
```

| Část | Význam |
|---|---|
| `User Bank 00` / `Library Bank NN` | Aktuální programová banka. Banka 00 je uživatelský program, banky 01 a vyšší jsou knihovní (přepínají se `2nd Pgm nn`, zpět do banky 00 vrátí `RST`). |
| `PC: nnn` | Ukazatel programu – číslo aktuálního kroku. |
| `Regs: nn` | ⚠️ Počítá se jako `100 − RegOffset / 10`, ve výchozím stavu tedy `52`. Číslo **neodpovídá** skutečnému počtu dostupných registrů – emulátor dovoluje používat všech 100 registrů R00–R99 bez ohledu na rozdělení paměti ([Odchylky 3.10](ODCHYLKY_A_OMEZENI.md#310-rozdělení-paměti)). |

Po operaci se soubory se na stavovém řádku dočasně objeví zpráva, např. `Loaded program from kruh.ti5p`, `Saved program to kruh.ti5p`, `Loaded library ml.ti5l (1 programs)` nebo `Error: …` při chybě. Při další aktualizaci stavu (např. stisku klávesy kalkulačky) se stavový řádek vrátí k zobrazení banky, PC a registrů.

---

## 3. Menu

### File

| Položka | Činnost |
|---|---|
| `New Program` | Smaže program banky 0 a provede úplný reset kalkulačky: vynuluje registry, příznaky, zásobníky, displej, nastaví `DEG`, standardní formát, PC = 0 a banku 0. ⚠️ **Odstraní také všechny načtené knihovny včetně vestavěné Master Library** ([Odchylky, bod 8](ODCHYLKY_A_OMEZENI.md#2-nejdůležitější-omezení)). Knihovnu obnovíte novým spuštěním aplikace nebo načtením `src/TI5X.Avalonia/Assets/ml.ti5l` přes `Load Library Module (.ti5l)...`. Na dotaz se neptá. |
| `Open Program (.ti5p)...` | Otevře dialog pro výběr souboru `.ti5p` (filtr `TI-5X Program (*.ti5p)` nebo `All files (*.*)`). Program se nahraje do banky 0, viz [kapitola 5](#5-programy-ti5p). |
| `Save Program (.ti5p)...` | Otevře dialog pro uložení (výchozí přípona `ti5p`). Uloží program banky 0, viz [kapitola 5](#5-programy-ti5p). |
| `Load Library Module (.ti5l)...` | Otevře dialog pro výběr knihovny `.ti5l` (filtr `TI-5X Library (*.ti5l)` nebo `All files (*.*)`), viz [kapitola 6](#6-knihovny-ti5l-a-vestavěná-master-library). |
| `Exit` | Zavře hlavní okno. Aplikace skončí po zavření všech svých oken, takže případně zavřete i okno tiskárny, debuggeru a nápovědy. Program ani registry se automaticky neukládají. |

### View

| Položka | Činnost |
|---|---|
| `Virtual Printer (PC-100A)` | Otevře okno tiskárny ([kapitola 7](#7-virtuální-tiskárna-pc-100a)). Je-li už otevřené, přenese ho do popředí. |
| `Program Debugger / Steps` | Otevře okno debuggeru ([kapitola 8](#8-debugger-programu)). Je-li už otevřené, přenese ho do popředí. |

### Help

| Položka | Činnost |
|---|---|
| `TI-59 / Master Library Guide` | Otevře okno se stručnou nápovědou. Každým výběrem se otevře nové okno. |
| `About TI5X Avalonia` | Zobrazí modální okno s informacemi o aplikaci. |

Menu lze ovládat i klávesnicí podle zvyklostí operačního systému (podtržená písmena v názvech, na Windows a Linuxu s klávesou `Alt`). Jiné klávesové zkratky pro položky menu aplikace nemá.

---

## 4. Fyzická klávesnice počítače

Hlavní okno převádí některé klávesy počítače na klávesy kalkulačky (`MainWindow.OnKeyDown`). Ostatní funkce se ovládají myší.

| Klávesa počítače | Klávesa TI-59 |
|---|---|
| `0`–`9` (horní řada i numerická klávesnice) | `0`–`9` |
| `.` (tečka) a desetinná tečka numerické klávesnice | `.` |
| `+` (klávesa `OemPlus`) a `+` na numerické klávesnici | `+` |
| `-` (klávesa `OemMinus`) a `-` na numerické klávesnici | `-` |
| `*` na numerické klávesnici | `×` |
| `/` na numerické klávesnici | `÷` |
| `Enter` | `=` |
| `Backspace` | `CE` |
| `Escape` | `CLR` |
| `F1`, `F2`, `F3`, `F4`, `F5` | `A`, `B`, `C`, `D`, `E` |

Omezení ([Odchylky 3.1](ODCHYLKY_A_OMEZENI.md#31-klávesnice-2nd-a-inv)):

- Klávesy pro `2nd`, `INV`, `STO`, `RCL`, `R/S`, závorky, `A'`–`E'` ani další funkce neexistují – použijte myš. Klávesu `A'` vyvoláte jako `2nd` (myší) a `F1`.
- Modifikátor `Shift` se nevyhodnocuje. Na americkém rozložení je `OemPlus` klávesa `=`/`+`, takže i stisk `=` vloží `+`. Kombinace `Shift+8` (`*` na hlavní klávesnici) vloží číslici `8`; násobení a dělení proto zadávejte z numerické klávesnice.
- Klávesy fungují, jen když je aktivní hlavní okno. V okně tiskárny ani debuggeru se nepřevádějí.
- Kliknutím myší získá klávesa kalkulačky fokus jako běžné tlačítko. Pokud `Enter` nebo mezerník nereagují podle očekávání, může je zachytit tlačítko s fokusem; klikněte mimo klávesy (např. na displej) a zkuste to znovu.
- Během běhu programu se stisky kromě `R/S` (jen myší) zahazují.

---

## 5. Programy .ti5p

Soubor `.ti5p` obsahuje jeden uživatelský program a nahrazuje magnetickou kartu originálu. Je to archiv ZIP s položkami `mimetype`, `prog00` (kódy kroků), volitelně `card00` (popis karty) a `help00` (nápověda). Podrobný popis formátu je ve [vývojářské dokumentaci](EMULATOR_VYVOJ.md#6-formáty-souborů); ruční vytvoření souboru ukazuje [Příručka TI-59, 15.3](PRIRUCKA_TI59.md#153-vytvoření-souboru-ti5p).

### Načtení programu

`File > Open Program (.ti5p)...`:

- program se zkopíruje do banky 0 od kroku 000, zbývající kroky se vynulují,
- karta a nápověda ze souboru se přiřadí bance 0 a karta se zobrazí na proužku,
- PC se nastaví na 000 a vybere se banka 0,
- **registry, příznaky, úhlový režim, formát zobrazení ani knihovny se nemění.** Pokud program počítá s vynulovanými registry, použijte předem `2nd CMs` (nebo `File > New Program`, který ale smaže i knihovny).

Pokud soubor neobsahuje položku `prog00`, nic se nenačte. Chyba při čtení (poškozený ZIP apod.) se zobrazí na stavovém řádku jako `Error: …`.

### Spuštění programu

Programy obvykle začínají návěštím `2nd Lbl A` (až `E'`). Zadejte vstupní hodnotu a stiskněte příslušnou klávesu `A`–`E` (myší nebo `F1`–`F5`), pro `A'`–`E'` nejdřív `2nd`. Program lze také spustit od aktuálního kroku klávesou `R/S` (po `RST` od kroku 000).

### Uložení programu

`File > Save Program (.ti5p)...` uloží:

- **všech 960 kroků programu banky 0** (i když je vybraná knihovní banka),
- kartu a nápovědu **aktuálně vybrané** banky – pokud je zvolená knihovní banka, uloží se k uživatelskému programu její karta. Před uložením proto vraťte banku 0 klávesou `RST`.

Soubor **neobsahuje** registry, příznaky, úhlový režim, formát zobrazení ani rozdělení paměti ([Odchylky 3.11](ODCHYLKY_A_OMEZENI.md#311-knihovní-programy-karty-a-soubory)). Data, která program potřebuje, je nutné po načtení zadat znovu.

### Vytváření programů

Programy lze zapisovat dvěma způsoby:

1. **Z klávesnice v režimu `LRN`** (nyní ukládá standardní kódy instrukcí TI-59): `LRN` zapne režim, displej ukazuje `PPP KK`, každá klávesa uloží kód instrukce a posune ukazatel, `SST`/`BST` posouvají po krocích, `2nd Ins`/`2nd Del` vkládají a mažou kroky, druhý stisk `LRN` režim ukončí.
2. **Jako soubor `.ti5p`** sestavený podle [Tabulky kódů](PRIRUCKA_TI59.md#15-tabulka-kódů-instrukcí) a [Pravidel kódování](PRIRUCKA_TI59.md#152-pravidla-kódování-programu), načtený přes `File > Open Program (.ti5p)...`.

Hotové ověřené programy najdete v [Ukázkových programech](PRIRUCKA_TI59.md#16-ukázkové-programy).

---

## 6. Knihovny .ti5l a vestavěná Master Library

Soubor `.ti5l` je knihovní modul – archiv ZIP s více programy (`prog01`, `prog02`, …), jejich kartami (`cardNN`), nápovědou (`helpNN`) a celkovou nápovědou (`help`). Každý program se uloží do knihovní banky s odpovídajícím číslem.

### Vestavěná knihovna

Při startu aplikace se automaticky načte `src/TI5X.Avalonia/Assets/ml.ti5l`, která je zabudovaná přímo do aplikace. Obsahuje všech **25 programů** Master Library, sestavených z dumpu originálního CROM TMC0541 ([Odchylky 3.11](ODCHYLKY_A_OMEZENI.md#311-knihovní-programy-karty-a-soubory)). Volání návěšťím přijímá libovolný nečíslicový klíč (ML-01 používá např. `SBR =`); korelace je navíc dostupná i jako `Op 14`, jak ji čísuluje originál.

### Načtení vlastní knihovny

`File > Load Library Module (.ti5l)...`:

- programy z knihovny se uloží do bank podle svých čísel a **přepíší** banky se stejným číslem; ostatní banky zůstanou,
- uživatelský program v bance 0 ani registry se nemění,
- stavový řádek ukáže `Loaded library název.ti5l (N programs)`.

### Volání knihovního programu

1. `2nd Pgm nn` – vybere banku `nn` (stavový řádek ukáže `Library Bank NN` a proužek kartu banky),
2. zadejte vstup a stiskněte klávesu `A`–`E'` podle karty programu.

⚠️ Na rozdíl od originálu zůstane banka vybraná i po skončení programu, takže další stisky `A`–`E'` se znovu hledají v knihovně. Do uživatelského programu (banka 00) se vrátíte klávesou `RST` nebo `2nd Pgm 00`. Podrobnosti viz [Příručka TI-59, kapitola 11](PRIRUCKA_TI59.md#11-knihovní-programy-a-pgm).

Nápověda uložená v knihovně (`help`, `helpNN`) se načte, ale aplikace ji zatím nikde nezobrazuje.

---

## 7. Virtuální tiskárna PC-100A

Okno `PC-100A Virtual Thermal Printer` otevřete přes `View > Virtual Printer (PC-100A)`. Napodobuje termotiskárnu PC-100A s papírovou páskou o šířce 20 znaků.

Tiskárna je k emulátoru připojena trvale – tiskne se i při zavřeném okně a po otevření okno ukáže celou dosavadní pásku. Páska pojme nejvýše **1000 řádků**, nejstarší řádky se zahazují. Zobrazuje se jako obyčejný text neproporcionálním písmem.

| Tlačítko | Činnost |
|---|---|
| `Advance` | Přidá na pásku prázdný řádek (jako `2nd Adv`). |
| `Copy Tape` | Zkopíruje celou pásku jako text do schránky (řádky oddělené koncem řádku). |
| `Clear Tape` | Smaže celou pásku. Akce je nevratná a nevyžaduje potvrzení. |

Na pásku tisknou tyto funkce kalkulačky:

| Funkce | Výstup |
|---|---|
| `2nd Prt` | Obsah displeje zarovnaný doprava. |
| `2nd Adv` | Prázdný řádek. |
| `2nd List` | Výpis programu `PPP  KK MNE` – od aktuálního kroku, jako v originále; počátek posune `GTO nnn` mimo LRN. Výpis výchozího rozdělení (960 kroků) se na pásku (1000 řádků) vejde celý. |
| `Op 0`–`Op 5` | Alfanumerický tisk (Op 0 zde zapíná trasovací tisk, Op 1–4 plní tiskový registr, Op 5 ho vytiskne). |
| `Op 6`, `Op 7` | Jen posun papíru. |
| `Op 8` | Výpis návěští aktuální banky. |
| příznak 7 | Trasovací tisk každého výsledku. |

Rozdíly oproti originální tiskárně jsou v [Odchylkách 3.12](ODCHYLKY_A_OMEZENI.md#312-tiskárna-pc-100a), funkce tisku popisuje [Příručka TI-59, kapitola 12](PRIRUCKA_TI59.md#12-tisk).

---

## 8. Debugger programu

Okno `Program Debugger / Step Inspector` otevřete přes `View > Program Debugger / Steps`. Slouží k prohlížení a krokování programu.

### Obsah okna

| Část | Obsah |
|---|---|
| `Program Steps` (vlevo) | Všechny kroky **aktuální banky**: číslo kroku (`000`), kód (`76`) a mnemonika podle `KeyCodes.KeyCodeSym` (`LBL`). V bance 0 je ve výchozím stavu 960 kroků. Mnemonika se zobrazuje i u bajtů, které jsou ve skutečnosti operandy (např. adresa `42` za `STO` se ukáže jako `STO`). |
| `Data Registers` (vpravo) | Obsah registrů `R00:`, `R01:`, … ve formátu s 8 platnými číslicemi. ⚠️ Zobrazuje jen R00–R51 (počet podle stejného přepočtu jako `Regs` na stavovém řádku), přestože program může používat i R52–R99. |

Aktuální krok (PC) se v seznamu nijak nezvýrazňuje; jeho číslo ukazuje stavový řádek hlavního okna.

### Tlačítka

| Tlačítko | Činnost |
|---|---|
| `Step (SST)` | Provede jednu instrukci od aktuálního kroku (stejně jako `SST` mimo `LRN`) a obnoví zobrazení. Výsledek se ukáže na displeji hlavního okna. |
| `Run / Halt (R/S)` | Spustí program od aktuálního kroku, nebo běžící program zastaví. |
| `Reset (PC=0)` | Nastaví PC na krok 000. Banku, registry ani příznaky nemění (na rozdíl od klávesy `RST`, která vrací i banku 0). |
| `Refresh` | Znovu načte kroky a registry z emulátoru. |

Debugger se obnovuje automaticky po většině operací kalkulačky (např. `STO`, `=`, `Pgm`, `RST`, `2nd`, spuštění a zastavení programu). Ruční `Refresh` je potřeba hlavně po `File > Open Program (.ti5p)...` a `File > Load Library Module (.ti5l)...`, které engine žádnou změnu stavu nehlásí, a po tlačítku `Reset (PC=0)`, po kterém stavový řádek hlavního okna ukazuje starý PC až do další změny stavu. Úpravy kroků ani registrů v debuggeru nejsou možné.

---

## 9. Ukázka: Lunar Lander

Jako ukázkový program je přiložena hra **Lunar Lander** (přistání na Měsíci) v souboru [`src/TI5X.Avalonia/Assets/lunar_lander.ti5p`](../src/TI5X.Avalonia/Assets/lunar_lander.ti5p). Pravidla, fyzikální model a podrobné ovládání popisuje [LUNAR_LANDER_SPEC.md](LUNAR_LANDER_SPEC.md).

Postup:

1. `File > Open Program (.ti5p)...` a vyberte `src/TI5X.Avalonia/Assets/lunar_lander.ti5p` v adresáři projektu (soubor se automaticky nenačítá).
2. Proužek karty ukáže `LUNAR LANDER`, `TI-58 / TI-59` a štítky `INIT`, `BURN`, `FUEL`, `ALT`, `VEL` (a nad nimi `PHYS`, `TOUCH`, `HUD`, `WIN`, `CRASH`).
3. Stiskněte `A` (nebo `F1`) – hra se inicializuje (palivo 600, rychlost −500, výška 2500) a displej ukáže stav ve tvaru `-V.AAAA` (rychlost a výška).
4. V každém kole zadejte množství spáleného paliva a stiskněte `R/S` (myší) nebo `B` (`F2`).
5. `C`, `D`, `E` (`F3`–`F5`) zobrazí zbývající palivo, výšku a rychlost. Po takovém dotazu pokračujte zadáním paliva a klávesou `B`, ne `R/S`.
6. Bezpečné přistání zobrazí koncovou rychlost ve formátu `Fix 2`, havárie se ohlásí blikajícím displejem (chybový stav).

Kódy programu s komentáři jsou v `tests/TI5X.Core.Tests/TestHelpers.cs` (`LunarLanderProgram`) a slouží také jako vzor pro psaní vlastních programů.

---

## 10. Nápověda a informace o aplikaci

- `Help > TI-59 / Master Library Guide` zobrazí stručnou nápovědu přímo v aplikaci. Úplná dokumentace je v adresáři `docs/`.
- `Help > About TI5X Avalonia` zobrazí okno `About TI5X` se stručnými informacemi o aplikaci (desktopový port projektu TI5X, .NET 10 / Avalonia 12) a odkazem na licenční soubor `COPYING` původního projektu.

---

## 11. Časté problémy

| Problém | Příčina a řešení |
|---|---|
| Program zapsaný v `LRN` počítá nesmysly nebo končí chybou. | Programy zapsané **starší verzí emulátoru** (surové kódy kláves) je potřeba přepsat – nový `LRN` ukládá standardní kódy TI-59. Zkontrolujte kódy kroků v debuggeru ([kapitola 5](#vytváření-programů)). |
| Po `File > New Program` nefunguje `2nd Pgm 01`. | `New Program` odstraní i knihovny. Načtěte `src/TI5X.Avalonia/Assets/ml.ti5l` přes `File > Load Library Module (.ti5l)...` nebo aplikaci spusťte znovu. |
| Klávesy `A`–`E` spouštějí jiný program, než se čeká. | Je vybraná knihovní banka (stavový řádek ukazuje `Library Bank NN`). Stiskněte `RST`. |
| Po načtení programu jsou v registrech staré hodnoty. | Načtení `.ti5p` registry nemaže. Použijte `2nd CMs`. |
| Displej bliká a kalkulačka nereaguje. | Pokud displej mění jas, běží program – zastavte ho `R/S`. Pokud číslice blikají naplno, jde o chybu – stiskněte `CE` nebo `CLR`. |
| Debugger ukazuje zastaralé hodnoty (typicky po načtení souboru). | Stiskněte `Refresh`. |
| Klávesa `=` na klávesnici počítače vkládá `+`. | Na americkém rozložení je to tatáž klávesa `OemPlus`. Pro `=` použijte `Enter`. |
| Program se po volání podprogramu zastaví. | `SBR` uvnitř běžícího programu se v emulátoru po návratu zastaví ([Odchylky, bod 3](ODCHYLKY_A_OMEZENI.md#2-nejdůležitější-omezení)). Použijte `GTO`. |

Úplný seznam odchylek a omezení je v dokumentu [Odchylky emulace a známá omezení](ODCHYLKY_A_OMEZENI.md).

---

## 12. Android aplikace (MAUI)

Androidová verze (`src/TI5X.Maui`) používá stejné emulační jádro, formáty souborů i view modely jako desktop. Liší se pouze prezentační vrstva a práce se soubory.

### 12.1 Instalace a spuštění

Aplikace cílí na Android 5.0 (API 21) a vyšší; sestavení používá target API 36 a je ověřené na Androidu 13 (API 33). Nainstalujte APK ze systémového souborového manažeru nebo příkazem:

```sh
adb install -r nz.gen.geek_central.ti5x.maui-Signed.apk
```

Aplikace je uzamčená na orientaci na výšku, stejně jako původní Android projekt.

### 12.2 Ovládání

- Kalkulačka se ovládá dotykem na stejné klávesnici 9 × 5; indikátory `2nd`, `INV`, `LRN` a `DEG/RAD/GRAD` jsou v horní liště.
- Všechny položky menu jsou pod tlačítkem **Menu** v horní liště: `New Program`, `Open Program (.ti5p)`, `Save Program (.ti5p)`, `Load Library Module (.ti5l)`, `Virtual Printer`, `Program Debugger`, `Help`, `About` a `Exit`.
- Virtuální tiskárna, debugger i nápověda se otevírají jako podstránky; vrátíte se tlačítkem zpět nebo šipkou v horní liště. Tlačítka tiskárny a debuggeru odpovídají desktopové verzi, včetně `Copy Tape` do schránky.
- Hardwarová klávesnice (například u tabletu nebo v emulátoru) podporuje stejné klávesy jako desktop: číslice a numerickou klávesnici, `.` `+` `-` `*` `/`, `Enter`, `Backspace`/`Del` = CE, `Escape` = CLR a `F1`–`F5` = A–E.

### 12.3 Soubory a oprávnění

- Načtení i uložení používá systémový výběr souborů (Storage Access Framework). Aplikace nevyžaduje oprávnění `WRITE_EXTERNAL_STORAGE` ani přístup k celému úložišti.
- `Open Program (.ti5p)`, `Load Library Module (.ti5l)` a `Save Program (.ti5p)` otevřou dokumentový výběr. Uložený soubor se jmenuje `program.ti5p` a lze zvolit cílovou složku.
- Vestavěná knihovna `ml.ti5l` je součástí APK a načte se při prvním zobrazení hlavní stránky.
- Ukázkový `lunar_lander.ti5p` je také součástí APK v adresáři `Resources/Raw`, ale v aplikaci se nenabízí automaticky; pro vyzkoušení ho zkopírujte do úložiště zařízení a otevřete přes picker.

### 12.4 Omezení Android verze

- Aplikace je pouze pro Android; ostatní cíle MAUI (iOS, Windows, Mac Catalyst) se nesestavují.
- Běh na pozadí a obnovení po ukončení procesu nejsou ošetřené: pokud Android aplikaci z paměti ukončí, po návratu začne s prázdným stavem (program ani registry se neukládají automaticky).
- Automatizované UI testy neexistují; Android verze je ověřená sestavením a ručním smoke testem na API 33.
- Fyzická klávesnice nemusí být dostupná na všech zařízeních; plnohodnotné ovládání je dotykové.

---

## 13. Android aplikace (Avalonia, experimentální)

Vedle MAUI verze existuje experimentální Android aplikace postavená přímo na Avalonia 12 (`src/TI5X.Avalonia.Android`), tedy bez .NET MAUI. Používá tentýž sdílený UI kód jako desktop (stránky, ovládací prvky i nápověda), jen v jednoduchém mobilním rozložení bez horní lišty s nabídkami. Funkčně se od MAUI verze liší minimálně; celkové vyhodnocení obou variant je v [AVALONIA_ANDROID_VYHODNOCENI.md](AVALONIA_ANDROID_VYHODNOCENI.md).

### 13.1 Instalace a spuštění

Aplikace cílí na Android 6.0 (API 23) a vyšší (viz poznámku níže); sestavení používá target API 36 a je ověřené na Androidu 13 (API 33). Instalace:

```sh
adb install -r nz.gen.geek_central.ti5x.avalonia-Signed.apk
```

APK vznikne příkazem:

```sh
dotnet publish src/TI5X.Avalonia.Android/TI5X.Avalonia.Android.csproj -c Release -f net10.0-android -p:AndroidPackageFormats=apk
```

Aplikace `TI5X (Avalonia)` se instaluje vedle MAUI verze (odlišné ID `nz.gen.geek_central.ti5x.avalonia`), obě lze mít nainstalované současně.

### 13.2 Ovládání

- Ovládání je shodné s MAUI verzí (kapitola 12.2): kalkulačka na dotyk, všechna menu pod tlačítkem **Menu**, tiskárna a debugger jako podstránky, stejný kód hardware kláves včetně podpory `F1`–`F5`.
- Podstránky (tiskárna, debugger, nápověda, About) mají v horní liště název a šipku zpět; systémová klávesa zpět funguje korektně – navrací v navigačním zásobníku, na hlavní stránce ukončí aplikaci.
- Chybí ikona aplikace (nainstaluje se výchozí robot Androidu) a nástěná/splash obrazovka; jedná se o kosmetické omezení experimentu.

### 13.3 Soubory a oprávnění

- Stejný tok jako MAUI verze: open/save přes Storage Access Framework, bez oprávnění k úložišti. Uložený soubor má výchozí název `program.ti5p`.
- Vestavěná knihovna `ml.ti5l` je v sestavení desktopové aplikace (sdílený projekt) a načte se při startu aplikace, jako na desktopu.
- Výběr souborů na Androidu není omezený na přípony (SAF dialog s MIME `*/*`); lze vybrat libovolný soubor a aplikace ho zkusí načíst jako program/knihovnu (při chybě stavový řádek vypíše hlášku).

### 13.4 Omezení Android (Avalonia) verze

- Závislosti Avalonia.Android (AndroidX lifecycle) vyžadují minimálně API 23 (Android 6.0) – oproti MAUI/originálu je minimum posunuté.
- Běh na pozadí a obnovení po ukončení procesu nejsou ošetřené (stejně jako u MAUI verze).
- Automatizované UI testy neexistují; ověření proběhlo sestavením a ručním smoke testem na API 33 (viz [vyhodnocení](AVALONIA_ANDROID_VYHODNOCENI.md)).
