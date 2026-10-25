#!/bin/bash
# Sestaví vendored C orákulum (pristine + patches) → ti59d v oracle/build/.
# Pristine adresář oracle/TMS0500-simulator/ zůstává beze změn — patch se
# aplikuje výhradně na pracovní kopii v oracle/build/.
# Použití: ./scripts/build-oracle.sh
set -e
HERE="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$HERE/oracle/TMS0500-simulator"
BUILD="$HERE/oracle/build/TMS0500-simulator"

[ -d "$SRC" ] || { echo "chybí $SRC"; exit 1; }
rm -rf "$HERE/oracle/build"
mkdir -p "$HERE/oracle/build"
cp -R "$SRC/." "$BUILD/"
# ROM dumpy: výchozí zdroj dat pro gen-traces (relative 'rom/...'-cesty ti59d)
cp -R "$HERE/rom/." "$BUILD/rom/"

cd "$BUILD"
for p in "$HERE"/patches/*.patch; do
    git apply "$p"
    echo "applied: $(basename "$p")"
done
make ti59d
echo "OK: $BUILD/ti59d"