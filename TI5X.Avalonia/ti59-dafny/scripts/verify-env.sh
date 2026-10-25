#!/bin/bash
# Kontrola prostředí pro práci s ti59-dafny (nová worktree / CI).
# Použití: ./scripts/verify-env.sh
set -u
HERE="$(cd "$(dirname "$0")/.." && pwd)"
ok=1
check() {
    local desc="$1"; shift
    if "$@" >/dev/null 2>&1; then
        echo "OK   $desc = $("$@" 2>/dev/null | head -1)"
    else
        echo "FAIL $desc"; ok=0
    fi
}

# 1) nástroje
check "dafny >= 4.11"       dafny --version
check "dotnet >= 10"        dotnet --version
check "python3"             python3 --version
# 2) Dafny model a generated
[ -f "$HERE/dfy/TI59/Machine.dfy" ]  && echo "OK   dfy/TI59/Machine.dfy"  || { echo "FAIL dfy model"; ok=0; }
[ -f "$HERE/generated/Ti59RomCore.cs" ] && echo "OK   generated/Ti59RomCore.cs" || { echo "FAIL generated"; ok=0; }
# 3) ROM dumpy
for r in rom/rom-ti59/TMC0582.txt rom/rom-ti59/TMC0582-CONST-K.txt \
         rom/rom-ti59/TMC0583.txt rom/rom-ti59/TMC0583-CONST-K.txt \
         rom/rom-ti59/TMC0571B.txt rom/module-lib/TMC0541.txt; do
    [ -f "$HERE/$r" ] && echo "OK   $r" || { echo "FAIL $r"; ok=0; }
done
# 4) vendored orákulum (pristine) + build ti59d
[ -f "$HERE/oracle/TMS0500-simulator/Makefile" ] && echo "OK   oracle pristine" || { echo "FAIL oracle pristine (git -C . checkout oracle/)"; ok=0; }
if [ -x "$HERE/oracle/build/TMS0500-simulator/ti59d" ]; then
    echo "OK   oracle/build ti59d"
else
    echo "FAIL oracle/build/ti59d — spusťte ./scripts/build-oracle.sh"; ok=0
fi
# 5) korpusová stopa
[ -f "$HERE/tests/traces/boot.trace.txt.gz" ] && echo "OK   traces" || { echo "FAIL traces"; ok=0; }

[ $ok -eq 1 ] && echo "PROSTŘEDÍ OK" || { echo "PROSTŘEDÍ NEKOMPLETNÍ"; exit 1; }