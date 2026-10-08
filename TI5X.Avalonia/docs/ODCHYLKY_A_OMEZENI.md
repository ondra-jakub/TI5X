# Odchylky emulace a známá omezení

Tento dokument shrnuje, v čem se desktopový emulátor TI5X (Avalonia) liší od originální kalkulačky TI-59 a které funkce chybí. Je to jediné místo, kde je seznam odchylek vedený. Příručka kalkulačky, uživatelská příručka emulátoru i vývojářská dokumentace na něj odkazují.

Popis odpovídá stavu kódu v `Core/CalculatorEngine.cs`, `Core/KeyCodes.cs`, `Core/PrinterEngine.cs`, `Core/PersistentState.cs`, `Core/LibraryModule.cs`, `MainWindow.axaml(.cs)` a `ViewModels/`. Pokud se kód změní, je potřeba tento dokument aktualizovat.

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

- ruční procházení větví `ProcessKey`, `CollectOperand`, `ExecuteKeyInteractive`, `ExecuteSingleKey`, `ExecuteSingleProgramStep`, `ProcessLrnKey` a `ExecuteOp` v `CalculatorEngine.cs`,
- kontrolní konzolová aplikace (mimo repozitář), která volala `CalculatorEngine.ProcessKey` a spouštěla krátké programy. Body označené *(ověřeno)* jsou výsledkem tohoto běhu.

Jako „originál“ se zde myslí kalkulačka TI-59 s tiskárnou PC-100A podle manuálu Texas Instruments.

---

## 2. Nejdůležitější omezení

Tyto odchylky mají na běžné používání největší dopad:

