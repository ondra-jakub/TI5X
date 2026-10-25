# ti59-dafny — verifikovaný interpretr mikrokódu TMS0500 (TI-59)

Port instrukčního jádra C simulátoru [TMS0500-simulator](/Users/ondra/ti59_chips/TMS0500-simulator)
do Dafny 4.11, s formálními důkazy BCD aritmetiky ALU a **100% bitovou
shodou proti C orákulu** na celém testovacím korpusu (včetně ML-01
diagnostiky Master Library → displej „1").

## Výsledky

| korpus | instrukcí | výsledek |
|---|---|---|
| boot | 2 994 | 100 % bitová shoda |
| 2+3=, 5×7= | 3 994 | 100 % bitová shoda |
| P→R, INV P→R (5 x<>t 30) | 4 994 | 100 % bitová shoda |
| Σ+ řada, x̄, INV x̄ | 7 994 | 100 % bitová shoda |
| Op 11/12/13/14 | 7 994 | 100 % bitová shoda |
| D.MS | 4 994 | 100 % bitová shoda |
| LRN zápis + SST/BST | 7 994 | 100 % bitová shoda |
| **ML-01 diagnostika** | **214 994** | **100 % bitová shoda, displej „1."** |

Dafny verify: **1504 ověřovacích podmínek, 0 chyb** (~85 s),
`dafny verify ... --isolate-assertions --verification-time-limit 120`.

## Struktura

```
dfy/TI59/Nibbles.dfy   typy (bv8/bv16, Reg, masky, flagy, EXT signály)
dfy/TI59/AluOps.dfy    Alu() + Xch() port + specifikace a DŮKAZY BCD
dfy/TI59/Cpu.dfy       Cpu, tabulky ALU_OP/ALU_DST, run_early, Execute(),
                       alu_gen_digit (celý execute() z alu.c)
dfy/TI59/Bus.dfy       sbernice + start cyklu/reset D-linky (vbus.c)
dfy/TI59/Chips.dfy     brom, scom (v2), ram, lib (CROM), key (-K skript), display
dfy/TI59/Print.dfy     tiskárna TMC0251 (print.c: fonty, mnemoniky, buffer/head)
dfy/TI59/Crd.dfy       čtečka karet (crd.c), Ti59Event události (Ti59Events.dfy)
dfy/TI59/Machine.dfy   alu_process port, fold 16 stavů × W/R, StepInstruction,
                       TraceRec (LOG_HRAST pole), MachineWF invarianty
dfy/Smoke.dfy          mini-ROM smoke test (dafny run)
generated/Ti59RomCore.cs   vygenerovaný C# (dafny translate cs --include-runtime)
harness/Ti59Runner.cs       runner: parse dumpů, běh, HRAST výstup, tape/card
scripts/gen-traces.sh       regenerace orákulum-tras z ti59d
scripts/corpus.sh           diferenciální test celého korpusu
scripts/compare-trace.py    diff tras (první divergence + kontext ±50)
scripts/gen-core.sh         verify + translate + build (CI vstupní bod)
tests/scripts/*.keys        klávesové skripty korpusu (matrix kódy z key.c)
tests/cards/*.bin           obrazy karet pro cardr
tests/traces/*.trace.txt.gz C orákulum-trasy (LOG_HRAST, gzip)
tests/traces/*.events.txt.gz  -v 5 LOG události (PRINT_/CRD. subsekvence)
tests/traces/*.tape.txt     páska tiskárny (display_print řádky)
patches/0001-*.patch        additive opt-in patch C simulátoru (-K, -n, ti59d)
```

## Použití

```sh
# 1) verifikace + generování + build (vše zelené)
./scripts/gen-core.sh

# 2) regenerace orákula (vyžaduje C simulátor s patchem, viz patches/)
./scripts/gen-traces.sh          # ~30 s, deterministické

# 3) diferenciální test celého korpusu
./scripts/corpus.sh              # 12/12 PASS (ml01print ~45 s, ml01 ~40 s)
./scripts/corpus.sh ml01         # jen jeden vstup

# 4) ruční běh (HRAST kompatibilní výstup)
#   CLI: rom0582 const0582 rom0583 const0583 rom0571B libdump|- card|- \
#        printer:0/1 script maxCycles traceOut [cardOut] [tapeOut]
harness/bin/Debug/net10.0/Ti59Runner \
  $SIM/rom/rom-ti59/TMC0582.txt $SIM/rom/rom-ti59/TMC0582-CONST-K.txt \
  $SIM/rom/rom-ti59/TMC0583.txt $SIM/rom/rom-ti59/TMC0583-CONST-K.txt \
  $SIM/rom/rom-ti59/TMC0571B.txt $SIM/rom/module-lib/TMC0541.txt \
  - 1 tests/scripts/ml01print.keys 220000 /tmp/ml01print-dafny.txt - /tmp/tape.txt
python3 scripts/compare-trace.py tests/traces/ml01.trace.txt.gz /tmp/ml01-dafny.txt
```

## Verifikační hranice (co Dafny ručí a co ne)

- **Dokázáno (z3/z3 axiomy, 1504 VC):**
  - totalita a indexová bezpečnost všech 6 modulů (MachineWF invarianty
    se zachovávají přes celý fold 32 fází × 10 chipů),
  - `AluDigits(OP_ADD/OP_SUB, maska ALL) == učebnicová BCD specifikace`
    pro BCD-validní vstupy (cislice < 10) — včetně věrného hex-carry
    cislice 0 C modelu (`L_AlusAddBcd`/`L_AlusSubBcd` v AluOps.dfy),
  - determinismus: StepInstruction je čistá funkce stroj → (stroj, záznam).
- **Ověřeno diferenciálně (ne dokazem):** věrnost ROM — celý korpus je
  bit-shodný s C orákulem. ROM obsah je data, ne důkazový cíl.
- **Dokumentovaný důkazový TODO:** bridge „digit-level spec ⟹ hodnota
  (X+Y) mod 10^16" přes `ValueOf` (nonlinear 10^k lemmata); spec na
  úrovni cistic je hotová a dokázaná.

## Věrná rozhodnutí v modelu (odkazy do C)

Každé nejasné místo v C je v `.dfy` komentované odkazem `[alu.c:NNN]`:

- **run_early / IO.ALL** (alu.c:689-726): dvoufázové vykonávání — brzy
  v S0W (flags/keyboard/wait/IO.ALL zápisy), ostatní v S15R. IO.ALL navíc
  nastavuje FLG_IO_VALID před execute (SR51 workaround).
- **Hex carry cislice 0** (alu.c:136-141): carry při >=16 bez BCD korekce
  pod 16 — vlastnost používaná IO sbernicí (hex adresy RAM > 99).
- **R5→Adder special** (alu.c:589-604): dst[start]=R5, dst[cpos]=cval,
  dst[start+1..end]=0 + BCD korekce druhým Alu volanim; cislice mimo
  rozsah masky zůstávají (nalezeno diff testy — původní verze je mazala).
- **EXKR** (alu.c:494): aktivní varianta `KR = ((EXT<<1)&0xFFF0)|(EXT>>15)`
  (zakomentovaná varianta s KR[0] se nepoužívá).
- **Wait Busy** (alu.c:408-411): `#warning "Unknown behaviour"` → no-op.
- **Scan-all KR** (alu.c:361): plný přepis `KR = (digit<<4)|((bit<<8)&0x0700)`
  (bez zakomentované varianty `(KR & ~0x07F0) |`).
- **gen_digit B[3]** (alu.c:749-750): `"` dekód čte B[3] místo B[i]
  (XXX v C) — věrně zachováno.
- **Key phantom** (key.c:576-584): init key_code=1, key_count=1.
- **Scom v2** (32 konstant + 8 SCOM registrů; R8/R9 = polar/rect skryté
  registry ML-01) a „force 0 STO" quirk (scom.c:190-193).
- **RAM clear-10** (ram.c): C memset by pro addr>20 přetekl do jiných polí
  structu (UB); model čisti jen registry chipu — divergence v UB zemi,
  korpus ji netrefí.
- **aux.c**: jen logování → vynechán úplně.

## Deterministický -K skript (patch C simulátoru)

`patches/0001-ti59d-keyscript-cycle-limit.patch` — **additivní, opt-in**
(uživatel schválil jen `-K`/`-n`; bez přepínačů je chování nezměněné,
původní binář `main` nebyl přestavěn; druhý binář `make ti59d`).

Formát skriptu (řádky): `KEYHEX [not_before_cycle] [hold_scans] [toggle]`
- injektáž pouze na „read new key" události (idle scan-all, key_code==0),
  konzumace při `bus->cycle_count >= not_before` — na přesné hranici
  instrukce, bit-shodné s Dafny modelem,
- scan-row čtení (!scan) ve skriptovém režimu nečtou nic (fixuje
  „ztracené klávesy" při rychlé pipe zadávání — dřívější diagnostika),
- `toggle` přepíná persistentní bit (card reader `4A toggle`),
- bez stdinů a bez EOF-exitu; `-n max` = bezpečný halt pro CI
  (C semantika: proběhne max-1 cyklů).

## Stav fází plánu

- [x] fáze 0 — toolchain, patch, orákulum-trasy (tests/traces, gzip)
- [x] fáze 1 — Dafny model, ALU důkazy, verify zelený
- [x] fáze 2 — replay harness, korpus 10/10 bit-shodný, ML-01 displej „1."
- [x] fáze 3 — tiskárna (print.c) + čtečka karet (crd.c) do modelu;
      korpus 12/12 (ml01print 220k cyklů s tiskem pasky „MASTER"/„1.",
      cardr s obrazem karty). `cardw` (op 0x0A38 zapisovacka) je ve
      modelu verifikovaný, ale mimo rozsah testovani (nema directni
      korpusovy test).
- [ ] fáze 4 — `src/TI5X.RomCore/` + integrace za feature flagu (A/B proti
      ručnímu jádru CalculatorEngine.cs; klávesy UI → matrix kódy dle
      key_table_ti58) — ODKLÁDÁNO (print integrace jde přímo do Core)
- [ ] fáze 5 — cut-over a přepojení testů — ODKLÁDÁNO

## Poznámky k výkonu

ML-01 (215k instrukcí × 32 fází × 13 chipů) ~29 s v C# (immutable
datatypes, `dafny translate cs` + .NET 8). Dafny interpretr (`dafny run`)
je pro korpus nepoužitelný — jen Smoke.dfy (~100 instrukcí).