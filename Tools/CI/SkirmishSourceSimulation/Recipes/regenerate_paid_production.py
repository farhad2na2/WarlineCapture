"""CURRENT paid queue/capacity/refund service with explicit non-admitted direct birth API omission."""
from pathlib import Path
import argparse,hashlib,json,re
def mask(text):
 out=list(text);i=0;state='code'
 while i<len(text):
  c=text[i];n=text[i+1]if i+1<len(text)else''
  if state=='code':
   if c=='/'and n in['/','*']:state='line'if n=='/'else'block';out[i]=out[i+1]=' ';i+=2;continue
   if c=='@'and n=='"':state='verbatim';out[i]=out[i+1]=' ';i+=2;continue
   if c in['"',"'"]:state='string'if c=='"'else'char';out[i]=' ';i+=1;continue
  elif state=='line':
   if c=='\n':state='code'
   else:out[i]=' '
  elif state=='block':
   if c=='*'and n=='/':out[i]=out[i+1]=' ';i+=2;state='code';continue
   if c!='\n':out[i]=' '
  elif state=='verbatim':
   if c=='"'and n=='"':out[i]=out[i+1]=' ';i+=2;continue
   if c=='"':state='code'
   if c!='\n':out[i]=' '
  else:
   if c=='\\':out[i]=' ';i+=1
   elif c==('"'if state=='string'else"'"):state='code'
   if i<len(text)and text[i]!='\n':out[i]=' '
  i+=1
 if state not in['code','line']:raise ValueError('Unclosed native C# lexical value')
 return ''.join(out)

SOURCE='Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionService.cs'
def spans(text,name):
 code=mask(text);matches=list(re.finditer(r'(?m)^        public static bool '+re.escape(name)+r'\s*\(',code));result=[]
 for match in matches:
  cursor=match.end();depth=1
  while depth:
   if cursor>=len(code):raise ValueError('Unclosed native paid API signature')
   if code[cursor]=='(':depth+=1
   elif code[cursor]==')':depth-=1
   cursor+=1
  while code[cursor].isspace():cursor+=1
  if code.startswith('=>',cursor):
   end=code.find(';',cursor)
   if end<0 or'{'in code[cursor:end]:raise ValueError('Unsupported non-admitted birth forwarding shape')
   end+=1;kind='forwarding-expression'
  elif code[cursor]=='{':
   end=cursor+1;depth=1
   while depth:
    if end>=len(code):raise ValueError('Unclosed native paid API body')
    if code[end]=='{':depth+=1
    elif code[end]=='}':depth-=1
    end+=1
   kind='block'
  else:raise ValueError('Unknown native direct production API shape')
  result.append((match.start(),end,kind))
 return result

def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);path=repo/SOURCE;raw=path.read_bytes();text=raw.decode('utf-8').replace('\r\n','\n');produce=spans(text,'TryProduce');dispatch=spans(text,'TryDispatchQueued')
 if len(produce)!=2 or sorted(x[2]for x in produce)!=['block','forwarding-expression']or len(dispatch)!=1:raise ValueError('Native direct birth API domain changed; explicit profile review required')
 omitted=[{'method':name,'kind':kind,'source_sha256':hashlib.sha256(text[start:end].encode()).hexdigest()}for name,items in [('TryProduce',produce),('TryDispatchQueued',dispatch)]for start,end,kind in items]
 groups=[(produce[0][0],produce[-1][1]),(dispatch[0][0],dispatch[0][1])];result=text
 for start,end in sorted(groups,reverse=True):
  if text[end:end+2]!='\n\n':raise ValueError('Explicit direct API declaration boundary changed')
  result=result[:start]+result[end+2:]
 if path.read_bytes()!=raw:raise ValueError('Native paid service drift during extraction')
 with(output/'SkirmishProductionService.cs').open('w',encoding='utf-8',newline='')as file:file.write(result)
 proof={'kind':'CurrentNativePaidProductionBinding','source_inputs':{str(path):hashlib.sha256(raw).hexdigest()},'output_sha256':hashlib.sha256(result.encode()).hexdigest(),'omitted_nonadmitted_direct_birth_APIs':omitted,'preserved':'ALL current paid reserve/start/cancel/refund/death/delivery/ledger/private methods and constants unchanged. Original native service is the source on every build.','boundary':'Actual configured S002 uses source Native.TryReserve + SAME paid delivery/timeline/instantiate/initializer/NativeAdopt. Direct ScenarioSpawnSystem entity birth and visual AttachMissing APIs are not admitted; they are omitted, never replaced with fake success. A new direct caller fails compile.','bindings':['Original raw read bytes pinned and rechecked','CRLF working text normalizes to LF only']};(output/'source-extraction.json').write_text(json.dumps(proof,indent=2))
 if path.read_bytes()!=raw:raise ValueError('Native paid service drift during output')
 return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
