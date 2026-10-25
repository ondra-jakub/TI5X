# ROM dumpy

Obsah:
- `rom-ti59/` — ROM dumpy TI-59 (TMC0582, TMC0583, TMC0571B) + CONST-K soubory
  (konstanty pro scom čipy). Výchozí zdroj: C simulátor TMS0500-simulator
  (M. Castet), `bin/get_rom.sh` (datamath.org). Bitově nezměněno.
- `module-lib/TMC0541.txt` — CROM dump Master Library (5000 slov).

Použití:
- diferenční korpus (`scripts/corpus.sh`, `scripts/gen-traces.sh`),
- embedded resource aplikace `src/TI5X.RomCore` (fáze 4) — ROM/CROM dumpy
  jsou součástí dat TI kalkulátorů (firmware fixní tisku); pozor na
  licenční kontext simulátoru (LGPL 2.1) — viz `oracle/TMS0500-simulator/LICENSE.md`.