1. **Programy zapsané v režimu LRN z klávesnice nefungují správně.** *(ověřeno)* `ProcessLrnKey` ukládá do paměti přímo kódy stisknutých kláves:
   - `2nd` se uloží jako samostatný krok `21`, za ním primární kód klávesy (např. `2nd log` → `21 23` místo `28`),
   - číslice se uloží jako kódy pozic na klávesnici (`7` → `62`, `0` → `92`, `5` → `73`) místo `07`, `00`, `05`,
   - `IND` se uloží jako `21 45` a sloučené kódy (`72` = STO IND apod.) nevznikají.

   Číslice v kódu klávesy se při běhu sice vyhodnotí správně, ale adresa operandu se přečte jako surový bajt. Například `5 STO 0 1` se uloží jako `73 42 92 82` a při běhu se provede `STO 92`. Funkční programy je proto nutné připravit jako soubor `.ti5p` se správnými kódy TI-59 (formát popisuje [vývojářská dokumentace](EMULATOR_VYVOJ.md#6-formáty-souborů)).
2. **Sloučené kódy `62, 63, 64, 72, 73, 74, 82, 83, 84` a `RTN` (`92`) se v programu provedou jako číslice.** *(ověřeno)* `KeyToDigit` mapuje tyto hodnoty na číslice 7, 8, 9, 4, 5, 6, 1, 2, 3 a 0. Místo `RTN` je potřeba použít `INV SBR` (`22 71`) a místo sloučených kódů zápis s `IND` (např. `42 40 05` pro STO IND 05). Ani programy z Master Library, které tyto kódy používají, proto neběží správně.
3. **`SBR` uvnitř běžícího programu zastaví program po návratu.** *(ověřeno)* Každé `SBR` ukládá návrat s příznakem „voláno z klávesnice“. Po `INV SBR` se program vrátí na místo za voláním a zastaví se.
4. **Číslo zadané v programu těsně před `R/S` se ztratí.** *(ověřeno)* Program `7 R/S` skončí s displejem `0.`, protože se zadávání čísla neukončí a po zastavení se zobrazí stará hodnota X. Číslo je potřeba ukončit operací, například `=` nebo `STO nn`.
5. **Absolutní adresa skoku v programu má jen jeden bajt (0–99).** *(ověřeno)* Originál zapisuje `GTO nnn` jako dva kroky (`61 0n nn`). Emulátor přečte jako adresu jen první bajt, takže dvoubajtový zápis originálu skočí jinam. Bajty `10`–`19` se v roli adresy berou jako návěští A–E'.
6. **Každá chyba zastaví běžící program**, bez ohledu na příznak 8. *(ověřeno)*
7. **Pád aplikace při chybném rozdělení paměti.** *(ověřeno)* Po `0 Op 17` vrací `GetMaxProg()` 2000 kroků, ale pole programu má jen 960 bajtů. Hledání neexistujícího návěští pak vyvolá `IndexOutOfRangeException`. Stejné riziko existuje v knihovních bankách, které jsou kratší než 1000 kroků (vestavěná banka 01 má 127 kroků): hrozí při hledání chybějícího návěští, při `2nd List` (`ExecuteList`), při `Op 8` (`ExecuteListLabels`), při `SST` za koncem banky nebo když program doběhne za konec banky. Po `0 Op 17` platí totéž pro `List` a `Op 8` v bance 0. Technické pozadí popisuje [vývojářská dokumentace, kapitola 9](EMULATOR_VYVOJ.md#9-technická-úskalí).
8. **`File > New Program` smaže i načtené knihovny.** Volá `Reset()` s `clearLibs: true`, takže zmizí i vestavěná banka Master Library. Obnoví ji nové spuštění aplikace nebo `File > Load Library Module (.ti5l)...` s `Assets/ml.ti5l`.

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
| `HIR` (kód 82) | ❌ | Kód 82 se provede jako číslice 1. |
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
| `2nd P→R`, `INV 2nd P→R` | ⚠️ | *(ověřeno)* Opačné pořadí registrů. Emulátor bere **r na displeji a θ v registru t** a vrací **x na displeji a y v t**. Originál bere r v t a θ na displeji a vrací y na displeji a x v t. `INV P→R` je zrcadlové: x na displeji a y v t → r na displeji a θ v t. |
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
| Registry 21 a 40 v programu | ⚠️ | *(ověřeno)* Operand se v programu čte jako surový bajt přes `CollectOperand`: bajt `21` přepne 2nd (např. `42 21` R21 nezmění) a bajt `40` se bere jako IND (`42 40 05` provede STO IND 05). Tyto registry lze v programu adresovat jen nepřímo. |

### 3.6 Statistika

Registry pro statistiku: R01 Σy, R02 Σy², R03 n, R04 Σx, R05 Σx², R06 Σxy (stejně jako v originále).

| Funkce | Stav | Poznámka |
|---|---|---|
| `2nd Σ+`, `INV 2nd Σ+` | ⚠️ | *(ověřeno)* **Prohozené role x a y.** Emulátor bere hodnotu na displeji jako **x** (R04/R05) a obsah registru t jako **y** (R01/R02). Originál bere displej jako y a t jako x. Pro data s jednou proměnnou to nevadí, protože průměr i odchylka se počítají z hodnot na displeji. |
| `2nd x̄` | ⚠️ | Zobrazí průměr hodnot zadaných na displeji a do t uloží průměr hodnot z t. |
| `INV 2nd x̄` | ⚠️ | Výběrová směrodatná odchylka (n−1) hodnot z displeje, do t uloží odchylku hodnot z t. Pro n ≤ 1 chyba. |
| Op 11–15 | ⚠️ | Jiné číslování než originál, viz [Op kódy](#4-op-kódy). |

### 3.7 Programování a režim LRN

| Funkce | Stav | Poznámka |
|---|---|---|
| Programová paměť 960 kroků | ✅ | Výchozí `RegOffset = 480`, takže `GetMaxProg()` = 960 kroků. |
| `LRN`, zobrazení `PPP KK` (krok a kód) | ✅ | |
| Zápis instrukcí v LRN | ❌ | Ukládají se surové kódy kláves, viz [bod 1 v kap. 2](#2-nejdůležitější-omezení). |
| `SST`, `BST` v LRN | ✅ | |
| `2nd Ins` v LRN | ❌ | Z virtuální klávesnice nedostupné. `ProcessLrnKey` reaguje jen na přímý kód 46, ale tlačítka posílají primární kódy a `2nd` se v LRN nepřepíná. `2nd SST` proto uloží krok `21` a `SST` pak posune ukazatel. Samotná funkce `InsertInstruction` by vložila `68` (Nop) místo `00`. |
| `2nd Del` v LRN | ❌ | Stejné omezení jako `Ins`: `2nd BST` uloží krok `21` a `BST` se vrátí zpět. |
| Úpravy knihovních bank | ✅ | Nelze je upravovat, stejně jako v originále. Zápis v LRN funguje jen v bance 0. |
| `SST` mimo LRN (krokování) | ✅ | Provede jeden krok programu. |
| `BST` mimo LRN | ⚠️ | Jen sníží PC o 1. |
| `RST` | ⚠️ | *(ověřeno)* Nastaví PC = 000 a banku 0. **Nesmaže příznaky ani zásobník návratů**, což originál dělá. |
| `R/S` | ✅ | Spustí nebo zastaví program. Běh probíhá ve vlákně na pozadí. |
| `2nd Pause` v programu | ⚠️ | Zpoždění asi 500 ms, displej se během pauzy nijak zvlášť nezvýrazňuje. |
| `2nd Pause` z klávesnice | ✅ | Nemá účinek. |
| `2nd Nop` | ✅ | |
| `2nd CP` | ✅ | Smaže program banky 0 a nastaví PC = 0. Rozdělení paměti nemění. |
| `2nd Lbl` | ⚠️ | Návěštím může být libovolný kód. Hledá se jen v aktuální bance. Z klávesnice (`GTO`/`SBR`) a v roli cíle skoku se jako symbolická berou jen A–E' (kódy 10–19). |

### 3.8 Skoky, podprogramy a podmínky

| Funkce | Stav | Poznámka |
|---|---|---|
| `GTO nnn` z klávesnice | ⚠️ | Vyžaduje 3 číslice nebo návěští A–E'. *(ověřeno)* `GTO IND nn` z klávesnice čeká na **3** číslice místo 2. |
| `GTO` v programu | ⚠️ | Jednobajtová adresa 0–99, nebo návěští A–E', nebo `IND nn`. Viz [bod 5 v kap. 2](#2-nejdůležitější-omezení). |
| `SBR` z klávesnice, klávesy A–E' | ✅ | Uloží návrat, spustí program a po `INV SBR` zastaví. |
| `SBR` v programu | ⚠️ | Po návratu se program zastaví, viz [bod 3](#2-nejdůležitější-omezení). |
| `SBR` s jiným návěštím než A–E' z klávesnice | ❌ | *(ověřeno)* Např. `SBR lnx` vede k chybě. |
| `INV SBR` (návrat) | ✅ | |
| `RTN` jako kód 92 | ❌ | Provede se jako číslice 0. |
| Zásobník podprogramů 8 úrovní | ✅ | 9. úroveň vede k chybě. |
| Podprogram ukončený `R/S` | ⚠️ | *(ověřeno)* Volání `A`–`E'` z klávesnice uloží návratovou adresu. Skončí-li podprogram `R/S` místo `INV SBR`, adresa v zásobníku zůstane. Po 8 takových voláních vede 9. volání k chybě a `RST` zásobník nevyprázdní. |
| `2nd x=t`, `2nd x≥t` a jejich `INV` | ✅ | Při splnění se skočí na následující adresu nebo návěští, jinak se adresa přeskočí. V programu ✅. Z klávesnice podmínka při splnění čeká na adresu. Při nesplnění se posune PC, pokud je PC > 0. |
| `2nd Dsz n` a `INV 2nd Dsz n` | ⚠️ | Jen registry 0–9 bez IND. *(ověřeno)* Emulátor vždy odečte 1; odchylka je patrná u záporné hodnoty (`−2` → `−3`), zatímco originál zmenšuje absolutní hodnotu směrem k nule. |
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
| `Pgm nn` v programu | ⚠️ | *(ověřeno)* Skočí na krok 000 vybrané banky místo výběru pro následující `SBR`. |
| `Pgm IND nn` | ✅ | Neexistující banka vede k chybě. |
| Op 9 (stažení knihovního programu) | ❌ | |
| Magnetické karty (čtení, zápis, `2nd Write`) | ❌ | Kartu nahrazují soubory `.ti5p` a `.ti5l`. Karta se zobrazuje jen jako popisný proužek (štítky A–E'). |
| Master Library | ⚠️ | Vestavěná `Assets/ml.ti5l` obsahuje **jen banku 01** (127 kroků, bez popisu karty). Program používá sloučené kódy (`62`, `92`), takže v emulátoru neběží správně. Ostatní programy Master Library chybí. |
| Nápověda v knihovnách (`help`, `helpNN`) | ❌ | Načte se, ale aplikace ji nikde nezobrazuje. |
| `.ti5p` – uložení | ⚠️ | Uloží jen program banky 0 (všech 960 kroků) a kartu/nápovědu **aktuální** banky. Neukládá registry, příznaky, režimy ani rozdělení. Ze štítků karty uloží jen `titleleft`, `titleright` a `a`–`eprime` (ztratí se `subtitle*`, `primecenter`, `base*`). Texty štítků se neescapují pro XML. |
| `.ti5p` – načtení | ✅ | Nahradí program banky 0 a nastaví PC = 0 a banku 0. Registry nemění. |

### 3.12 Tiskárna PC-100A

| Funkce | Stav | Poznámka |
|---|---|---|
| `2nd Prt` | ✅ | Vytiskne obsah displeje zarovnaný doprava na 20 znaků. |
| `2nd Adv` | ✅ | Prázdný řádek. |
| `2nd List` | ⚠️ | *(ověřeno)* Vytiskne **všechny** kroky od 000 do konce rozdělení ve tvaru `PPP  KK MNE`, včetně prázdných. Páska pojme 500 řádků, takže začátek výpisu (prvních 460 kroků z 960) se ztratí. V originále výpis začíná od aktuálního kroku. |
| `INV 2nd List` a `INV 2nd Prt` | ❌ | INV se ignoruje. |
| Trasovací tisk | ⚠️ | Přes příznak 7 / Op 0 se tisknou jen výsledky (hodnota displeje), ne kódy kláves s mnemonikami. |
| Op 1–4, Op 5 (alfanumerický tisk) | ⚠️ | Funguje. Znaky s kódy 00–47 odpovídají sadě PC-100A. *(ověřeno)* Od kódu 50 výš se mapování liší: `50`=`*`, `51`=`/`, `52`=`(`, `53`=`)`, `54`=`,`, `55`=`=`, `56`=`'`, `57`=`"`, `60`=`#`, `62`=`?`, `66`=`π`, ostatní (`61`, `63`–`65`, `67`, `70`–`77`) jako mezera. Kódy s číslicí 8 nebo 9 dávají nedefinované znaky. |
| Grafická znaková sada 5×7 | ⚠️ | Tabulka `PrinterEngine.Chars` existuje, ale nepoužívá se. Páska je obyčejný text. |
| Délka pásky | ⚠️ | Max. 500 řádků, nejstarší se zahazují. |
| Op 6, Op 7 (tisk s popisem, vykreslování grafu) | ⚠️ | Jen posunou papír o jeden řádek. |

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
| 13 | Korelační koeficient | Korelační koeficient. | ✅ |
| 14 | Odhad y' z x | Ignoruje se. | ❌ |
| 15 | Odhad x' z y | Ignoruje se. | ❌ |
| 16 | Zobrazit rozdělení paměti | Zobrazí chybnou hodnotu (`4852`), viz [3.10](#310-rozdělení-paměti). | ⚠️ |
| 17 | Nové rozdělení paměti | Jiný výpočet počtu kroků, viz [3.10](#310-rozdělení-paměti). | ⚠️ |
| 18 | Při bezchybném stavu nastavit příznak 7 | Ignoruje se. | ❌ |
| 19 | Při chybě nastavit příznak 7 | Ignoruje se. | ❌ |
| 20–29 | Zvýšit R0–R9 o 1 | Zvýší registr a **novou hodnotu zapíše do X a na displej**. | ⚠️ |
| 30–39 | Snížit R0–R9 o 1 | Sníží registr a novou hodnotu zapíše do X a na displej. | ⚠️ |
| 40+ | – | Ignoruje se. | – |

Op 11–13 počítají s rolemi x a y popsanými v [3.6](#36-statistika), tedy x = hodnota na displeji při `Σ+`.

---

## 5. Sloučené kódy instrukcí

Tabulka `KeyCodes.PrintMnemonics` obsahuje mnemoniky pro všechny kódy 00–99 a slouží debuggeru a výpisu `List`. Engine ale sloučené kódy při běhu nedekóduje:

| Kód | Mnemonika | Význam v originále | Chování v emulátoru |
|---|---|---|---|
| 62 | `PG*` | Pgm IND | Číslice 7 |
| 63 | `EX*` | Exc IND | Číslice 8 |
| 64 | `PD*` | Prd IND | Číslice 9 |
| 72 | `ST*` | STO IND | Číslice 4 |
| 73 | `RC*` | RCL IND | Číslice 5 |
| 74 | `SM*` | SUM IND | Číslice 6 |
| 82 | `HIR` | Operace se skrytými registry | Číslice 1 |
| 83 | `GO*` | GTO IND | Číslice 2 |
| 84 | `OP*` | Op IND | Číslice 3 |
| 92 | `RTN` | Návrat z podprogramu | Číslice 0 |
| 96 | `WRT` | Zápis karty | Bez účinku |
| 20, 26, 27 | `CLR`, `2ND`, `INV` | kódy bez samostatné instrukce | `20` bez účinku, `26`/`27` bez účinku (kód `21` přepne 2nd, `22` INV) |

Kódy 00–09 se provedou jako číslice, kódy 10–19 jako volání návěští A–E'. Ostatní kódy odpovídají své klávese (primární kód nebo kód 2nd funkce).

---

## 6. Obecná omezení aplikace

- **Neúplná Master Library.** K dispozici je jen banka 01 z `Assets/ml.ti5l` a kvůli [sloučeným kódům](#5-sloučené-kódy-instrukcí) nefunguje správně. Vlastní knihovny lze načíst přes `File > Load Library Module (.ti5l)...`. Načtená knihovna přepíše banky se stejným číslem.
- **`.ti5p` neukládá registry**, příznaky, režim úhlů, formát zobrazení ani rozdělení paměti. Obsah registrů se při načtení programu nemění.
- **Omezená fyzická klávesnice.** Viz [3.1](#31-klávesnice-2nd-a-inv). Ostatní funkce se ovládají myší na virtuální klávesnici.
- **Debugger** (`View > Program Debugger / Steps`) zobrazuje kroky aktuální banky, přičemž bajty operandů ukazuje také jako mnemoniky (např. adresa `42` se zobrazí jako `STO`). Registry ukazuje jen R00–R51 podle chybného přepočtu z `RegOffset`. `Reset` nastaví pouze PC = 0.
- **Stavový řádek** ukazuje `Regs: 100 − RegOffset/10`, ve výchozím stavu tedy `52`.
- **Rychlost běhu** neodpovídá originálu. Program běží tak rychle, jak dovolí vlákno na pozadí (s příznakem 9 50 ms na krok).
- **Grafické rozhraní nemá automatizované testy.** Testovací projekt `TI5X.Avalonia.Tests` testuje jen engine: `TestBasicMath`, `TestAngleConversions`, sadu `TestLunarLander*` a `TestLunarLanderTi5pRoundtrip` (uložení a načtení `.ti5p`). Odchylky popsané v tomto dokumentu testy nepokrývají.
- **Vlákna.** Program běží v `Task.Run` a sdílí stav enginu s UI vláknem bez zamykání. Při rychlém zásahu (např. úprava v debuggeru během běhu) se může zobrazit nekonzistentní stav.

---

## 7. Doporučené postupy

Dokud se omezení neodstraní, fungují spolehlivě tyto postupy:

- Programy připravujte jako `.ti5p` s kódy TI-59: číslice `00`–`09`, funkce 2nd jedním kódem (např. `58` = Fix), adresy registrů a příznaků jako jeden bajt. Vzorem je `Assets/lunar_lander.ti5p` a [LUNAR_LANDER_SPEC.md](LUNAR_LANDER_SPEC.md).
- Místo `RTN` (`92`) používejte `INV SBR` (`22 71`), místo sloučených IND kódů zápis `kód 40 nn` (např. `42 40 05`).
- Podprogramy volejte z klávesnice (A–E'). Uvnitř programu místo `SBR` používejte `GTO`.
- Skoky na absolutní adresy používejte jen v rozsahu 000–099 (jednobajtová adresa), jinak raději návěští A–E'.
- Číslo před `R/S` ukončete (např. `STO nn` nebo `=`), jinak se nezobrazí.
- Nepoužívejte `Op 17` s malým počtem registrů (hrozí pád aplikace) a nespoléhejte na `Op 16`.
