"""CURRENT native selected-move APIs, no stored selection/order policy bodies."""
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

PATHS={'main':'Assets/Game/Scripts/Systems/SelectedMoveOrderCommandSystem.cs','cache':'Assets/Game/Scripts/Systems/SelectionStateCompositionSystemHelper.cs','guided':'Assets/Game/Scripts/Systems/SelectedMoveOrderCommandSystem.GuidedMove.cs','route':'Assets/Game/Scripts/Runtime/Missions/CampaignMissionGuidedMoveRouteUtility.cs'}
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as file:file.write(text)
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);paths={k:repo/v for k,v in PATHS.items()};raw={k:v.read_bytes()for k,v in paths.items()};text={k:v.decode('utf-8').replace('\r\n','\n')for k,v in raw.items()};main=text['main'];omitted=[]
 for name in ['OnCreate','OnUpdate']:
  body=member(main,name);omitted.append({'method':name,'source_sha256':hashlib.sha256(body.encode()).hexdigest()});main=main.replace(body,'        ',1)
 cache=member(text['cache'],'IsCacheableSelectedMoveEntity');active=member(text['route'],'IsGuidedMovePhaseActive');already=member(text['guided'],'IsAlreadyMovingToGoal');guided=member(text['guided'],'TryIssueCampaignGuidedMove');brace=mask(guided).index('{');signature=guided[:brace+1];body=guided[brace+1:];guard=re.match(r'\s*result = default;\s*if \(!CampaignMissionGuidedMoveRouteUtility\.IsGuidedMovePhaseActive\((\w+)\)\)\s*return false;',body)
 if not guard:raise ValueError('Current Campaign guided inactive guard changed; explicit profile review required')
 source_guard=body[:guard.end()];guided_binding=signature+source_guard+'\n            throw new System.NotSupportedException("Active Campaign-guided move not in ground Skirmish domain");\n        }'
 constants=re.findall(r'^        private static readonly FixedString64Bytes MissionId = [^;]+;',text['route'],re.M)
 if len(constants)!=1:raise ValueError('Unique current guided mission identity required')
 files={'SelectedMoveCachePredicate.cs':'using Game.Components;using Unity.Entities;namespace Game.Runtime{public class SelectionStateCompositionSystemHelper{'+cache.strip()+'}}\n','SelectedMoveGuidedGuards.cs':'using System;using Game.Components;using Game.Missions.Contracts;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;namespace Game.Runtime{public static class CampaignMissionGuidedMoveRouteUtility{'+constants[0].strip()+active+'}public partial struct SelectedMoveOrderCommandSystem{'+guided_binding+already+'}}\n','SelectedMoveOrderCommandSystem.cs':main}
 if any(paths[k].read_bytes()!=v for k,v in raw.items()):raise ValueError('Selected-move source drift during extraction')
 for filename,value in files.items():write_lf(output/filename,value)
 proof={'kind':'CurrentNativeSelectedMoveBinding','source_inputs':{str(paths[k]):hashlib.sha256(v).hexdigest()for k,v in raw.items()},'outputs':{n:hashlib.sha256(v.encode()).hexdigest()for n,v in files.items()},'bindings':['All current selected-cell/cache/eligibility/formation/queue/result methods retained unchanged','Only native OnCreate/OnUpdate physical pre-resolved input queue dispatch omitted; CLI invokes real source cell API and does not fake pointer/camera readiness','Exact current Campaign identity+phase predicate retained; inactive guard returns original false, active branch fails before order writes; no Campaign guidance success shim','LF output; exact raw source bytes pinned/rechecked'],'omitted_physical_entrypoints':omitted,'guided_inactive_guard':source_guard,'guided_full_native_method_sha256':hashlib.sha256(guided.encode()).hexdigest()};(output/'source-extraction.json').write_text(json.dumps(proof,indent=2))
 if any(paths[k].read_bytes()!=v for k,v in raw.items()):raise ValueError('Selected-move source drift during output')
 return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
