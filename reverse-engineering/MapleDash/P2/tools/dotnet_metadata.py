#!/usr/bin/env python3
"""Minimal ECMA-335 metadata parser for MapleDash CLI assembly.
Reads metadata tables, signatures and tokens WITHOUT executing the target.
It is not a high-level .NET decompiler; output signatures are verifiable from blobs.
"""
import struct,sys, pathlib, collections, json
class Reader:
 def __init__(self,p):
  self.file=pathlib.Path(p);self.data=self.file.read_bytes();b=self.data
  self.get16=lambda off:struct.unpack_from('<H',b,off)[0]
  self.get32=lambda off:struct.unpack_from('<I',b,off)[0]
  pe=self.get32(0x3c);sec=pe+24+self.get16(pe+20);self.sections=[]
  for i in range(self.get16(pe+6)):
   o=sec+i*40;vs,va,sz,pt=struct.unpack_from('<IIII',b,o+8)
   self.sections.append((va,max(vs,sz),pt))
  opt=pe+24;cli=self.get32(opt+(112 if self.get16(opt)==0x20b else 96)+14*8)
  cpos=self.off(cli);mdrva=self.get32(cpos+8);md=self.off(mdrva)
  assert b[md:md+4]==b'BSJB'
  verlen=self.get32(md+12);cur=md+16+verlen;cur=(cur+3)&~3;count=self.get16(cur+2);cur+=4
  self.streams={}
  for _ in range(count):
   off,sz=struct.unpack_from('<II',b,cur);cur+=8;start=cur;end=b.index(0,cur);name=b[cur:end].decode('ascii');cur=(end+4)&~3
   self.streams[name]=b[md+off:md+off+sz]
  self.strings=self.streams.get('#Strings',b'');self.blobs=self.streams.get('#Blob',b'');self.tables=self.streams.get('#~',self.streams.get('#-',b''))
  s=self.tables;self.heapflags=s[6];self.valid=struct.unpack_from('<Q',s,8)[0];i=24;self.rows={}
  for tab in range(64):
   if self.valid>>tab&1:self.rows[tab]=struct.unpack_from('<I',s,i)[0];i+=4
  self.data_start=i;self.schemas=schemas();self.offsets={};self.records={};self.coded=coded
  for tab in range(64):
   if tab not in self.rows:continue
   if tab not in self.schemas:raise ValueError(f'missing schema {tab}')
   self.offsets[tab]=i
   i+=self.row_size(tab)*self.rows[tab]
  for tab in self.rows:
   o=self.offsets[tab];cols=self.schemas[tab];self.records[tab]=[None]
   for ix in range(self.rows[tab]):
    entry={}
    for name,typ in cols:
     size=self.col_size(typ);v=int.from_bytes(s[o:o+size],'little');o+=size;entry[name]=v
    self.records[tab].append(entry)
  self._nested={row['NestedClass']:row['EnclosingClass'] for row in self.records.get(41,[None])[1:]}
  self._method_owner={};self._field_owner={}
  ty=self.records.get(2,[None]);meth=self.records.get(6,[None]);fields=self.records.get(4,[None]);
  for k in range(1,len(ty)):
   x=ty[k]; end=ty[k+1]['MethodList'] if k+1<len(ty) else len(meth)
   for m in range(x['MethodList'],end):self._method_owner[m]=k
   endf=ty[k+1]['FieldList'] if k+1<len(ty) else len(fields)
   for f in range(x['FieldList'],endf):self._field_owner[f]=k
 def off(self,rva):
  for va,l,ptr in self.sections:
   if va<=rva<va+l:return ptr+rva-va
  raise ValueError('rva '+hex(rva))
 def string(self,n):
  if n==0:return ''
  j=self.strings.find(b'\x00',n)
  return self.strings[n:j].decode('utf-8','replace')
 def blob(self,n):
  if n==0:return b''
  length,ofs=uncompress(self.blobs,n)
  return self.blobs[ofs:ofs+length]
 def col_size(self,typ):
  if typ=='u2':return 2
  if typ=='u4':return 4
  if typ=='str':return 4 if self.heapflags&1 else 2
  if typ=='guid':return 4 if self.heapflags&2 else 2
  if typ=='blob':return 4 if self.heapflags&4 else 2
  if typ.startswith('i'):return 4 if self.rows.get(int(typ[1:]),0)>=65536 else 2
  if typ.startswith('c'):
   a,b=coded[typ[1:]];return 4 if any(self.rows.get(t,0)>=1<<(16-b) for t in a if t is not None) else 2
  raise ValueError(typ)
 def row_size(self,tab):return sum(self.col_size(t) for _,t in self.schemas[tab])
 def fullname(self,tab,n):
  if not n:return 'void'
  row=self.records[tab][n];name=self.string(row['Name']);ns=self.string(row.get('Namespace',0));name=f'{ns}.{name}' if ns else name
  if tab==2 and n in self._nested:name=self.fullname(2,self._nested[n])+'+'+self.string(row['Name'])
  return name
 def typetoken(self,token):
  tab=(token>>24)&255;n=token&0xffffff
  if tab in (1,2):return self.fullname(tab,n)
  if tab==27:return Sig(self).type(memoryview(self.blob(self.records[27][n]['Signature'])),[0])
  return f'TypeToken_0x{token:08X}'
 def coded_token(self,cvalue,cname):
  arr,bits=coded[cname];tab=arr[cvalue&((1<<bits)-1)]
  if tab is None:return 0
  return (tab<<24)|(cvalue>>bits)
 def field(self,n):
  row=self.records[4][n];owner=self._field_owner.get(n);typesig=self.blob(row['Signature']);typ=Sig(self).field(typesig)
  return {'name':self.string(row['Name']),'type':typ,'owner':self.fullname(2,owner) if owner else None,'flags':row['Flags'],'token':0x04000000|n}
 def method(self,n):
  row=self.records[6][n];owner=self._method_owner.get(n);sig=Sig(self).method(self.blob(row['Signature']))
  paramlist=row['ParamList'];nxt=self.records[6][n+1]['ParamList'] if n+1<len(self.records[6]) else len(self.records.get(8,[None]));pnames={x['Sequence']:self.string(x['Name']) for x in self.records.get(8,[None])[paramlist:nxt] if x}
  return {'name':self.string(row['Name']),'owner':self.fullname(2,owner) if owner else None,'flags':row['Flags'],'rva':row['RVA'],'params':sig['params'],'ret':sig['ret'],'param_names':[pnames.get(i+1,f'arg{i+1}') or f'arg{i+1}' for i in range(len(sig['params']))],'has_this':sig['has_this'],'token':0x06000000|n}
 def token(self,t):
  tb=t>>24;n=t&0xffffff
  if tb==6:
   x=self.method(n);return f"{x['owner']}::{x['name']}({', '.join(x['params'])}) : {x['ret']}"
  if tb==4:
   x=self.field(n);return f"{x['owner']}::{x['name']} : {x['type']}"
  if tb in (1,2,27):return self.typetoken(t)
  if tb==10:
   x=self.records[10][n];owner=self.coded_token(x['Class'],'MemberRefParent');parent=self.typetoken(owner) if owner>>24 in (1,2,27) else self.token(owner) if owner>>24 in (6,26) else hex(owner)
   blob=self.blob(x['Signature']);s=Sig(self)
   if blob and blob[0]==6:
    return parent+'::'+self.string(x['Name'])+' : '+s.field(blob)
   try:
    sig=s.method(blob);return parent+'::'+self.string(x['Name'])+'('+', '.join(sig['params'])+') : '+sig['ret']
   except:return parent+'::'+self.string(x['Name'])
  if tb==26:return self.string(self.records[26][n]['Name'])
  if tb==17:return 'locals: '+str(Sig(self).locals(self.blob(self.records[17][n]['Signature'])))
  if tb==43:return 'MethodSpec 0x'+format(t,'08X')
  return f'0x{t:08X}'

