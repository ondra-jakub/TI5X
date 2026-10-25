# Odchylky emulace a známá omezení

Tento dokument shrnuje, v čem se emulátor TI5X (Avalonia a Android/MAUI) liší od originální kalkulačky TI-59 a které funkce chybí. Je to jediné místo, kde je seznam odchylek vedený. Příručka kalkulačky, uživatelská příručka emulátoru i vývojářská dokumentace na něj odkazují.

Popis odpovídá stavu kódu v `src/TI5X.Core/` (`CalculatorEngine`, `KeyCodes`, `PrinterEngine`, `PersistentState`, `LibraryModule`), `src/TI5X.ViewModels/`, `src/TI5X.Avalonia/MainWindow.axaml(.cs)` a `src/TI5X.Maui/`. Pokud se kód změní, je potřeba tento dokument aktualizovat.

## Obsah

1. [Legenda a metodika](#1-legenda-a-metodika)
2. [Nejdůležitější omezení](#2-nejdůležitější-omezení)
3. [Matice podpory funkcí](#3-matice-podpory-funkcí)
   - [3.1 Klávesnice, 2nd a INV](#31-klávesnice-2nd-a-inv)
   - [3.2 Zadávání čísel a formáty zobrazení](#32-zadávání-čísel-a-formáty-zobrazení)
   - [3.3 Aritmetika, AOS a závorky](#33-aritmetika-aos-a-závorky)
   - [3.4 Matematické funkce](#34-matematické-funkce)
   - [3.5 Paměti a nepřímé adresování](#35-paměti-a-nepřímé-adresování)
   - [3.6 Statistika](#36-statistika)
   - [3.7 Programování a režim LRN](#37-programování-a-režim-lrn)
   - [3.8 Skoky, podprogramy a podmínky](#38-skoky-podprogramy-a-podmínky)
   - [3.9 Příznaky](#39-příznaky)
   - [3.10 Rozdělení paměti](#310-rozdělení-paměti)
   - [3.11 Knihovní programy, karty a soubory](#311-knihovní-programy-karty-a-soubory)
   - [3.12 Tiskárna PC-100A](#312-tiskárna-pc-100a)
   - [3.13 Chybové stavy](#313-chybové-stavy)
4. [Op kódy](#4-op-kódy)
5. [Sloučené kódy instrukcí](#5-sloučené-kódy-instrukcí)
6. [Obecná omezení aplikace](#6-obecná-omezení-aplikace)
7. [Doporučené postupy](#7-doporučené-postupy)

---

## 1. Legenda a metodika

| Značka | Význam |
|---|---|
| ✅ | Podporováno, chová se jako originál (v rozsahu, který šlo ověřit). |
| ⚠️ | Částečně podporováno nebo se chová jinak než originál. |
| ❌ | Nepodporováno, klávesa nebo kód nemá žádný účinek, nebo vede k chybě. |

Jak byl obsah ověřen:

- ruční procházení větví `ProcessKey`, `CollectOperand`, `ExecuteKeyInteractive`, `ExecuteSingleKey`, `ExecuteSingleProgramStep`, `ProcessLrnKey` a `ExecuteOp` v jádru,
- **testovací sadou `TI5X.Core.Tests`** (TUnit): migrované scénáře Lunar Lander, conformance testy instrukční sady, vyčerpávající sweep všech opcodů × operandů (10 000 kombinací), paralelní a souběhové testy. Body *(ověřeno)* nyní odpovídají automatizovaným testům v `TI5X.Core.Tests`,
- **Dafny modelem** `src/TI5X.Verified/partition.dfy`, jehož vygenerovaný kód test porovnává s produkční aritmetikou rozdělení paměti.

Jako „originál“ se zde myslí kalkulačka TI-59 s tiskárnou PC-100A podle manuálu Texas Instruments.

---

## 2. Nejdůležitější omezení

Tyto odchylky mají na běžné používání největší dopad:

1. **Přeloženo na standard TI-59 (dřívější body 1–5 jsou opraveny).** Režim LRN ukládá kódy instrukcí standardu TI-59: číslice `00`–`09`, 2nd funkce jedním kódem, `INV SBR` jako jediný krok `92`, sloučené IND kódy (`72 05` = STO IND 05) a absolutní adresy skoků ve tvaru `61 0n nn`. Sloučené kódy i `RTN` (`92`) se v běhu programu provádějí správně, `SBR` z programu se po `RTN` vrací a pokračuje, číslo zapsané před `R/S` se zachová a `SST` provádí celou instrukci včetně operandů. Viz [3.7](#37-programování-a-režim-lrn) a [3.8](#38-skoky-podprogramy-a-podmínky).
   - ⚠️ **Zlomová změna:** programy dříve zapsané emulátorem (surové kódy kláves, jednobajtové adresy `61 nn` s `nn` ≤ 9, dvoukrokové `2nd`/`INV`) je nutné přepsát; nový dekodér čte bajty `00`–`09` na místě adresy skoku jako první bajt dvoubajtové adresy a bajt `21` na místě operandu vede k chybě. Expanded IND (`kód 40 nn`) zůstává podporován i nadále.
2. **`HIR` (kód 82) je definovaný no-op.** Původní sémantika skrytých registrů není v dostupných pramenech doložena, a proto se kód v programu provádí bez účinku (dříve se vykonal jako číslice 1).
3. **Každá chyba zastaví běžící program**, bez ohledu na příznak 8. *(ověřeno)* Chování příznaku 8 odpovídající originálu zatím není doloženo z primárního zdroje.
4. **`File > New Program` smaže i načtené knihovny.** Volá `Reset()` s `clearLibs: true`, takže zmizí i vestavěná banka Master Library. Obnoví ji nové spuštění aplikace nebo `File > Load Library Module (.ti5l)...` s `src/TI5X.Avalonia/Assets/ml.ti5l`.

---

## 3. Matice podpory funkcí

### 3.1 Klávesnice, 2nd a INV

| Funkce | Stav | Poznámka |
|---|---|---|
| 45 kláves v mřížce 9×5 s primárními kódy 11–95 | ✅ | `MainWindow.axaml`, každé tlačítko posílá svůj primární kód. |
| `2nd` | ✅ | Přepíná se (opakovaný stisk ho zruší) a mapuje se přes `MapSecond`. V režimu LRN viz [3.7](#37-programování-a-režim-lrn). |
| `INV` | ✅ | Přepíná se a platí pro následující funkci. Funkce, které INV nepoužívají, ho zruší. |
| `2nd R/S` (Write, kód 96) | ❌ | `MapSecond` pro `R/S` nemá mapování a tlačítko nemá druhou funkci. |
| `2nd CLR` | ✅ | `CLR` nemá druhou funkci ani na originálu; kód `20` nemá v emulátoru samostatný účinek. |
| `HIR` (kód 82) | ⚠️ | V programu definovaný no-op (původní sémantika skrytých registrů není doložena); na klávesnici nemá kód 82 jako takový význam mimo zadávání čísel. |
| Klávesy během běhu programu | ⚠️ | Funguje jen `R/S` (zastaví program). Ostatní stisky se zahodí. |
| Fyzická klávesnice PC | ⚠️ | Jen číslice (i numerická klávesnice), `.`, `+` (`OemPlus`/`Add`), `-` (`OemMinus`/`Subtract`), `×` a `÷` pouze z numerické klávesnice, `Enter` = `=`, `Backspace` = `CE`, `Escape` = `CLR`, `F1`–`F5` = `A`–`E`. Chybí 2nd, INV, STO, RCL, závorky a další. Pozor: `OemPlus` je na americkém rozložení klávesa `=`, která proto vloží `+`; na rozložení, kde `*` vyžaduje Shift+8, se událost `Key.D8` mapuje na číslici 8, protože handler modifikátor Shift nevyhodnocuje. |

### 3.2 Zadávání čísel a formáty zobrazení

| Funkce | Stav | Poznámka |
|---|---|---|
| Mantisa max. 10 číslic, `.`, `+/-` | ✅ | |
| `EE` – zadání exponentu (2 číslice, `+/-` mění znaménko exponentu) | ✅ | |
| `EE` po výsledku | ✅ | Přepne do vědeckého formátu (`FORMAT_FLOAT`). |
| `INV EE` – návrat z vědeckého formátu | ❌ | *(ověřeno)* `ProcessEE` INV ignoruje, formát zůstane vědecký a INV zůstane aktivní. Náhradní postup: `INV 2nd Eng` přepne zpět na pevný formát. |
| `2nd Eng` / `INV 2nd Eng` | ⚠️ | Funguje po dokončení čísla. *(ověřeno)* Stisk během zadávání čísla zahodí rozepsané číslo (zobrazí se `0`). |
| `2nd Fix n` (0–8), `Fix 9` a `INV Fix` = standardní formát | ✅ | Zaokrouhluje v pevném formátu. `Fix IND nn` není podporován. |
| `Fix` ve vědeckém a inženýrském formátu | ⚠️ | *(ověřeno)* Ignoruje se, mantisa má vždy 8 číslic (`2.0000000 00`). |
| Standardní formát | ⚠️ | Max. 10 číslic včetně úvodní nuly, nuly na konci se ořežou (`2/3` → `0.666666667`). Čísla ≥ 1E10 nebo < 1E-10 se automaticky přepnou do vědeckého formátu. |
| Rozsah 1E-99 až 9.999999999E99 | ⚠️ | *(ověřeno)* Emulátor počítá v `double` a chybu hlásí až při `NaN`/`Infinity` (cca 1.8E308). Například `1 EE 99 × 100 =` zobrazí `1.0000000 101` bez chyby. |
| Přesnost 13 číslic interně | ⚠️ | Emulátor používá `double` (asi 15–16 platných číslic), takže výsledky se mohou v posledních řádech lišit. |

### 3.3 Aritmetika, AOS a závorky

| Funkce | Stav | Poznámka |
|---|---|---|
| `+ - × ÷ =`, priorita AOS (yˣ > ×÷ > +−) | ✅ | Operace se stejnou prioritou se vyhodnocují zleva doprava. |
| `yˣ`, `INV yˣ` (odmocnina ˣ√y) | ✅ | `INV yˣ` s x = 0 hlásí chybu. |
| `(` `)` | ⚠️ | Odložené operace a závorky sdílejí jeden zásobník s 10 položkami. *(ověřeno)* 11. `(` vede k chybě. |
| Přebytečná `)` | ⚠️ | *(ověřeno)* Vede k chybě. |
| `=` uzavře otevřené závorky | ✅ | |
| `CE` | ✅ | Smaže celé zadávané číslo (ne jen poslední číslici). |
| `CLR` | ✅ | Zruší odložené operace a vynuluje displej. Paměti a program nemění. |

### 3.4 Matematické funkce

| Funkce | Stav | Poznámka |
|---|---|---|
| `lnx`, `INV lnx` (eˣ), `2nd log`, `INV 2nd log` (10ˣ) | ✅ | Pro x ≤ 0 chyba. |
| `x²`, `√x`, `1/x` | ✅ | |
| `INV x²` = √x, `INV √x` = x² | ⚠️ | Rozšíření emulátoru. Originál pro tyto klávesy inverzní funkci nemá. |
| `sin cos tan` a jejich `INV` | ✅ | `INV sin` a `INV cos` mimo ⟨−1, 1⟩ hlásí chybu. |
| `Deg`, `Rad`, `Grad` | ✅ | Výchozí je DEG. Režim ukazuje štítek v hlavním okně. |
| `2nd P→R`, `INV 2nd P→R` | ✅ | Konvence odpovídají originálu (ověřeno proti C simulátoru TMS0500 v režimu TI-59): `P→R` bere r v t, θ na displeji a vrací y na displeji, x v t. `INV P→R` zrcadlově: y na displeji, x v t, vrací θ na displeji, r v t. |
| `2nd D.MS`, `INV 2nd D.MS` | ⚠️ | *(ověřeno)* **Prohozený směr.** `D.MS` převádí desetinné stupně na tvar DD.MMSS (`1.30` → `1.18`), `INV D.MS` převádí DD.MMSS na desetinné stupně. V originále je to obráceně. |
| `2nd Int`, `INV 2nd Int` (zlomková část) | ✅ | |
| `2nd \|x\|` | ✅ | |
| `2nd π` | ✅ | |
| `x⇌t` | ✅ | |

### 3.5 Paměti a nepřímé adresování

| Funkce | Stav | Poznámka |
|---|---|---|
| `STO`, `RCL`, `SUM`, `INV SUM`, `2nd Prd`, `INV 2nd Prd`, `2nd Exc` | ✅ | Vždy se zadávají **dvě číslice** adresy. *(ověřeno)* Jednociferná adresa následovaná jinou klávesou (`STO 3 +`) vede k chybě. |
| `2nd CMs` | ⚠️ | Vynuluje všech 100 registrů bez ohledu na rozdělení paměti. |
| `IND` u STO/RCL/SUM/Prd/Exc/GTO/SBR/Pgm/Op | ✅ | Adresa se bere z celé části obsahu registru. Mimo rozsah 0–99 vede k chybě. |
| `IND` u `Fix`, `St flg`, `If flg`, `Dsz` | ❌ | Tyto funkce IND nepřijímají. |
| Dostupné registry | ⚠️ | *(ověřeno)* Vždy je dostupných všech 100 registrů (00–99) bez ohledu na rozdělení (Op 17). V originále jsou k dispozici jen registry v aktuálním rozdělení (výchozí 00–59). |
| Registry 21 a 40 v programu | ⚠️ | Operand se v programu čte strukturálně: bajt `40` na místě operandu znamená IND (`42 40 05` provede STO IND 05, sloučený kód `72 05` je ekvivalentní) a bajt `21` vede k chybě. Tyto registry lze v programu adresovat jen nepřímo. |

### 3.6 Statistika

Registry pro statistiku: R01 Σy, R02 Σy², R03 n, R04 Σx, R05 Σx², R06 Σxy (stejně jako v originále).

| Funkce | Stav | Poznámka |
|---|---|---|
| `2nd Σ+`, `INV 2nd Σ+` | ✅ | Konvence originálu: displej je **y** (R01/R02), obsah registru t je **x** (R04/R05/R06). Pro data s jednou proměnnou je výsledek stejný jako na originálu. *(ověřeno)* |
| `2nd x̄` | ✅ | Zobrazí průměr hodnot z displeje (ȳ), v t je průměr hodnot z t (x̄). |
| `INV 2nd x̄` | ✅ | Výběrová směrodatná odchylka (n−1) hodnot z displeje (sy) na displeji, odchylka hodnot z t (sx) v t. Pro n ≤ 1 chyba. |
| Op 11–15 | ⚠️ | Op 11 (směrnice) a Op 12 (odhad y′) odpovídají tabulce níže; korelace je kód 13 a navíc 14 (číslování originálu, používá ji ML-01). Originální Op 15 chybí, viz [Op kódy](#4-op-kódy). |

### 3.7 Programování a režim LRN

| Funkce | Stav | Poznámka |
|---|---|---|
| Programová paměť 960 kroků | ✅ | Výchozí `RegOffset = 480`, takže `GetMaxProg()` = 960 kroků. |
| `LRN`, zobrazení `PPP KK` (krok a kód) | ✅ | |
| Zápis instrukcí v LRN | ✅ | Ukládá kódy standardu TI-59: číslice `00`–`09`, 2nd funkce jedním kódem, `INV SBR` jako `92`, sloučené IND kódy a adresy skoků `0n nn`. |
| `SST`, `BST` v LRN | ✅ | Posun po jednotlivých bajtech (editační navigace). |
| `2nd Ins` v LRN | ✅ | Vloží krok `68` (Nop) na aktuální pozici. |
| `2nd Del` v LRN | ✅ | Smaže krok na aktuální pozici. |
| Úpravy knihovních bank | ✅ | Nelze je upravovat, stejně jako v originále. Zápis v LRN funguje jen v bance 0. |
| `SST` mimo LRN (krokování) | ✅ | Provede jednu celou instrukci včetně operandů (např. `STO 05` je jeden krok). |
| `BST` mimo LRN | ⚠️ | Jen sníží PC o 1. |
| `RST` | ⚠️ | *(ověřeno)* Nastaví PC = 000 a banku 0. **Nesmaže příznaky ani zásobník návratů**, což originál dělá. |
| `R/S` | ✅ | Spustí nebo zastaví program. Běh probíhá ve vlákně na pozadí. |
| `2nd Pause` v programu | ⚠️ | Zpoždění asi 500 ms, displej se během pauzy nijak zvlášť nezvýrazňuje. |
| `2nd Pause` z klávesnice | ✅ | Nemá účinek. |
| `2nd Nop` | ✅ | |
| `2nd CP` | ✅ | Smaže program banky 0; PC vynuluje jen v bance 0 (běhu programu z knihovní banky tedy nepřekáží). Rozdělení paměti nemění. |
| `2nd Lbl` | ✅ | Návěštím je libovolný nečíslicový klíč (v ML-01 např. `=`, `CLR`, `WRT`), hledá se v aktuální bance. Z klávesnice (`GTO`/`SBR`) i v roli cíle skoku programu lze použít kódy 10–99; jednobajtová absolutní adresa 20–99 se použije jen tehdy, když odpovídající návěští neexistuje (doplňková forma emulátoru). |

### 3.8 Skoky, podprogramy a podmínky

| Funkce | Stav | Poznámka |
|---|---|---|
| `GTO nnn` z klávesnice | ⚠️ | Vyžaduje 3 číslice nebo libovolné návěští (nečíslicový klíč). *(ověřeno)* `GTO IND nn` z klávesnice čeká na **3** číslice místo 2. |
| `GTO` v programu | ✅ | Cíl se čte strukturálně: návěští libovolné klávesy (bajt 10–99), absolutní adresa ve tvaru `0n nn`, `40 nn` pro IND; jednobajtová absolutní adresa `20`–`99` se použije, jen pokud odpovídající návěští v bance neexistuje. Sloučený kód `83 nn` = GTO IND. |
| `SBR` z klávesnice, klávesy A–E' | ✅ | Uloží návrat, spustí program a po `INV SBR` zastaví. |
| `SBR` v programu | ✅ | Po `RTN`/`INV SBR` se vrací za volání a běh pokračuje. |
| `SBR` s jiným návěštím než A–E' z klávesnice | ✅ | Přijímá libovolný nečíslicový klíč jako návěští, např. `SBR =` nebo `SBR lnx`. |
| `INV SBR` (návrat) | ✅ | |
| `RTN` jako kód 92 | ✅ | Provede se jako návrat z podprogramu. |
| Zásobník podprogramů 8 úrovní | ✅ | 9. úroveň vede k chybě. |
| Podprogram ukončený `R/S` | ⚠️ | *(ověřeno)* Volání `A`–`E'` z klávesnice uloží návratovou adresu. Skončí-li podprogram `R/S` místo `INV SBR`, adresa v zásobníku zůstane. Po 8 takových voláních vede 9. volání k chybě a `RST` zásobník nevyprázdní. |
| `2nd x=t`, `2nd x≥t` a jejich `INV` | ✅ | Při splnění se skočí na následující adresu nebo návěští, jinak se adresa přeskočí. V programu ✅. Z klávesnice podmínka při splnění čeká na adresu. Při nesplnění se posune PC, pokud je PC > 0. |
| `2nd Dsz n` a `INV 2nd Dsz n` | ⚠️ | Jen registry 0–9 bez IND, což originál umí. Zmenšování odpovídá originálu: absolutní hodnota se zmenšuje směrem k nule, nula zůstává nula a smyčka se zastaví. *(ověřeno)* |
| Op 20–29 / Op 30–39 | ⚠️ | Viz [Op kódy](#4-op-kódy). |

### 3.9 Příznaky

10 příznaků (0–9). `2nd St flg n`, `INV 2nd St flg n` a `2nd If flg n` / `INV 2nd If flg n` přijímají jednu číslici a fungují *(ověřeno)*. `IND` podporováno není.

| Příznak | Emulátor | Originál TI-59 |
|---|---|---|
| 0–6 | ✅ Obecné příznaky. | Obecné příznaky. |
| 7 | ⚠️ **Trasovací tisk.** Při nastaveném příznaku se každý výsledek (`SetX`) vytiskne. Nastavuje ho i Op 0. | Nastavují ho Op 18/19 (test chyby). |
| 8 | ❌ Bez účinku. Chyba program zastaví vždy. | Zastavení programu při chybě. |
| 9 | ⚠️ **Zpomalený běh** (50 ms na krok). | Trasovací tisk na PC-100A. |

### 3.10 Rozdělení paměti

| Funkce | Stav | Poznámka |
|---|---|---|
| Výchozí rozdělení 479.59 | ⚠️ | Interně `RegOffset = 480`, ale v bance 0 je k dispozici **960** kroků a všech 100 registrů. Stavový řádek ukazuje `Regs: 52` a debugger registry R00–R51. |
| Op 16 (zobrazení rozdělení) | ⚠️ | *(ověřeno)* Ve výchozím stavu zobrazí `4852` místo `479.59`. Po `60 Op 17` zobrazí `4060`. |
| Op 17 (nové rozdělení) | ⚠️ | *(ověřeno)* Bere počet registrů 0–100 (nezaokrouhluje na desítky). Počet kroků se ale počítá jako `(100 − n) × 20`, takže `60 Op 17` dá 800 kroků a `0 Op 17` 2000 kroků s rizikem pádu. Výchozímu stavu (960 kroků) odpovídá `52 Op 17`. Originál bere počet desítek registrů (`6 Op 17` → 479.59, tj. 480 kroků a 60 registrů). Přístup k registrům se nijak neomezuje. |

### 3.11 Knihovní programy, karty a soubory

| Funkce | Stav | Poznámka |
|---|---|---|
| `2nd Pgm nn` z klávesnice, pak A–E' | ⚠️ | Přepne banku na `nn` a nastaví PC = 0. Banka **zůstane vybraná** i po návratu z podprogramu, takže další A–E' se hledají v knihovně. Do banky 0 vrátí `RST` nebo `Pgm 00`. V originále platí `Pgm` jen pro nejbližší volání. |
| `Pgm nn` v programu | ✅ | Vybere banku pro nejbližší `SBR`/návěští; po návratu z podprogramu se běh vrací do volající banky. *(ověřeno)* |
| `Pgm IND nn` | ✅ | Neexistující banka vede k chybě. |
| Op 9 (stažení knihovního programu) | ❌ | |
| Diagnostika ML-01 (`2nd Pgm 01`, `SBR =`) | ⚠️ | Inicializace, statistická řada, přesahy mezi bankami (`Pgm 15` a volání `SBR DMS`) i `P/R`/`Σ+` konvence nyní odpovídají originálu; selže jen závěrečný kontrolní součet — porovnání `x=t` (banka 01, PC 70) nedá shodu, vznikne záměrná instrukční chyba (`0`, `1/x`) místo tisku `MASTER` a `1`. Rutina DMS vrací `-0,50242` místo očekávaných `377,2580954`: originální mikrokód konverze `P/R` a statistických Op používá skryté pracovní registry R8/R9 (viz `doc/memory-map.txt` v TMS0500-simulátoru), které emulátor nemá napodobené. |
| Magnetické karty (čtení, zápis, `2nd Write`) | ❌ | Kartu nahrazují soubory `.ti5p` a `.ti5l`. Karta se zobrazuje jen jako popisný proužek (štítky A–E'). |
| Master Library | ⚠️ | Vestavěná `src/TI5X.Avalonia/Assets/ml.ti5l` je sestavena z dumpu originálního CROM TMC0541 (celkem 25 programů) nástrojem `util/mklibfromdump`. Návěští libovolných kláves a přesahy mezi bankami se nyní provádějí správně; celá ML-01 diagnostika ale stále selhává na závěrečném kontrolním součtu (viz další řádek). |
| Nápověda v knihovnách (`help`, `helpNN`) | ❌ | Načte se, ale aplikace ji nikde nezobrazuje. |
| `.ti5p` – uložení | ⚠️ | Uloží jen program banky 0 (všech 960 kroků) a kartu/nápovědu **aktuální** banky. Neukládá registry, příznaky, režimy ani rozdělení. Ze štítků karty uloží jen `titleleft`, `titleright` a `a`–`eprime` (ztratí se `subtitle*`, `primecenter`, `base*`). Texty štítků se neescapují pro XML. |
| `.ti5p` – načtení | ✅ | Nahradí program banky 0 a nastaví PC = 0 a banku 0. Registry nemění. |

### 3.12 Tiskárna PC-100A

Všechny tiskové cesty jsou vedeny přes **ověřený model TMC0251** (port `ti59-dafny/dfy/TI59/Print.dfy`, věrný C orákulu `print.c`; v aplikaci jako `src/TI5X.Core/Ti59PrintModel.cs`). Font, hlava (init 5, sweep zprava doleva), 20znakový buffer a mnemoniky `print_func` nyní odpovídají hardwaru; konzistence s orákulem je bit-exaktně doložena korpus 12/12 (`ti59-dafny/scripts/corpus.sh`) a testy `Ti59PrintModelTests`.

| Funkce | Stav | Poznámka |
|---|---|---|
| `2nd Prt` | ✅ | Vytiskne obsah displeje zarovnaný doprava na 20 znaků. |
| `2nd Adv` | ✅ | Prázdný řádek (op `0x0AB8` = `display_print("")`, věrně modelu). |
| `2nd List` | ✅ | *(ověřeno)* Vytiskne kroky **od aktuálního kroku** do konce rozdělení ve tvaru `PPP  KK MNE`, včetně prázdných — jako v originále; počátek výpisu posune `GTO nnn` mimo LRN. Mnemoniky: u 28 kódů autentická TMC0251 mnemonika z `print_func` (`÷`→` / `, `√X`→`sX `, `π`→`pi `, `∑+`→`$+ `, `EXC`→`X|Y`, …), jinde klávesní zkratka `KeyCodeSym`. Páska pojme 1000 řádků, takže výpis výchozího rozdělení (960 kroků) se vejde celý i se začátkem. |
| `INV 2nd List` a `INV 2nd Prt` | ❌ | INV se ignoruje. |
| Trasovací tisk | ⚠️ | Přes příznak 7 / Op 0 se tisknou jen výsledky (hodnota displeje), ne kódy kláves s mnemonikami. |
| Op 1–4, Op 5 (alfanumerický tisk) | ✅ | Mapování dvouciferných kódů XY na znaky overeno empiricky proti reálné ROM (Master Library ML-01, orákulum TMS0500-sim): `((X mod 8) << 3) | Y`, tj. desitky 8/9 se maskují a jednotky 8/9 ORují bit 3 — celá tabulka 00–99 popsána ve `PrinterEngine.PrintRegisterChar` (14 měřených bodů). Dříve dokládané odlišení „od kódu 50 výš / kódy s číslicí 8–9 nedefinované” se ukázalo jako chyba starší ruční mapy `CodeToChar`; správnou sadu udává ověřený font v `Ti59PrintMachine.FontTable` (= `print_font` z print.c, 64 znaků). |
| Grafická znaková sada 5×7 | ⚠️ | Tabulka `PrinterEngine.Chars` existuje, ale nepoužívá se. Páska je obyčejný text. |
| Délka pásky | ⚠️ | Max. 1000 řádků, nejstarší se zahazují (model si pásku drží celou; modelové řádky mají vždy 20 sloupců). |
| Op 6, Op 7 (tisk s popisem, vykreslování grafu) | ⚠️ | Jen posunou papír o jeden řádek. |
| Tisk řízený ROM (diagnostika ML-01 „MASTER/1.”) | ⚠️ | Model i orákulum jej reprodukují bit-exaktně (korpus `ml01print`), ale **aplikace jej netiskne** — nespouští reálnou ROM, takže autotisk z knihovních programů chybí. Odkládá se do fáze 4/5 (integrace jádra za feature flagu), viz `ti59-dafny/README.md`. |

### 3.13 Chybové stavy

| Funkce | Stav | Poznámka |
|---|---|---|
| Chyba při dělení nulou, ln/log x ≤ 0, √ záporného čísla, asin/acos mimo rozsah, 1/0, nedefinovaná statistika | ✅ | |
| Přetečení nad 9.999999999E99 | ⚠️ | Viz [3.2](#32-zadávání-čísel-a-formáty-zobrazení). |
| Neplatná klávesa během zadávání adresy | ⚠️ | Vede k chybě a zruší rozepsanou instrukci. |
| Zobrazení při chybě | ⚠️ | Bliká naposledy zobrazená hodnota (ne nutně `0.`). |
| Neznámý Op kód (9, 10, 14, 15, 18, 19, ≥ 40) | ⚠️ | *(ověřeno)* Tiše se ignoruje, bez chyby. |
| Zrušení chyby | ✅ | Chybu zruší `CE`, `CLR` nebo zadání nového čísla. |

---

## 4. Op kódy

Podle `CalculatorEngine.ExecuteOp`. `Op IND nn` je podporován.

| Op | Originál TI-59 | Emulátor | Stav |
|---|---|---|---|
| 00 | Vymazat tiskový registr | Nastaví příznak 7 (trasovací tisk). Tiskový registr **nemaže**. | ⚠️ |
| 01–04 | Načíst 5 znaků do čtvrtiny tiskového registru | Stejně (celá část \|x\|, 10 číslic → 5 dvojic). | ✅ |
| 05 | Vytisknout tiskový registr; zda jej originál po tisku také vymaže, zde není ověřeno. | Vytiskne tiskový registr a potom jej vymaže. | ⚠️ |
| 06 | Vytisknout displej s textem z Op 04 | Jen posun papíru. | ⚠️ |
| 07 | Vykreslit bod grafu | Jen posun papíru. | ⚠️ |
| 08 | Vypsat návěští | Vypíše `PPP  LBL X` pro aktuální banku. | ✅ |
| 09 | Stáhnout knihovní program do uživatelské paměti | Ignoruje se. | ❌ |
| 10 | Signum | Ignoruje se. | ❌ |
| 11 | Rozptyl | **Lineární regrese:** směrnice na displeji, úsek v t. | ⚠️ |
| 12 | Směrnice a úsek | **Odhad y' z x na displeji.** | ⚠️ |
| 13 | Korelační koeficient | Korelační koeficient (emulátorová konvence). | ✅ |
| 14 | Odhad y' z x | Korelační koeficient, stejný výsledek jako Op 13 (číslování tak, jak jej používá ML-01). | ⚠️ |
| 15 | Odhad x' z y | Ignoruje se. | ❌ |
| 16 | Zobrazit rozdělení paměti | Zobrazí chybnou hodnotu (`4852`), viz [3.10](#310-rozdělení-paměti). | ⚠️ |
| 17 | Nové rozdělení paměti | Jiný výpočet počtu kroků, viz [3.10](#310-rozdělení-paměti). | ⚠️ |
| 18 | Při bezchybném stavu nastavit příznak 7 | Ignoruje se. | ❌ |
| 19 | Při chybě nastavit příznak 7 | Ignoruje se. | ❌ |
| 20–29 | Zvýšit R0–R9 o 1 | Zvýší registr a **novou hodnotu zapíše do X a na displej**. | ⚠️ |
| 30–39 | Snížit R0–R9 o 1 | Sníží registr a novou hodnotu zapíše do X a na displej. | ⚠️ |
| 40+ | – | Ignoruje se. | – |

Op 11–14 počítají s rolemi x a y popsanými v [3.6](#36-statistika): displej při `Σ+` je y, obsah registru t je x.

---

## 5. Sloučené kódy instrukcí

Tabulka `KeyCodes.PrintMnemonics` obsahuje mnemoniky pro všechny kódy 00–99 a slouží debuggeru a výpisu `List`. Sloučené kódy se v běhu programu provádějí podle standardu TI-59 (na klávesnici jsou tytéž hodnoty číslicemi):

| Kód | Mnemonika | Význam | Chování v emulátoru |
|---|---|---|---|
| 62 | `PG*` | Pgm IND | ✅ Pgm Ind nn (i jako `36 40 nn`) |
| 63 | `EX*` | Exc IND | ✅ Exc Ind nn |
| 64 | `PD*` | Prd IND | ✅ Prd Ind nn |
| 72 | `ST*` | STO IND | ✅ STO Ind nn |
| 73 | `RC*` | RCL IND | ✅ RCL Ind nn |
| 74 | `SM*` | SUM IND | ✅ SUM Ind nn |
| 82 | `HIR` | Operace se skrytými registry | ⚠️ Definovaný no-op (sémantika nedoložena) |
| 83 | `GO*` | GTO IND | ✅ GTO Ind nn (i jako `61 40 nn`) |
| 84 | `OP*` | Op IND | ✅ Op Ind nn |
| 92 | `RTN` | Návrat z podprogramu | ✅ RTN |
| 96 | `WRT` | Zápis karty | Bez účinku |
| 20, 26, 27 | `CLR`, `2ND`, `INV` | kódy bez samostatné instrukce | `20` bez účinku, `26`/`27` bez účinku (kód `22` na místě instrukce přepne INV) |

Kódy 00–09 se v programu provedou jako číslice, kódy 10–19 jako volání návěští A–E'. Ostatní kódy odpovídají své klávese (primární kód nebo kód 2nd funkce). V režimu LRN se sloučené kódy tvoří automaticky (`STO 2nd Ind 05` uloží `72 05`).

---

## 6. Obecná omezení aplikace

- **Master Library.** Vestavěná `src/TI5X.Avalonia/Assets/ml.ti5l` obsahuje všech 25 bank (sestaveno z dumpu CROM TMC0541, viz [3.11](#311-knihovní-programy-karty-a-soubory)). Vlastní knihovny lze načíst přes `File > Load Library Module (.ti5l)...` (v Androidu `Menu > Load Library Module (.ti5l)`). Načtená knihovna přepíše banky se stejným číslem.
- **`.ti5p` neukládá registry**, příznaky, režim úhlů, formát zobrazení ani rozdělení paměti. Obsah registrů se při načtení programu nemění.
- **Omezená fyzická klávesnice.** Viz [3.1](#31-klávesnice-2nd-a-inv). Ostatní funkce se ovládají myší na virtuální klávesnici.
- **Debugger** (`View > Program Debugger / Steps`) zobrazuje kroky aktuální banky, přičemž bajty operandů ukazuje také jako mnemoniky (např. adresa `42` se zobrazí jako `STO`). Registry ukazuje jen R00–R51 podle chybného přepočtu z `RegOffset`. `Reset` nastaví pouze PC = 0; `Step` i `Reset` jsou při běhu programu ignorovány.
- **Stavový řádek** ukazuje `Regs: 100 − RegOffset/10`, ve výchozím stavu tedy `52`.
- **Rychlost běhu** neodpovídá originálu. Program běží tak rychle, jak dovolí vlákno na pozadí (s příznakem 9 50 ms na krok).
- **GUI nemá automatizované testy.** Testovací projekt `TI5X.Core.Tests` (TUnit) pokrývá jádro: základní aritmetiku, DisplayCodes, celou sadu Lunar Lander včetně roundtripu `.ti5p`, conformance sloučených kódů, LRN zápis, bezpečnost mezí a souběhu (viz [EMULATOR_VYVOJ.md, kap. 7](EMULATOR_VYVOJ.md)). Androidová aplikace je navíc ověřená jen ručním smoke testem na API 33.
- **Android verze je pouze pro Android.** MAUI projekt necílí na iOS, Windows ani Mac Catalyst. Navigace je mobilní (toolbar s položkou **Menu**, podstránky tiskárny/debuggeru/nápovědy), ale emulační chování je shodné s desktopem.
- **Soubory v Androidu přes SAF.** Místo cest k souborům se používají content URI systémového výběru; obsah se vždy načítá/ukládá přes `MemoryStream`. Aplikace nevyžaduje žádné oprávnění k úložišti.
- **Stav se neobnovuje po ukončení procesu.** Android může aplikaci kdykoli ukončit; program, registry ani příznaky se automaticky neukládají a po novém startu se načte jen vestavěná knihovna.
- **Vlákna.** Běh programu používá generaci běhu a lokální token zrušení; rychlé zastavení a nové spuštění již nemůže ukončit nový běh ani číst zrušený token. Stav enginu ale nadále sdílí UI vlákno bez zamykání, proto se při editaci v debuggeru v průběhu kroku může krátce zobrazit mezistav.
- **Experimentální Android verze na Avalonii** (`src/TI5X.Avalonia.Android`) má oproti MAUI verzi tato omezení: vyžaduje Android 6.0 (API 23), protože závislosti `Avalonia.Android` (AndroidX lifecycle) nesestaví s minSdk 21; nemá ikonu aplikace ani splash obrazovku (nainstaluje se výchozí robot Androidu); navigační lišta (`NavigationPage`) má systémové světlé téma, nemodifikované na tmavé barevné schéma aplikace; SAF dialog s MIME `*/*` nezakáže vybrat soubor s jinou příponou (při neúspěšném načtení vypíše chybu). Ověřeno ručním smoke testem na API 33, viz [AVALONIA_ANDROID_VYHODNOCENI.md](AVALONIA_ANDROID_VYHODNOCENI.md).

---

## 7. Doporučené postupy

Vzhledem k zbývajícím omezením fungují spolehlivě tyto postupy:

- Programy zapisujte v režimu LRN (nyní ukládá standardní kódy TI-59) nebo je připravujte jako `.ti5p` s kódy TI-59: číslice `00`–`09`, funkce 2nd jedním kódem, sloučené IND kódy, adresy skoků `0n nn`. Vzorem je `src/TI5X.Avalonia/Assets/lunar_lander.ti5p` a [LUNAR_LANDER_SPEC.md](LUNAR_LANDER_SPEC.md).
- Podprogramy ukončujte `INV SBR` (nebo `RTN`, `92`). Podprogram volaný z klávesnice (`A`–`E'`) a ukončený `R/S` nechá návratovou adresu v zásobníku.
- Expanded IND (`kód 40 nn`) i sloučené kódy (`72 05`) jsou zaměnitelné; pro registry 21 a 40 používejte vždy nepřímé adresování.
- Absolutní adresy skoků zapisujte třímístně (`GTO 123` → `61 01 23`); jednobajtovou formu `20`–`99` emulátor akceptuje, ale standard TI-59 ji nedefinuje.
- `Op 17` je nyní bezpečný (mez jsou fyzicky omezeny na délku banky), ale přepočet kroků stále neodpovídá originálu (viz [3.10](#310-rozdělení-paměti)); nespoléhejte na `Op 16`.
