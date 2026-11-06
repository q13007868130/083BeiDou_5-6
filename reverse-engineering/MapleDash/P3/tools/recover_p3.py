#!/usr/bin/env python3
"""MapleDash P3: conservative IL-to-C# reconstruction for whole straight-line methods.

Not an ILSpy replacement. Rejects branches, EH, unknown/generic operands rather than
manufacturing stubs. Calls, field mutations and object construction are emitted only
when token signatures are decoded and evaluation stack is balanced.
"""
from __future__ import annotations
import json, re, hashlib, collections, pathlib, sys, math
from pathlib import Path

HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE/'MapleDash-CSharp-Recovery-P2'))
from dotnet_metadata import Reader, Sig, uncompress

ASSEMBLY=HERE/'MapleDash-CSharp-Recovery-P2'/'Assembly-CSharp.dll'
ILROOT=HERE/'il-recovery'
OUT=HERE/'P3'/'source'
OUT.mkdir(parents=True,exist_ok=True)
r=Reader(ASSEMBLY)
P2_REPORT=HERE/'MapleDash-CSharp-Recovery-P2'/'recovery-report.json'
BASELINE={row['class']:set(m['token'].upper() for m in row['details'] if m['status'].startswith('recovered')) for row in json.loads(P2_REPORT.read_text())}
CLASSES=['InProcessTransport','GameServer','DbStore','LoginService','WorldService','AdminCtx','GameSession',
         'AdminApi','AdminBackend','AdminHttpServer','AdminHttpRequest','AdminHttpResponse','AdminDbUtil',
         'AccountsDao','CharactersDao','AdminAccount','AdminHost','AdminJson','AdminLive','AdminMainThread',
         'Server.Bot.BotWorkPump','Server.Bot.BotMovementPlayback','Server.Bot.BotAttack','Server.Bot.BotActor']
# Excluding compiler generated and transparent UI classes by explicit allowlist.
CSharpKeywords={'params','event','base','delegate','ref','out','in','object','string','namespace','internal','class','public','private','protected','static','new','void','long','char','int','default','bool','float','double','var','return','this','async','await','as','is','null','true','false','value','operator','lock','record','required'}
Id=r'^[A-Za-z_][A-Za-z0-9_]*$'

def name_id(s):
 if not re.match(Id,s):raise ValueError('invalid identifier: '+str(s))
 return '@'+s if s in CSharpKeywords else s

def typ(s):
 s=s.replace('+','.')
 # Unbound method generics/generic placeholders => skip so signatures are not invented
 if re.search(r'!\d',s) or re.search(r'\bTypeToken_',s):raise ValueError('unresolved type: '+s)
 return re.sub(r'`\d+','',s)

def fref(token):
 if (token>>24)!=4:raise ValueError('not field token '+hex(token))
 f=r.field(token&0xffffff)
 name=f['name']
 match=re.fullmatch(r'<([^>]+)>k__BackingField',name)
 return f, name_id(match.group(1) if match else name_id(name))

def adapt(value, expected):
 # CLR bool and enum constants are encoded with ldc.i4; C# requires typed syntax.
 expected=typ(expected)
 if expected in ('bool','System.Boolean'):
  if value in ('0','1'):return ('false' if value=='0' else 'true')
 if expected in ('char','System.Char') and re.fullmatch(r'-?\d+',value):
  return f'(char){value}'
 if expected in ('System.StringComparison','System.StringSplitOptions','System.DateTimeKind'):
  if re.fullmatch(r'-?\d+',value):return f'({expected}){value}'
 return value

def user_string(tok):
 if tok>>24!=0x70:raise ValueError('not user string token '+hex(tok))
 heap=r.streams.get('#US',b'')
 p=tok&0xffffff
 size,pos=uncompress(heap,p)
 seq=heap[pos:pos+size]
 if len(seq)>0:seq=seq[:-1]  # trailing special-character byte
 return json.dumps(seq.decode('utf-16-le','surrogatepass'),ensure_ascii=True)

