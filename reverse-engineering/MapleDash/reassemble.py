#!/usr/bin/env python3
"""Reassemble and hash-verify the P1 recovered IL archive."""
from pathlib import Path
from hashlib import sha256
DIR=Path(__file__).resolve().parent
EXPECTED="9d333bdf7fd81f81fefd0fcfc1316d4d17b058c075ecdc9d82c64c49624c5de3"
parts=[DIR/f"MapleDash-IL-Recovery-P1.zip.part{i:02d}" for i in range(10)]
missing=[str(p) for p in parts if not p.is_file()]
if missing:
    raise SystemExit("Missing parts: "+", ".join(missing))
dst=DIR/"MapleDash-IL-Recovery-P1.zip"
h=sha256()
with dst.open("wb") as out:
    for part in parts:
        data=part.read_bytes()
        h.update(data)
        out.write(data)
actual=h.hexdigest()
if actual!=EXPECTED:
    dst.unlink(missing_ok=True)
    raise SystemExit(f"SHA-256 mismatch: {actual}, expected {EXPECTED}")
print(f"OK: {dst} (SHA-256 {actual})")