coded={
 'TypeDefOrRef':([2,1,27],2), 'HasConstant':([4,8,23],2),'HasCustomAttribute':([6,4,1,2,8,9,10,0,14,23,20,17,26,27,32,35,38,39,40,42,44,43],5),
 'HasFieldMarshal':([4,8],1),'HasDeclSecurity':([2,6,32],2),'MemberRefParent':([2,1,26,6,27],3), 'HasSemantics':([20,23],1), 'MethodDefOrRef':([6,10],1),
 'MemberForwarded':([4,6],1), 'Implementation':([38,35,39],2),'CustomAttributeType':([None,None,6,10,None],3),'ResolutionScope':([0,26,35,1],2),'TypeOrMethodDef':([2,6],1)
}
def schemas():
 d={
 0:[('Generation','u2'),('Name','str'),('Mvid','guid'),('EncId','guid'),('EncBaseId','guid')],
 1:[('ResolutionScope','cResolutionScope'),('Name','str'),('Namespace','str')],
 2:[('Flags','u4'),('Name','str'),('Namespace','str'),('Extends','cTypeDefOrRef'),('FieldList','i4'),('MethodList','i6')],
 3:[('Field','i4')],4:[('Flags','u2'),('Name','str'),('Signature','blob')],5:[('Method','i6')],
 6:[('RVA','u4'),('ImplFlags','u2'),('Flags','u2'),('Name','str'),('Signature','blob'),('ParamList','i8')],7:[('Param','i8')],
 8:[('Flags','u2'),('Sequence','u2'),('Name','str')],
 9:[('Class','i2'),('Interface','cTypeDefOrRef')],
 10:[('Class','cMemberRefParent'),('Name','str'),('Signature','blob')],
 11:[('Type','u2'),('Parent','cHasConstant'),('Value','blob')],
 12:[('Parent','cHasCustomAttribute'),('Type','cCustomAttributeType'),('Value','blob')],
 13:[('Parent','cHasFieldMarshal'),('NativeType','blob')],
 14:[('Action','u2'),('Parent','cHasDeclSecurity'),('PermissionSet','blob')],
 15:[('PackingSize','u2'),('ClassSize','u4'),('Parent','i2')],
 16:[('Offset','u4'),('Field','i4')],17:[('Signature','blob')],
 18:[('Parent','i2'),('EventList','i20')],19:[('Event','i20')],
 20:[('EventFlags','u2'),('Name','str'),('EventType','cTypeDefOrRef')],
 21:[('Parent','i2'),('PropertyList','i23')],22:[('Property','i23')],
 23:[('Flags','u2'),('Name','str'),('Type','blob')],
 24:[('Semantics','u2'),('Method','i6'),('Association','cHasSemantics')],
 25:[('Class','i2'),('MethodBody','cMethodDefOrRef'),('MethodDeclaration','cMethodDefOrRef')],
 26:[('Name','str')],27:[('Signature','blob')],
 28:[('MappingFlags','u2'),('MemberForwarded','cMemberForwarded'),('ImportName','str'),('ImportScope','i26')],
 29:[('RVA','u4'),('Field','i4')],30:[('Token','u4'),('FuncCode','u4')],31:[('Token','u4')],
 32:[('HashAlgId','u4'),('Major','u2'),('Minor','u2'),('Build','u2'),('Revision','u2'),('Flags','u4'),('PublicKey','blob'),('Name','str'),('Culture','str')],
 33:[('Processor','u4')],34:[('OSPlatformId','u4'),('OSMajor','u4'),('OSMinor','u4')],
 35:[('Major','u2'),('Minor','u2'),('Build','u2'),('Revision','u2'),('Flags','u4'),('PublicKeyOrToken','blob'),('Name','str'),('Culture','str'),('HashValue','blob')],
 36:[('Processor','u4'),('AssemblyRef','i35')],37:[('OSPlatformId','u4'),('OSMajor','u4'),('OSMinor','u4'),('AssemblyRef','i35')],
 38:[('Flags','u4'),('Name','str'),('HashValue','blob')],
 39:[('Flags','u4'),('TypeDefId','u4'),('TypeName','str'),('TypeNamespace','str'),('Implementation','cImplementation')],
 40:[('Offset','u4'),('Flags','u4'),('Name','str'),('Implementation','cImplementation')],
 41:[('NestedClass','i2'),('EnclosingClass','i2')],
 42:[('Number','u2'),('Flags','u2'),('Owner','cTypeOrMethodDef'),('Name','str')],
 43:[('Method','cMethodDefOrRef'),('Instantiation','blob')],
 44:[('Owner','i42'),('Constraint','cTypeDefOrRef')]
 }
 return d

