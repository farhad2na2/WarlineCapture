"""CURRENT native configuration metadata adapters; no copied getter/identity policy."""
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

def member(text,name):
 code=mask(text);pattern=r'(?m)^[ \t]*(?:public|private|internal|protected)\s+(?:(?:static|readonly|unsafe|virtual|override|async|sealed|partial)\s+)*[^\n{};=]+?\b'+re.escape(name)+r'\s*\('
 matches=list(re.finditer(pattern,code))
 if len(matches)!=1:raise ValueError('Unique native member required: '+name)
 start=matches[0].start();cursor=matches[0].end();depth=1
 while depth:
  if cursor>=len(code):raise ValueError('Unclosed native method signature')
  if code[cursor]=='(':depth+=1
  elif code[cursor]==')':depth-=1
  cursor+=1
 expr=code.find('=>',cursor);brace=code.find('{',cursor)
 if brace<0:
  if expr>=0:raise ValueError('Unsupported scalar expression-bodied native method: '+name)
  raise ValueError('Native member body required: '+name)
 if 0<=expr<brace and not re.fullmatch(r'\s*[\w.]+\s+switch\s*',code[expr+2:brace]):raise ValueError('Unsupported scalar/non-switch expression-bodied native method: '+name)
 end=brace+1;depth=1
 while depth:
  if end>=len(code):raise ValueError('Unclosed native member: '+name)
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 if 0<=expr<brace:
  while end<len(code)and code[end].isspace():end+=1
  if end>=len(code)or code[end]!=';':raise ValueError('Expected expression-switch terminator')
  end+=1
 return text[start:end]

PATHS={'grid':'Assets/Game/Scripts/Configs/GameplayConfigModels.cs','placement':'Assets/Game/Scripts/Configs/MapBuildingPlacementConfig.cs','operation':'Assets/Game/Scripts/Configs/OperationMapDefinition.cs'}
def class_span(text,name):
 code=mask(text);matches=list(re.finditer(r'(?m)^    public class '+re.escape(name)+r'\s*:\s*ScriptableObject\s*',code))
 if len(matches)!=1:raise ValueError('Unique current native config class required: '+name)
 start=matches[0].start();brace=code.find('{',matches[0].end());end=brace+1;depth=1
 while depth:
  if end>=len(code):raise ValueError('Unclosed current config class')
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 return text[start:end]
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as file:file.write(text)
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);paths={name:repo/path for name,path in PATHS.items()};raw={name:path.read_bytes()for name,path in paths.items()};text={name:data.decode('utf-8').replace('\r\n','\n')for name,data in raw.items()};grid=class_span(text['grid'],'GridAuthoringConfig');operation=text['operation'];blob=member(operation,'TryCreatePersistentMetadataBlob');operation=operation.replace(blob,'',1)
 files={'GridAuthoringConfig.source.cs':'extern alias ActualCore;using Color=ActualCore::UnityEngine.Color;using UnityEngine;namespace Game.Configs{\n'+grid+'\n}', 'MapBuildingPlacementConfig.cs':'extern alias ActualCore; using GameObject = ActualCore::UnityEngine.GameObject;\n'+text['placement'],'OperationMapDefinition.cs':'extern alias ActualCore; using Quaternion=ActualCore::UnityEngine.Quaternion;\n'+operation}
 if any(paths[n].read_bytes()!=data for n,data in raw.items()):raise ValueError('Native config source drift during extraction')
 for name,value in files.items():write_lf(output/name,value)
 proof={'kind':'CurrentNativeConfigMetadataBindings','source_inputs':{str(paths[n]):hashlib.sha256(data).hexdigest()for n,data in raw.items()},'outputs':{name:hashlib.sha256(value.encode()).hexdigest()for name,value in files.items()},'bindings':['Extract only complete GridAuthoringConfig class, retaining all current fields/getters/defaults','Actual installed managed Core Color/GameObject/Quaternion aliases; no replacement renderer APIs','All OperationMap identity/getter/validation logic retained CURRENT','Working text normalized LF; raw source bytes pinned/rechecked'],'omitted_native_domain':{'method':'OperationMapDefinition.TryCreatePersistentMetadataBlob','native_method_sha256':hashlib.sha256(blob.encode()).hexdigest(),'reason':'Native BlobBuilder allocation is outside the admitted authored-data metadata compiler. No native-streaming/physical-validation flags or blob success invented. A consumer requiring this method fails compile, never receives a stub success.'}};(output/'source-extraction.json').write_text(json.dumps(proof,indent=2))
 if any(paths[n].read_bytes()!=data for n,data in raw.items()):raise ValueError('Native config source drift during output')
 return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
