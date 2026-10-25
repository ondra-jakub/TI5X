#!/usr/bin/env python3
"""Porovna trasu C simulatoru (LOG_HRAST) s trasou Dafny runneru.

Oba soubory se parsuji na zaznamy (addr, opcode, A-E, FA, KR, EXT, COND,
IDLE, IOi, IO, FB, SR, R5); prvni divergence se vytiskne s kontextem.

Navic (faze 3) porovnava PODSEKVENCE udalosti tiskarny/karty
(PRINT_CHAR/PRINT_FUNC/PRINT/PRINT_CLEAR/CRD.rd/CRD.wr) - obsah i
poradi. C log jine LOG() udalosti (klice, per-cycle EXT) ignoruje.

Pouziti: compare-trace.py <c_trace_gz|c_trace> <dafny_trace> [--full]
"""
import gzip
import re
import sys

FIELDS = ["A", "B", "C", "D", "E", "FA", "KR", "EXT", "COND", "IDLE",
          "IOi", "IO", "FB", "SR", "R5"]

# Udalosti C logu (LOG() bez newline, konkatenuji se v jedne radce).
# Alternace zacina specializovanym "not fund", aby nekolislo vseobecny
# PRINT[...] (obsah ma prave 20 znaku); buffer muze obsahovat apostrof
# - proto pevna sirka, ne [^'].
EVENT_RE = re.compile(
    r"PRINT_CLEAR\[(\d+)\]='(.{20})' "
    r"|CRD\.rd\[(\d+)\]=([0-9a-f][0-9a-f]) "
    r"|CRD\.wr\[(\d+)\]=([0-9a-f][0-9a-f]) "
    r"|PRINT_FUNC\[(\d+)\]='(.{3})' "
    r"|PRINT\[(\d+)\]='(\d+) not fund' "
    r"|PRINT\[(\d+)\]='(.{20})' "
    r"|PRINT_CHAR\[(\d+)\]='(.)' "
)


def extract_events(line):
    """Vyskyty udalosti v jedne radce (vice na radku = konkatenace LOG)."""
    out = []
    for m in EVENT_RE.finditer(line):
        g = m.groups()
        if g[0] is not None:
            out.append(("PRINT_CLEAR", g[0], g[1]))
        elif g[2] is not None:
            out.append(("CRD.rd", g[2], g[3]))
        elif g[4] is not None:
            out.append(("CRD.wr", g[4], g[5]))
        elif g[6] is not None:
            out.append(("PRINT_FUNC", g[6], g[7]))
        elif g[8] is not None:
            out.append(("PRINT_NOTFUND", g[8], g[9]))
        elif g[10] is not None:
            out.append(("PRINT", g[10], g[11]))
        elif g[12] is not None:
            out.append(("PRINT_CHAR", g[12], g[13]))
    return out


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


def iter_lines(path):
    opener = gzip.open if path.endswith(".gz") else open
    with opener(path, "rt", errors="replace") as f:
        for line in f:
            yield line.rstrip("\n")


def parse(path):
    recs = []
    cur = None
    for line in iter_lines(path):
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


def parse_events(path):
    evs = []
    for line in iter_lines(path):
        evs.extend(extract_events(line))
    return evs


def fmt_ev(ev):
    return f"{ev[0]}[{ev[1]}]='{ev[2]}'"


def compare_events(c_evs, d_evs):
    n = min(len(c_evs), len(d_evs))
    for i in range(n):
        if c_evs[i] != d_evs[i]:
            print(f"DIVERGENCE v udalosti #{i}:")
            print(f"  C:     {fmt_ev(c_evs[i])}")
            print(f"  Dafny: {fmt_ev(d_evs[i])}")
            lo = max(0, i - 10)
            hi = min(n, i + 10)
            print(f"Kontext C     [{lo}..{hi}]:")
            for j in range(lo, hi):
                mark = ">>>" if j == i else "   "
                print(f"{mark} {fmt_ev(c_evs[j])}")
            print(f"Kontext Dafny [{lo}..{hi}]:")
            for j in range(lo, hi):
                mark = ">>>" if j == i else "   "
                print(f"{mark} {fmt_ev(d_evs[j])}")
            return True
    if len(c_evs) != len(d_evs):
        print(f"POZOR: stejne udalosti do {n}, ale pocty se lisi "
              f"(C={len(c_evs)}, Dafny={len(d_evs)})")
        for j in range(min(n, len(d_evs)), max(len(c_evs), len(d_evs))):
            src = c_evs if len(c_evs) > len(d_evs) else d_evs
            side = "C" if len(c_evs) > len(d_evs) else "Dafny"
            print(f"  navic {side}: {fmt_ev(src[j])}")
        return True
    return False


def main():
    args = [a for a in sys.argv[1:] if a != "--full"]
    full = "--full" in sys.argv
    # volitelny 3. argument: vlastni soubor s udalostmi C (-v 5 beh).
    # Stary format (2 argumenty) = udalosti se cteni z obou tras.
    c_records_path = args[0]
    d_records_path = args[1]
    c_events_path = args[2] if len(args) > 2 else c_records_path
    c_recs = parse(c_records_path)
    d_recs = parse(d_records_path)
    c_evs = parse_events(c_events_path)
    d_evs = parse_events(d_records_path)
    n = min(len(c_recs), len(d_recs))
    evinfo = ""
    if len(c_evs) > 0 or len(d_evs) > 0:
        evinfo = f" + {len(c_evs)}/{len(d_evs)} udalosti C/Dafny"
    print(f"C: {len(c_recs)} zaznamu, Dafny: {len(d_recs)} zaznamu{evinfo}")
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
            print(f"Vysledek: SHODA {n}/{min(len(c_recs), len(d_recs))} "
                  f"(delky: C={len(c_recs)}, Dafny={len(d_recs)}){evinfo}")
            sys.exit(1)
        # zaznamy shodne: porovnej udalosti (obsah i poradi)
        if compare_events(c_evs, d_evs):
            sys.exit(1)
        print(f"Vysledek: 100% BITOVA SHODA ({n} zaznamu"
              + (f", {len(c_evs)} udalosti" if len(c_evs) else "") + ")")
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