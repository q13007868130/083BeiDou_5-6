#!/usr/bin/env python3
"""Reproducible, bounded static audit of full ILSpy output; no IL interpretation."""
from pathlib import Path
import argparse,json,re

p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
a=p.parse_args();files=list(a.source.rglob('*.cs'));contents={f.relative_to(a.source).as_posix():f.read_text(errors='replace') for f in files}
needle={'unity_engine':r'\bUnityEngine\b','mono_behaviour':r'\bMonoBehaviour\b','transport_interface':r'\bITransport\b','network_sockets':r'\bTcpListener\b','game_server':r'\bGameServer\b'}
report={'source_files':len(files),'direct_references':{k:{'file_count':sum(bool(re.search(p,v)) for v in contents.values()),'files':sorted(k2 for k2,v in contents.items() if re.search(p,v))} for k,p in needle.items()},'deployment':'Not a network-ready independent server: in-process transport and ID-supplied APIs require authenticated remote session binding','reviewed_risks':[
{'id':'AUTH-001','source':'LoginService.cs','detail':'admin account bypassed password check in original decompilation','fix':'Hardened candidate requires password for all accounts'},
{'id':'AUTH-002','source':'AccountsDao.cs','detail':'admin name auto-granted GM level 6','fix':'Hardened candidate removes name-based GM privilege'},
{'id':'AUTH-003','source':'AdminBackend.cs','detail':'first-run seed admin/admin password and credential exposed in status output','fix':'Hardened candidate requires 16+ char bootstrap password from environment'},
{'id':'AUTH-004','source':'InProcessTransport.cs','detail':'CharList, CreateChar, CharSelect and other requests trust caller supplied account/character IDs','fix':'Remote service must bind these operations to a validated session and verify ownership'},
{'id':'NET-001','source':'GameServer.cs','detail':'GameServer.Start creates InProcessTransport and static Local singleton','fix':'Refactor into platform-neutral server and authenticated transport'},
{'id':'NET-002','source':'AdminHttpServer.cs','detail':'HTTP server listens on IPAddress.Loopback only','fix':'Do not expose admin HTTP endpoint to public network'}]}
a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8'); print('Audited',len(files),'C# files; results at',a.output)