def member(token):
 tab,n=token>>24,token&0xffffff
 if tab==6:
  m=r.method(n)
  return dict(owner=typ(m['owner']),name=m['name'],ret=typ(m['ret']),params=[typ(x) for x in m['params']],has_this=m['has_this'])
 if tab==10:
  row=r.records[10][n];cls=r.coded_token(row['Class'],'MemberRefParent');parent=cls>>24
  if parent not in (1,2,27):raise ValueError('unresolved MemberRef owner '+hex(cls))
  owner=typ(r.typetoken(cls));b=r.blob(row['Signature'])
  if not b or b[0]==6:raise ValueError('MemberRef field not callable')
  md=Sig(r).method(b)
  return dict(owner=owner,name=r.string(row['Name']),ret=typ(md['ret']),params=[typ(x) for x in md['params']],has_this=md['has_this'])
 raise ValueError('unsupported call token '+hex(token))

def fetch_il(cls):
 p=(ILROOT/('Server' if cls.startswith('Server.') else 'Core')/((cls.removeprefix('Server.'))+'.il'))
 if not p.exists():p=ILROOT/'Other'/(cls+'.il')
 if not p.exists():return {}
 s=p.read_text(encoding='utf-8')
 blocks=re.findall(r'^\.method\s+([^\n]+?)\s+// RVA 0x([0-9A-Fa-f]+),\s+code (\d+) bytes, maxstack (\d+), localSig 0x([0-9A-Fa-f]+)\n(.*?)^\.end method',s,re.M|re.S)
 out={}
 for name,rva,n,maxstack,local,body in blocks:
  ops=[]
  for line in body.splitlines():
   m=re.match(r'\s*IL_([0-9A-Fa-f]+):\s+([A-Za-z0-9._]+)(?:\s+(.*))?',line)
   if m:ops.append((int(m[1],16),m[2],(m[3] or '').strip()))
  out[int(rva,16)]=dict(name=name,code_len=int(n),maxstack=int(maxstack),localsig=int(local,16),ops=ops)
 return out

class Fail(ValueError):pass

