#!/usr/bin/env python3
"""Produce a separate hardened offline Unity C# source tree.

The ILSpy extraction remains immutable; Unity assets, scenes and editor are not
restored. This is SOURCE PREPARATION, not a playable game build.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import sys

REL = Path('reverse-engineering/MapleDash')
PATCHES = ('LoginService.cs', 'AccountsDao.cs', 'AdminBackend.cs')


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[5])
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    repo = args.repo.resolve()
    source = repo / REL / 'full-source/DecompiledCSharp'
    patch_dir = repo / REL / 'server-readiness/hardening'
    dest = args.output.resolve()
    if not (source / 'GameSession.cs').is_file() or not (source / 'ITransport.cs').is_file():
        raise SystemExit(f'Original decompilation not found: {source}')
    if source == dest or source in dest.parents or dest in source.parents:
        raise SystemExit('Output must be outside the original decompilation tree')
    if dest.exists():
        raise SystemExit('Output already exists; choose a new directory to avoid accidental overwrite')
    for name in PATCHES:
        if not (patch_dir / name).is_file():
            raise SystemExit(f'Missing reviewed auth patch: {patch_dir / name}')
    original_boot = (source / 'GameSession.cs').read_text(encoding='utf-8-sig')
    original_server = (source / 'GameServer.cs').read_text(encoding='utf-8-sig')
    assert 'Server = GameServer.Start();' in original_boot
    assert 'Net = Server.Transport;' in original_boot
    assert 'srv.Transport = new InProcessTransport(srv);' in original_server
    shutil.copytree(source, dest)
    for name in PATCHES:
        shutil.copy2(patch_dir / name, dest / name)
    assert 'return false;' in (dest / 'LoginService.cs').read_text(encoding='utf-8')
    assert 'MAPLEDASH_ADMIN_BOOTSTRAP_PASSWORD' in (dest / 'AdminBackend.cs').read_text(encoding='utf-8')
    records = {}
    for f in sorted(dest.rglob('*')):
        if f.is_file() and f.suffix in ('.cs', '.csproj'):
            records[str(f.relative_to(dest)).replace('\\', '/')] = hashlib.sha256(f.read_bytes()).hexdigest()
    if len([x for x in records if x.endswith('.cs')]) < 900:
        raise SystemExit('Decompilation unexpectedly incomplete')
    manifest = {'mode': 'offline-unity-source-preparation', 'originals_untouched': True,
                'transport': 'InProcessTransport', 'source_file_count': len(records),
                'hardening_overlays': list(PATCHES), 'files_sha256': records,
                'compile_verified': False, 'game_runtime_verified': False}
    (dest / '_OFFLINE_BUILD_MANIFEST.json').write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'Prepared {len(records)} source files under {dest} with local transport and auth patches.')
    print('NOT a compiled Unity player; original scenes/assets are still required.')

if __name__ == '__main__':
    main()