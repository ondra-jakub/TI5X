# Příručka kalkulačky TI-59

Tato příručka popisuje programovatelnou kalkulačku Texas Instruments TI-59 tak, jak ji napodobuje desktopový emulátor TI5X (Avalonia). Každá kapitola vysvětluje, jak se funkce chová na **originální kalkulačce**, a tam, kde se emulátor chová jinak, je to výslovně uvedeno.

Značení v textu:

| Značka | Význam |
|---|---|
| ⚠️ **Emulátor:** | Emulátor se v tomto bodě chová jinak než originál nebo funkci podporuje jen částečně. |
| ❌ **Emulátor:** | Emulátor funkci nemá. |
| *(ověřeno)* | Příklad byl ověřen spuštěním v enginu emulátoru (`Core/CalculatorEngine.cs`). |

Úplný seznam rozdílů je na jednom místě v dokumentu [Odchylky emulace a známá omezení](ODCHYLKY_A_OMEZENI.md). Ovládání samotné aplikace (menu, soubory, tiskárna, debugger) popisuje [uživatelská příručka emulátoru](EMULATOR_UZIVATEL.md). Architekturu, formáty souborů a testy popisuje [vývojářská dokumentace](EMULATOR_VYVOJ.md).

> **Důležité upozornění pro programování v emulátoru.** Program zapsaný v režimu `LRN` z klávesnice se v emulátoru uloží chybně a nefunguje (viz [9.2 Režim LRN](#92-režim-lrn)). Postupy z této příručky pro zápis v `LRN` popisují originál. Funkční program pro emulátor je nutné připravit jako soubor `.ti5p` s kódy instrukcí podle [kapitoly 15](#15-tabulka-kódů-instrukcí). Všechny [ukázkové programy](#16-ukázkové-programy) jsou uvedeny i v této podobě a byly v emulátoru ověřeny.

## Obsah

1. [Úvod a technické parametry](#1-úvod-a-technické-parametry)
2. [Klávesnice](#2-klávesnice)
   - [2.1 Rozložení kláves](#21-rozložení-kláves)
   - [2.2 Klávesa 2nd](#22-klávesa-2nd)
   - [2.3 Klávesa INV](#23-klávesa-inv)
   - [2.4 Kódy kláves a kódy instrukcí](#24-kódy-kláves-a-kódy-instrukcí)
3. [Zadávání čísel a formáty zobrazení](#3-zadávání-čísel-a-formáty-zobrazení)
   - [3.1 Zadání čísla](#31-zadání-čísla)
   - [3.2 Exponent EE](#32-exponent-ee)
   - [3.3 Inženýrský formát Eng](#33-inženýrský-formát-eng)
   - [3.4 Pevný počet desetinných míst Fix](#34-pevný-počet-desetinných-míst-fix)
   - [3.5 Mazání CE a CLR](#35-mazání-ce-a-clr)
4. [Aritmetika, AOS a závorky](#4-aritmetika-aos-a-závorky)
5. [Matematické funkce](#5-matematické-funkce)
   - [5.1 Mocniny, odmocniny a logaritmy](#51-mocniny-odmocniny-a-logaritmy)
   - [5.2 Úhlové režimy a goniometrické funkce](#52-úhlové-režimy-a-goniometrické-funkce)
   - [5.3 Polární a pravoúhlé souřadnice](#53-polární-a-pravoúhlé-souřadnice)
   - [5.4 Stupně, minuty a vteřiny](#54-stupně-minuty-a-vteřiny)
   - [5.5 Celá část, absolutní hodnota a π](#55-celá-část-absolutní-hodnota-a-π)
6. [Paměti](#6-paměti)
   - [6.1 Datové registry](#61-datové-registry)
   - [6.2 Ukládání a paměťová aritmetika](#62-ukládání-a-paměťová-aritmetika)
   - [6.3 Nepřímé adresování IND](#63-nepřímé-adresování-ind)
   - [6.4 Mazání pamětí](#64-mazání-pamětí)
7. [Registr t a porovnání](#7-registr-t-a-porovnání)
8. [Statistika](#8-statistika)
9. [Programování](#9-programování)
   - [9.1 Programová paměť](#91-programová-paměť)
   - [9.2 Režim LRN](#92-režim-lrn)
   - [9.3 Úpravy programu](#93-úpravy-programu)
   - [9.4 Spuštění, zastavení a krokování](#94-spuštění-zastavení-a-krokování)
   - [9.5 Návěští](#95-návěští)
   - [9.6 Nepodmíněné skoky](#96-nepodmíněné-skoky)
   - [9.7 Podprogramy](#97-podprogramy)
   - [9.8 Podmíněné skoky](#98-podmíněné-skoky)
   - [9.9 Příznaky](#99-příznaky)
   - [9.10 Smyčky s Dsz](#910-smyčky-s-dsz)
   - [9.11 Zvýšení a snížení registru](#911-zvýšení-a-snížení-registru)
   - [9.12 Pause a Nop](#912-pause-a-nop)
10. [Rozdělení paměti](#10-rozdělení-paměti)
11. [Knihovní programy a Pgm](#11-knihovní-programy-a-pgm)
12. [Tisk](#12-tisk)
13. [Přehled Op kódů](#13-přehled-op-kódů)
14. [Chybové stavy](#14-chybové-stavy)
15. [Tabulka kódů instrukcí](#15-tabulka-kódů-instrukcí)
   - [15.1 Úplná tabulka kódů 00 až 99](#151-úplná-tabulka-kódů-00-až-99)
   - [15.2 Pravidla kódování programu](#152-pravidla-kódování-programu)
   - [15.3 Vytvoření souboru ti5p](#153-vytvoření-souboru-ti5p)
16. [Ukázkové programy](#16-ukázkové-programy)
   - [16.1 Plocha kruhu](#161-plocha-kruhu)
   - [16.2 Faktoriál pomocí Dsz](#162-faktoriál-pomocí-dsz)
   - [16.3 Převod teplot s příznakem](#163-převod-teplot-s-příznakem)
   - [16.4 Součet řady se smyčkou](#164-součet-řady-se-smyčkou)
   - [16.5 Zastavení programu pro vstup](#165-zastavení-programu-pro-vstup)
   - [16.6 Lunar Lander](#166-lunar-lander)

---

## 1. Úvod a technické parametry

TI-59 (1977) je kapesní programovatelná kalkulačka s červeným LED displejem, čtečkou magnetických karet a možností připojit tiskárnu PC-100A. Počítá v algebraickém systému **AOS** (Algebraic Operating System), který respektuje přednost operací, a programy ukládá jako posloupnost kódů kláves.

| Parametr | Originál TI-59 | Emulátor TI5X |
|---|---|---|
| Datové registry | max. 100 (00–99), podle rozdělení | vždy dostupných všech 100 ⚠️ |
| Programové kroky | max. 960, podle rozdělení | 960 v bance 0 |
| Výchozí rozdělení | 479.59 (480 kroků, registry 00–59) | interně stejné, ale dostupných 960 kroků a 100 registrů ⚠️ |
| Úrovně závorek | 9 (sdílené s odloženými operacemi) | 10 položek zásobníku pro závorky i odložené operace |
| Úrovně podprogramů | 6 | 8 |
| Příznaky | 10 (0–9) | 10 (0–9), jiný význam 7–9 ⚠️ |
| Zobrazení | 10 číslic mantisy, exponent ±99 | 10 číslic, výpočet v `double` (bez kontroly ±99) ⚠️ |
| Vnitřní přesnost | 13 číslic | `double`, asi 15–16 platných číslic |
| Úhlové režimy | Deg, Rad, Grad | Deg, Rad, Grad (výchozí Deg) |
| Formáty | standardní, Fix 0–8, vědecký (EE), inženýrský (Eng) | stejné |
| Knihovní modul | Master Library (25 programů) | jen banka 01 ze souboru `src/TI5X.Avalonia/Assets/ml.ti5l` ⚠️ |
| Magnetické karty | ano | ne – nahrazují je soubory `.ti5p`/`.ti5l` ❌ |
| Tiskárna | PC-100A, 20 znaků na řádek | virtuální PC-100A, 20 znaků na řádek |

Podrobnosti k označeným řádkům: [rozdělení paměti](ODCHYLKY_A_OMEZENI.md#310-rozdělení-paměti), [příznaky](ODCHYLKY_A_OMEZENI.md#39-příznaky), [formáty](ODCHYLKY_A_OMEZENI.md#32-zadávání-čísel-a-formáty-zobrazení), [knihovny](ODCHYLKY_A_OMEZENI.md#311-knihovní-programy-karty-a-soubory).

---

## 2. Klávesnice

### 2.1 Rozložení kláves

Klávesnice má 45 kláves v mřížce 9 řádků × 5 sloupců. Každá klávesa má **primární funkci** (popis na klávese) a většina i **druhou funkci** (popis nad klávesou), která se vyvolá klávesou `2nd`.

V tabulce je u každé klávesy primární funkce a za lomítkem druhá funkce. Číslo v závorce je **kód klávesy** (řádek × 10 + sloupec) a kód druhé funkce (řádek × 10 + sloupec + 5, poslední sloupec má jednotky 0).

| Řádek | Sloupec 1 | Sloupec 2 | Sloupec 3 | Sloupec 4 | Sloupec 5 |
|---|---|---|---|---|---|
| 1 | `A` (11) / `A'` (16) | `B` (12) / `B'` (17) | `C` (13) / `C'` (18) | `D` (14) / `D'` (19) | `E` (15) / `E'` (10) |
| 2 | `2nd` (21) | `INV` (22) | `lnx` (23) / `log` (28) | `CE` (24) / `CP` (29) | `CLR` (25) / – |
| 3 | `LRN` (31) / `Pgm` (36) | `x⇌t` (32) / `P→R` (37) | `x²` (33) / `sin` (38) | `√x` (34) / `cos` (39) | `1/x` (35) / `tan` (30) |
| 4 | `SST` (41) / `Ins` (46) | `STO` (42) / `CMs` (47) | `RCL` (43) / `Exc` (48) | `SUM` (44) / `Prd` (49) | `yˣ` (45) / `Ind` (40) |
| 5 | `BST` (51) / `Del` (56) | `EE` (52) / `Eng` (57) | `(` (53) / `Fix` (58) | `)` (54) / `Int` (59) | `÷` (55) / `\|x\|` (50) |
| 6 | `GTO` (61) / `Pause` (66) | `7` (62) / `x=t` (67) | `8` (63) / `Nop` (68) | `9` (64) / `Op` (69) | `×` (65) / `Deg` (60) |
| 7 | `SBR` (71) / `Lbl` (76) | `4` (72) / `x≥t` (77) | `5` (73) / `Σ+` (78) | `6` (74) / `x̄` (79) | `−` (75) / `Rad` (70) |
| 8 | `RST` (81) / `St flg` (86) | `1` (82) / `If flg` (87) | `2` (83) / `D.MS` (88) | `3` (84) / `π` (89) | `+` (85) / `Grad` (80) |
| 9 | `R/S` (91) / `Write` (96) | `0` (92) / `Dsz` (97) | `.` (93) / `Adv` (98) | `+/-` (94) / `Prt` (99) | `=` (95) / `List` (90) |

Rozložení i popisky odpovídají virtuální klávesnici emulátoru (`MainWindow.axaml`), převod na druhou funkci tabulce `MapSecond` v `CalculatorEngine.cs`.

❌ **Emulátor:** `2nd R/S` (Write, kód 96) nemá v emulátoru žádnou funkci a tlačítko nemá druhý popisek. Kód 20 (pozice „2nd CLR“) nemá ani na originálu samostatnou funkci.

### 2.2 Klávesa 2nd

`2nd` vybere druhou funkci **následující** klávesy. Po jejím provedení se `2nd` automaticky zruší. Opakovaný stisk `2nd` výběr zruší. Aktivní `2nd` ukazuje emulátor žlutým štítkem `2nd` nad displejem.

V této příručce se druhé funkce zapisují s předponou `2nd`, například `2nd log`, `2nd Fix 2`, `2nd St flg 3`. U kláves `A'`–`E'` se předpona často vynechává.

### 2.3 Klávesa INV

`INV` obrací význam následující funkce, například `INV lnx` = eˣ, `INV 2nd Fix` = zrušení pevného formátu, `INV SUM` = odečtení od registru. Opakovaný stisk `INV` ho zruší. Funkce, které inverzi nemají, `INV` pouze zruší. Aktivní `INV` ukazuje štítek `INV` nad displejem.

Přehled funkcí s inverzí:

| Funkce | S `INV` |
|---|---|
| `lnx` | eˣ |
| `2nd log` | 10ˣ |
| `yˣ` | ˣ√y (odmocnina) |
| `2nd sin`, `2nd cos`, `2nd tan` | arcsin, arccos, arctg |
| `2nd P→R` | převod pravoúhlých souřadnic na polární |
| `2nd D.MS` | opačný převod úhlu (viz [5.4](#54-stupně-minuty-a-vteřiny)) |
| `2nd Int` | zlomková část |
| `SUM`, `2nd Prd` | odečtení od registru, dělení registru |
| `EE`, `2nd Eng`, `2nd Fix` | zrušení příslušného formátu |
| `2nd Σ+` | odebrání datového bodu |
| `2nd x̄` | směrodatná odchylka |
| `2nd x=t`, `2nd x≥t` | x ≠ t, x < t |
| `2nd St flg`, `2nd If flg`, `2nd Dsz` | nulování příznaku, test nenastaveného příznaku, skok při nule |
| `SBR` | návrat z podprogramu (`RTN`) |
| `2nd List` | výpis datových registrů (originál) |

⚠️ **Emulátor:**
- `INV EE` nemá účinek a `INV` zůstane aktivní. Ze zobrazení s exponentem se vrátíte pomocí `INV 2nd Eng`.
- `INV 2nd List` a `INV 2nd Prt` se chovají jako bez `INV`.
- Navíc proti originálu: `INV x²` počítá √x a `INV √x` počítá x².

### 2.4 Kódy kláves a kódy instrukcí

Je potřeba rozlišovat dva druhy kódů:

1. **Kód klávesy** – pozice fyzické klávesy (řádek a sloupec), viz tabulka v [2.1](#21-rozložení-kláves). Číslice mají kódy podle polohy: `7` = 62, `8` = 63, `9` = 64, `4` = 72, `5` = 73, `6` = 74, `1` = 82, `2` = 83, `3` = 84, `0` = 92. Tyto kódy posílají tlačítka virtuální klávesnice emulátoru.
2. **Kód instrukce** – hodnota uložená v programovém kroku (00–99), kterou zobrazuje režim `LRN` a tiskne `List`. Originál při zápisu do programu převádí:
   - číslice na kódy **00–09** (`7` → `07`),
   - druhou funkci na **jediný** kód (`2nd log` → `28`, `2nd Fix` → `58`),
   - kombinace s `IND` na **sloučené kódy** (`STO 2nd Ind` → `72`, `INV SBR` → `92` = `RTN`), viz [15.1](#151-úplná-tabulka-kódů-00-až-99).

Kódy 62–64, 72–74, 82–84 a 92 tedy znamenají něco jiného jako kód klávesy (číslice) a jako kód instrukce (sloučená instrukce).

✅ **Emulátor:** v režimu `LRN` se ukládají kódy instrukcí standardu TI-59 (včetně sloučených kódů) a při běhu se sloučené kódy provádějí správně. Podrobnosti viz [9.2](#92-režim-lrn) a [Sloučené kódy instrukcí](ODCHYLKY_A_OMEZENI.md#5-sloučené-kódy-instrukcí).

---

## 3. Zadávání čísel a formáty zobrazení

### 3.1 Zadání čísla

Číslo se zadává číslicemi `0`–`9`, desetinnou tečkou `.` a změnou znaménka `+/-`. Mantisa má nejvýše 10 číslic. `+/-` lze stisknout kdykoli během zadávání i u výsledku.

Zadávání čísla končí stiskem libovolné operace nebo funkce. Číslo zadané po výsledku začíná nové zadání.

### 3.2 Exponent EE

`EE` zahájí zadávání exponentu (dvě číslice, `+/-` po `EE` mění znaménko exponentu). Displej ukazuje mantisu a vpravo dvoumístný exponent.

| Klávesy | Displej |
|---|---|
| `1.5 EE 3` | `1.5 03` *(ověřeno)* |
| `=` | `1500.` *(ověřeno)* |

Stisk `EE` po výsledku přepne zobrazení do **vědeckého formátu** (mantisa a exponent). `INV EE` vědecký formát zruší.

⚠️ **Emulátor:** `INV EE` vědecký formát nezruší, použijte `INV 2nd Eng`.

### 3.3 Inženýrský formát Eng

`2nd Eng` zobrazuje čísla s exponentem, který je násobkem 3 (mantisa 1 až 999). `INV 2nd Eng` formát zruší.

| Klávesy | Displej |
|---|---|
| `12345 = 2nd Eng` | `12.345000 03` *(ověřeno)* |

⚠️ **Emulátor:** stisk `2nd Eng` během zadávání čísla rozepsané číslo zahodí. Číslo nejdřív ukončete (např. `=`).

### 3.4 Pevný počet desetinných míst Fix

`2nd Fix n` (n = 0–8) zaokrouhluje zobrazení na n desetinných míst. Vnitřní hodnota se nemění. `INV 2nd Fix` nebo `2nd Fix 9` vrátí standardní formát.

| Klávesy | Displej |
|---|---|
| `2 ÷ 3 =` | `0.666666667` *(ověřeno)* |
| `2nd Fix 2` | `0.67` *(ověřeno)* |
| `2nd Fix 4` | `0.6667` *(ověřeno)* |
| `INV 2nd Fix` (nebo `2nd Fix 9`) | `0.666666667` *(ověřeno)* |

V originále lze `Fix` kombinovat s vědeckým a inženýrským formátem (počet desetinných míst mantisy) a lze použít `2nd Fix 2nd Ind nn`.

⚠️ **Emulátor:** `Fix` se ve vědeckém a inženýrském formátu ignoruje (mantisa má vždy 8 platných číslic) a `Fix Ind` není podporován.

Ve standardním formátu se zobrazí nejvýše 10 číslic a nuly na konci se vynechají. Čísla mimo rozsah zobrazení (≥ 10¹⁰ nebo < 10⁻¹⁰ v absolutní hodnotě) se automaticky zobrazí s exponentem.

### 3.5 Mazání CE a CLR

| Klávesa | Účinek |
|---|---|
| `CE` | Smaže zadávané číslo (ne celý výpočet) a zruší chybový stav. Odložené operace zůstávají. |
| `CLR` | Smaže displej, všechny odložené operace a závorky a zruší chybový stav. Registry, program ani formát nemění. |

---

## 4. Aritmetika, AOS a závorky

TI-59 vyhodnocuje výrazy v pořadí, jak jsou zapsány na papíře. Systém AOS respektuje přednost operací:

1. funkce jedné proměnné (`x²`, `√x`, `1/x`, `lnx`, `sin` …) se provedou ihned po stisku,
2. `yˣ` a `INV yˣ`,
3. `×` a `÷`,
4. `+` a `−`.

Operace se stejnou prioritou se provádějí zleva doprava. `=` dokončí všechny odložené operace a uzavře otevřené závorky.

| Klávesy | Výsledek |
|---|---|
| `3 + 4 × 5 =` | `23.` *(ověřeno)* |
| `( 3 + 4 ) × 5 =` | `35.` *(ověřeno)* |
| `2 yˣ 10 =` | `1024.` *(ověřeno)* |
| `8 INV yˣ 3 =` | `2.` (třetí odmocnina z 8) *(ověřeno)* |

Závorky `(` a `)` mění pořadí vyhodnocení. Originál dovoluje 9 otevřených závorek, přičemž odložené operace zabírají stejné místo.

⚠️ **Emulátor:** zásobník odložených operací a závorek má 10 položek. 11. úroveň a přebytečná `)` vedou k chybě.

---

## 5. Matematické funkce

Všechny funkce v této kapitole působí na hodnotu na displeji (registr x) a výsledek ihned zobrazí.

### 5.1 Mocniny, odmocniny a logaritmy

| Klávesy | Funkce | Chyba při |
|---|---|---|
| `x²` | druhá mocnina | – |
| `√x` | druhá odmocnina | x < 0 |
| `1/x` | převrácená hodnota | x = 0 |
| `lnx` | přirozený logaritmus | x ≤ 0 |
| `INV lnx` | eˣ (`2 INV lnx` → `7.389056099` *(ověřeno)*) | – |
| `2nd log` | dekadický logaritmus | x ≤ 0 |
| `INV 2nd log` | 10ˣ | – |
| `y yˣ x =` | y na x-tou | – |
| `y INV yˣ x =` | x-tá odmocnina z y | x = 0 |

### 5.2 Úhlové režimy a goniometrické funkce

| Klávesy | Režim |
|---|---|
| `2nd Deg` | stupně (výchozí) |
| `2nd Rad` | radiány |
| `2nd Grad` | grady (400 grad = plný úhel) |

Aktuální režim ukazuje emulátor štítkem `DEG`/`RAD`/`GRAD` nad displejem. Režim ovlivňuje `sin`, `cos`, `tan`, jejich inverze a `P→R`.

| Klávesy | Výsledek |
|---|---|
| `30 2nd sin` (Deg) | `0.5` *(ověřeno)* |
| `2nd Rad 2nd π ÷ 6 = 2nd sin` | `0.5` *(ověřeno)* |
| `0.5 INV 2nd sin` (Deg) | `30.` |

`INV 2nd sin` a `INV 2nd cos` pro |x| > 1 hlásí chybu.

### 5.3 Polární a pravoúhlé souřadnice

**Originál TI-59:** zadejte poloměr r, stiskněte `x⇌t`, zadejte úhel θ a stiskněte `2nd P→R`. Na displeji je souřadnice y, po `x⇌t` souřadnice x. Opačný převod: x `x⇌t` y `INV 2nd P→R` → θ na displeji, r v registru t.

⚠️ **Emulátor:** pořadí je obrácené. Emulátor bere **r z displeje a θ z registru t** a vrací **x na displeji a y v registru t**. *(ověřeno)*

| Postup v emulátoru | Výsledek |
|---|---|
| `30 x⇌t 2 2nd P→R` (Deg, θ = 30°, r = 2) | displej `1.732050808` (x), po `x⇌t` `1.` (y) |
| y `x⇌t` x `INV 2nd P→R` | displej r, po `x⇌t` θ |

### 5.4 Stupně, minuty a vteřiny

**Originál TI-59:** `2nd D.MS` převádí úhel zapsaný jako DD.MMSS (stupně.minutyvteřiny) na desetinné stupně a `INV 2nd D.MS` převádí zpět.

⚠️ **Emulátor:** směr je prohozený. *(ověřeno)*

| Klávesy v emulátoru | Výsledek |
|---|---|
| `1.3 2nd D.MS` (1,3° → DD.MMSS) | `1.18` (1° 18′) |
| `1.3 INV 2nd D.MS` (1° 30′ → stupně) | `1.5` |

### 5.5 Celá část, absolutní hodnota a π

| Klávesy | Funkce | Příklad *(ověřeno)* |
|---|---|---|
| `2nd Int` | celá část | `7.25 +/- 2nd Int` → `-7.` |
| `INV 2nd Int` | zlomková část | `7.25 +/- INV 2nd Int` → `-0.25` |
| `2nd \|x\|` | absolutní hodnota | `-0.25` → `0.25` |
| `2nd π` | konstanta π | `3.141592654` |

---

## 6. Paměti

### 6.1 Datové registry

TI-59 má až 100 datových registrů R00–R99. Kolik jich je k dispozici, určuje [rozdělení paměti](#10-rozdělení-paměti), ve výchozím stavu 60 (R00–R59). Některé registry mají zvláštní roli:

| Registry | Použití |
|---|---|
| R00–R09 | Čítače pro `Dsz` a `Op 20`–`Op 39`. |
| R01–R06 | Statistické součty (viz [kapitola 8](#8-statistika)). |

⚠️ **Emulátor:** všech 100 registrů je dostupných vždy, bez ohledu na rozdělení.

### 6.2 Ukládání a paměťová aritmetika

Adresa registru se zadává vždy **dvěma číslicemi** (`STO 05`, ne `STO 5`).

| Klávesy | Účinek |
|---|---|
| `STO nn` | uloží x do Rnn |
| `RCL nn` | zobrazí obsah Rnn |
| `SUM nn` | Rnn = Rnn + x |
| `INV SUM nn` | Rnn = Rnn − x |
| `2nd Prd nn` | Rnn = Rnn × x |
| `INV 2nd Prd nn` | Rnn = Rnn ÷ x |
| `2nd Exc nn` | vymění x a Rnn |

Příklad *(ověřeno)*:

| Klávesy | Displej | Poznámka |
|---|---|---|
| `5 STO 01` | `5.` | R01 = 5 |
| `3 SUM 01` | `3.` | R01 = 8 |
| `CLR RCL 01` | `8.` | |
| `2 2nd Prd 01 RCL 01` | `16.` | R01 = 16 |

⚠️ **Emulátor:** pokud po jediné číslici adresy následuje jiná klávesa (`STO 3 +`), nastane chyba.

### 6.3 Nepřímé adresování IND

`2nd Ind` za instrukcí znamená, že číslo registru se vezme z **obsahu** uvedeného registru (z jeho celé části). Například když R10 = 7, pak `STO 2nd Ind 10` uloží x do R07.

| Klávesy | Výsledek *(ověřeno)* |
|---|---|
| `7 STO 10` | R10 = 7 (ukazatel) |
| `42 STO 2nd Ind 10` | R07 = 42 |
| `9 2nd Exc 07` | displej `42.`, R07 = 9 |

V originále lze `Ind` použít u `STO`, `RCL`, `SUM`, `Prd`, `Exc`, `GTO`, `SBR`, `Pgm`, `Op`, `Fix`, `St flg`, `If flg`, `Dsz`, `x=t`, `x≥t` a dalších. V programu se kombinace s `Ind` u `STO`, `RCL`, `SUM`, `Prd`, `Exc`, `GTO`, `Pgm` a `Op` ukládají jako jediný sloučený kód (viz [15.1](#151-úplná-tabulka-kódů-00-až-99)).

⚠️ **Emulátor:**
- `Ind` funguje u `STO`, `RCL`, `SUM`, `Prd`, `Exc`, `GTO`, `SBR`, `Pgm`, `Op` a u cíle podmíněného skoku. U `Fix`, `St flg`, `If flg` a `Dsz` podporováno není.
- Adresa mimo 0–99 vede k chybě.
- V programu se `Ind` zapisuje dvěma kroky (`kód 40 nn`, např. `42 40 05`), sloučené kódy emulátor neprovádí.

### 6.4 Mazání pamětí

`2nd CMs` vynuluje všechny datové registry. Program, příznaky ani displej nemění.

⚠️ **Emulátor:** vynuluje vždy všech 100 registrů.

---

## 7. Registr t a porovnání

Registr **t** (test register) je pomocný registr pro porovnání a pro funkce se dvěma hodnotami (`P→R`, statistika).

| Klávesy | Účinek |
|---|---|
| `x⇌t` | vymění x a t |
| `2nd x=t` | v programu: skok, pokud x = t |
| `INV 2nd x=t` | skok, pokud x ≠ t |
| `2nd x≥t` | skok, pokud x ≥ t |
| `INV 2nd x≥t` | skok, pokud x < t |

Za porovnáním následuje cíl skoku (adresa nebo návěští). Pokud podmínka splněna není, cíl se přeskočí a pokračuje se další instrukcí. Podrobnosti v [9.8](#98-podmíněné-skoky).

---

## 8. Statistika

Statistické funkce sbírají součty do registrů R01–R06. Před novou sadou dat je vynulujte (`2nd CMs`).

| Registr | Obsah |
|---|---|
| R01 | Σy |
| R02 | Σy² |
| R03 | n (počet bodů) |
| R04 | Σx |
| R05 | Σx² |
| R06 | Σxy |

**Originál TI-59:** datový bod se zadává jako x `x⇌t` y `2nd Σ+` (y na displeji, x v registru t). `INV 2nd Σ+` bod odebere. `2nd x̄` zobrazí průměr y (průměr x je po `x⇌t` v registru t), `INV 2nd x̄` směrodatnou odchylku. `Op 11` až `Op 15` počítají rozptyl, lineární regresi, korelaci a odhady.

⚠️ **Emulátor:** role x a y jsou prohozené a Op kódy mají jiné číslování. *(ověřeno)*

| Klávesy v emulátoru | Výsledek |
|---|---|
| y `x⇌t` x `2nd Σ+` | přidá bod (hodnota na displeji je **x**, t je **y**), zobrazí n |
| `INV 2nd Σ+` | odebere bod |
| `2nd x̄` | průměr x (hodnot z displeje), v t průměr y |
| `INV 2nd x̄` | výběrová směrodatná odchylka x (dělení n − 1), v t odchylka y |
| `2nd Op 11` | směrnice regresní přímky y = a·x + b, v t úsek b |
| x `2nd Op 12` | odhad y′ pro zadané x |
| `2nd Op 13` | korelační koeficient |

Pro data s **jednou proměnnou** stačí zadávat hodnoty a mačkat `2nd Σ+`; výsledky jsou stejné jako na originálu. Příklad *(ověřeno)*: `2nd CMs 2 2nd Σ+ 4 2nd Σ+ 6 2nd Σ+` → `3.`, `2nd x̄` → `4.`, `INV 2nd x̄` → `2.`.

Příklad se dvěma proměnnými v emulátoru – body (1; 2), (2; 4), (3; 6,5) *(ověřeno)*:

| Klávesy | Displej |
|---|---|
| `2nd CMs 2 x⇌t 1 2nd Σ+` | `1.` |
| `4 x⇌t 2 2nd Σ+` | `2.` |
| `6.5 x⇌t 3 2nd Σ+` | `3.` |
| `2nd Op 11` | `2.25` (směrnice) |
| `x⇌t` | `-0.333333333` (úsek) |
| `4 2nd Op 12` | `8.666666667` (odhad y pro x = 4) |
| `2nd Op 13` | `0.997948716` (korelace) |

Viz také [Statistika v přehledu odchylek](ODCHYLKY_A_OMEZENI.md#36-statistika).

---

## 9. Programování

### 9.1 Programová paměť

Program je posloupnost **kroků** číslovaných od 000. Každý krok obsahuje jeden dvoumístný kód instrukce (viz [kapitola 15](#15-tabulka-kódů-instrukcí)). Instrukce s operandem zabírá více kroků, například `STO 05` = dva kroky `42 05`.

Počet kroků určuje [rozdělení paměti](#10-rozdělení-paměti): výchozí 480 (000–479), nejvýše 960. Uživatelský program je v **bance 0**, knihovní programy jsou v bankách 01 a vyšších.

⚠️ **Emulátor:** v bance 0 je ve výchozím stavu k dispozici 960 kroků.

### 9.2 Režim LRN

**Originál TI-59:** `LRN` přepne do režimu zápisu programu. Displej ukazuje `PPP KK`, tedy číslo kroku a kód v něm. Každá stisknutá klávesa (nebo kombinace `2nd`/`Ind`) se uloží jako kód instrukce do aktuálního kroku a ukazatel se posune. Druhým stiskem `LRN` se režim ukončí. Při zápisu se obvykle začíná od kroku 000 (`RST` nebo `GTO 000` před `LRN`).

✅ **Emulátor:** režim `LRN` ukazuje `PPP KK` a ukládá kódy instrukcí standardu TI-59.

| Stisknuté klávesy | Uložené kódy |
|---|---|
| `2nd Lbl A x²` | `76 11 33` |
| `5 STO 01` | `05 42 01` |
| `STO 2nd Ind 05` | `72 05` |
| `GTO 123` | `61 01 23` |
| `INV SBR` | `92` |

Poznámky: `INV` se ukládá jako samostatný krok `22` (jen `INV SBR` se sloučí do `92`), `INV 2nd Fix` nebere operand a programy zapsané **starší verzí emulátoru** (surové kódy kláves) je potřeba přepsat. Program lze prohlížet i v okně `View > Program Debugger / Steps`.

### 9.3 Úpravy programu

| Klávesy (v LRN) | Účinek v originále |
|---|---|
| `SST` | posun na další krok |
| `BST` | posun na předchozí krok |
| `2nd Ins` | vloží prázdný krok (`00`) na aktuální pozici, další kroky se posunou |
| `2nd Del` | smaže aktuální krok, další kroky se posunou zpět |
| `2nd CP` (mimo LRN) | smaže celý program a nastaví krok 000 |
| `GTO nnn` (mimo LRN) | nastaví ukazatel na krok nnn |

✅ **Emulátor:** `SST`, `BST`, `2nd Ins`, `2nd Del` i `2nd CP` fungují v `LRN` (Ins vkládá krok `68` = Nop). Programy knihovních bank upravovat nelze, stejně jako v originále.

### 9.4 Spuštění, zastavení a krokování

| Klávesy | Účinek |
|---|---|
| `RST` | ukazatel na krok 000 banky 0; v originále navíc nuluje příznaky a zásobník podprogramů |
| `R/S` | spustí program od aktuálního kroku; za běhu ho zastaví |
| `R/S` v programu | zastaví program (např. kvůli zadání vstupu) |
| `SST` (mimo LRN) | provede jednu instrukci |
| `BST` (mimo LRN) | vrátí ukazatel o krok zpět |
| `A`–`E`, `A'`–`E'` | spustí program od příslušného návěští jako podprogram |

Během běhu programu displej v emulátoru ztlumeně bliká a reaguje se jen na `R/S`.

⚠️ **Emulátor:**
- `RST` nenuluje příznaky ani zásobník podprogramů.
- Číslo zadané v programu těsně před `R/S` se zachová (program `7 R/S` skončí s `7`).

### 9.5 Návěští

`2nd Lbl X` označí místo v programu. Návěštím může být libovolná klávesa kromě několika vyhrazených. Nejčastěji se používají `A`–`E` a `A'`–`E'`, protože je lze spustit přímo stiskem klávesy. V programu zabírá návěští dva kroky (`76 kk`).

⚠️ **Emulátor:** jako cíl `GTO`/`SBR` a podmíněného skoku lze použít jen návěští `A`–`E'` (kódy 10–19). Návěští se hledá v aktuální bance.

### 9.6 Nepodmíněné skoky

| Zápis | Účinek |
|---|---|
| `GTO X` | skok na návěští X |
| `GTO nnn` | skok na absolutní krok nnn (v programu 3 kroky `61 0n nn`) |
| `GTO 2nd Ind nn` | skok na adresu v registru nn (v programu `83 nn`) |

✅ **Emulátor:**
- V programu se absolutní adresa čte ve tvaru `0n nn` (standard TI-59), návěští A–E' jako bajt `1x` a `GTO Ind` jako `61 40 nn` nebo sloučený kód `83 nn`. Jednobajtová adresa `20`–`99` se také akceptuje (doplňková forma emulátoru).
- Z klávesnice `GTO 2nd Ind nn` čeká na 3 číslice.

### 9.7 Podprogramy

`SBR X` (nebo `SBR nnn`) zavolá podprogram: uloží návratovou adresu a skočí. `INV SBR` (`RTN`, kód 92) se vrátí za místo volání. Stisk `A`–`E'` z klávesnice je totéž jako `SBR A`–`E'`: program se spustí a po `RTN` se zastaví.

✅ **Emulátor:**
- `SBR` uvnitř běžícího programu se po `RTN` vrací za místo volání a běh pokračuje; `SBR` z klávesnice (včetně `A`–`E'`) program po `RTN` zastaví.
- Kód `92` (`RTN`) funguje stejně jako `INV SBR`.
- `INV SBR` bez rozpracovaného podprogramu program zastaví.
- Podprogram volaný klávesou `A`–`E'`, který skončí instrukcí `R/S` místo `INV SBR`, nechá v zásobníku nevyužitou návratovou adresu. Po 8 takových voláních nastane při 9. volání chyba (přetečení zásobníku), kterou `RST` nevyřeší. *(ověřeno)* Proto podprogramy ukončujte `INV SBR`.

### 9.8 Podmíněné skoky

Podmíněné instrukce testují podmínku. Je-li splněna, provede se skok na cíl uvedený za instrukcí. Jinak se cíl přeskočí.

| Zápis | Skok, když |
|---|---|
| `2nd x=t X` | x = t |
| `INV 2nd x=t X` | x ≠ t |
| `2nd x≥t X` | x ≥ t |
| `INV 2nd x≥t X` | x < t |
| `2nd If flg n X` | příznak n je nastaven |
| `INV 2nd If flg n X` | příznak n není nastaven |
| `2nd Dsz n X` | po snížení Rn je Rn ≠ 0 |
| `INV 2nd Dsz n X` | po snížení Rn je Rn = 0 |

Cílem X může být návěští, absolutní adresa nebo `Ind nn`. V emulátoru se cíl v programu čte stejně jako u `GTO`: návěští jako bajt `1x`, absolutní adresa `0n nn` (nebo jednobajtová `20`–`99`), `Ind nn` jako `40 nn`.

### 9.9 Příznaky

TI-59 má 10 příznaků (0–9). `2nd St flg n` příznak nastaví, `INV 2nd St flg n` ho vynuluje a `2nd If flg n X` ho testuje (viz [9.8](#98-podmíněné-skoky)).

| Příznak | Originál TI-59 | Emulátor |
|---|---|---|
| 0–6 | obecné | obecné |
| 7 | nastavují ho `Op 18`/`Op 19` | ⚠️ trasovací tisk každého výsledku, nastavuje ho i `Op 0` |
| 8 | při nastavení se program při chybě zastaví | ❌ bez účinku, program se při chybě zastaví vždy |
| 9 | trasovací tisk na PC-100A | ⚠️ zpomalený běh, 50 ms na krok |

⚠️ **Emulátor:** příznak se zadává jednou číslicí bez `Ind`. V programu je číslo příznaku jeden bajt (`86 03` = `St flg 3`). Podrobnosti viz [Příznaky v přehledu odchylek](ODCHYLKY_A_OMEZENI.md#39-příznaky).

### 9.10 Smyčky s Dsz

`2nd Dsz n X` (n = 0–9) sníží obsah registru Rn o 1 a skočí na X, pokud výsledek není nula. Hodí se pro smyčky s pevným počtem opakování: do R0–R9 uložte počet průchodů a na konec smyčky dejte `Dsz n` s cílem na začátku smyčky. Viz [ukázku 16.2](#162-faktoriál-pomocí-dsz).

V originále se hodnota posouvá k nule (záporné číslo se zvyšuje).

⚠️ **Emulátor:** vždy odečte 1 (−2 → −3) a nepodporuje `Ind`.

### 9.11 Zvýšení a snížení registru

| Zápis | Účinek |
|---|---|
| `2nd Op 2n` (Op 20–29) | Rn = Rn + 1 (n = 0–9) |
| `2nd Op 3n` (Op 30–39) | Rn = Rn − 1 (n = 0–9) |

⚠️ **Emulátor:** novou hodnotu registru navíc zobrazí na displeji (přepíše x). *(ověřeno: `2nd Op 20` → R00 = 1, displej `1.`)*

### 9.12 Pause a Nop

| Zápis | Účinek |
|---|---|
| `2nd Pause` | v programu krátce (asi 0,5 s) zobrazí displej a pokračuje |
| `2nd Nop` | prázdná instrukce, nic nedělá (vhodná jako výplň) |

---

## 10. Rozdělení paměti

Paměť TI-59 je sdílená mezi programové kroky a datové registry. Jeden registr odpovídá 8 krokům.

**Originál TI-59:**
- `2nd Op 16` zobrazí aktuální rozdělení ve tvaru `KKK.RR` (poslední krok a poslední registr), výchozí `479.59`.
- `n 2nd Op 17` nastaví nové rozdělení, kde n je **počet desítek registrů** (0–10). Například `6 2nd Op 17` → `479.59`, `3 2nd Op 17` → `719.29`, `10 2nd Op 17` → `159.99`.

⚠️ **Emulátor:** *(ověřeno)*
- `Op 16` zobrazí ve výchozím stavu `4852` místo `479.59`.
- `Op 17` bere **počet registrů** (0–100) a počet kroků počítá jako (100 − n) × 20, takže výchozí stav (960 kroků) odpovídá `52 2nd Op 17`.
- Přístup k registrům se nijak neomezuje.
- `0 2nd Op 17` může vést k pádu aplikace.

Op 16 a 17 v emulátoru raději nepoužívejte. Viz [Rozdělení paměti v přehledu odchylek](ODCHYLKY_A_OMEZENI.md#310-rozdělení-paměti).

---

## 11. Knihovní programy a Pgm

Do TI-59 se zasouvá knihovní modul s předem nahranými programy, standardně **Master Library** (programy 01–25). Program z modulu se spouští takto:

| Klávesy | Účinek |
|---|---|
| `2nd Pgm nn` | vybere knihovní program nn pro **následující** volání návěští |
| `2nd Pgm nn A` (…`E'`) | spustí návěští A (…`E'`) programu nn |
| `2nd Pgm nn SBR nnn` | spustí program nn od kroku nnn |
| `2nd Pgm 2nd Ind nn` | číslo programu se vezme z registru nn |
| `2nd Op 09` | zkopíruje knihovní program do programové paměti |

Program z modulu používá datové registry uživatele a jejich obsah může přepsat. Popis kláves k programům Master Library je na kartičkách v manuálu modulu.

⚠️ **Emulátor:**
- Vestavěná knihovna `src/TI5X.Avalonia/Assets/ml.ti5l` obsahuje jen **banku 01**, která kvůli sloučeným kódům neběží správně.
- `Pgm nn` nechá banku nn vybranou i po návratu. Do banky 0 se vrátíte stiskem `RST`.
- `Pgm` v programu skočí na krok 000 vybrané banky.
- `Op 09` není implementován.
- Další knihovny lze načíst přes `File > Load Library Module (.ti5l)...`.

Viz [Knihovní programy v přehledu odchylek](ODCHYLKY_A_OMEZENI.md#311-knihovní-programy-karty-a-soubory).

---

## 12. Tisk

S tiskárnou PC-100A (v emulátoru `View > Virtual Printer (PC-100A)`) jsou k dispozici tyto funkce. Řádek má 20 znaků.

| Klávesy | Účinek |
|---|---|
| `2nd Prt` | vytiskne obsah displeje (zarovnaný doprava) |
| `2nd Adv` | posune papír o řádek |
| `2nd List` | vypíše program ve tvaru `PPP  KK MNE` (krok, kód, mnemonika) |
| `2nd Op 08` | vypíše návěští programu (`PPP  LBL X`) |
| `2nd Op 00` | vymaže tiskový registr |
| `2nd Op 01`–`Op 04` | načte 5 znaků (10 číslic, dvojice = kód znaku) z displeje do 1.–4. čtvrtiny tiskového registru (20 znaků) |
| `2nd Op 05` | vytiskne tiskový registr |
| `2nd Op 06` | vytiskne displej s textem ze 4. čtvrtiny tiskového registru |
| `2nd Op 07` | vykreslí bod grafu (sloupec 0–19 podle x) |

Alfanumerický tisk: kód znaku je dvoumístné číslo, například `13` = A. Text „AB“ se připraví jako `1314000000 2nd Op 01`. Plnou tabulku znaků PC-100A uvádí manuál tiskárny.

⚠️ **Emulátor:**
- `2nd List` vypíše všechny kroky od 000 (páska pojme 500 řádků, začátek se ztratí).
- `Op 00` místo mazání zapne trasovací tisk (příznak 7).
- `Op 05` po tisku tiskový registr vymaže.
- `Op 06` a `Op 07` jen posunou papír.
- Znaky s kódy od 50 výše se mapují jinak.

Viz [Tiskárna v přehledu odchylek](ODCHYLKY_A_OMEZENI.md#312-tiskárna-pc-100a).

---

## 13. Přehled Op kódů

`2nd Op nn` vyvolá zvláštní funkci nn. `2nd Op 2nd Ind nn` vezme číslo funkce z registru nn.

| Op | Originál TI-59 | Emulátor |
|---|---|---|
| 00 | vymazat tiskový registr | ⚠️ zapne trasovací tisk (příznak 7) |
| 01–04 | načíst znaky do tiskového registru | ✅ |
| 05 | vytisknout tiskový registr | ⚠️ vytiskne a vymaže |
| 06 | tisk displeje s textem | ⚠️ jen posun papíru |
| 07 | bod grafu | ⚠️ jen posun papíru |
| 08 | výpis návěští | ✅ |
| 09 | stažení knihovního programu | ❌ ignoruje se |
| 10 | signum (−1, 0, 1) | ❌ ignoruje se |
| 11 | rozptyl | ⚠️ lineární regrese (směrnice, v t úsek) |
| 12 | směrnice a úsek | ⚠️ odhad y′ z x |
| 13 | korelační koeficient | ✅ |
| 14 | odhad y′ z x | ❌ ignoruje se |
| 15 | odhad x′ z y | ❌ ignoruje se |
| 16 | zobrazit rozdělení paměti | ⚠️ chybný tvar (`4852`) |
| 17 | nové rozdělení paměti | ⚠️ počet registrů místo desítek |
| 18 | bez chyby → nastavit příznak 7 | ❌ ignoruje se |
| 19 | při chybě → nastavit příznak 7 | ❌ ignoruje se |
| 20–29 | Rn + 1 (n = 0–9) | ⚠️ navíc zobrazí novou hodnotu |
| 30–39 | Rn − 1 (n = 0–9) | ⚠️ navíc zobrazí novou hodnotu |

Neznámé Op kódy emulátor tiše ignoruje bez chyby. Úplný popis je v [Op kódech v přehledu odchylek](ODCHYLKY_A_OMEZENI.md#4-op-kódy).

---

## 14. Chybové stavy

Při neplatné operaci přejde kalkulačka do chybového stavu a **displej bliká**. Chybu vyvolá například:

- dělení nulou a `1/x` pro x = 0,
- `lnx`, `log` pro x ≤ 0, `√x` pro x < 0,
- `INV sin`, `INV cos` pro |x| > 1,
- `INV yˣ` s x = 0,
- statistika s nedostatkem dat (`x̄` pro n = 0, odchylka pro n ≤ 1),
- přetečení rozsahu (v originále nad 9.999999999 × 10⁹⁹),
- příliš mnoho závorek nebo podprogramů, přebytečná `)`,
- skok na neexistující návěští nebo adresu, neplatná adresa registru.

Chybový stav zruší `CE`, `CLR` nebo zadání nového čísla. Při chybě v programu se program v originále zastaví jen při nastaveném příznaku 8.

⚠️ **Emulátor:**
- Displej při chybě bliká s naposledy zobrazenou hodnotou.
- Přetečení se hlásí až kolem 1,8 × 10³⁰⁸.
- Každá chyba zastaví program.
- Neplatná klávesa při zadávání adresy vede k chybě.

---

## 15. Tabulka kódů instrukcí

### 15.1 Úplná tabulka kódů 00 až 99

Tabulka obsahuje všech 100 kódů instrukcí. Sloupec **Mnemonika** je přesný text z `KeyCodes.PrintMnemonics` (bez úvodních a koncových mezer), který emulátor používá při `List`, `Op 08` a v debuggeru. Najdete v něm i neobvyklé tvary (`Yⓧ`, `ẍ`, `I×I`, `EQ`, `GE`).

Sloupec **Emulátor při běhu** popisuje, co engine s kódem udělá, když ho najde v programu.

| Kód | Mnemonika | Klávesy (originál) | Emulátor při běhu |
|---|---|---|---|
| 00 | `00` | `0` | číslice 0 |
| 01 | `01` | `1` | číslice 1 |
| 02 | `02` | `2` | číslice 2 |
| 03 | `03` | `3` | číslice 3 |
| 04 | `04` | `4` | číslice 4 |
| 05 | `05` | `5` | číslice 5 |
| 06 | `06` | `6` | číslice 6 |
| 07 | `07` | `7` | číslice 7 |
| 08 | `08` | `8` | číslice 8 |
| 09 | `09` | `9` | číslice 9 |
| 10 | `E'` | `2nd E'` | volání návěští E' |
| 11 | `A` | `A` | volání návěští A |
| 12 | `B` | `B` | volání návěští B |
| 13 | `C` | `C` | volání návěští C |
| 14 | `D` | `D` | volání návěští D |
| 15 | `E` | `E` | volání návěští E |
| 16 | `A'` | `2nd A'` | volání návěští A' |
| 17 | `B'` | `2nd B'` | volání návěští B' |
| 18 | `C'` | `2nd C'` | volání návěští C' |
| 19 | `D'` | `2nd D'` | volání návěští D' |
| 20 | `CLR` | – (pozice `2nd CLR`, nepoužívá se) | bez účinku |
| 21 | `2ND` | `2nd` (jen jako kód klávesy) | přepne 2nd pro další krok |
| 22 | `INV` | `INV` | INV |
| 23 | `LNX` | `lnx` | lnx |
| 24 | `CE` | `CE` | CE |
| 25 | `CLR` | `CLR` | CLR |
| 26 | `2ND` | – (nepoužívá se) | bez účinku |
| 27 | `INV` | – (nepoužívá se) | bez účinku |
| 28 | `LOG` | `2nd log` | log |
| 29 | `CP` | `2nd CP` | CP |
| 30 | `TAN` | `2nd tan` | tan |
| 31 | `LRN` | `LRN` | přepne LRN |
| 32 | `X⇌T` | `x⇌t` | x⇌t |
| 33 | `X²` | `x²` | x² |
| 34 | `√X` | `√x` | √x |
| 35 | `1/X` | `1/x` | 1/x |
| 36 | `PGM` | `2nd Pgm` | Pgm (operand 1 bajt) |
| 37 | `P/R` | `2nd P→R` | P→R |
| 38 | `SIN` | `2nd sin` | sin |
| 39 | `COS` | `2nd cos` | cos |
| 40 | `IND` | `2nd Ind` | Ind (jen za instrukcí, která ho přijímá) |
| 41 | `SST` | `SST` | krok programu |
| 42 | `STO` | `STO` | STO (operand 1 bajt) |
| 43 | `RCL` | `RCL` | RCL (operand 1 bajt) |
| 44 | `SUM` | `SUM` | SUM (operand 1 bajt) |
| 45 | `Yⓧ` | `yˣ` | yˣ |
| 46 | `INS` | `2nd Ins` | bez účinku |
| 47 | `CMS` | `2nd CMs` | CMs |
| 48 | `EXC` | `2nd Exc` | Exc (operand 1 bajt) |
| 49 | `PRD` | `2nd Prd` | Prd (operand 1 bajt) |
| 50 | `I×I` | `2nd \|x\|` | \|x\| |
| 51 | `BST` | `BST` | PC − 1 |
| 52 | `EE` | `EE` | EE |
| 53 | `(` | `(` | ( |
| 54 | `)` | `)` | ) |
| 55 | `÷` | `÷` | ÷ |
| 56 | `DEL` | `2nd Del` | bez účinku |
| 57 | `ENG` | `2nd Eng` | Eng |
| 58 | `FIX` | `2nd Fix` | Fix (operand 1 bajt) |
| 59 | `INT` | `2nd Int` | Int |
| 60 | `DEG` | `2nd Deg` | Deg |
| 61 | `GTO` | `GTO` | GTO (cíl 1 bajt) |
| 62 | `PG*` | `2nd Pgm 2nd Ind` | ⚠️ číslice 7 |
| 63 | `EX*` | `2nd Exc 2nd Ind` | ⚠️ číslice 8 |
| 64 | `PD*` | `2nd Prd 2nd Ind` | ⚠️ číslice 9 |
| 65 | `×` | `×` | × |
| 66 | `PAU` | `2nd Pause` | pauza asi 0,5 s |
| 67 | `EQ` | `2nd x=t` | x=t (cíl 1 bajt) |
| 68 | `NOP` | `2nd Nop` | nic |
| 69 | `OP` | `2nd Op` | Op (operand 1 bajt) |
| 70 | `RAD` | `2nd Rad` | Rad |
| 71 | `SBR` | `SBR` | SBR (cíl 1 bajt, po návratu stop) |
| 72 | `ST*` | `STO 2nd Ind` | ⚠️ číslice 4 |
| 73 | `RC*` | `RCL 2nd Ind` | ⚠️ číslice 5 |
| 74 | `SM*` | `SUM 2nd Ind` | ⚠️ číslice 6 |
| 75 | `-` | `−` | − |
| 76 | `LBL` | `2nd Lbl` | návěští (přeskočí 1 bajt) |
| 77 | `GE` | `2nd x≥t` | x≥t (cíl 1 bajt) |
| 78 | `∑+` | `2nd Σ+` | Σ+ |
| 79 | `ẍ` | `2nd x̄` | x̄ |
| 80 | `GRD` | `2nd Grad` | Grad |
| 81 | `RST` | `RST` | PC = 000, banka 0 |
| 82 | `HIR` | – (operace se skrytými registry) | ⚠️ číslice 1 |
| 83 | `GO*` | `GTO 2nd Ind` | ⚠️ číslice 2 |
| 84 | `OP*` | `2nd Op 2nd Ind` | ⚠️ číslice 3 |
| 85 | `+` | `+` | + |
| 86 | `STF` | `2nd St flg` | St flg (operand 1 bajt) |
| 87 | `IFF` | `2nd If flg` | If flg (příznak 1 bajt, cíl 1 bajt) |
| 88 | `DMS` | `2nd D.MS` | D.MS |
| 89 | `π` | `2nd π` | π |
| 90 | `LST` | `2nd List` | List |
| 91 | `R/S` | `R/S` | zastavení programu |
| 92 | `RTN` | `INV SBR` | ⚠️ číslice 0 |
| 93 | `.` | `.` | desetinná tečka |
| 94 | `+/-` | `+/-` | +/- |
| 95 | `=` | `=` | = |
| 96 | `WRT` | `2nd Write` | bez účinku |
| 97 | `DSZ` | `2nd Dsz` | Dsz (registr 1 bajt, cíl 1 bajt) |
| 98 | `ADV` | `2nd Adv` | Adv |
| 99 | `PRT` | `2nd Prt` | Prt |

### 15.2 Pravidla kódování programu

Emulátor používá standardní kódování instrukcí TI-59. Vzorem je program Lunar Lander v `tests/TI5X.Core.Tests/TestHelpers.cs` a v souboru `src/TI5X.Avalonia/Assets/lunar_lander.ti5p`.

1. **Číslice** mají kódy `00`–`09` (číslo 32 = `03 02`). Na klávesnici jsou totéž hodnoty 62–64, 72–74, 82–84, 92 číslicemi, v programu mají sloučený význam (viz pravidlo 7).
2. **Druhé funkce** zapisujte jedním kódem (`58` = Fix, `77` = x≥t, `67` = x=t). Kód `21` ve zdrojovém programu nemá význam.
3. **INV** je samostatný krok `22` před instrukcí (`22 44 00` = `INV SUM 00`, `22 77 17` = `INV x≥t B'`); jen `INV SBR` se sloučí do jediného kroku `92` (RTN).
4. **Adresa registru** (STO, RCL, SUM, Prd, Exc), číslo `Fix`, `Op`, příznaku a registru `Dsz` je **jeden bajt** (`42 05` = `STO 05`, `58 04` = `Fix 4`, `69 20` = `Op 20`, `86 00` = `St flg 0`). Bajt `21` na místě operandu vede k chybě.
5. **Cíl skoku** (`GTO`, `SBR`, `x=t`, `x≥t`, `If flg`, `Dsz`): návěští `10`–`19` (A–E') jako jeden bajt, absolutní adresa jako dva bajty `0n nn` (adresa 123 = `01 23`); jednobajtová forma `20`–`99` zůstává akceptována kvůli starším souborům.
6. **Nepřímé adresování** lze zapsat dvěma kroky `40 nn` za instrukcí (`42 40 05` = `STO Ind 05`, `61 40 07` = `GTO Ind 07`), ale standardně používejte sloučené kódy (pravidlo 7).
7. **Sloučené kódy** zapisujte jedním kódem: `62 05` = `Pgm Ind 05`, `63 05` = `Exc Ind 05`, `64 05` = `Prd Ind 05`, `72 05` = `STO Ind 05`, `73 05` = `RCL Ind 05`, `74 05` = `SUM Ind 05`, `83 05` = `GTO Ind 05`, `84 05` = `Op Ind 05`, `92` = `RTN`.
8. **Registry 21 a 40 nelze v programu adresovat přímo.** Bajt `40` na místě operandu znamená `Ind`, bajt `21` vede k chybě. Pro tyto registry použijte nepřímé adresování.
9. **SBR v programu** se po `RTN`/`INV SBR` vrací za volání a běh pokračuje; `SBR` z klávesnice (i přes `A`–`E'`) program po návratu zastaví.
10. **Číslo před `R/S`** se zachová (zadávání se ukončí).

### 15.3 Vytvoření souboru ti5p

Soubor `.ti5p` je ZIP archiv se dvěma položkami:

- `mimetype` – text `application/vnd.nz.gen.geek_central.ti5x`, uložený bez komprese jako první položka,
- `prog00` – XML s kódy kroků oddělenými mezerou:

```xml
<?xml version="1.0" encoding="utf-8"?>
<state><calc><prog>
76 11 33 65 89 95 92
</prog></calc></state>
```

Volitelně může obsahovat popis karty `card00` a nápovědu `help00` (formát popisuje [vývojářská dokumentace](EMULATOR_VYVOJ.md#6-formáty-souborů)). V systému macOS nebo Linux soubor vytvoříte například takto *(ověřeno)*:

```sh
printf 'application/vnd.nz.gen.geek_central.ti5x' > mimetype
# soubor prog00 s obsahem podle ukázky výše
zip -X0 kruh.ti5p mimetype
zip -X kruh.ti5p prog00
```

Soubor načtete přes `File > Open Program (.ti5p)...`. Program se uloží do banky 0 od kroku 000 a zbytek paměti se vynuluje. Program načtený ze souboru lze také uložit zpět přes `File > Save Program (.ti5p)...`.

---

## 16. Ukázkové programy

Ukázky obsahují instrukční kódy pro emulátor, připravené jako soubory `.ti5p`. U některých programů je uveden i původní zápis kláves v `LRN` pro porovnání s originálem. Všechny programy byly v emulátoru ověřeny *(ověřeno)*, včetně opakovaného spuštění.

### 16.1 Plocha kruhu

Vypočítá S = π·r². Zadejte r a stiskněte `A`.

Originál (LRN): `2nd Lbl A x² × 2nd π = INV SBR`

| Krok | Kód | Instrukce |
|---|---|---|
| 000 | 76 | `2nd Lbl` |
| 001 | 11 | `A` |
| 002 | 33 | `x²` |
| 003 | 65 | `×` |
| 004 | 89 | `2nd π` |
| 005 | 95 | `=` |
| 006 | 92 | `INV SBR` (návrat) |

Obsah `prog00`: `76 11 33 65 89 95 92`

Použití: `2 A` → `12.56637061`, `3 A` → `28.27433388`.

### 16.2 Faktoriál pomocí Dsz

Vypočítá n! pro celé n ≥ 1. Zadejte n a stiskněte `A`. Registr R01 je čítač smyčky, R02 průběžný součin.

Originál (LRN): `2nd Lbl A STO 01 1 STO 02 RCL 01 2nd Prd 02 2nd Dsz 1 007 RCL 02 INV SBR`. Adresa `007` se ukládá do dvou kroků (`00 07`) a `INV SBR` jako jediný krok `92`, takže čísla kroků odpovídají originálu.

| Krok | Kód | Instrukce | Poznámka |
|---|---|---|---|
| 000 | 76 11 | `2nd Lbl A` | |
| 002 | 42 01 | `STO 01` | čítač = n |
| 004 | 01 | `1` | |
| 005 | 42 02 | `STO 02` | součin = 1 |
| 007 | 43 01 | `RCL 01` | začátek smyčky |
| 009 | 49 02 | `2nd Prd 02` | součin × čítač |
| 011 | 97 01 00 07 | `2nd Dsz 1 007` | R01 − 1, pokud ≠ 0, skok na 007 |
| 015 | 43 02 | `RCL 02` | výsledek |
| 017 | 92 | `INV SBR` | konec |

Obsah `prog00`: `76 11 42 01 01 42 02 43 01 49 02 97 01 00 07 43 02 92`

Použití: `5 A` → `120.`, `6 A` → `720.`. Pro n = 0 by smyčka neskončila (čítač by klesal do záporných hodnot).

### 16.3 Převod teplot s příznakem

Klávesa `A` převádí teplotu podle příznaku 0: když není nastaven, °C → °F, když je nastaven, °F → °C. `B` nastaví příznak (režim °F → °C), `C` ho zruší (režim °C → °F).

| Krok | Kód | Instrukce | Poznámka |
|---|---|---|---|
| 000 | 76 11 | `2nd Lbl A` | |
| 002 | 87 00 17 | `2nd If flg 0 B'` | příznak 0 → skok na B' |
| 005 | 65 09 55 05 85 03 02 95 | `× 9 ÷ 5 + 32 =` | °C → °F |
| 013 | 92 | `INV SBR` | |
| 014 | 76 17 | `2nd Lbl B'` | |
| 016 | 75 03 02 95 65 05 55 09 95 | `− 32 = × 5 ÷ 9 =` | °F → °C |
| 024 | 92 | `INV SBR` | |
| 025 | 76 12 | `2nd Lbl B` | |
| 027 | 86 00 | `2nd St flg 0` | režim °F → °C |
| 029 | 92 | `INV SBR` | |
| 030 | 76 13 | `2nd Lbl C` | |
| 032 | 22 86 00 | `INV 2nd St flg 0` | režim °C → °F |
| 035 | 92 | `INV SBR` | |

Obsah `prog00`:

```
76 11 87 00 17 65 09 55 05 85 03 02 95 92
76 17 75 03 02 95 65 05 55 09 95 92
76 12 86 00 92
76 13 22 86 00 92
```

Použití: `100 A` → `212.`; `B`, pak `212 A` → `100.`; `C`, pak `37 A` → `98.6`.

### 16.4 Součet řady se smyčkou

Sečte 1 + 2 + … + n. Zadejte n a stiskněte `A`. Ukazuje `Op 20`, `SUM`, porovnání `x≥t` a skok na návěští.

| Krok | Kód | Instrukce | Poznámka |
|---|---|---|---|
| 000 | 76 11 | `2nd Lbl A` | |
| 002 | 42 01 | `STO 01` | R01 = n |
| 004 | 00 | `0` | |
| 005 | 42 00 | `STO 00` | čítač R00 = 0 |
| 007 | 42 02 | `STO 02` | součet R02 = 0 |
| 009 | 76 16 | `2nd Lbl A'` | začátek smyčky |
| 011 | 69 20 | `2nd Op 20` | R00 + 1 (emulátor ho zároveň zobrazí) |
| 013 | 44 02 | `SUM 02` | součet + x |
| 015 | 43 01 | `RCL 01` | |
| 017 | 32 | `x⇌t` | t = n |
| 018 | 43 00 | `RCL 00` | x = čítač |
| 020 | 77 19 | `2nd x≥t D'` | čítač ≥ n → konec |
| 022 | 61 16 | `GTO A'` | další průchod |
| 024 | 76 19 | `2nd Lbl D'` | |
| 026 | 43 02 | `RCL 02` | výsledek |
| 028 | 92 | `INV SBR` | |

Obsah `prog00`: `76 11 42 01 00 42 00 42 02 76 16 69 20 44 02 43 01 32 43 00 77 19 61 16 76 19 43 02 92`

Použití: `10 A` → `55.`

Na originálu `Op 20` displej nemění, proto by krok 013 musel být doplněn o `RCL 00` (originál: `2nd Op 20 RCL 00 SUM 02`). Emulátor zobrazí novou hodnotu R00 sám.

### 16.5 Zastavení programu pro vstup

Program se zastaví instrukcí `R/S`, uživatel zadá další číslo a pokračuje stiskem `R/S`. Ukázka sečte dvě čísla: zadejte první a stiskněte `A`, zadejte druhé a stiskněte `R/S`.

| Krok | Kód | Instrukce |
|---|---|---|
| 000 | 76 11 | `2nd Lbl A` |
| 002 | 42 01 | `STO 01` |
| 004 | 91 | `R/S` (čeká na druhé číslo) |
| 005 | 44 01 | `SUM 01` |
| 007 | 43 01 | `RCL 01` |
| 009 | 92 | `INV SBR` |

Obsah `prog00`: `76 11 42 01 91 44 01 43 01 92`

Použití: `5 A` → `5.`, `3 R/S` → `8.`

### 16.6 Lunar Lander

Rozsáhlejší ukázkou je hra Lunar Lander (přistání na Měsíci). Program je připravený v souboru `src/TI5X.Avalonia/Assets/lunar_lander.ti5p` a jeho kódy s komentáři jsou v `tests/TI5X.Core.Tests/TestHelpers.cs` (`LunarLanderProgram`). Pravidla hry, fyzikální model a ovládání popisuje [LUNAR_LANDER_SPEC.md](LUNAR_LANDER_SPEC.md).