def conv(m,il):
 ops=il['ops'];s=[];stack=[];localtypes=[];localnames={}
 if not ops:raise Fail('no IL body')
 if len(ops)>350:raise Fail('complex method >350 IL instructions')
 # avoid emitting a method that has an exception table (requires EH region parse)
 # marked with 'MoreSects' in fat header flags, bit 0x08.
 off=r.off(m['rva']);flags=r.get16(off)
 if flags&3==3 and flags&0x08:raise Fail('contains structured exception handling')
 if il['localsig']:
  if il['localsig']>>24!=17:raise Fail('unresolved local sig token')
  localtypes=[typ(x) for x in Sig(r).locals(r.blob(r.records[17][il['localsig']&0xffffff]['Signature']))]
 def pop():
  if not stack:raise Fail('IL stack underflow')
  return stack.pop()
 def push(x):stack.append(x)
 def arg(i):
  if m['has_this']:
   if i==0:return 'this'
   i-=1
  if i<0 or i>=len(m['params']):raise Fail('invalid param index '+str(i))
  return name_id(m['param_names'][i])
 def local(i):
  if i<0 or i>=len(localtypes):raise Fail('invalid local index '+str(i))
  return 'V'+str(i)
 def unsafe_expr(x):
  return '('+x+')' if (' ' in x or x.startswith('new ')) else x
 def place(x):
  if x=='this':return 'this.'
  return unsafe_expr(x)+'.'
 # Collapse a verified dup/brtrue/pop/literal idiom into a null coalescing expression.
 # This is a local CFG proof: target must be the immediate continuation.
 pre=[];j=0
 while j<len(ops):
  if j+4<len(ops):
   a,b,c,d,n=ops[j:j+5]
   if (a[1]=='dup' and b[1] in ('brtrue','brtrue.s') and c[1]=='pop'
       and d[1] in ('ldstr','ldnull') and b[2].upper()==f'IL_{n[0]:04X}'):
    pre.append((a[0],'coalesce.literal',d[2] if d[1]=='ldstr' else 'null'))
    j+=4;continue
  pre.append(ops[j]);j+=1
 ops=pre
 for off,op,operand in ops:
  if op in ('nop',):continue
  if op in ('volatile.','readonly.','tail.','constrained.','unaligned.'):
   raise Fail('prefix '+op)
  if op in ('ldarg.0','ldarg.1','ldarg.2','ldarg.3'):push(arg(int(op.split('.')[-1])));continue
  if op in ('ldarg.s','ldarg'):push(arg(int(operand)));continue
  if op in ('ldloc.0','ldloc.1','ldloc.2','ldloc.3'):push(local(int(op.split('.')[-1])));continue
  if op in ('ldloc.s','ldloc'):push(local(int(operand)));continue
  if op in ('stloc.0','stloc.1','stloc.2','stloc.3'):s.append(f'{local(int(op.split(".")[-1]))} = {adapt(pop(), localtypes[int(op.split(".")[-1])])};');continue
  if op in ('stloc.s','stloc'):s.append(f'{local(int(operand))} = {adapt(pop(), localtypes[int(operand)])};');continue
  if op=='ldnull':push('null');continue
  if op=='coalesce.literal':
   prior=pop();push('('+prior+' ?? '+operand+')');continue
  if op=='ldstr':
   if not (operand.startswith('"') and operand.endswith('"')):raise Fail('truncated/invalid string')
   push(operand);continue
  if op in ('ldc.i4.s','ldc.i4'):push(operand);continue
  if op.startswith('ldc.i4.'):
   val=op.split('.')[-1];push('-1' if val=='m1' else val);continue
  if op=='ldc.i8':push(operand+'L');continue
  if op.startswith('conv.'):
   target={'conv.i4':'int','conv.i8':'long','conv.u4':'uint','conv.u8':'ulong',
           'conv.r4':'float','conv.r8':'double','conv.u1':'byte','conv.i1':'sbyte',
           'conv.u2':'ushort','conv.i2':'short','conv.i':'IntPtr','conv.u':'UIntPtr'}.get(op)
   if not target:raise Fail('nonstandard conversion '+op)
   old=pop();push(f'(({target})({old}))');continue
  if op=='ldlen':
   arr=pop();push('('+arr+'.Length)');continue
  if op in ('ldc.r4','ldc.r8'):
   if operand in ('nan','inf','-inf'):raise Fail('non-finite constant')
   push(operand+('f' if op=='ldc.r4' else 'd'));continue
  if op in ('ldfld','ldsfld'):
   f,n=fref(int(operand,16))
   if op=='ldfld':push(place(pop())+n)
   else:push(typ(f['owner'])+'.'+n)
   continue
  if op in ('stfld','stsfld'):
   f,n=fref(int(operand,16));value=adapt(pop(),f['type'])
   if op=='stfld':s.append(f'{place(pop())}{n} = {value};')
   else:s.append(f'{typ(f["owner"])}.{n} = {value};')
   continue
  if op=='dup':
   a=pop()
   # expressions with evaluation side-effects must be materialized before dup
   if '(' in a or a.startswith('new '):
    # Use explicit temp only if the type can be inferred from a new expression; others skipped
    if a.startswith('new '):
     t='dup'+str(off);s.append('var '+t+' = '+a+';');a=t
    else:raise Fail('dup of evaluated call expression')
   push(a);push(a);continue
  if op=='pop':
   x=pop()
   # Avoid losing side-effect expressions, or incorrect unused return treatment
   if '(' in x or x.startswith('new '):s.append(x+';')
   continue
  if op in ('castclass','isinst','box','unbox.any'):
   x=pop();t=typ(r.typetoken(int(operand,16)))
   if op=='isinst':
    if t in ('int','bool','long','float','double','byte','short','uint','ulong','System.Int32','System.Boolean','System.Int64'):
     raise Fail('isinst on value type: '+t)
    push('('+x+' as '+t+')');continue
   if op=='box':push('((object)('+x+'))');continue
   push('('+t+')'+unsafe_expr(x));continue
  if op=='newarr':
   elem=typ(r.typetoken(int(operand,16)));length=pop();push('new '+elem+'['+length+']');continue
  if op in ('ldelem.ref','ldelem.i4','ldelem.u4'):
   index=pop();arr=pop();push(arr+'['+index+']');continue
  if op in ('stelem.ref','stelem.i4'):
   value=pop();index=pop();arr=pop();s.append(arr+'['+index+'] = '+value+';');continue
  if op in ('add','sub','mul','div','rem','and','or','xor','shl','shr'):
   rhs=pop();lhs=pop();sym={'add':'+','sub':'-','mul':'*','div':'/','rem':'%','and':'&','or':'|','xor':'^','shl':'<<','shr':'>>'}[op]
   push('('+lhs+' '+sym+' '+rhs+')');continue
  if op in ('ceq','cgt','clt','cgt.un'):
   rhs=pop();lhs=pop()
   boolargs={name_id(n) for p,n in zip(m['params'],m['param_names']) if p in ('bool','System.Boolean')}
   if op=='ceq' and lhs in boolargs and rhs in ('0','1'):
    push('(!'+lhs+')' if rhs=='0' else lhs);continue
   if op=='ceq' and rhs in boolargs and lhs in ('0','1'):
    push('(!'+rhs+')' if lhs=='0' else rhs);continue
   if op=='cgt.un':
    if rhs=='null':push('('+lhs+' != null)');continue
    raise Fail('unsigned cgt without null comparison')
   sym={'ceq':'==','cgt':'>','clt':'<'}[op]
   push('('+lhs+' '+sym+' '+rhs+')');continue
  if op in ('neg','not'):
   x=pop();push(('~' if op=='not' else '-')+unsafe_expr(x));continue
  if op in ('call','callvirt','newobj'):
   c=member(int(operand,16));params=[adapt(pop(),p) for p in c['params'][::-1]][::-1]
   if op=='newobj':
    if c['name']!='.ctor':raise Fail('newobj not constructor')
    push('new '+c['owner']+'('+', '.join(params)+')');continue
   if c['name']=='.ctor':
    recv=pop() if c['has_this'] else ''
    if recv=='this' and c['owner']=='System.Object':continue
    raise Fail('explicit constructor chaining unsupported')
   recv=pop() if c['has_this'] else c['owner']
   if c['name'].startswith('get_'):
    n=c['name'][4:]
    if n=='Item':expr=recv+'['+', '.join(params)+']'
    elif not params:expr=place(recv)+name_id(n)
    else:raise Fail('non-indexed property getter has args')
   elif c['name'].startswith('set_'):
    n=c['name'][4:]
    if n=='Item' and len(params)>=2:expr=recv+'['+', '.join(params[:-1])+'] = '+params[-1]
    elif len(params)==1:expr=place(recv)+name_id(n)+' = '+params[0]
    else:raise Fail('setter signature ambiguous')
   elif re.match(Id,c['name']):expr=place(recv)+name_id(c['name'])+'('+', '.join(params)+')'
   else:raise Fail('unsupported method name '+c['name'])
   if c['ret']=='void':s.append(expr+';')
   else:push(expr)
   continue
  if op=='ret':
   if m['ret']=='void':
    if stack:raise Fail('nonempty stack at void return')
    s.append('return;')
   else:
    x=pop()
    if stack:raise Fail('nonempty stack at return')
    s.append('return '+adapt(x,m['ret'])+';')
   continue
  raise Fail('unsupported opcode '+op)
 if not s or s[-1].split()[0] not in ('return;','return'):
  raise Fail('IL method does not end in ret')
 if len(s)>1 and s[-1]=='return;':s.pop()
 # All locals declared first, C# zero-initialization mandated by original CLI init locals.
 decl=[f'{x} V{i} = default({x});' for i,x in enumerate(localtypes)]
 return decl+s

