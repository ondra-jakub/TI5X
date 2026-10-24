#!/bin/bash
# Differential test: for the entire corpus, runs the Dafny model and compares
# records against the C oracle trace (tests/traces/*.trace.txt.gz).
# Usage: ./scripts/corpus.sh [name ...]   (without arguments = entire corpus)
set -u
HERE="$(cd "$(dirname "$0")/.." && pwd)"
SIM="${TI59_SIM:-/Users/ondra/ti59_chips/TMS0500-simulator}"
RUNNER="$HERE/harness/bin/Debug/net8.0/Ti59Runner"
ROMS="$SIM/rom/rom-ti59/TMC0582.txt $SIM/rom/rom-ti59/TMC0582-CONST-K.txt \
$SIM/rom/rom-ti59/TMC0583.txt $SIM/rom/rom-ti59/TMC0583-CONST-K.txt \
$SIM/rom/rom-ti59/TMC0571B.txt $SIM/rom/module-lib/TMC0541.txt"

# name=cycles (must match scripts/gen-traces.sh!)
declare -a ALL=(boot 3000 add23 4000 mul57 4000 prconv 5000 invpr 5000
                 stats 8000 ops 8000 dms 5000 lrn 8000 ml01 215000)
NAMES=("$@")
[ ${#NAMES[@]} -eq 0 ] && NAMES=("${ALL[@]}")

pass=0; fail=0
i=0
while [ $i -lt ${#ALL[@]} ]; do
  name="${ALL[$i]}"; cycles="${ALL[$((i+1))]}"
  if [ "x${NAMES[*]}" != "x${ALL[*]}" ] && ! printf '%s\n' "${NAMES[@]}" | grep -qx "$name"; then
    i=$((i+2)); continue
  fi
  out="/tmp/trace-$name-dafny.txt"
  "$RUNNER" $ROMS "$HERE/tests/scripts/$name.keys" "$cycles" "$out" > /dev/null 2>&1 \
    || { echo "$name: RUNNER FAILED"; fail=$((fail+1)); i=$((i+2)); continue; }
  res=$(python3 "$HERE/scripts/compare-trace.py" "$HERE/tests/traces/$name.trace.txt.gz" "$out" 2>/dev/null)
  verdict=$(echo "$res" | tail -1)
  if echo "$res" | grep -q "100% BITOVA SHODA"; then
    secs=$(grep '^# secs' "$out" | tail -1)
    echo "PASS $name: $verdict ($secs)"
    pass=$((pass+1))
  else
    echo "FAIL $name:"
    echo "$res" | head -8
    fail=$((fail+1))
  fi
  i=$((i+2))
done
echo "----"
echo "PASS: $pass  FAIL: $fail"
[ $fail -eq 0 ]