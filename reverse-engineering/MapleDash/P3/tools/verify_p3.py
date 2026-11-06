#!/usr/bin/env python3
"""Static verification checks for MapleDash P3 generated partial C# sources."""
from pathlib import Path
import sys,re,json,hashlib
BASE=Path(__file__).resolve().parent
sys.path.insert(0,str(BASE))
import recover_p3 as p3
report=json.loads((BASE/'P3'/'report.json').read_text())
assert hashlib.sha256(p3.ASSEMBLY.read_bytes()).hexdigest()==report['assembly_sha256']=='cfaa6d7f088a66259eb6a29c6e55dacfe2c6d3029c21d813daa7951d030c14a1'
classes=report['classes']
assert len(classes)==23,len(classes)
assert sum(r['checked'] for r in classes)==377
assert sum(r['newly_recovered'] for r in classes)==51
assert sum(r['already_in_p2'] for r in classes)+sum(r['newly_recovered'] for r in classes)+sum(r['skipped'] for r in classes)==377
allnew=set();allold=set()
for c in classes:
    cls=c['class_name']; source,expected=p3.generate(cls)
    assert expected==c,(cls,'generated metadata changed')
    target=BASE/'P3'/'source'/(cls.replace('.','_')+'.partial.cs')
    if c['newly_recovered']:
        assert target.is_file(),cls
        actual=target.read_text()
        assert actual==source,(cls,'source does not match regeneration')
        assert 'NotImplementedException' not in actual
        assert actual.count('{')==actual.count('}')
        assert not re.search(r'= s;',actual), (cls,'short integer constant mistranslated')
        for m in c['methods']:
            if m['status']=='recovered':
                allnew.add(m['token'])
                assert m['token'] in source, (cls,m['token'])
    for m in c['methods']:
        if m['status']=='already-in-P2':allold.add(m['token'])
assert not (allold&allnew)
assert 'StringComparison)5' in (BASE/'P3/source/LoginService.partial.cs').read_text()
assert 'this.server.Db' in (BASE/'P3/source/InProcessTransport.partial.cs').read_text()
assert 'SwitchForeground((!paused))' in (BASE/'P3/source/AdminHost.partial.cs').read_text()
assert 'Split((char)47, (System.StringSplitOptions)0)' in (BASE/'P3/source/AdminApi.partial.cs').read_text()
print('P3 verification passed: 23 types, 377 checked, 51 new C# methods; no P2 duplicate tokens')
