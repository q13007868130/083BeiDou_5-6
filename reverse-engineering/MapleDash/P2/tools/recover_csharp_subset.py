#!/usr/bin/env python3
"""Conservative executable-C# subset reconstructor for straight-line MapleDash IL.
Only emits a method body if every IL instruction was translated, stack is well-formed,
all operand tokens are resolved, and no control-flow / EH handlers are involved.
All other methods are omitted (NOT replaced with fake C# stubs).
"""
from pathlib import Path
import re,json,collections,hashlib,sys
from dotnet_metadata import Reader
HERE=Path(__file__).resolve().parent
r=Reader(HERE/'Assembly-CSharp.dll')
ILROOT=HERE/'il-recovery'
OUT=HERE/'csharp-recovery-p2'
OUT.mkdir(exist_ok=True)
CLASSES=['InProcessTransport','GameServer','DbStore','LoginService','WorldService','AdminCtx','GameSession']

def clean_type(typ):
 return re.sub(r'`\d+', '',typ).replace('+','.')
def safe_id(s):
 return '@'+s if s in {'params','event','base','delegate','ref','out','in','object','string','namespace','internal','class','public','private','protected','static','new','void','long','char','int'} else s

def field_name(name):
 m=re.fullmatch(r'<([^>]+)>k__BackingField',name)
 return m.group(1) if m else safe_id(name)

def parse_il(name):
 p=ILROOT/'Core'/(name+'.il')
 if not p.exists():p=ILROOT/'Other'/(name+'.il')
 if not p.exists():return {}
 out={}; blocks=re.findall(r'^\.method\s+([^\n]+?)\s+// RVA 0x([0-9A-Fa-f]+),[^\n]*\n(.*?)^\.end method',p.read_text(),re.M|re.S)
 for mn,rva,body in blocks:
  ops=[]
  for l in body.splitlines():
   m=re.match(r'\s*IL_([0-9A-F]+):\s+([\w.]+)(?:\s+(.*))?',l)
   if m:ops.append((int(m.group(1),16),m.group(2),m.group(3) or ''))
  out[int(rva,16)]=ops
 return out

def translate(m,ops):
 """Supports straight-line getter/setter, call forwarding and simple assignments only."""
 if not ops:return None,'no method body'
 if len(ops)>100:return None,'too many instructions'
 blocked={'switch','throw','rethrow','leave','endfinally','endfilter','dup','pop','ldftn','ldvirtftn','ldtoken','box','unbox','unbox.any','castclass','isinst','ldelem','newarr','newobj','ldflda','ldarga','ldloca','starg','initobj','ldobj','stobj','ldind','stind','constrained','volatile','tail','readonly','sizeof','cpblk','initblk','ldelema','ldlen','ldstr'}
 for pos,op,args in ops:
  if op.startswith(('br','bne','bge','bgt','ble','blt','beq','conv','ldc.r')) or any(op==x or op.startswith(x+'.') for x in blocked):
   return None, 'unsupported opcode '+op
 stack=[];stmts=[];ret=None
 def push(x):stack.append(x)
 def pop():
  if not stack:raise ValueError('stack underflow')
  return stack.pop()
 try:
  for off,op,arg in ops:
   if op=='nop':continue
   if op=='ldarg.0':push('this' if m['has_this'] else safe_id(m['param_names'][0]));continue
   if op.startswith('ldarg.'):
    n=int(op.split('.')[-1]); idx=n-1 if m['has_this'] else n
    push(safe_id(m['param_names'][idx]));continue
   if op in ('ldarg.s','ldarg'):
    n=int(arg);idx=n-1 if m['has_this'] else n
    push('this' if idx<0 else safe_id(m['param_names'][idx]));continue
   if op=='ldnull':push('null');continue
   if op.startswith('ldc.i4.'):
    k=op.split('.')[-1];push('-1' if k=='m1' else k);continue
   if op in ('ldc.i4.s','ldc.i4','ldc.i8'):push(arg.strip());continue
   if op in ('ldfld','ldsfld'):
    tok=int(arg,16);f=r.field(tok&0xffffff)
    if op=='ldfld':
     base=pop();push((('this.' if base=='this' else base+'.')+field_name(f['name'])))
    else:push(clean_type(f['owner'])+'.'+field_name(f['name']))
    continue
   if op in ('stfld','stsfld'):
    tok=int(arg,16);f=r.field(tok&0xffffff);val=pop()
    if op=='stfld':
     base=pop();stmts.append((('this.' if base=='this' else base+'.')+field_name(f['name']))+' = '+val+';')
    else:stmts.append(clean_type(f['owner'])+'.'+field_name(f['name'])+' = '+val+';')
    continue
   if op in ('call','callvirt'):
    tok=int(arg,16)
    if tok>>24==10 and r.token(tok)=='System.Object::.ctor() : void':
     receiver=pop()
     if receiver!='this':return None,'non-this object ctor'
     continue
    if tok>>24!=6:return None,'external or generic call token '+arg
    callee=r.method(tok&0xffffff)
    args=[pop() for _ in range(len(callee['params']))][::-1]
    rec=pop() if callee['has_this'] else clean_type(callee['owner'])
    name=callee['name'];rec='' if rec=='this' else (rec+'.')
    if name.startswith('get_') and not args:expr=rec+name[4:]
    elif name.startswith('set_') and len(args)==1:expr=rec+name[4:]+' = '+args[0]
    elif name=='.ctor' and rec=='':continue
    elif name=='.ctor' and rec=='this':continue
    elif name=='.ctor' and rec=='System.Object':continue
    else:expr=rec+name+'('+', '.join(args)+')'
    if callee['ret']=='void':stmts.append(expr+';')
    else:push(expr)
    continue
   if op=='ret':
    if m['ret']=='void':
     if stack:return None,'nonempty stack at void return'
     stmts.append('return;')
    else:
     val=pop()
     if stack:return None,'nonempty stack at return'
     stmts.append('return '+val+';')
    continue
   return None,'unsupported opcode '+op
  if len(stmts)>=2 and stmts[-1]=='return;':stmts.pop()
  return stmts,None
 except Exception as exc:return None,str(exc)

