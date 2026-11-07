#!/usr/bin/env python3
"""Recover uploaded Assembly-CSharp.dll from reproducible base64/XZ segments.
This script does not execute the input assembly.
"""
from __future__ import annotations
import base64,hashlib,json,lzma,sys
from pathlib import Path
root=Path(__file__).resolve().parent.parent
inp=root/'inputs'
meta=json.loads((inp/'input-metadata.json').read_text(encoding='utf-8'))
parts=sorted(inp.glob('part*.txt'))
if len(parts)!=meta['parts']:
    sys.exit(f'ERROR: expected {meta["parts"]} fragments, found {len(parts)}')
encoded=''.join(p.read_text(encoding='ascii').strip() for p in parts)
if len(encoded)!=meta['b64_chars']:
    sys.exit('ERROR: segment length differs from manifest')
xz=base64.b64decode(encoded, validate=True)
if hashlib.sha256(xz).hexdigest()!=meta['xz_sha256']:
    sys.exit('ERROR: compressed archive digest mismatch')
raw=lzma.decompress(xz)
if len(raw)!=meta['bytes'] or hashlib.sha256(raw).hexdigest()!=meta['original_sha256']:
    sys.exit('ERROR: original DLL checksum or length mismatch')
out=root/'tmp'/'Assembly-CSharp.dll'
out.parent.mkdir(parents=True,exist_ok=True)
out.write_bytes(raw)
print(f'Assembly verified: {out} ({len(raw)} bytes, sha256={meta["original_sha256"]})')
