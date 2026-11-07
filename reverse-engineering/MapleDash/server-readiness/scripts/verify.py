#!/usr/bin/env python3
"""Fail-closed integrity checks, distinct from C# runtime test."""
from pathlib import Path
import json,re
base=Path(__file__).resolve().parents[1]
login=(base/'hardening/LoginService.cs').read_text()
acct=(base/'hardening/AccountsDao.cs').read_text()
admin=(base/'hardening/AdminBackend.cs').read_text()
assert 'if (!AccountsDao.VerifyPassword(pwd, stored))' in login
assert 'if (!IsNoPasswordAccount(name) &&' not in login
assert 'automatic_register", fallback: false' in login
assert 'return 6;' not in re.search(r'public static int DefaultGmLevel\(.*?\n\t\}',acct,re.S).group(0)
assert 'AutoRegister(db, "admin", "admin")' not in admin
assert 'MAPLEDASH_ADMIN_BOOTSTRAP_PASSWORD' in admin
assert 'admin / admin' not in admin
j=json.loads((base/'docs/source-audit.json').read_text())
assert j['source_files']==1003
print('PASS: static hardening and source audit invariants')