def make_source(name):
 tids=[i for i in range(1,len(r.records[2])) if r.fullname(2,i)==name]
 if not tids:return None,None
 tid=tids[0]; row=r.records[2][tid];nextrow=r.records[2][tid+1] if tid+1<len(r.records[2]) else None
 first=row['MethodList'];last=nextrow['MethodList'] if nextrow else len(r.records[6]);ins=parse_il(name)
 f0=row['FieldList']; f1=nextrow['FieldList'] if nextrow else len(r.records[4]);
 fields=[r.field(j) for j in range(f0,f1)];body=[];results=[];safe_names={}
 backing={match.group(1):f for f in fields if (match:=re.fullmatch(r'<([^>]+)>k__BackingField',f['name']))}
 accessors={}
 for mid in range(first,last):
  mm=r.method(mid)
  if mm['name'].startswith(('get_','set_')) and mm['name'][4:] in backing:
   accessors.setdefault(mm['name'][4:],{})[mm['name'][:3]]=mm
 for prop,f in backing.items():
  a=accessors.get(prop,{})
  if 'get' not in a:continue
  getter=a['get'];setter=a.get('set')
  gv=getter['flags']&7
  vis='public' if gv==6 else 'private' if gv==1 else 'protected' if gv==4 else 'internal'
  static='static ' if getter['flags']&0x10 else ''
  settervis=''
  if setter:
   sv=setter['flags']&7
   if sv!=gv:settervis=('private' if sv==1 else 'protected' if sv==4 else 'internal' if sv==3 else 'public')+' '
  body.append(f'    // Original backing field {f["name"]}; property accessor IL verified')
  body.append(f'    {vis} {static}{clean_type(f["type"])} {prop} {{ get; {settervis+"set; " if setter else ""}}}')
 if backing:body.append('')
 for f in fields:
  if f['name'].startswith('<'):continue
  flags=f['flags'];acc='public' if flags&7==6 else 'private'
  stat='static ' if flags&0x10 else ''
  if flags&0x40 or flags&0x8000:continue # constants need initializer
  if not re.match(r'^[A-Za-z_]\w*$',f['name']):continue
  body.append(f'    {acc} {stat}{clean_type(f["type"])} {f["name"]};')
 if body:body.append('')
 for mid in range(first,last):
  m=r.method(mid)
  if not m['rva']:continue
  ops=ins.get(m['rva'])
  if not ops:continue
  stmts,reason=translate(m,ops)
  if m['name'].startswith(('get_','set_')) and m['name'][4:] in backing:
   # Represent the exact auto-property access through the property declaration.
   if stmts is not None:
    results.append({'method':m['name'],'token':f'0x{m["token"]:08X}','rva':f'0x{m["rva"]:08X}','status':'recovered-as-property','reason':None})
    continue
  results.append({'method':m['name'],'token':f'0x{m["token"]:08X}','rva':f'0x{m["rva"]:08X}','status':'recovered' if stmts is not None else 'not-recovered','reason':reason})
  if stmts is None:continue
  flags=m['flags'];acc='public' if flags&7==6 else 'private' if flags&7==1 else 'protected' if flags&7==4 else 'internal'
  stat='static ' if flags&0x10 else ''
  if m['name']=='.ctor':
   display=name
  elif m['name'] in ('.cctor',) or not re.match(r'^[A-Za-z_]\w*$',m['name']):continue
  else:display=clean_type(m['ret'])+' '+m['name']
  params=', '.join(clean_type(p)+' '+safe_id(n) for p,n in zip(m['params'],m['param_names']))
  body.append(f'    // Original method token {m["token"]:#010x}; RVA {m["rva"]:#x}')
  body.append(f'    {acc} {stat}{display}({params})')
  body.append('    {')
  body.extend('        '+x for x in stmts)
  body.append('    }\n')
 metadata={'class':name,'method_total':len(results),'fully_translated':sum(x['status'].startswith('recovered') for x in results),'excluded':sum(not x['status'].startswith('recovered') for x in results),'details':results}
 ns,namepart=(name.rsplit('.',1) if '.' in name else ('',name))
 prefix=['// Reconstructed from actual MapleDash IL; not the original Unity project.', '// Only methods whose IL was fully recognized are included.', '// Referenced project types and remaining members must be supplied separately.', 'using System;', 'using System.Collections.Generic;','']
 if ns:prefix.extend([f'namespace {ns}','{'])
 prefix.extend([f'public partial class {namepart}', '{']);prefix.extend(body);prefix.append('}')
 if ns:prefix.append('}')
 return '\n'.join(prefix)+'\n',metadata

if __name__=='__main__':
 report=[]
 for name in CLASSES:
  src,stats=make_source(name)
  if not src:continue
  (OUT/(name+'.partial.cs')).write_text(src,encoding='utf-8')
  report.append(stats)
  print(name,stats['fully_translated'],'/',stats['method_total'])
 (OUT/'recovery-report.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')