def generate(cls):
 il=fetch_il(cls)
 if not il:return None,None
 tids=[i for i in range(1,len(r.records[2])) if r.fullname(2,i)==cls]
 if len(tids)!=1:return None,None
 tid=tids[0];row=r.records[2][tid];nextrow=r.records[2][tid+1] if tid+1<len(r.records[2]) else None
 first=row['MethodList'];last=nextrow['MethodList'] if nextrow else len(r.records[6])
 methods=[];body=[]
 for mid in range(first,last):
  m=r.method(mid)
  if not m['rva']:continue
  data=il.get(m['rva']);
  if not data:continue
  status='recovered';reason=None
  try:
   if m['name'] in ('.cctor',) or (m['name']!='.ctor' and not re.match(Id,m['name'])):raise Fail('special method skipped')
   if m['name'].startswith(('get_','set_')):raise Fail('property accessors omitted to avoid incomplete declarations')
   if m['flags'] & 0x0010 and m['name']=='.ctor':raise Fail('static ctor not supported')
   emitted=conv(m,data)
  except Exception as e:
   emitted=[];status='skipped';reason=str(e)
  is_base=(f'0x{m["token"]:08X}'.upper() in BASELINE.get(cls,set()))
  methods.append(dict(method=m['name'],token=f'0x{m["token"]:08X}',rva=f'0x{m["rva"]:08X}',status='already-in-P2' if status=='recovered' and is_base else status,reason=reason))
  if status!='recovered' or is_base:continue
  flags=m['flags'];visibility={1:'private',2:'private protected',3:'internal',4:'protected',5:'protected internal',6:'public'}.get(flags&7,'private')
  static='static ' if flags&0x10 else ''
  try:
   if m['name']=='.ctor':display=cls.rsplit('.',1)[-1]
   else:display=typ(m['ret'])+' '+name_id(m['name'])
   signature=', '.join(typ(p)+' '+name_id(n) for p,n in zip(m['params'],m['param_names']))
  except Exception as e:
   methods[-1].update(status='skipped',reason='invalid signature: '+str(e));continue
  body.extend([f'    // IL token 0x{m["token"]:08X} RVA 0x{m["rva"]:08X}',
               f'    {visibility} {static}{display}({signature})','    {'])
  body.extend('        '+x for x in emitted)
  body.extend(['    }',''])
 ns,cname=cls.rsplit('.',1) if '.' in cls else ('',cls)
 header=['// MapleDash P3: conservative C# recovery from compiled Mono IL.',
         '// Generated only for wholly handled straight-line method bodies.',
         '// This is NOT a complete project, class, or exact author-authored source.',
         '// Missing fields, properties and referenced types remain dependencies.',
         'using System;','using System.Collections.Generic;','']
 if ns:header +=[f'namespace {ns}','{']
 header +=[f'public partial class {cname}','{']
 header +=body
 header+=['}']
 if ns:header+=['}']
 source='\n'.join(header)+'\n'
 return source, dict(class_name=cls,checked=len(methods),newly_recovered=sum(m['status']=='recovered' for m in methods),already_in_p2=sum(m['status']=='already-in-P2' for m in methods),skipped=sum(m['status']=='skipped' for m in methods),methods=methods)

if __name__=='__main__':
 stats=[]
 for cls in CLASSES:
  source,report=generate(cls)
  if not report:continue
  stats.append(report)
  if report['newly_recovered']>0:
   path=OUT/(cls.replace('.','_')+'.partial.cs')
   path.write_text(source,encoding='utf-8')
  print(f'{cls:45s} recovered={report["newly_recovered"]:3d} total={report["checked"]:4d}')
 dst=HERE/'P3'/'report.json'
 dst.write_text(json.dumps({'assembly_sha256':hashlib.sha256(ASSEMBLY.read_bytes()).hexdigest(),'classes':stats},ensure_ascii=False,indent=2),encoding='utf-8')
 print('TOTAL',sum(x['newly_recovered'] for x in stats),'/',sum(x['checked'] for x in stats),'output',OUT)
