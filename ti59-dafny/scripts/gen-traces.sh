#!/bin/bash
# Regeneruje orakulum-trasy z C simulatoru (ti59d s -K patchem) do tests/traces/.
# Deterministicke: stejne skripty + stejne parametry chipu = byte-by-byte stejny log.
# Faze 3: tiskárna/karta - navíc -v 5 events-trace (události LOG) + tape stdout.
# Pouziti: ./scripts/gen-traces.sh [sim_dir]
set -e

HERE="$(cd "$(dirname "$0")/.." && pwd)"
# Výchozí: vendored orákulum (scripts/build-oracle.sh); $TI59_SIM / 1. param
# přepne na externí C simulátor (např. /Users/ondra/ti59_chips/TMS0500-simulator).
SIM="${1:-${TI59_SIM:-$HERE/oracle/build/TMS0500-simulator}}"
[ -x "$SIM/ti59d" ] || { echo "ti59d nenalezen v $SIM — spusť ./scripts/build-oracle.sh nebo nastav TI59_SIM"; exit 1; }
OUT="$HERE/tests/traces"
mkdir -p "$OUT"

# presne poradi chipu jako bin/ti59.sh (+ -l Master Library jako bin/ti59.sh -l)
CHIPS=(-r rom/rom-ti59/TMC0582.txt -s rom/rom-ti59/TMC0582-CONST-K.txt \
       -r rom/rom-ti59/TMC0583.txt -s rom/rom-ti59/TMC0583-CONST-K.txt \
       -r rom/rom-ti59/TMC0571B.txt)

run_trace() {
    local name="$1" cycles="$2" script="$3"
    rm -f "$SIM/log.txt"
    # -K vzdy (i prazdny boot script): bez nej key chip cte stdin a na EOF exituje
    (cd "$SIM" && ./ti59d "${CHIPS[@]}" -k ti59 -R -R -R -R \
       -l rom/module-lib/TMC0541.txt -K "$HERE/tests/scripts/$script" -n "$cycles" -v 2 > /dev/null 2>&1)
    # -n: gzip bez mtime v hlavičce = regenerace byte-za-byte stejná
    gzip -9 -nc "$SIM/log.txt" > "$OUT/$name.trace.txt.gz"
    printf '%-10s %8d cycles, %8d instr records, gz=%s\n' \
        "$name" "$cycles" "$(grep -c 'FA=' "$SIM/log.txt")" "$(du -h "$OUT/$name.trace.txt.gz" | cut -f1)"
}

# Faze 3: obchazi -K chipů (tiskárna/karta) a dělá DVA běhy:
#  -v 2: HRAST zaznamy (zaznamovy trace; format beze zmeny)
#  -v 5: LOG_SHORT+LOG_DEBUG (události PRINT/CRD v log.txt; BEZ HRAST)
# stdout z 1. behu se uchova kvuli tase (display_print radky '|      ...').
run_trace3() {
    local name="$1" cycles="$2" script="$3" extra_chips="$4"
    rm -f "$SIM/log.txt"
    (cd "$SIM" && ./ti59d "${CHIPS[@]}" -k ti59 -R -R -R -R $extra_chips \
       -l rom/module-lib/TMC0541.txt -K "$HERE/tests/scripts/$script" \
       -n "$cycles" -v 2 > "$OUT/$name.stdout.tmp" 2>/dev/null)
    gzip -9 -nc "$SIM/log.txt" > "$OUT/$name.trace.txt.gz"
    (cd "$SIM" && ./ti59d "${CHIPS[@]}" -k ti59 -R -R -R -R $extra_chips \
       -l rom/module-lib/TMC0541.txt -K "$HERE/tests/scripts/$script" \
       -n "$cycles" -v 5 > /dev/null 2>&1)
    gzip -9 -nc "$SIM/log.txt" > "$OUT/$name.events.txt.gz"
    printf '%-10s %8d cycles, %4d events, gz=%s\n' \
        "$name" "$cycles" \
        "$(grep -ac 'PRINT_\|CRD\.' "$OUT/$name.events.txt.gz")" \
        "$(du -h "$OUT/$name.trace.txt.gz" | cut -f1)"
}

run_trace boot     3000   ""
run_trace add23    4000   add23.keys
run_trace mul57    4000   mul57.keys
run_trace prconv    5000  prconv.keys
run_trace invpr    5000   invpr.keys
run_trace stats    8000   stats.keys
run_trace ops      8000   ops.keys
run_trace dms      5000   dms.keys
run_trace lrn      8000   lrn.keys
run_trace ml01   215000   ml01.keys

# --- faze 3 -----------------------------------------------
# ml01print: ML-01 diagnostika s tiskárnou (-p); paska = display_print radky
run_trace3 ml01print 40000 ml01print.keys "-p"
python3 - <<'PYEOF'
import re
out, dst = "tests/traces/ml01print.stdout.tmp", "tests/traces/ml01print.tape.txt"
data = open(out, "rb").read()
lines = re.findall(rb"\|      (.*)\n", data)
open(dst, "wb").write(b"".join(b"|      " + l + b"\n" for l in lines))
PYEOF
rm -f "$OUT/ml01print.stdout.tmp"

# cardr: reader otevren 4A toggle (po bootu); karta = CISTY fixture
# (crd_init CTE existujici soubor! - pred behem se kopiruje z tests/cards/)
CARD="$SIM/card-corpus.img"
cp "$HERE/tests/cards/card92.bin" "$CARD"
run_trace3 cardr 40000 cardr.keys "-c card-corpus.img"
cp "$CARD" "$HERE/tests/cards/cardr-written.bin"
rm -f "$CARD"

echo "OK: traces in $OUT"
