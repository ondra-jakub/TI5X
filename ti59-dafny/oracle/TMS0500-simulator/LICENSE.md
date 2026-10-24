# Vendored C oracle — TMS0500/TMC0501 microcode simulator

Tento adresář obsahuje **pristine** (nezměněné) kopii zdrojových kódů C
simulátoru TMS0500/TMC0501 (ti59d), který slouží jako diferenční orákulum
pro Dafny model v `dfy/TI59/`.

- Autor / původní projekt: **M. (Mickaël) Castet** — TMS0500-simulator
  (https://www.datamath.org a git repozitář autorův); vychází z prací
  Hyneka Sladkýho (http://hsl.wz.cz/ti_59.htm, https://www.hrastprogrammer.com/emulators.htm).
- Licence: **LGPL 2.1** (viz https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html).
- Změny provedené pro deterministické CI běhy (volby `-K`, `-n`) NENÍ v tomto
  adresáři: aplikují se **při buildu** z `../../patches/*.patch`
  (viz `../../scripts/build-oracle.sh`). Patch zůstává jediným
  source-of-truth pro odchylky simulátoru; tento adresář musí zůstat
  bitově shodný s upstreamem.

Struktura je kopie repozitáře simulátoru (Makefile, *.c/*.h, bin/, doc/,
README.md). ROM dumpy nejsou součástí tohoto adresáře — viz `../../rom/`.