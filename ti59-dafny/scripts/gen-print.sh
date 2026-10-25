#!/bin/bash
# Prekladi print cast verifikovaneho Dafny modelu do C# pro TI5X.Verified
# (vzor generated/partition.cs z partition.dfy).
#
# BEZ --include-runtime: Dafny runtime uz je v partition.cs (duplikace
# Dafny.Runtime namespaces by shazovala build). Verze dafny musi zustat
# 4.11 pro OBE generovane soubory najednou (regen-diff v CI).
#
# Filter odstrani:
#  - hlavicku "// Command-line arguments: ..." (normalizace jako u
#    partition: regen-diff nesmi byt citlivy na cesty/parametry volani),
#  - assembly atribut DafnySourceAttribute (CELKOVY jeden na assembly;
#    partition.cs jiz jeden ma - duplikat delal CS0579),
#  - bloky Dafny.ArrayHelpers a FuncExtensions ("compiler support" dafny
#    4.11, byte-identicke v kazdem vystupu; partition.cs je ma nezavazitelne
#    - duplikat by delal CS0101).
# Vysledny soubor: src/TI5X.Verified/generated/Ti59PrintCore.cs (vuci koreni repo)
# (+ .dtr manifest jako u partition-cs.dtr)
# (overrid pro CI regen-diff: TI59_PRINT_OUT=/cesta/regen.cs)
set -e
HERE="$(cd "$(dirname "$0")/.." && pwd)"
cd "$HERE"
DEF_OUT="$(dirname "$HERE")/src/TI5X.Verified/generated/Ti59PrintCore.cs"
OUT="${TI59_PRINT_OUT:-$DEF_OUT}"

TMPD="$(mktemp -d)"
trap 'rm -rf "$TMPD"' EXIT
# dafny --output NAME vytvori NAME.cs + NAME-cs.dtr
dafny translate cs dfy/TI59/Nibbles.dfy dfy/TI59/Bus.dfy \
     dfy/TI59/Ti59Events.dfy dfy/TI59/Print.dfy \
     --no-verify --output "$TMPD/Ti59PrintCore" >/dev/null 2>&1

awk '
  /^\/\/ Command-line arguments:/ { next }
  /^namespace Dafny \{$/ { skip=1; next }
  skip && /^\} \/\/ end of namespace Dafny$/ { skip=0; next }
  /^internal static class FuncExtensions \{$/ { skip2=1; next }
  skip2 && /^\/\/ end of class FuncExtensions$/ { skip2=0; next }
  /^\[assembly: DafnyAssembly\.DafnySourceAttribute\(@"/ { skip3=1; next }
  skip3 && /^\")\]$/ { skip3=0; next }
  !skip && !skip2 && !skip3 { print }
' "$TMPD/Ti59PrintCore.cs" > "$OUT"
cp "$TMPD/Ti59PrintCore-cs.dtr" "${OUT%.cs}-cs.dtr"
echo "OK: $OUT"