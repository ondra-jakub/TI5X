#!/bin/bash
# Vygeneruje C# core z verifikovaneho Dafny modelu a zbuilduje runner.
# Pouziti: ./scripts/gen-core.sh
# Generated se nikdy needituje rucne - jedina kopie vznika tady a zkopiruje
# se do src/TI5X.RomCore/generated (CI regen-diffobe obe).
set -e
HERE="$(cd "$(dirname "$0")/.." && pwd)"
cd "$HERE"

echo "== dafny verify (plna sada) =="
time dafny verify dfy/TI59/Nibbles.dfy dfy/TI59/AluOps.dfy dfy/TI59/Cpu.dfy \
     dfy/TI59/Bus.dfy dfy/TI59/Chips.dfy dfy/TI59/PrintCrd.dfy dfy/TI59/Machine.dfy \
     --isolate-assertions --verification-time-limit 120

echo "== dafny translate cs =="
dafny translate cs dfy/TI59/Nibbles.dfy dfy/TI59/AluOps.dfy dfy/TI59/Cpu.dfy \
     dfy/TI59/Bus.dfy dfy/TI59/Chips.dfy dfy/TI59/PrintCrd.dfy dfy/TI59/Machine.dfy \
     --no-verify --include-runtime -o generated/Ti59RomCore.cs

echo "== dotnet build runner =="
dotnet build harness/Ti59Runner.csproj -v q

echo "OK: harness/bin/Debug/net10.0/Ti59Runner"
