#!/usr/bin/env python3
"""Summarize generated .cs output without treating decompilation as original source."""
from pathlib import Path
import datetime,json,sys,hashlib
root=Path(__file__).resolve().parent.parent
out=root/'DecompiledCSharp'
source=list(out.rglob('*.cs')) if out.is_dir() else []
if not source: sys.exit('ERROR: ILSpy did not produce C# source files')
info={
 'description':'Best-effort ILSpy decompilation of user-provided Unity Mono Assembly-CSharp.dll, not original Unity project.',
 'generated_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
 'method':'ICSharpCode ILSpy ilspycmd -p or -o',
 'source_file_count':len(source),
 'csharp_source_bytes':sum(x.stat().st_size for x in source),
 'line_count':sum(x.read_bytes().count(b'\n') for x in source),
 'file_extension_counts':{s:len(list(out.rglob('*'+s))) for s in ('.cs','.csproj')},
 'original_assembly_sha256':'cfaa6d7f088a66259eb6a29c6e55dacfe2c6d3029c21d813daa7951d030c14a1',
 'limitations':['Not the original author source; comments and original project metadata are lost','No full Unity Editor project or large game resource pack in this repository','Not compiled or multiplayer runtime tested']
}
(root/'decompilation-audit.json').write_text(json.dumps(info,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(info,ensure_ascii=False,indent=2))
