#!/bin/bash
# Differential test: for the entire corpus, runs the Dafny model and compares
# records against the C oracle trace (tests/traces/*.trace.txt.gz).
# Faze 3: ml01print/cardr navíc porovnávají události (events.txt.gz),
# tiskové pasky (tests/traces/*.tape.txt) a obrázky karet (tests/cards/).
# Usage: ./scripts/corpus.sh [name ...]   (without arguments = entire corpus)
set -u
HERE="$(cd "$(dirname "$0")/.." && pwd)"
RUNNER="$HERE/harness/bin/Debug/net10.0/Ti59Runner"
# ROM dumpy: vendored v repozitáři (bez externí závislosti na simulátoru)
ROMS="$HERE/rom/rom-ti59/TMC0582.txt $HERE/rom/rom-ti59/TMC0582-CONST-K.txt \
$HERE/rom/rom-ti59/TMC0583.txt $HERE/rom/rom-ti59/TMC0583-CONST-K.txt \
$HERE/rom/rom-ti59/TMC0571B.txt $HERE/rom/module-lib/TMC0541.txt"

# name=cycles (must match scripts/gen-traces.sh!)
# ml01print/cardr: chip-args (print/card), extra výstupy runneru
declare -a ALL=(
  boot 3000 add23 4000 mul57 4000 prconv 5000 invpr 5000
  stats 8000 ops 8000 dms 5000 lrn 8000 ml01 215000
  ml01print 220000 cardr 40000
)
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
  cardin="-"; printer="0"; cardout=""; tapeout=""
  case "$name" in
    ml01print) printer="1"; tapeout="/tmp/trace-$name-tape.txt" ;;
    cardr) cardin="$HERE/tests/cards/card92.bin"; cardout="/tmp/trace-$name-card.bin" ;;
  esac
  # [11]=cardOut vdycky (i kdyz je to "-"), jinak by tapeOut spadl do 11
  "$RUNNER" $ROMS "$cardin" "$printer" "$HERE/tests/scripts/$name.keys" \
    "$cycles" "$out" "${cardout:--}" ${tapeout:+"$tapeout"} > /dev/null 2>&1 \
    || { echo "$name: RUNNER FAILED"; fail=$((fail+1)); i=$((i+2)); continue; }
  evargs=""
  [ -f "$HERE/tests/traces/$name.events.txt.gz" ] && \
    evargs="$HERE/tests/traces/$name.events.txt.gz"
  res=$(python3 "$HERE/scripts/compare-trace.py" \
        "$HERE/tests/traces/$name.trace.txt.gz" "$out" $evargs 2>/dev/null)
  verdict=$(echo "$res" | tail -1)
  ok=1
  echo "$res" | grep -q "100% BITOVA SHODA" || ok=0
  # páska a obraz karty (fáze 3)
  if [ -n "$tapeout" ]; then
    if cmp -s "$tapeout" "$HERE/tests/traces/$name.tape.txt"; then
      verdict="$verdict + PASCA OK"
    else
      ok=0; verdict="$verdict + PASJA DIVERGUJE"
    fi
  fi
  if [ -n "$cardout" ] && [ -f "$HERE/tests/cards/$name-written.bin" ]; then
    if cmp -s "$cardout" "$HERE/tests/cards/$name-written.bin"; then
      verdict="$verdict + KARTA OK"
    else
      ok=0; verdict="$verdict + KARTA DIVERGUJE"
    fi
  fi
  if [ $ok -eq 1 ]; then
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