def uncompress(b,i):
 c=b[i];i+=1
 if c&0x80==0:return c,i
 if c&0xC0==0x80:return (c&0x3F)<<8|b[i],i+1
 return ((c&0x1F)<<24)|int.from_bytes(b[i:i+3],'big'),i+3
class Sig:
 primitives={0x01:'void',0x02:'bool',0x03:'char',0x04:'sbyte',0x05:'byte',0x06:'short',0x07:'ushort',0x08:'int',0x09:'uint',0x0a:'long',0x0b:'ulong',0x0c:'float',0x0d:'double',0x0e:'string',0x16:'TypedReference',0x18:'IntPtr',0x19:'UIntPtr',0x1c:'object'}
 def __init__(self,r):self.r=r
 def t(self,b,i):
  if i[0]>=len(b):return '?'
  kind=b[i[0]];i[0]+=1
  if kind in (0x1f,0x20):
   v,i[0]=uncompress(b,i[0]);return self.t(b,i) # modifier skip
  if kind in self.primitives:return self.primitives[kind]
  if kind==0x0f:return self.t(b,i)+'*'
  if kind==0x10:return 'ref '+self.t(b,i)
  if kind in (0x11,0x12):
   x,i[0]=uncompress(b,i[0]);tok=self.r.coded_token(x,'TypeDefOrRef');return self.r.typetoken(tok)
  if kind==0x13:
   v,i[0]=uncompress(b,i[0]);return f'T{v}'
  if kind==0x1e:
   v,i[0]=uncompress(b,i[0]);return f'M{v}'
  if kind==0x1d:return self.t(b,i)+'[]'
  if kind==0x15:
   _=b[i[0]];i[0]+=1;x,i[0]=uncompress(b,i[0]);tok=self.r.coded_token(x,'TypeDefOrRef');ct,i[0]=uncompress(b,i[0]);args=[self.t(b,i) for _ in range(ct)];return self.r.typetoken(tok)+'<'+', '.join(args)+'>'
  if kind==0x14:
   elem=self.t(b,i);rank,i[0]=uncompress(b,i[0]);sizes,i[0]=uncompress(b,i[0]);
   for _ in range(sizes):_,i[0]=uncompress(b,i[0])
   lows,i[0]=uncompress(b,i[0]);
   for _ in range(lows):_,i[0]=uncompress(b,i[0])
   return elem+'['+','*(max(rank,1)-1)+']'
  if kind==0x1b:
   # fnptr signature
   return 'fnptr'
  if kind==0x45:return self.t(b,i)+' pinned'
  if kind==0x41:return '...'
  return f'UNKNOWN_ELEM_0x{kind:02x}'
 def type(self,b,i):return self.t(b,i)
 def method(self,data):
  if not data:return {}
  b=memoryview(data);i=[0];flag=b[i[0]];i[0]+=1
  if flag&0x10:_,i[0]=uncompress(b,i[0])
  count,i[0]=uncompress(b,i[0]);ret=self.t(b,i);params=[self.t(b,i) for _ in range(count)];return {'ret':ret,'params':params,'has_this':bool(flag&0x20),'flags':flag}
 def field(self,data):
  if not data:return '?'
  return self.t(memoryview(data),[1])
 def locals(self,data):
  if not data:return []
  b=memoryview(data);i=[1];count,i[0]=uncompress(b,i[0]);return [self.t(b,i) for _ in range(count)]

if __name__=='__main__':
 r=Reader(sys.argv[1]);print('tables:',{k:len(v)-1 for k,v in r.records.items()});
 for name in ('GameServer','InProcessTransport','LoginService','DbStore','WorldService'):
  tids=[i for i in range(1,len(r.records[2])) if r.fullname(2,i)==name]
  for tid in tids:
   print('\n',name,'#',tid)
   for f in range(r.records[2][tid]['FieldList'],r.records[2][tid+1]['FieldList'] if tid+1<len(r.records[2]) else len(r.records[4])):
    print('field',r.field(f))
   for mid in range(r.records[2][tid]['MethodList'],min(r.records[2][tid+1]['MethodList'] if tid+1<len(r.records[2]) else len(r.records[6]),r.records[2][tid]['MethodList']+12)):
    print('method',r.method(mid))