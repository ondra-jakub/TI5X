# TI-59 Calculator Manual (TI5X Emulator)

This manual describes the Texas Instruments TI-59 programmable calculator as emulated by the TI5X emulator (Avalonia). Every chapter explains how a feature behaves on the **original calculator**, and every point where the emulator behaves differently is stated explicitly.

Conventions used in this manual:

| Mark | Meaning |
|---|---|
| ⚠️ **Emulator:** | The emulator differs from the original here, or supports the feature only partially. |
| ❌ **Emulator:** | The emulator does not have this feature. |
| ✔ *Emulator:* | The emulator matches the original here. |
| *(verified)* | The example was verified by running it in the emulator engine (`Core/CalculatorEngine.cs`) and against the TMC0500 microcode oracle. |

The complete list of differences lives in one place in the (Czech) document [Odchylky emulace a známá omezení](ODCHYLKY_A_OMEZENI.md). The application itself (menus, files, printer, debugger) is described in the Czech [user guide](EMULATOR_UZIVATEL.md); architecture, file formats and tests are covered by the Czech [developer documentation](EMULATOR_VYVOJ.md).

> **Important note about programming in the emulator.** A program keyed in from the keyboard in `LRN` mode is stored faithfully in TI-59 instruction codes (see [9.2](#92-lrn-mode)). The example programs in [chapter 16](#16-sample-programs) are also provided as ready-made `.ti5p` files whose instruction codes follow [chapter 15](#15-instruction-code-table); all of them were verified in the emulator.

## Contents

0. [Quick start — five minutes with the calculator](#0-quick-start-—-five-minutes-with-the-calculator)
1. [Introduction and technical parameters](#1-introduction-and-technical-parameters)
2. [Keyboard](#2-keyboard)
   - [2.1 Key layout](#21-key-layout)
   - [2.2 The 2nd key](#22-the-2nd-key)
   - [2.3 The INV key](#23-the-inv-key)
   - [2.4 Key codes and instruction codes](#24-key-codes-and-instruction-codes)
   - [2.5 Reference card: common key sequences](#25-reference-card-common-key-sequences)
3. [Number entry and display formats](#3-number-entry-and-display-formats)
   - [3.1 Entering a number](#31-entering-a-number)
   - [3.2 The EE exponent](#32-the-ee-exponent)
   - [3.3 Engineering notation (Eng)](#33-engineering-notation-eng)
   - [3.4 Fixed decimal places (Fix)](#34-fixed-decimal-places-fix)
   - [3.5 CE and CLR](#35-ce-and-clr)
4. [Arithmetic, AOS and parentheses](#4-arithmetic-aos-and-parentheses)
5. [Mathematical functions](#5-mathematical-functions)
   - [5.1 Powers, roots and logarithms](#51-powers-roots-and-logarithms)
   - [5.2 Angle modes and trigonometry](#52-angle-modes-and-trigonometry)
   - [5.3 Polar and rectangular coordinates](#53-polar-and-rectangular-coordinates)
   - [5.4 Degrees, minutes and seconds](#54-degrees-minutes-and-seconds)
   - [5.5 Integer part, absolute value and π](#55-integer-part-absolute-value-and-π)
6. [Memories](#6-memories)
   - [6.1 Data registers](#61-data-registers)
   - [6.2 Storing and memory arithmetic](#62-storing-and-memory-arithmetic)
   - [6.3 Indirect addressing (IND)](#63-indirect-addressing-ind)
   - [6.4 Clearing memories](#64-clearing-memories)
7. [The T register and comparisons](#7-the-t-register-and-comparisons)
8. [Statistics](#8-statistics)
9. [Programming](#9-programming)
   - [9.1 Program memory](#91-program-memory)
   - [9.2 LRN mode](#92-lrn-mode)
   - [9.3 Editing a program](#93-editing-a-program)
   - [9.4 Running, stopping, single-stepping](#94-running-stopping-single-stepping)
   - [9.5 Labels](#95-labels)
   - [9.6 Unconditional jumps](#96-unconditional-jumps)
   - [9.7 Subroutines](#97-subroutines)
   - [9.8 Conditional jumps](#98-conditional-jumps)
   - [9.9 Flags](#99-flags)
   - [9.10 Loops with Dsz](#910-loops-with-dsz)
   - [9.11 Incrementing and decrementing a register](#911-incrementing-and-decrementing-a-register)
   - [9.12 Pause and Nop](#912-pause-and-nop)
10. [Memory partitioning](#10-memory-partitioning)
11. [Library programs and Pgm](#11-library-programs-and-pgm)
12. [Printing](#12-printing)
13. [Op codes reference](#13-op-codes-reference)
14. [Error conditions](#14-error-conditions)
    - [14.1 Troubleshooting](#141-troubleshooting)
15. [Instruction code table](#15-instruction-code-table)
   - [15.1 Complete table of codes 00–99](#151-complete-table-of-codes-00-99)
   - [15.2 Program encoding rules](#152-program-encoding-rules)
   - [15.3 Creating a ti5p file](#153-creating-a-ti5p-file)
16. [Sample programs](#16-sample-programs)
   - [16.1 Area of a circle](#161-area-of-a-circle)
   - [16.2 Factorial with Dsz](#162-factorial-with-dsz)
   - [16.3 Temperature conversion with a flag](#163-temperature-conversion-with-a-flag)
   - [16.4 Series sum with a loop](#164-series-sum-with-a-loop)
   - [16.5 Stopping a program for input](#165-stopping-a-program-for-input)
   - [16.6 Lunar Lander](#166-lunar-lander)
17. [Glossary](#17-glossary)

---

## 0. Quick start — five minutes with the calculator

This chapter is for the very first contact: what to do right after launching so that you compute something, store it, and run a library program. All examples in this chapter are **verified** — the display shows exactly what is written.

### Computing an expression

The calculator evaluates in the **AOS** system (see [chapter 4](#4-arithmetic-aos-and-parentheses)): key a number, an operation, a number, an operation … and press `=`. Precedence: `yˣ`, then `×` and `÷`, then `+` and `−`.

| Keys | Display |
|---|---|
| `2` `+` `3` `=` | `5.` |
| `7` `+` `5` `×` `4` `=` | `27.` *(verified)* — first `5 × 4`, then `+ 7` |
| `(` `7` `+` `5` `)` `×` `4` `=` | `48.` *(verified)* |
| `8` `÷` `(` `3` `+` `1` `)` `=` | `2.` *(verified)* |
| `2` `√x` | `1.414213562` *(verified)* |
| `2nd π` | `3.141592654` *(verified)* |

### Storing and reusing a number

| Keys | Display | Explanation |
|---|---|---|
| `25` `STO 0 1` | `25.` | stores 25 into register 01 *(verified)* |
| `10` `SUM 0 1` | `10.` | adds 10 to register 01 (now 35) |
| `RCL 0 1` | `35.` | recalls the register content *(verified)* |

The register address is **two digits** (`STO 0 1` = register 01). More about memories in [chapter 6](#6-memories).

### Running a library program

Take Master Library program **ML-25 UNIT CONVERSIONS (2)** (temperature conversion) — convert 41 °F to °C. Keys: `2nd Pgm 25` (select the library program, bank 25), then `41` and `A` (°F → °C):

| Keys | Display |
|---|---|
| `2nd Pgm 2 5` | |
| `41` `A` | `5.` |

*(verified; identical to the microcode oracle)* — 41 °F equals 5 °C. The library is described in detail in [chapter 11](#11-library-programs-and-pgm); the program card appears in the strip under the display.

### Saving a program to a file

The emulator replaces magnetic cards with files: `.ti5p` (a program) and `.ti5l` (a library). Desktop: menu `File > Save Program (.ti5p)...` — Android: the hamburger **☰** button, `Save Program (.ti5p)`. Open: `File > Open Program (.ti5p)...`. Full procedures: the [user guide](EMULATOR_UZIVATEL.md) and [chapter 15.3](#153-creating-a-ti5p-file).

---

## 1. Introduction and technical parameters

The TI-59 (1977) is a pocket programmable calculator with a red LED display, a magnetic card reader and the ability to attach the PC-100A printer. It evaluates in the algebraic **AOS** system (Algebraic Operating System) that respects operator precedence, and stores programs as sequences of key codes.

| Parameter | Original TI-59 | TI5X emulator |
|---|---|---|
| Data registers | max. 100 (00–99), depending on partition | always all 100 available ⚠️ |
| Program steps | max. 960, depending on partition | 960 in bank 0 |
| Default partition | 479.59 (480 steps, registers 00–59) | internally the same, but 960 steps and 100 registers available ⚠️ |
| Parentheses levels | 9 (shared with pending operations) | 10-stack entries for parens and pending ops |
| Subroutine levels | 6 | 8 |
| Flags | 10 (0–9) | 10 (0–9), different meaning of 7–9 ⚠️ |
| Display | 10-digit mantissa, exponent ±99 | 10 digits, computed in `double` (±99 not enforced) ⚠️ |
| Internal precision | 13 digits | `double`, ~15–16 significant digits |
| Angle modes | Deg, Rad, Grad | Deg, Rad, Grad (default Deg) |
| Formats | standard, Fix 0–8, scientific (EE), engineering (Eng) | the same |
| Library module | Master Library (25 programs) | all 25 banks from `src/TI5X.Avalonia/Assets/ml.ti5l`; ML-01 fails its final checksum ⚠️ |
| Magnetic cards | yes | replaced by `.ti5p`/`.ti5l` files ❌ |
| Printer | PC-100A, 20 characters per line | virtual PC-100A, 20 characters per line |

Details for the marked rows: [memory partitioning](ODCHYLKY_A_OMEZENI.md#310-rozdělení-paměti), [flags](ODCHYLKY_A_OMEZENI.md#39-příznaky), [formats](ODCHYLKY_A_OMEZENI.md#32-zadávání-čísel-a-formáty-zobrazení), [libraries](ODCHYLKY_A_OMEZENI.md#311-knihovní-programy-karty-a-soubory).

---

## 2. Keyboard

### 2.1 Key layout

The keyboard has 45 keys in a 9-row × 5-column grid. Every key has a **primary function** (on the key top) and most also a **second function** (above the key), invoked with `2nd`.

The table lists the primary function per key with the second function after a slash. The number in parentheses is the **key code** (row × 10 + column) and the second function's code (row × 10 + column + 5, the last column has the units digit 0).

| Row | Column 1 | Column 2 | Column 3 | Column 4 | Column 5 |
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

The layout and labels match the emulator's virtual keyboard (`MainWindow.axaml`); the second-function mapping is the `MapSecond` table in `CalculatorEngine.cs`.

❌ **Emulator:** `2nd R/S` (Write, code 96) has no function in the emulator and the button carries no second label. Code 20 (the "2nd CLR" position) has no separate function on the original either.

### 2.2 The 2nd key

`2nd` selects the second function of the **following** key. After it executes, `2nd` resets itself. Repeating `2nd` cancels the selection. The active `2nd` is shown by the yellow `2nd` pill above the display.

This manual writes second functions with the `2nd` prefix, e.g. `2nd log`, `2nd Fix 2`, `2nd St flg 3`. For the keys `A'`–`E'` the prefix is often omitted.

### 2.3 The INV key

`INV` inverts the meaning of the following function, e.g. `INV lnx` = eˣ, `INV 2nd Fix` = cancel fixed format, `INV SUM` = subtract from a register. Repeating `INV` cancels it. Functions that have no inverse simply cancel the `INV`. The active `INV` is shown by the `INV` pill above the display.

Functions that support INV:

| Function | With INC/`INV` |
|---|---|
| `lnx` | eˣ |
| `2nd log` | 10ˣ |
| `yˣ` | ˣ√y (root) |
| `2nd sin`, `2nd cos`, `2nd tan` | arcsin, arccos, arctan |
| `2nd P→R` | rectangular → polar conversion |
| `2nd D.MS` | the reverse angle conversion (see [5.4](#54-degrees-minutes-and-seconds)) |
| `2nd Int` | fractional part |
| `SUM`, `2nd Prd` | subtract from register, divide register |
| `EE`, `2nd Eng`, `2nd Fix` | cancel the given format |
| `2nd Σ+` | remove a data point |
| `2nd x̄` | standard deviation |
| `2nd x=t`, `2nd x≥t` | x ≠ t, x < t |
| `2nd St flg`, `2nd If flg`, `2nd Dsz` | reset a flag, test for an unset flag, jump when zero |
| `SBR` | return from a subroutine (`RTN`) |
| `2nd List` | print the data registers (original) |

⚠️ **Emulator:**
- `INV EE` has no effect and `INV` stays active. To get back from a display with an exponent use `INV 2nd Eng`.
- `INV 2nd List` and `INV 2nd Prt` behave as without `INV`.
- In addition to the original: `INV x²` computes √x and `INV √x` computes x².

### 2.4 Key codes and instruction codes

There are two kinds of codes:

1. **Key code** — the physical key position (row and column), see the table in [2.1](#21-key-layout). Digits carry positional codes: `7` = 62, `8` = 63, `9` = 64, `4` = 72, `5` = 73, `6` = 74, `1` = 82, `2` = 83, `3` = 84, `0` = 92. The emulator's virtual keyboard sends these.
2. **Instruction code** — the value stored in a program step (00–99), shown by `LRN` mode and printed by `List`. The original converts when writing a program:
   - digits to codes **00–09** (`7` → `07`),
   - a second function to a **single** code (`2nd log` → `28`, `2nd Fix` → `58`),
   - `IND` combinations to **merged codes** (`STO 2nd Ind` → `72`, `INV SBR` → `92` = `RTN`), see [15.1](#151-complete-table-of-codes-00-99).

Codes 62–64, 72–74, 82–84 and 92 therefore mean different things as key codes (digits) and as instruction codes (merged instructions).

✔ *Emulator:* in `LRN` mode the instruction codes of the TI-59 standard are stored (including merged codes) and the merged codes execute correctly while running. Details in [9.2](#92-lrn-mode) and [Merged instruction codes](ODCHYLKY_A_OMEZENI.md#5-sloučené-kódy-instrukcí).

### 2.5 Reference card: common key sequences

A pocket reference for the most common sequences (every item is described in detail in the referenced chapter):

| I want to … | Keys | Detail |
|---|---|---|
| evaluate an expression | numbers and operations, finally `=` | [4](#4-arithmetic-aos-and-parentheses) |
| power / root | `yˣ` and `INV yˣ` | [5.1](#51-powers-roots-and-logarithms) |
| trigonometric function | `2nd sin` / `cos` / `tan`; inverse by adding `INV` | [5.2](#52-angle-modes-and-trigonometry) |
| switch Deg/Rad/Grad | `2nd Deg` / `2nd Rad` / `2nd Grad` | [5.2](#52-angle-modes-and-trigonometry) |
| DD.MMSS ↔ decimal degrees | `2nd D.MS` / `INV 2nd D.MS` | [5.4](#54-degrees-minutes-and-seconds) |
| polar ↔ rectangular coordinates | r `x⇌t` θ `2nd P→R` | [5.3](#53-polar-and-rectangular-coordinates) |
| store / recall a number | `STO nn` / `RCL nn` (nn = two digits!) | [6.2](#62-storing-and-memory-arithmetic) |
| add/subtract/multiply in memory | `SUM nn`, `INV SUM nn`, `2nd Prd nn`, `INV 2nd Prd nn`, `2nd Exc nn` | [6.2](#62-storing-and-memory-arithmetic) |
| address through a variable | `STO 2nd Ind nn` / merged codes | [6.3](#63-indirect-addressing-ind) |
| compare the display with the T register | `x⇌t`, `2nd x=t nn`, `2nd x≥t nn` (and `INV`) | [7](#7-the-t-register-and-comparisons) |
| statistics data | y `2nd Σ+` (y in the display, x in T), mean `2nd x̄` | [8](#8-statistics) |
| switch to writing a program | `LRN` (and `LRN` again to leave) | [9.2](#92-lrn-mode) |
| run a program | `RST` `R/S` | [9.4](#94-running-stopping-single-stepping) |
| jump / call | `GTO nnn` / `SBR nnn` or labels `A`–`E'` | [9.6](#96-unconditional-jumps), [9.7](#97-subroutines) |
| loop | `2nd Dsz nn nnn` (through register nn) | [9.10](#910-loops-with-dsz) |
| partition memory | `2nd Op 17 mm.f` | [10](#10-memory-partitioning) |
| call a library program | `2nd Pgm nn` then `A`–`E'` | [11](#11-library-programs-and-pgm) |
| print | `2nd Prt`, `2nd List`, `2nd Op nn` | [12](#12-printing) |
| enter a number with an exponent | `mantissa EE exponent` | [3.2](#32-the-ee-exponent) |
| display format | `2nd Fix n`, `EE`, `2nd Eng` (cancel with `INV`) | [3](#3-number-entry-and-display-formats) |

---

## 3. Number entry and display formats

### 3.1 Entering a number

Numbers are entered with the digit keys `0`–`9`, the decimal point `.` and the sign key `+/-` (toggles the sign of the number being entered). The display holds at most 10 digits; numbers that do not fit appear with an exponent automatically.

*(verified)* example — `1.25 + 2.5 =` → `3.75`.

### 3.2 The EE exponent

`EE` starts the entry of the exponent of 10 (two digits, with sign via `+/-`). The exponent is entered with two digits after pressing `EE`; the mantissa keeps at most 10 digits.

| Keys | Display |
|---|---|
| `1.5234 EE 35` | `1.5234 35` *(verified)* |
| `12 EE 3 =` | `12000.` *(verified)* |

`INV EE` (the original would cancel the exponent entry) is described among the deviations above ([2.3](#23-the-inv-key)).

### 3.3 Engineering notation (Eng)

`2nd Eng` displays numbers with an exponent that is a multiple of 3 (mantissa 1–999). `INV 2nd Eng` cancels the format.

| Keys | Display |
|---|---|
| `12345 = 2nd Eng` | `12.345000 03` *(verified)* |

⚠️ **Emulator:** pressing `2nd Eng` while a number is being entered discards the number being written. Terminate the number first (e.g. with `=`).

### 3.4 Fixed decimal places (Fix)

`2nd Fix n` (n = 0–8) rounds the display to n decimal places. The internal value does not change. `INV 2nd Fix` or `2nd Fix 9` returns the standard format.

| Keys | Display |
|---|---|
| `2 ÷ 3 =` | `0.666666667` *(verified)* |
| `2nd Fix 2` | `0.67` *(verified)* |
| `2nd Fix 4` | `0.6667` *(verified)* |
| `INV 2nd Fix` (or `2nd Fix 9`) | `0.666666667` *(verified)* |

On the original, `Fix` can combine with the scientific and engineering formats (mantissa decimal places) and `2nd Fix 2nd Ind nn` can be used.

⚠️ **Emulator:** `Fix` is ignored in the scientific and engineering formats (the mantissa always has 8 significant digits) and `Fix Ind` is not supported.

In the standard format, at most 10 digits are shown and trailing zeros are cut off. Numbers out of the display range (≥ 10¹⁰ or < 10⁻¹⁰ in absolute value) are automatically shown with an exponent.

### 3.5 CE and CLR

| Key | Effect |
|---|---|
| `CE` | Clears the number being entered (not the whole computation) and cancels an error state. Pending operations are not affected. When nothing is being entered, `CE` has no effect — the display value survives (verified against the microcode oracle: `2 + 3 = CE + 1 =` gives `6.`); in an error state it only stops the flashing. |
| `CLR` | Clears the display, all pending operations and parentheses, and cancels the error state. Does not change registers, program or formats. |

---

## 4. Arithmetic, AOS and parentheses

The TI-59 evaluates expressions the way they are written on paper. The AOS system respects operator precedence:

1. single-variable functions (`x²`, `√x`, `1/x`, `lnx`, `sin` …) execute immediately when pressed,
2. `yˣ` and `INV yˣ`,
3. `×` and `÷`,
4. `+` and `−`.

Operations with the same precedence evaluate left to right. `=` completes all pending operations and closes all open parentheses.

| Keys | Result |
|---|---|
| `3 + 4 × 5 =` | `23.` *(verified)* |
| `( 3 + 4 ) × 5 =` | `35.` *(verified)* |
| `2 yˣ 10 =` | `1024.` *(verified)* |
| `8 INV yˣ 3 =` | `2.` (cube root of 8) *(verified)* |

An important detail: `yˣ` completes its operation as soon as another operation key is pressed — `8 yˣ 2 ÷ 3 =` is `8² ÷ 3` = `21.33333333` *(verified)*, not 8^(2/3). To raise to a compound exponent, close it first: `8 yˣ ( 2 ÷ 3 ) =` → `4.` *(verified)*.

Parentheses `(` and `)` change the evaluation order. The original allows 9 open parentheses, and pending operations share the same space.

⚠️ **Emulator:** the stack of pending operations and parentheses has 10 entries. The 11th level and a surplus `)` lead to an error.

---

## 5. Mathematical functions

All functions in this chapter act on the displayed value (the X register) and display the result immediately.

### 5.1 Powers, roots and logarithms

| Keys | Function |
|---|---|
| `x²` | square; `INV x²` in the emulator also computes √x ⚠️ |
| `√x` | square root; `INV √x` computes x² |
| `1/x` | reciprocal |
| `yˣ` | power; `INV yˣ` = ˣ√y |
| `lnx` | natural logarithm; `INV lnx` = eˣ |
| `2nd log` | common logarithm; `INV 2nd log` = 10ˣ |

*(verified)* examples: `2 x²` → `4.`; `2 √x` → `1.414213562`; `5 1/x` → `0.2`; `25 yˣ . 5 =` → `5.`; `log 1000` → `3.`; `ln 10` → `2.302585093`; `2 INV lnx` → `7.389056099`; `2 INV 2nd log` → `100.`; `50 2nd log` → `1.698970004`.

### 5.2 Angle modes and trigonometry

`2nd Deg`, `2nd Rad` and `2nd Grad` set the angle mode (the active mode is shown in the pill above the display; the default is Deg). `2nd sin`, `2nd cos` and `2nd tan` compute the trigonometric functions; `INV` inverts each of them.

*(verified)* examples: `sin 30` → `0.5`; `cos 60` → `0.5`; `tan 45` → `1.`; `INV sin . 5` → `30.`; `INV tan 1` → `45.`; in Rad: `sin ( π / 2 )` = `1.`; in Grad: `sin 50` (grad) → `0.707106781`.

### 5.3 Polar and rectangular coordinates

**Original TI-59:** enter the radius r, press `x⇌t` (moves r into the T register), enter the angle θ and press `2nd P→R`. The display shows the y coordinate; after `x⇌t` the x coordinate. The reverse conversion: x `x⇌t` y `INV 2nd P→R` → θ in the display, r after `x⇌t`.

| Procedure | Result |
|---|---|
| `45 x⇌t 31.6 2nd P→R` (Deg) | display `23.57936577` (y), after `x⇌t` `38.32771204` (x) *(verified; identical to the original manual's example)* |
| `38.32771204 x⇌t 23.57936577 INV 2nd P→R` | display `31.599…` (θ), after `x⇌t` r *(verified)* |

✔ *Emulator:* behaves like the original here (the conventions `P→R` = r in the T register, θ in the display, results y in the display and x in T; `INV …` mirrored), verified against the microcode oracle.

### 5.4 Degrees, minutes and seconds

**Original TI-59:** `2nd D.MS` converts an angle written as DD.MMSS (degrees.minutesseconds) into decimal degrees and `INV 2nd D.MS` converts back.

⚠️ **Emulátor:** the direction is swapped. *(verified)*

| Keys in the emulator | Result |
|---|---|
| `1.3 2nd D.MS` (1.3° → DD.MMSS) | `1.18` (1° 18′) |
| `1.3 INV 2nd D.MS` (1° 30′ → degrees) | `1.5` |

The hour conversion the original manual mentions works the same way with decimal hours; in the emulator mind the swapped direction.

### 5.5 Integer part, absolute value and π

| Keys | Function | Example *(verified)* |
|---|---|---|
| `2nd Int` | integer part | `7.25 +/- 2nd Int` → `-7.` |
| `INV 2nd Int` | fractional part | `7.25 +/- INV 2nd Int` → `-0.25` |
| `2nd \|x\|` | absolute value | `-0.25` → `0.25` |
| `2nd π` | the constant π | `3.141592654` |

---

## 6. Memories

### 6.1 Data registers

The calculator has data registers numbered 00–99. The number of registers available at run time depends on [memory partitioning](#10-memory-partitioning). Register 00 is special only in that some library conventions use it like the T register.

⚠️ **Emulator:** all 100 registers are always available regardless of the partition.

### 6.2 Storing and memory arithmetic

The register address is entered with **two digits** (00–99).

| Keys | Function |
|---|---|
| `STO nn` | stores the display into register nn |
| `RCL nn` | recalls register nn to the display |
| `SUM nn` | adds the display to register nn; `INV SUM nn` subtracts |
| `2nd Prd nn` | multiplies register nn by the display; `INV 2nd Prd nn` divides |
| `2nd Exc nn` | exchanges the display with register nn *(verified)* |

*(verified)* examples: `25 STO 01`, `10 SUM 01`, `RCL 01` → `35.`; `10 STO 02`, `4 2nd Prd 02`, `RCL 02` → `40.`; `3 STO 05`, `8 2nd Exc 05` → display `3.` (memory 8), after `x⇌t` again `3.`; `12 x⇌t` → display shows the T register (`0.`), pressing `x⇌t` once more returns `12.`.

### 6.3 Indirect addressing (IND)

`STO 2nd Ind nn` (and the same for `RCL`, `SUM`, `2nd Prd`, `2nd Exc`, `GTO`) takes the **actual register address from register nn**. In the LRN encoding these combinations store as merged codes ([15.2](#152-program-encoding-rules)).

⚠️ **Emulator:** indirect addressing is supported for STO/RCL/SUM/Prd/Exc/GTO/SBR/Nop.

### 6.4 Clearing memories

`2nd CMs` zeroes all data registers. It does not change the program, flags or the display.

⚠️ **Emulator:** always zeroes all 100 registers.

---

## 7. The T register and comparisons

The **T** (test) register is a helper register for comparisons and for functions that take two values (`P→R`, statistics).

| Keys | Effect |
|---|---|
| `x⇌t` | exchanges x and t |
| `2nd x=t` | in a program: a jump when x = t |
| `INV 2nd x=t` | a jump when x ≠ t |

*(verified)* example: `12 x⇌t` shows the old T (`0.`); pressing `x⇌t` again returns `12.`.

---

## 8. Statistics

The statistical functions accumulate sums into registers R01–R06. Clear them with `2nd CMs` before a new data set.

| Register | Content |
|---|---|
| R01 | Σy |
| R02 | Σy² |
| R03 | n (the number of points) |
| R04 | Σx |
| R05 | Σx² |
| R06 | Σxy |

**Original TI-59:** a data point is entered as x `x⇌t` y `2nd Σ+` (y in the display, x in the T register). `INV 2nd Σ+` removes a point. `2nd x̄` shows the mean of y (the mean of x after `x⇌t` in the T register), `INV 2nd x̄` the standard deviation. `Op 11` through `Op 15` compute variance, linear regression, correlation and estimates.

The emulator's conventions match the original: the display is **y**, the T register is **x**. The only difference: the correlation coefficient in the emulator is also available on `2nd Op 13` — the key where some summaries list it; the original uses `Op 14`.

| Keys | Result |
|---|---|
| x `x⇌t` y `2nd Σ+` | adds a point (the display is **y**, T is **x** — as on the original), shows n |
| `INV 2nd Σ+` | removes a point |
| `2nd x̄` | mean of y (display); the mean of x in T |
| `INV 2nd x̄` | sample standard deviation of the display values (y), and of the T values in T |
| `2nd Op 11` | the slope of the regression line y = a·x + b; intercept b in T |
| x `2nd Op 12` | the estimate y′ for the given x |
| `2nd Op 13` (and `2nd Op 14`) | correlation coefficient |

For **one-variable** data just enter the values and press `2nd Σ+`; the results are the same as on the original. Example *(verified)*: `2nd CMs 2 2nd Σ+ 4 2nd Σ+ 6 2nd Σ+` → `3.`, `2nd x̄` → `4.`, `INV 2nd x̄` → `2.`.

A two-variable example — points (1; 2), (2; 4), (3; 6.5) *(verified, with the same results as on the original)*:

| Keys | Display |
|---|---|
| `2nd CMs 1 x⇌t 2 2nd Σ+` | `2.` (the entered y) |
| `2 x⇌t 4 2nd Σ+` | `4.` |
| `3 x⇌t 6.5 2nd Σ+` | `6.5` |
| `2nd Op 11` | `2.25` (slope) |
| `x⇌t` | `-0.333333333` (intercept) |
| `4 2nd Op 12` | `8.666666667` (estimate of y for x = 4) |
| `2nd Op 13` | `0.997948716` (correlation) |

See also [Statistics in the deviations overview](ODCHYLKY_A_OMEZENI.md#36-statistika).

---

## 9. Programming

> The example programs of this chapter are listed with their codes and are verified in the emulator; the ready-made `.ti5p` files are in [chapter 15.3](#153-creating-a-ti5p-file).

### 9.1 Program memory

A program is a sequence of **steps** numbered from 000. Every step holds one two-digit instruction code (see [chapter 15](#15-instruction-code-table)). Instructions with an operand take more steps, e.g. `STO 05` = two steps `42 05`.

The number of steps follows from [memory partitioning](#10-memory-partitioning): 480 (000–479) by default, at most 960. The user's program lives in **bank 0**, library programs in banks 01 and higher.

⚠️ **Emulator:** 960 steps are available in bank 0 by default.

### 9.2 LRN mode

**Original TI-59:** `LRN` switches to program-writing mode. The display shows `PPP KK` — the step number and its code. Every key pressed (or a `2nd`/`Ind` combination) is stored as an instruction code at the current step and the pointer advances. A second `LRN` leaves the mode. When writing, you usually start from step 000 (`RST` or `GTO 000` before `LRN`).

✔ *Emulator:* `LRN` mode shows `PPP KK` and stores TI-59 standard instruction codes.

| Pressed keys | Stored codes |
|---|---|
| `2nd Lbl A x²` | `76 11 33` |
| `5 STO 01` | `05 42 01` |
| `STO 2nd Ind 05` | `72 05` |
| `GTO 123` | `61 01 23` |
| `INV SBR` | `92` |

Notes: `INV` is stored as its own step `22` (only `INV SBR` merges into `92`), `INV 2nd Fix` takes no operand, and programs written by **older emulator versions** (raw key codes) must be rewritten. A program can also be inspected in the window `View > Program Debugger / Steps`.

### 9.3 Editing a program

| Keys (in LRN) | Effect on the original |
|---|---|
| `SST` | advance one step |
| `BST` | go back one step |
| `2nd Ins` | insert an empty step (`00`) at the current position, the rest shift |
| `2nd Del` | delete the current step, the rest shift back |
| `2nd CP` (outside LRN) | erase the whole program, step 000 |
| `GTO nnn` (outside LRN) | move the pointer to step nnn |

✔ *Emulator:* `SST`, `BST`, `2nd Ins`, `2nd Del` and `2nd CP` work in `LRN` (Ins inserts step code `68` = Nop). The programs of library banks cannot be edited, same as on the original.

### 9.4 Running, stopping, single-stepping

| Keys | Effect |
|---|---|
| `RST` | pointer to step 000 of bank 0; the original additionally clears flags and the subroutine stack |
| `R/S` | runs the program from the current step; stops it while running |
| `R/S` in a program | stops the program (e.g. for input) |
| `SST` (outside LRN) | executes one instruction |
| `BST` (outside LRN) | moves the pointer one step back |
| `A`–`E`, `A'`–`E'` | runs the program from the given label as a subroutine |

While a program runs, the emulator's display blinks dimly and only `R/S` responds.

⚠️ **Emulator:**
- `RST` does not clear flags or the subroutine stack.
- A number keyed in right before `R/S` inside a program is kept (a program `7 R/S` ends with `7`).

### 9.5 Labels

`2nd Lbl X` marks a place in the program. A label can be any key except a few reserved ones. Most often `A`–`E` and `A'`–`E'` are used because they can be started by pressing the key directly. A label takes two steps (`76 kk`) in the program.

⚠️ **Emulator:** only labels `A`–`E'` (codes 10–19) can be used as `GTO`/`SBR` and conditional-jump targets. A label is searched in the current bank.

### 9.6 Unconditional jumps

| Notation | Effect |
|---|---|
| `GTO X` | jump to label X |
| `GTO nnn` | jump to absolute step nnn (in a program 3 steps `61 0n nn`) |
| `GTO 2nd Ind nn` | jump to the address stored in register nn (in a program `83 nn`) |

✔ *Emulator:*
- In a program an absolute address reads in the `0n nn` form (the TI-59 standard), labels A–E' as byte `1x`, and `GTO Ind` as `61 40 nn` or merged code `83 nn`. The one-byte address `20`–`99` is also accepted (an emulator extension).
- From the keyboard, `GTO 2nd Ind nn` waits for 3 digits.

### 9.7 Subroutines

`SBR X` (or `SBR nnn`) calls a subroutine: it stores the return address and jumps. `INV SBR` (`RTN`, code 92) returns past the call site. Pressing `A`–`E'` from the keyboard is the same as `SBR A`–`E'`: the program runs and stops after `RTN`.

✔ *Emulator:*
- An `SBR` inside a running program continues after `RTN`; an `SBR` from the keyboard (including `A`–`E'`) stops the program after `RTN`.
- Code `92` (`RTN`) works the same as `INV SBR`.

*(verified)* example — library programs may also be called this way, see [11](#11-library-programs-and-pgm).

### 9.8 Conditional jumps

`2nd x=t nn nnn` transfers to the target when the display equals the T register; `2nd x≥t nn nnn` when the display is greater or equal. With `INV`: not equal, and less than respectively. The target is a label or an absolute address.

*(verified)* examples: `2 2nd x=t 2` → the display stays `2.` (true → transfer); the tests `x < t`, `x ≠ t` come with `INV`.

### 9.9 Flags

The calculator has flags 0–9: `2nd St flg n` sets (with `INV`: resets) a flag; `2nd If flg n nnn` transfers to the target if the flag is set (with `INV` if it is *not* set).

⚠️ **Emulator:** the meaning of flags 7–9 differs from the original (7 = trace printing — see [12](#12-printing), 8/9 = internal; the original 8+9 = stop-on-error).

### 9.10 Loops with Dsz

`2nd Dsz n X` (n = 0–9) decrements register Rn by 1 and jumps to X when the result is **not** zero. Practical for loops with a fixed number of passes: store the pass count in R0–R9 and place `Dsz n` with a target at the loop start at the end of the loop body. See [sample 16.2](#162-factorial-with-dsz).

On the original the value moves toward zero (a negative number grows). *(verified: R00 = −2, after `Dsz` −1, `RCL 00` shows `−1.` like the oracle)*

⚠️ **Emulator:** the same as the original; the `Dsz 2nd Ind` form is not supported.

### 9.11 Incrementing and decrementing a register

| Notation | Effect |
|---|---|
| `2nd Op 2n` (Op 20–29) | Rn = Rn + 1 (n = 0–9) |
| `2nd Op 3n` (Op 30–39) | Rn = Rn − 1 (n = 0–9) |

⚠️ **Emulator:** also shows the new register value on the display (overwrites x). *(verified: `2nd Op 20` → R00 = 1, display `1.`)*

### 9.12 Pause and Nop

| Notation | Effect |
|---|---|
| `2nd Pause` | in a program, briefly (about 0.5 s) holds the display and continues |
| `2nd Nop` | an empty instruction that does nothing (useful as filler) |

---

## 10. Memory partitioning

The TI-59 memory is shared between program steps and data registers. One register equals 8 steps.

**Original TI-59:**
- `2nd Op 16` shows the current partition in the `KKK.RR` form (the last step and the last register), default `479.59`.
- `n 2nd Op 17` sets a new partition where n is the **number of register tens** (0–10). For example, `6 2nd Op 17` → `479.59`, `3 2nd Op 17` → `719.29`, `10 2nd Op 17` → `159.99`.

⚠️ **Emulator:** *(verified)*
- `Op 16` shows `4852` instead of `479.59` in the default state.
- `Op 17` takes the **number of registers** (0–100) and computes the number of steps as (100 − n) × 20, so the default state (960 steps) corresponds to `52 2nd Op 17`.
- Access to registers is not limited in any way.
- `0 2nd Op 17` can crash the application.

Prefer not to use Op 16 and 17 in the emulator. See [Memory partitioning in the deviations overview (Czech)](ODCHYLKY_A_OMEZENI.md#310-rozdělení-paměti).

---

## 11. Library programs and Pgm

The TI-59 accepts a library module with pre-programmed programs — by default the **Master Library** (programs 01–25). A module program is started like this:

| Keys | Effect |
|---|---|
| `2nd Pgm nn` | selects the library program nn for the **following** label call |
| `2nd Pgm nn A` (…`E'`) | runs the A (…`E'`) label of program nn |
| `2nd Pgm nn SBR nnn` | runs program nn from step nnn |
| `2nd Pgm 2nd Ind nn` | the program number is taken from register nn |
| `2nd Op 09` | copies a library program into program memory |

The steps of a library program can be printed: `2nd Pgm nn` + `2nd List` prints the selected bank on the virtual printer (the keys `Pgm` = `2nd` + `LRN`, `List` = `2nd` + `=`).

A module program uses the user's data registers and may overwrite their content. The key description of the Master Library programs lives on the cards of the module manual.

### Program cards

Each program has a **card** (a magnetic tape with the title and the inputs). The emulator shows it as a strip under the display: the program title on the left, the `ML-nn` number on the right, and below that two rows of labels — the top red row `A′`–`E′` and the bottom black row `A`–`E`. The strip shows the full card content including the boxed key buttons (e.g. ML-01 `DIAGNOSTIC: [SBR] [=]`) and subscripts (`a_ij` in ML-03).

**The programs of the built-in Master Library:**

| Bank | Program | What it computes |
|---|---|---|
| ML-01 | Diagnostic | Checks the calculator and the module (`Sbr =`); initializes linear regression; a universal print routine |
| ML-02 | Matrix Inversion, Determinants & Simult. Equations | inverse of a matrix, determinant, a system of n equations in n unknowns |
| ML-03 | Matrix Addition and Multiplication | sums and products of matrices |
| ML-04 | Complex Arithmetic | basic complex arithmetic (X±Y, X·Y, X/Y) |
| ML-05 | Complex Functions | X², √X, 1/X, eˣ, ln X and the polar form of a complex number |
| ML-06 | Complex Trig Functions | trigonometric functions and inverses over complex numbers |
| ML-07 | Polynomial Evaluation | the value of a polynomial with real coefficients |
| ML-08 | Zeros of Functions | real roots of a user-defined function |
| ML-09 | Simpson's Approximation (Continuous) | integral of a user-defined function |
| ML-10 | Simpson's Approximation (Discrete) | integral from n+1 equally-spaced samples |
| ML-11 | Triangle Solution (1) | a triangle from SSS, SSA or SAS |
| ML-12 | Triangle Solution (2) | a triangle from ASA, SAA; area from three sides |
| ML-13 | Curve Solution | chord and arc of a circle |
| ML-14 | Normal Distribution | areas under the standard normal curve |
| ML-15 | Random Number Generator | uniformly and normally distributed random numbers |
| ML-16 | Combinations, Permutations, Factorials | combinations, permutations, factorials |
| ML-17 | Moving Averages | the moving average of the n most recent values |
| ML-18 | Compound Interest | compound interest (four quantities) |
| ML-19 | Ordinary Annuity/PV | loan amortization with fixed payments; present value |
| ML-20 | Days Between Dates | the number of days between two dates |
| ML-21 | Number Guessing | a number-guessing game |
| ML-22 | Checking/Savings Account | checking and savings accounts; service charges |
| ML-23 | Clock Time | time + 12/24 h conversions, hours:minutes |
| ML-24 | Unit Conversions (1) | lengths and areas (in↔cm, ft↔m, yd↔m, mi↔km, mi↔n mi; pressing twice = area) |
| ML-25 | Unit Conversions (2) | volumes, weights and temperatures (°F/°C, l↔gal/oz, lb↔kg, …) |

⚠️ **Emulator:**
- The built-in library `src/TI5X.Avalonia/Assets/ml.ti5l` contains **all 25 banks** of the Master Library, built from the original CROM TMC0541 dump; the programs were verified against the microcode oracle (e.g. `2nd Pgm 24`, `6`, `A` → `15.24`; `2nd Pgm 25`, `41`, `A` → `5.`, see the `CeAndLibraryEntryTests` tests). Merged codes execute. ML-01 still fails its final checksum (see [Library programs in the deviations overview](ODCHYLKY_A_OMEZENI.md#311-knihovní-programy-karty-a-soubory)).
- The Master Library cards in the strip carry the exact labels and buttons from the original project's templates; the other built-in modules (`mod02`–`mod13`) lack card transcriptions so far and show just the program titles.
- `Pgm nn` leaves bank nn selected after the return. Press `RST` to return to bank 0.
- `Pgm` inside a program jumps to step 000 of the selected bank.
- `Op 09` is not implemented.
- Further libraries are loaded via `File > Load Library Module (.ti5l)...` or on Android from the **☰ > Built-in Libraries** menu (the same in MAUI and Avalonia).

See [Library programs in the deviations overview](ODCHYLKY_A_OMEZENI.md#311-knihovní-programy-karty-a-soubory).

---

## 12. Printing

With the PC-100A printer (in the emulator `View > Virtual Printer (PC-100A)`) these functions are available. A line holds 20 characters.

| Keys | Effect |
|---|---|
| `2nd Prt` | prints the display content (right-aligned) |
| `2nd Adv` | advances the paper by one line |
| `2nd List` | prints the program in the `PPP  KK MNE` form from the current step (move the start with `GTO nnn` outside LRN) |
| `2nd Op 08` | prints the program's labels (`PPP  LBL X`) |
| `2nd Op 00` | clears the print register |
| `2nd Op 01`–`Op 04` | loads 5 characters (10 digits, a pair = a character code) from the display into the 1st–4th quarter of the print register (20 characters) |
| `2nd Op 05` | prints the print register |
| `2nd Op 06` | prints the display with the text from the 4th quarter of the print register |
| `2nd Op 07` | draws a plot point (column 0–19 by x) |

The keys: `Prt` is the second function of `+/-`, `Adv` of `.` and `List` of `=` (see [2.1](#21-key-layout)).

Alphanumeric printing: a character code is a two-digit number, for example `13` = A. The text "AB" is prepared as `1314000000 2nd Op 01`. The full PC-100A character table is in the printer manual.

⚠️ **Emulator:**
- `Op 00` enables trace printing (flag 7) instead of clearing.
- `Op 05` clears the print register after printing.
- `Op 06` and `Op 07` only advance the paper.
- `INV 2nd Prt` and `INV 2nd List` behave as without `INV`.
- Flag 7 in the emulator enables trace printing of every completed result.

The print head, fonts and character mnemonics are modeled by the verified TMC0251 model; differences against the original are in [Deviations 3.12 (Czech)](ODCHYLKY_A_OMEZENI.md#312-tiskárna-pc-100a).

---

## 13. Op codes reference

`2nd Op nn` invokes the special function nn. `2nd Op 2nd Ind nn` takes the function number from register nn.

| Op | Original TI-59 | Emulator |
|---|---|---|
| 00 | clears the print buffer | ⚠️ enables trace printing (flag 7) |
| 01–04 | loads characters into the print register | ✔ |
| 05 | prints the print register | ⚠️ prints and clears |
| 06 | prints the display with text | ⚠️ only advances the paper |
| 07 | a plot point | ⚠️ only advances the paper |
| 08 | prints the labels | ✔ |
| 09 | downloads a library program | ❌ ignored |
| 10 | signum (−1, 0, 1) | ❌ ignored |
| 11 | sample variance | ⚠️ linear regression (slope; intercept in T) |
| 12 | slope and intercept | ⚠️ the estimate of y′ from x |
| 13 | correlation coefficient | ✔ |
| 14 | the estimate of y′ from x | ❌ ignored |
| 15 | the estimate of x′ from y | ❌ ignored |
| 16 | shows the memory partition | ⚠️ wrong shape (`4852`) |
| 17 | a new memory partition | ⚠️ the number of registers instead of tens |
| 18 | without an error → sets flag 7 | ❌ ignored |
| 19 | with an error → sets flag 7 | ❌ ignored |
| 20–29 | Rn + 1 (n = 0–9) | ⚠️ additionally displays the new value |
| 30–39 | Rn − 1 (n = 0–9) | ⚠️ additionally displays the new value |

Unknown Op codes are silently ignored without an error. The full description is in [Op codes in the deviations overview (Czech)](ODCHYLKY_A_OMEZENI.md#4-op-kódy).

---

## 14. Error conditions

An invalid operation puts the calculator into the error state and **the display flashes**. Errors come, for example, from:

- division by zero and `1/x` for x = 0,
- `lnx`, `log` for x ≤ 0, `√x` for x < 0,
- `INV sin`, `INV cos` for \|x\| > 1,
- `INV yˣ` with x = 0,
- statistics with insufficient data (`x̄` for n = 0, deviation for n ≤ 1),
- range overflow (on the original above 9.999999999 × 10⁹⁹),
- too many parentheses or subroutines, a surplus `)`,
- a jump to a non-existent label or address, an invalid register address.

The error state is cancelled by `CE`, `CLR` or by entering a new number. With an error in a program, the original only stops the program when flag 8 is set.

⚠️ **Emulator:**
- The display flashes with the last displayed value.
- Overflow is reported only around 1.8 × 10³⁰⁸.
- Every error stops the program.
- An invalid key during address entry leads to an error.

### 14.1 Troubleshooting

| Problem | Cause and procedure |
|---|---|
| A program / library routine "doesn't produce" the expected result | Missed an operand? Library programs take the number **after selecting the program**: e.g. ML-24: `2nd Pgm 2 4`, `6`, `A` → `15.24`; the input survives the routine's leading `CE` (see [11](#11-library-programs-and-pgm)). Check the selected bank (`Library Bank NN` in the status line) |
| Trouble after `RST` or `New Program` | `RST` in the emulator does not clear flags; `New Program` performs a full reset **including the loaded libraries** — reload the library (the [File menu](EMULATOR_UZIVATEL.md#3-menu), or `Built-in Libraries` on Android). `2nd CMs` and `CLR` do not touch libraries |
| The display flashes and computes wrongly | Error state: see the [list of errors](#14-error-conditions) above; `CE` stops the flashing |
| A number shows as `1.5234 35` | The `EE` format is active; press `EE` again to return, or `INV 2nd Eng` when in Eng |
| `Eng` while entering a number "swallowed" the number | `2nd Eng` during entry discards the number ([3.3](#33-engineering-notation-eng)) — terminate the number first (`=`), then `2nd Eng` |
| `2nd Fix nn` does not apply with EE/Eng | `Fix` is ignored in the EE and Eng formats — [3.4](#34-fixed-decimal-places-fix) |
| Memory operations give unexpected results | The address is **two digits**: `STO 0 1` = register 01. `STO 1 0 2` = an STO into register 10 and then a digit |
| A program in LRN "does nothing" | Check `RST` (PC = 000) and `R/S`; labels `A`–`E'` are called by pressing the key; programs written by an older emulator version need rewriting — see [9.2](#92-lrn-mode) |
| A library program runs, but no result is visible | The program may end with `INV SBR` and return to the calling bank; an intermediate value stays on the display. Module programs overwrite data registers — clear them (`2nd CMs`) before a new run |
| A loaded library program cannot be saved | `.ti5p` saves only **bank 0**; `2nd Op 09` (downloading a library program) is ❌ in the emulator. Copy the library program's content from the `2nd List` output (or the debugger) into a new `.ti5p` per [15.3](#153-creating-a-ti5p-file) |

---

## 15. Instruction code table

### 15.1 Complete table of codes 00–99

The table contains all 100 instruction codes. The **Mnemonic** column is the exact text from `KeyCodes.PrintMnemonics` (without leading and trailing spaces), which the emulator uses for `List`, `Op 08` and in the debugger. Unusual shapes appear in it (`Yⓧ`, `ẍ`, `I×I`, `EQ`, `GE`).

The **Emulator when running** column describes what the engine does with the code when it finds it in a program.

| Code | Mnemonic | Keys (original) | Emulator when running |
|---|---|---|---|
| 00 | `00` | `0` | digit 0 |
| 01 | `01` | `1` | digit 1 |
| 02 | `02` | `2` | digit 2 |
| 03 | `03` | `3` | digit 3 |
| 04 | `04` | `4` | digit 4 |
| 05 | `05` | `5` | digit 5 |
| 06 | `06` | `6` | digit 6 |
| 07 | `07` | `7` | digit 7 |
| 08 | `08` | `8` | digit 8 |
| 09 | `09` | `9` | digit 9 |
| 10 | `E'` | `2nd E'` | label call E' |
| 11 | `A` | `A` | label call A |
| 12 | `B` | `B` | label call B |
| 13 | `C` | `C` | label call C |
| 14 | `D` | `D` | label call D |
| 15 | `E` | `E` | label call E |
| 16 | `A'` | `2nd A'` | label call A' |
| 17 | `B'` | `2nd B'` | label call B' |
| 18 | `C'` | `2nd C'` | label call C' |
| 19 | `D'` | `2nd D'` | label call D' |
| 20 | `CLR` | – (the `2nd CLR` position, unused) | no effect |
| 21 | `2ND` | `2nd` (only as a key code) | switches 2nd for the next step |
| 22 | `INV` | `INV` | INV |
| 23 | `LNX` | `lnx` | lnx |
| 24 | `CE` | `CE` | CE |
| 25 | `CLR` | `CLR` | CLR |
| 26 | `2ND` | – (unused) | no effect |
| 27 | `INV` | – (unused) | no effect |
| 28 | `LOG` | `2nd log` | log |
| 29 | `CP` | `2nd CP` | CP |
| 30 | `TAN` | `2nd tan` | tan |
| 31 | `LRN` | `LRN` | toggles LRN |
| 32 | `X⇌T` | `x⇌t` | x⇌t |
| 33 | `X²` | `x²` | x² |
| 34 | `√X` | `√x` | √x |
| 35 | `1/X` | `1/x` | 1/x |
| 36 | `PGM` | `2nd Pgm` | Pgm (one operand byte) |
| 37 | `P/R` | `2nd P→R` | P→R |
| 38 | `SIN` | `2nd sin` | sin |
| 39 | `COS` | `2nd cos` | cos |
| 40 | `IND` | `2nd Ind` | Ind (only after an instruction that accepts it) |
| 41 | `SST` | `SST` | program step |
| 42 | `STO` | `STO` | STO (one operand byte) |
| 43 | `RCL` | `RCL` | RCL (one operand byte) |
| 44 | `SUM` | `SUM` | SUM (one operand byte) |
| 45 | `Yⓧ` | `yˣ` | yˣ |
| 46 | `INS` | `2nd Ins` | no effect |
| 47 | `CMS` | `2nd CMs` | CMs |
| 48 | `EXC` | `2nd Exc` | Exc (one operand byte) |
| 49 | `PRD` | `2nd Prd` | Prd (one operand byte) |
| 50 | `I×I` | `2nd \|x\|` | \|x\| |
| 51 | `BST` | `BST` | PC − 1 |
| 52 | `EE` | `EE` | EE |
| 53 | `(` | `(` | ( |
| 54 | `)` | `)` | ) |
| 55 | `÷` | `÷` | ÷ |
| 56 | `DEL` | `2nd Del` | no effect |
| 57 | `ENG` | `2nd Eng` | Eng |
| 58 | `FIX` | `2nd Fix` | Fix (one operand byte) |
| 59 | `INT` | `2nd Int` | Int |
| 60 | `DEG` | `2nd Deg` | Deg |
| 61 | `GTO` | `GTO` | GTO (one target byte) |
| 62 | `PG*` | `2nd Pgm 2nd Ind` | ⚠️ digit 7 |
| 63 | `EX*` | `2nd Exc 2nd Ind` | ⚠️ digit 8 |
| 64 | `PD*` | `2nd Prd 2nd Ind` | ⚠️ digit 9 |
| 65 | `×` | `×` | × |
| 66 | `PAU` | `2nd Pause` | a pause of about 0.5 s |
| 67 | `EQ` | `2nd x=t` | x=t (one target byte) |
| 68 | `NOP` | `2nd Nop` | nothing |
| 69 | `OP` | `2nd Op` | Op (one operand byte) |
| 70 | `RAD` | `2nd Rad` | Rad |
| 71 | `SBR` | `SBR` | SBR (one target byte; stops after the return) |
| 72 | `ST*` | `STO 2nd Ind` | ⚠️ digit 4 |
| 73 | `RC*` | `RCL 2nd Ind` | ⚠️ digit 5 |
| 74 | `SM*` | `SUM 2nd Ind` | ⚠️ digit 6 |
| 75 | `-` | `−` | − |
| 76 | `LBL` | `2nd Lbl` | a label (skips one byte) |
| 77 | `GE` | `2nd x≥t` | x≥t (one target byte) |
| 78 | `∑+` | `2nd Σ+` | Σ+ |
| 79 | `ẍ` | `2nd x̄` | x̄ |
| 80 | `GRD` | `2nd Grad` | Grad |
| 81 | `RST` | `RST` | PC = 000, bank 0 |
| 82 | `HIR` | – (hidden-register operations) | ⚠️ digit 1 |
| 83 | `GO*` | `GTO 2nd Ind` | ⚠️ digit 2 |
| 84 | `OP*` | `2nd Op 2nd Ind` | ⚠️ digit 3 |
| 85 | `+` | `+` | + |
| 86 | `STF` | `2nd St flg` | St flg (one operand byte) |
| 87 | `IFF` | `2nd If flg` | If flg (one flag byte, one target byte) |
| 88 | `DMS` | `2nd D.MS` | D.MS |
| 89 | `π` | `2nd π` | π |
| 90 | `LST` | `2nd List` | List |
| 91 | `R/S` | `R/S` | stops the program |
| 92 | `RTN` | `INV SBR` | ⚠️ digit 0 |
| 93 | `.` | `.` | decimal point |
| 94 | `+/-` | `+/-` | +/- |
| 95 | `=` | `=` | = |
| 96 | `WRT` | `2nd Write` | no effect |
| 97 | `DSZ` | `2nd Dsz` | Dsz (one register byte, one target byte) |
| 98 | `ADV` | `2nd Adv` | Adv |
| 99 | `PRT` | `2nd Prt` | Prt |

Note about the ⚠️ merged-code rows: as **key codes** the values 62–64, 72–74, 82–84, 92 are digits (or the `R/S`/hidden-register positions), and the emulator intentionally rejects using them as merged opcodes (they execute as digits, i.e. the value is interpreted digit-first); see [Merged codes in the deviations](ODCHYLKY_A_OMEZENI.md#5-sloučené-kódy-instrukcí) for the exact semantics.

### 15.2 Program encoding rules

- Digits store as codes 00–09 (`7` → `07`).
- A `2nd` + key combination stores as a single code when one exists (`2nd log` → `28`); `A'`–`E'` store as 10/16–19.
- The `IND` combinations merge: `STO 2nd Ind` → `72`, `RCL 2nd Ind` → `73`, `SUM 2nd Ind` → `74`, `2nd Exc 2nd Ind` → `63`, `2nd Prd 2nd Ind` → `64`, `GTO 2nd Ind` → `83`, `2nd Op 2nd Ind` → `84`, `2nd Pgm 2nd Ind` → `62`.
- `INV SBR` stores as `92` (RTN).
- A branch/operand byte follows depending on the instruction (two digits for a register, three digits for an address, a label byte for `A`–`E'`).

### 15.3 Creating a ti5p file

A `.ti5p` file is a ZIP archive with the item `prog00` (the program bytes of bank 0, one code per byte) and optionally a `card00`/`help00`. The first entry should be `mimetype` = `application/vnd.nz.gen.geek_central.ti5x`, stored uncompressed; `prog00` is an XML file with the step codes space-separated:

```xml
<?xml version="1.0" encoding="utf-8"?>
<state><calc><prog>
76 11 33 65 89 95 92
</prog></calc></state>
```

Create it on macOS/Linux like this *(verified)*:

```sh
printf 'application/vnd.nz.gen.geek_central.ti5x' > mimetype
# prog00 with the content according to the sample above
zip -X0 circle.ti5p mimetype
zip -X circle.ti5p prog00
```

Load the file via `File > Open Program (.ti5p)...`. The program lands in bank 0 from step 000 and the rest of memory zeroes. A program loaded from a file can also be saved back via `File > Save Program (.ti5p)...`.

⚠️ **Emulator:** the application's `.ti5p` writer saves the bank-0 program and the current bank's card/labels; registers, flags, modes and the partition are not saved — see [the user guide (Czech), chapter 5](EMULATOR_UZIVATEL.md#5-programy-ti5p).

---

## 16. Sample programs

The samples carry instruction codes for the emulator, prepared as `.ti5p` files. Some programs also show the original keyboard notation in `LRN` for comparison with the original. All programs were verified in the emulator *(verified)*, including running them repeatedly.

### 16.1 Area of a circle

Computes S = π·r². Key r and press `A`.

Program (LRN): `2nd Lbl A x² × 2nd π = INV SBR`

| Step | Code | Instruction |
|---|---|---|
| 000 | 76 | `2nd Lbl` |
| 001 | 11 | `A` |
| 002 | 33 | `x²` |
| 003 | 65 | `×` |
| 004 | 89 | `2nd π` |
| 005 | 95 | `=` |
| 006 | 92 | `INV SBR` (return) |

`prog00` content: `76 11 33 65 89 95 92`

Usage: `2 A` → `12.56637061`, `3 A` → `28.27433388` *(verified)*.

### 16.2 Factorial using Dsz

Computes n! for an integer n ≥ 1. Key n and press `A`. Register R01 is the loop counter, R02 the running product.

Program (LRN): `2nd Lbl A STO 01 1 STO 02 RCL 01 2nd Prd 02 2nd Dsz 1 007 RCL 02 INV SBR`. The address `007` takes two steps (`00 07`) and `INV SBR` a single step `92`, so the step numbers match the original.

| Step | Code | Instruction | Note |
|---|---|---|---|
| 000 | 76 11 | `2nd Lbl A` | |
| 002 | 42 01 | `STO 01` | counter = n |
| 004 | 01 | `1` | |
| 005 | 42 02 | `STO 02` | product = 1 |
| 007 | 43 01 | `RCL 01` | loop start |
| 009 | 49 02 | `2nd Prd 02` | product × counter |
| 011 | 97 01 00 07 | `2nd Dsz 1 007` | R01 − 1, when ≠ 0, jump to 007 |
| 015 | 43 02 | `RCL 02` | result |
| 017 | 92 | `INV SBR` | end |

`prog00` content: `76 11 42 01 01 42 02 43 01 49 02 97 01 00 07 43 02 92`

Usage: `5 A` → `120.`, `6 A` → `720.`. For n = 0 the loop would not end (the counter decreases into negatives). *(verified)*

### 16.3 Temperature conversion with a flag

The `A` key converts a temperature according to flag 0: when it is not set, °C → °F; when set, °F → °C. `B` sets the flag (the °F → °C mode), `C` clears it (the °C → °F mode).

| Step | Code | Instruction | Note |
|---|---|---|---|
| 000 | 76 11 | `2nd Lbl A` | |
| 002 | 87 00 17 | `2nd If flg 0 B'` | flag 0 → jump to B' |
| 005 | 65 09 55 05 85 03 02 95 | `× 9 ÷ 5 + 32 =` | °C → °F |
| 013 | 92 | `INV SBR` | |
| 014 | 76 17 | `2nd Lbl B'` | |
| 016 | 75 03 02 95 65 05 55 09 95 | `− 32 = × 5 ÷ 9 =` | °F → °C |
| 024 | 92 | `INV SBR` | |
| 025 | 76 12 | `2nd Lbl B` | |
| 027 | 86 00 | `2nd St flg 0` | the °F → °C mode |
| 029 | 92 | `INV SBR` | |
| 030 | 76 13 | `2nd Lbl C` | |
| 032 | 22 86 00 | `INV 2nd St flg 0` | the °C → °F mode |
| 035 | 92 | `INV SBR` | |

`prog00` content:

```
76 11 87 00 17 65 09 55 05 85 03 02 95 92
76 17 75 03 02 95 65 05 55 09 95 92
76 12 86 00 92
76 13 22 86 00 92
```

Usage: `100 A` → `212.`; `B`, then `212 A` → `100.`; `C`, then `37 A` → `98.6`. *(verified)*

### 16.4 Series sum with a loop

Adds 1 + 2 + … + n. Key n and press `A`. Shows `Op 20`, `SUM`, the `x≥t` comparison and a jump to a label.

| Step | Code | Instruction | Note |
|---|---|---|---|
| 000 | 76 11 | `2nd Lbl A` | |
| 002 | 42 01 | `STO 01` | R01 = n |
| 004 | 00 | `0` | |
| 005 | 42 00 | `STO 00` | counter R00 = 0 |
| 007 | 42 02 | `STO 02` | sum R02 = 0 |
| 009 | 76 16 | `2nd Lbl A'` | loop start |
| 011 | 69 20 | `2nd Op 20` | R00 + 1 (the emulator displays it too) |
| 013 | 44 02 | `SUM 02` | sum + x |
| 015 | 43 01 | `RCL 01` | |
| 017 | 32 | `x⇌t` | t = n |
| 018 | 43 00 | `RCL 00` | x = counter |
| 020 | 77 19 | `2nd x≥t D'` | counter ≥ n → end |
| 022 | 61 16 | `GTO A'` | the next pass |
| 024 | 76 19 | `2nd Lbl D'` | |
| 026 | 43 02 | `RCL 02` | result |
| 028 | 92 | `INV SBR` | |

`prog00` content: `76 11 42 01 00 42 00 42 02 76 16 69 20 44 02 43 01 32 43 00 77 19 61 16 76 19 43 02 92`

Usage: `10 A` → `55.` *(verified)*

On the original `Op 20` does not change the display, so step 013 would have to be extended with `RCL 00` (the original: `2nd Op 20 RCL 00 SUM 02`). The emulator displays the new R00 value itself.

A larger sample is the Lunar Lander game. The program file is `src/TI5X.Avalonia/Assets/lunar_lander.ti5p` and its codes with comments live in `tests/TI5X.Core.Tests/TestHelpers.cs` (`LunarLanderProgram`). The game rules, physics model and controls are in [LUNAR_LANDER_SPEC.md](LUNAR_LANDER_SPEC.md).

---

## 17. Glossary

| Term | Meaning |
|---|---|
| **AOS** | Algebraic Operating System — expressions evaluated by operator precedence (`yˣ` > `×`, `÷` > `+`, `−`), see [chapter 4](#4-arithmetic-aos-and-parentheses). |
| **Bank** | The storage of one program; bank 0 = the user's program, banks 01–99 = library programs (01–25 = the Master Library on the original). |
| **CROM** | The Control ROM chip of a Solid State Software module (TMC0540/‑0541 …); "CROM dump" = the module content's readout from which a `.ti5l` is built. |
| **CE / CLR** | Clear Entry / Clear (general) — see [3.5](#35-ce-and-clr). |
| **T register** | The register above the display; used by comparisons (`x=t`, `x≥t`), statistics (the x coordinate), `P→R` (r) and the `Σ+` target. |
| **Data registers** | Memory registers 00–99; addressed with `STO`/`RCL`/… by two digits; partitioned between memories and program steps (Op 17). |
| **Flag** | A hidden bit 0–9 set by `2nd St flg` and tested by `2nd If flg`; on the original flag 7 = trace printing. |
| **Label** | A place in the program named by a key (`2nd Lbl A`); `A′`–`E′` are the second functions. |
| **IND (indirect)** | Indirect addressing: the operand is read from a register's content (`STO 2nd Ind nn`). |
| **Op code** | A "special control operation" `2nd Op nn` — statistics, printing, memory and custom routines (0–99), see [13](#13-op-codes-reference). |
| **`Pgm`** | `2nd LRN` — library-program (bank) selection; see [11](#11-library-programs-and-pgm). |
| **Solid State Software** | A pre-programmed replaceable module (up to 5000 steps + the Master Library). Emulator: `.ti5l` files. |
| **Magnetic card** | A program card with a description, slid under the display; emulator: a `.ti5p` file and the card strip. |
| **`Σ+`, `x̄`** | Statistical sum (Σ+ adds a point; x̄ = mean/standard deviation), see [8](#8-statistics). |
| **DD.MMSS** | The degrees.minutes-seconds angle format (`2nd D.MS`). |
| **PC** | Program Counter — the pointer of the executing step (the LRN display: `PPP KK`). |
| **Card strip** | The app's visual card of a library program: title + the `A′`–`E′`/`A`–`E` labels, see [11](#11-library-programs-and-pgm). |