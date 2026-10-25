"""CURRENT finite source availability/read-policy regeneration; host contexts remain explicit adapters."""
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


def member(text,name,signature=None,expression=False):
 code=mask(text);pattern=r'\b(?:public|private|internal|protected)\s+(?:(?:static|readonly|unsafe|virtual|override|async|sealed|partial)\s+)*[^\n{};=]+?\b'+re.escape(name)+r'\s*\('
 matches=list(re.finditer(pattern,code))
 if signature:matches=[x for x in matches if signature in x.group()]
 if len(matches)!=1:raise ValueError('Unique native member required: '+name+' count='+str(len(matches)))
 start=matches[0].start();cursor=matches[0].end();depth=1
 while depth:
  if cursor>=len(code):raise ValueError('Unclosed source signature')
  if code[cursor]=='(':depth+=1
  elif code[cursor]==')':depth-=1
  cursor+=1
 rest=code[cursor:];arrow=rest.find('=>');brace=rest.find('{')
 if arrow>=0 and(brace<0 or arrow<brace):
  if expression:
   semi=rest.find(';',arrow)
   if semi<0 or any(x in rest[arrow+2:semi]for x in '{}'):raise ValueError('Unsupported finite expression shape: '+name)
   if name!='CanInspectUnavailable' or not rest[arrow+2:semi].strip().startswith('failure is'):raise ValueError('Unadmitted expression member: '+name)
   return text[start:cursor+semi+1]
  if brace<0 or not re.fullmatch(r'\s*[\w.]+\s+switch\s*',rest[arrow+2:brace]):raise ValueError('Unsupported scalar expression-bodied native method: '+name)
 if brace<0:raise ValueError('Native block body required: '+name)
 brace+=cursor;end=brace+1;depth=1
 while depth:
  if end>=len(code):raise ValueError('Unclosed native body: '+name)
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 if arrow>=0 and arrow+cursor<brace:
  while end<len(code)and code[end].isspace():end+=1
  if end>=len(code)or code[end]!=';':raise ValueError('Expected switch expression terminator')
  end+=1
 return text[start:end]
def declaration(text,kind,name):
 code=mask(text);matches=list(re.finditer(r'\bpublic\s+'+re.escape(kind)+r'\s+'+re.escape(name)+r'\b',code))
 if len(matches)!=1:raise ValueError('Unique source declaration required: '+name)
 start=matches[0].start();brace=code.index('{',matches[0].end());end=brace+1;depth=1
 while depth:
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 return text[start:end]
def apply(value,spec):
 for before,after in spec.get('replace',[]):
  if before not in value:raise ValueError('Explicit binding no longer present: '+before)
  value=value.replace(before,after)
 return value

def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as file:file.write(text)
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);here=Path(__file__).resolve().parent;manifest_raw=(here/'recipes.json').read_bytes();manifest=json.loads(manifest_raw);paths=sorted({x['source']for file in manifest for x in file['members']});raw={k:(repo/k).read_bytes()for k in paths};text={k:v.decode('utf-8').replace('\r\n','\n')for k,v in raw.items()};templates={x['output']:(here/'templates'/x['template']).read_bytes()for x in manifest};files={};proof=[]
 for file in manifest:
  value=templates[file['output']].decode()
  for spec in file['members']:
   source=text[spec['source']]
   if spec.get('declaration'):original=declaration(source,spec['declaration'],spec['member'])
   else:original=member(source,spec['member'],spec.get('signature'),spec.get('expression',False))
   bound=original
   if 'prefix_through'in spec:
    anchor=spec['prefix_through']
    if bound.count(anchor)!=1:raise ValueError('Explicit S002 prefix domain changed: '+spec['member'])
    bound=bound[:bound.index(anchor)+len(anchor)]+'\n'+spec['unsupported_tail']
   bound=apply(bound,spec);token='@@'+spec['id']+'@@'
   if value.count(token)!=1:raise ValueError('Owned context token mismatch: '+token)
   value=value.replace(token,bound,1);proof.append({'output':file['output'],'member':spec['member'],'signature':spec.get('signature'),'source':spec['source'],'source_member_sha256':hashlib.sha256(original.encode()).hexdigest(),'bound_member_sha256':hashlib.sha256(bound.encode()).hexdigest(),'source_prefix':bool('prefix_through'in spec),'bindings':spec.get('replace',[])})
  if '@@'in value:raise ValueError('Unresolved context adapter token')
  files[file['output']]=value
 def unchanged():
  if any((repo/k).read_bytes()!=v for k,v in raw.items()):raise ValueError('Current native availability source drift during generation')
  if(here/'recipes.json').read_bytes()!=manifest_raw:raise ValueError('Finite recipe drift')
  if any((here/'templates'/x['template']).read_bytes()!=templates[x['output']]for x in manifest):raise ValueError('Owned context adapter drift')
 unchanged()
 for name,value in files.items():write_lf(output/name,value)
 result={'kind':'CurrentNativeAvailabilityReadPolicies','source_inputs':{k:hashlib.sha256(v).hexdigest()for k,v in raw.items()},'recipe_sha256':hashlib.sha256(manifest_raw).hexdigest(),'owned_adapter_templates':{k:hashlib.sha256(v).hexdigest()for k,v in templates.items()},'members':proof,'outputs':{k:hashlib.sha256(v.encode()).hexdigest()for k,v in files.items()},'authority':'Each selected original source member regenerates CURRENT. Prefixes retain all original S002 predicates, then throw at unsupported legacy continuation. Existing resource/metadata adapters are declared separately, cannot grant paid success or initialize resources. No Quote substitution or fake producer/queue.'};write_lf(output/'source-extraction.json',json.dumps(result,indent=2));unchanged();return result
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
