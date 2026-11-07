#!/usr/bin/env python3
"""Static assertions for a clean offline source preparation."""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(os.environ.get("MAPLEDASH_REPO_ROOT", Path(__file__).resolve().parents[5])).resolve()
PREFIX = ROOT / 'reverse-engineering/MapleDash'
VARIANT = PREFIX / 'variants/singleplayer'

class OfflineSourceTests(unittest.TestCase):
    def test_expected_local_path(self):
        src = PREFIX / 'full-source/DecompiledCSharp'
        server = (src / 'GameServer.cs').read_text(encoding='utf-8-sig')
        boot = (src / 'GameSession.cs').read_text(encoding='utf-8-sig')
        self.assertIn('srv.Transport = new InProcessTransport(srv);', server)
        self.assertIn('Server = GameServer.Start();', boot)
        self.assertIn('Net = Server.Transport;', boot)

    def test_produces_complete_copy_with_hardening(self):
        src = PREFIX / 'full-source/DecompiledCSharp'
        original_login = (src / 'LoginService.cs').read_bytes()
        with tempfile.TemporaryDirectory() as td:
            dest = Path(td) / 'offline'
            subprocess.run([sys.executable, str(VARIANT / 'scripts/build_offline_source.py'),
                            '--repo', str(ROOT), '--output', str(dest)], check=True)
            manifest = json.loads((dest / '_OFFLINE_BUILD_MANIFEST.json').read_text())
            self.assertGreaterEqual(manifest['source_file_count'], 900)
            self.assertEqual(manifest['transport'], 'InProcessTransport')
            self.assertIn('fallback: false', (dest / 'LoginService.cs').read_text())
            self.assertIn('MAPLEDASH_ADMIN_BOOTSTRAP_PASSWORD', (dest / 'AdminBackend.cs').read_text())
            self.assertEqual(original_login, (src / 'LoginService.cs').read_bytes())

if __name__ == '__main__':
    unittest.main()