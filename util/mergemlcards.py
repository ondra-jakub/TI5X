#!/usr/bin/env python3
#+
# Replaces the generated program-name cards in a .ti5l built by mklibfromdump with the
# authentic card templates (button labels for the A-E keys) from the original TI-5X
# project sources (mlsrc/cardNN.xml) whenever one exists for the given bank number.
#
# mklibfromdump can only synthesize cards from the disassembly listing, which has the
# program titles but no key-button labels; the mlsrc templates carry the real card
# faces (subscripts, arrows, key boxes, prime/base row labels). Programs without an
# mlsrc template keep their generated card.
#
# Usage: python3 util/mergemlcards.py <in.ti5l> <mlsrc-dir> <out.ti5l>
#
# Copyright 2026 the TI5X contributors. GPL-3.0 like the util/ scripts.
#-

import re
import sys
import zipfile


def main(argv):
    if len(argv) != 4:
        raise SystemExit(__doc__)
    src_path, cards_dir, dst_path = argv[1], argv[2], argv[3]

    with zipfile.ZipFile(src_path) as src:
        entries = [(info, src.read(info.filename)) for info in src.infolist()]

    replaced, kept, dropped = [], [], []
    out_entries = []
    for info, data in entries:
        name = info.filename
        m = re.fullmatch(r"card(\d\d)", name)
        if m:
            bank = int(m.group(1))
            import os
            tpl_path = os.path.join(cards_dir, f"card{bank:02d}.xml")
            if os.path.exists(tpl_path):
                with open(tpl_path, "rb") as f:
                    tpl = f.read()
                if not title_left(tpl):
                    raise SystemExit(f"mlsrc template for bank {bank:2d} has no title")
                if not title_normalized_matches(data, tpl):
                    print(f"warning: bank {bank:2d} titles differ textually "
                          f"(generated {title_left(data)!r} vs mlsrc {title_left(tpl)!r}); "
                          f"using the mlsrc card face")
                out_entries.append((name, tpl))
                replaced.append(bank)
                continue
            out_entries.append((name, normalize(data)))
            kept.append(bank)
            continue
        if name == "mimetype":
            # mimetype must stay the first entry and be stored uncompressed
            out_entries.insert(0, (name, data))
            continue
        out_entries.append((name, data))

    if not replaced:
        raise SystemExit("no cards replaced - check the mlsrc path")

    with zipfile.ZipFile(dst_path, "w") as dst:
        # mimetype first, uncompressed (like the .ti5p writer does)
        for i, (name, data) in enumerate(out_entries):
            if i == 0 and name == "mimetype":
                dst.writestr(zipfile.ZipInfo(name), data)
            else:
                dst.writestr(name, data)

    print("replaced cards:", " ".join(f"{b:02d}" for b in sorted(replaced)))


def title_left(card_xml):
    m = re.search(rb'<label slot="titleleft">(.*?)</label>', card_xml, re.S)
    return m.group(1) if m else b""


def title_normalized_matches(generated, template):
    """loose comparison of plain-text titles: whitespace, punctuation (apostrophes,
    dashes, the ampersand) and tag markup are stripped first"""
    return _alnum(title_left(generated)) == _alnum(title_left(template))


def _alnum(raw):
    txt = re.sub(rb"<[^>]*>", b"", raw).replace(b"&amp;", b"&")
    return re.sub(rb"[^A-Za-z0-9]+", b"", txt).lower()


def normalize(data):
    """keeps entries structurally valid (bom etc.) if a template is unavailable"""
    text = data.decode("utf-8-sig") if data.startswith(b"\xef\xbb\xbf") else data.decode("utf-8")
    if not text.endswith("\n"):
        text += "\n"
    return text.encode("utf-8")


if __name__ == "__main__":
    main(sys.argv)