#!/bin/bash
# Regeneruje orakulum-trasy z C simulatoru (ti59d s -K patchem) do tests/traces/.
# Deterministicke: stejne skripty + stejne parametry chipu = byte-by-byte stejny log.
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
       -r rom/rom-ti59/TMC0571B.txt -k ti59 -R -R -R -R \
       -l rom/module-lib/TMC0541.txt)

run_trace() {
    local name="$1" cycles="$2" script="$3"
    rm -f "$SIM/log.txt"
    # -K vzdy (i prazdny boot script): bez nej key chip cte stdin a na EOF exituje
    (cd "$SIM" && ./ti59d "${CHIPS[@]}" -K "$HERE/tests/scripts/$script" -n "$cycles" -v 2 > /dev/null 2>&1)
    # -n: gzip bez mtime v hlavičce = regenerace byte-za-byte stejná
    gzip -9 -nc "$SIM/log.txt" > "$OUT/$name.trace.txt.gz"
    printf '%-10s %8d cycles, %8d instr records, gz=%s\n' \
        "$name" "$cycles" "$(grep -c 'FA=' "$SIM/log.txt")" "$(du -h "$OUT/$name.trace.txt.gz" | cut -f1)"
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

echo "OK: traces in $OUT"