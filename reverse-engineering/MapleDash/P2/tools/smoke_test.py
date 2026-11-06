#!/usr/bin/env python3
from pathlib import Path
import json, re
r=Path(__file__).resolve().parent
p=json.loads((r/'recovery-report.json').read_text('utf-8'))
expected={'InProcessTransport':(30,55),'GameServer':(26,37),'GameSession':(25,53),'AdminCtx':(6,10),'DbStore':(2,34),'LoginService':(1,5),'WorldService':(1,3)}
for row in p:
 name=row['class'];assert (row['fully_translated'],row['method_total'])==expected[name],name
 src=(r/(name+'.partial.cs')).read_text('utf-8')
 assert not re.search(r'\.<[A-Za-z]+>k__BackingField',src),name
 assert 'NotImplementedException' not in src,name
 assert src.count('{')==src.count('}'),name
 assert row['fully_translated'] + row['excluded']==row['method_total'],name
src=(r/'InProcessTransport.partial.cs').read_text('utf-8')
assert 'this.server = s;' in src
assert 'return this.server.ScriptValues.Handle(r);' in src
assert 'return this.server.Bots.tradeProtocol.Handle(r);' in src
assert 'return NativeInventoryService.Handle(this.server.Db, r);' in src
assert 'return this.server.Bosses.Handle(r);' in src
print('P2 smoke checks passed; recovered entries:',sum(v['fully_translated'] for v in p),'out of',sum(v['method_total'] for v in p))