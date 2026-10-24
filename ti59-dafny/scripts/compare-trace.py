#!/usr/bin/env python3
"""Porovna trasu C simulatoru (LOG_HRAST) s trasou Dafny runneru.

Oba soubory se parsuji na zaznamy (addr, opcode, A-E, FA, KR, EXT, COND,
IDLE, IOi, IO, FB, SR, R5); prvni divergence se vytiskne s kontextem.

Pouziti: compare-trace.py <c_trace_gz|c_trace> <dafny_trace> [--full]
"""
import gzip
import sys

FIELDS = ["A", "B", "C", "D", "E", "FA", "KR", "EXT", "COND", "IDLE",
          "IOi", "IO", "FB", "SR", "R5"]


def parse_line1(line):
    parts = line.split()
    if len(parts) < 2:
        return None
    try:
        addr = int(parts[0], 16)
        opcode = int(parts[1], 16)
    except ValueError:
        return None
    return addr, opcode


def parse(path):
    opener = gzip.open if path.endswith(".gz") else open
    recs = []
    cur = None
    with opener(path, "rt", errors="replace") as f:
        for line in f:
            line = line.rstrip("\n")
            h = parse_line1(line)
            if h is not None:
                if cur is not None:
                    recs.append(cur)
                cur = {"addr": h[0], "opcode": h[1]}
                continue
            if cur is None or "=" not in line:
                continue
            for tok in line.split():
                k, _, v = tok.partition("=")
                if k in FIELDS and k not in cur:
                    cur[k] = v
            # FB/SR/R5 radka muze prinest vice fieldu najednou
            if all(k in cur for k in FIELDS):
                recs.append(cur)
                cur = None
    if cur is not None and all(k in cur for k in FIELDS):
        recs.append(cur)
    return recs


def main():
    c_recs = parse(sys.argv[1])
    d_recs = parse(sys.argv[2])
    full = "--full" in sys.argv
    n = min(len(c_recs), len(d_recs))
    print(f"C: {len(c_recs)} zaznamu, Dafny: {len(d_recs)} zaznamu")
    div = None
    for i in range(n):
        c, d = c_recs[i], d_recs[i]
        for k in FIELDS:
            if c.get(k) != d.get(k):
                div = (i, k, c, d)
                break
        if div:
            break
    if div is None:
        if len(c_recs) != len(d_recs):
            print(f"POZOR: stejne zaznamy do {n}, ale delky se lisi")
            print(f"Vysledek: SHODA {n}/{min(len(c_recs), len(d_recs))} (delky: C={len(c_recs)}, Dafny={len(d_recs)})")
            sys.exit(1)
        print(f"Vysledek: 100% BITOVA SHODA ({n} zaznamu)")
        sys.exit(0)
    i, k, c, d = div
    print(f"DIVERGENCE na zaznamu #{i} (instrukce c. {i}), pole {k}:")
    print(f"  C:     {c.get(k)}")
    print(f"  Dafny: {d.get(k)}")
    print("  C rec:    ", " ".join(f"{f}={c.get(f, '?')}" for f in FIELDS))
    print("  Dafny rec:", " ".join(f"{f}={d.get(f, '?')}" for f in FIELDS))
    lo = max(0, i - 50)
    hi = min(n, i + 50)
    if not full:
        print(f"\nKontext (C): zaznamy #{lo}..#{hi}:")
        for j in range(lo, hi):
            r = c_recs[j]
            mark = ">>>" if j == i else "   "
            print(f"{mark} #{j}: {r['addr']:04X} {r['opcode']:04X} "
                  f"FA={r['FA']} KR={r['KR']} COND={r['COND']} R5={r['R5']}")
    sys.exit(1)


if __name__ == "__main__":
    main()