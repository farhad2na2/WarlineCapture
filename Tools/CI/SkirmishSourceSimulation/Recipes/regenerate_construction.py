"""CURRENT numeric building factory/foundation/footprint source, canonical registry remains external."""
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

PATHS={'factory':'Assets/Game/Scripts/Systems/BuildingRuntimeEntityCompositionSystemHelper.cs','foundation':'Assets/Game/Scripts/Systems/BuildingFoundationVisualPresentationSystemHelper.cs','center':'Assets/Game/Scripts/Systems/BuildingPlacementGridCameraSystemHelper.cs','shared_foundation':'Assets/Game/Scripts/Systems/BuildingRuntimeDataRegistrationSystemHelper.cs'}
METHODS=['CreateBlockerEntity','ShouldRuntimeBuildingBlockPathing','CreateBuildingCombatEntity','AddBuildingDefenseWeapon','AddResourceStorageComponent','AddMaterialFabricationComponents','AddAirDefenseSupportProvider']
PREFIX='extern alias ActualCore;\nusing Unity.Entities; using Unity.Collections; using Unity.Mathematics; using Unity.Transforms; using Game.Components;\nusing BuildingDefinition = Game.Runtime.RuntimeDefinition;\nusing Quaternion = ActualCore::UnityEngine.Quaternion; using Color = ActualCore::UnityEngine.Color;\nusing Mathf = ActualCore::UnityEngine.Mathf;\nnamespace Game.Runtime { internal sealed class BuildingRuntimeEntityCompositionSystemHelper {\n internal delegate bool TryManager(out EntityManager manager);\n internal delegate bool TryGrid(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockers);\n internal sealed class Context {\n  internal TryManager TryGetEntityManager;\n  internal TryGrid TryGetGridData;\n  internal System.Func<Vector2Int,Vector2Int,GridConfig,Vector3> GetFootprintCenter;\n }\n'
def visual_guard_block(text,field):
 code=mask(text);matches=list(re.finditer(r'(?m)^            if \(definition\.'+re.escape(field)+r' != null\)',code))
 if len(matches)!=1:raise ValueError('Native visual-reference guard changed: '+field)
 start=matches[0].start();brace=code.index('{',matches[0].end())
 if code[matches[0].end():brace].strip():raise ValueError('Native visual-reference guard requires an immediate block: '+field)
 end=brace+1;depth=1
 while depth:
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 return text[start:end]
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as file:file.write(text)
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);paths={k:repo/v for k,v in PATHS.items()};raw={k:v.read_bytes()for k,v in paths.items()};text={k:v.decode('utf-8').replace('\r\n','\n')for k,v in raw.items()};native=[member(text['factory'],n)for n in METHODS];bound=[]
 for name,body in zip(METHODS,native):
  if name in['CreateBlockerEntity','CreateBuildingCombatEntity']:
   if body.count('RuntimeActorBirthContext birth = default')!=1:raise ValueError('Native building birth signature changed')
   body=body.replace('RuntimeActorBirthContext birth = default','SourceBuildingBirthContext birth = default',1)
  if name=='CreateBuildingCombatEntity':
   anchor='            if (definition == null)\n                return Entity.Null;'
   if body.count(anchor)!=1:raise ValueError('Native actor preflight anchor changed')
   body=body.replace(anchor,anchor+'\n            if (definition.AttackImpactPrefab != null || definition.MuzzleFlashPrefab != null)\n                throw new System.NotSupportedException("Native visual-reference conversion not admitted by numeric building factory");',1)
  if name=='AddBuildingDefenseWeapon':
   for field in ['AttackImpactPrefab','MuzzleFlashPrefab']:
    original=visual_guard_block(body,field)
    body=body.replace(original,'            if (definition.'+field+' != null)\n                throw new System.NotSupportedException("Native '+field+' reference requires Unity presentation adapter");',1)
  bound.append(body.lstrip(' '))
 foundation=member(text['foundation'],'ApplyCombatEntityFoundation');center=member(text['center'],'GetFootprintCenter');shared=member(text['shared_foundation'],'ApplyCombatEntityFoundation')
 files={'BuildingRuntimeEntityFactory.source.cs':PREFIX+'\n\n'.join(bound)+'\n}}\n','BuildingFoundationVisualPresentationSystemHelper.cs':'using Unity.Entities;using Unity.Transforms;using Game.Components;namespace Game.Runtime{internal sealed class BuildingFoundationNumericSource{'+foundation.lstrip(' ')+'}}\n','FootprintCenter.source.cs':'using Game.Components;using UnityEngine;using Unity.Mathematics;namespace Game.Runtime{internal sealed class GeneratedSourceBuildingCenter{'+center.lstrip(' ')+'}}\n'}
 if any(paths[k].read_bytes()!=v for k,v in raw.items()):raise ValueError('Native construction source drift during extraction')
 for name,value in files.items():write_lf(output/name,value)
 proof={'kind':'CurrentNativeNumericBuildingConstruction','source_inputs':{str(paths[k]):hashlib.sha256(v).hexdigest()for k,v in raw.items()},'method_inputs':{n:hashlib.sha256(v.encode()).hexdigest()for n,v in zip(METHODS,native)},'outputs':{n:hashlib.sha256(v.encode()).hexdigest()for n,v in files.items()},'bindings':['Existing minimal same-world Context/delegates only; no new constructor or registry','Current original BuildingDefinition fields bind canonical RuntimeDefinition; actual Core math/color/quaternion aliases','Only birth PARAMETER type binds explicit no-native-diagnostic SourceBuildingBirthContext; actor/blocker/storage/fabrication/threat/defense writes unchanged','CURRENT native foundation forwarder calls already-linked CURRENT BuildingRuntimeDataRegistrationSystemHelper, preserving actual shared ownership','Footprint center method retained CURRENT; no camera or geometry alternative','Native VFX guards retained with explicit unsupported throws; constructor preflight rejects nonnull refs BEFORE actor writes. Actual current registrar already has the same domain gate. No UnityObjectRef conversion or fake renderer data','LF output; exact raw source inputs pinned/rechecked'],'required_current_shared_foundation_method_sha256':hashlib.sha256(shared.encode()).hexdigest(),'native_registry_replaced':False};(output/'source-extraction.json').write_text(json.dumps(proof,indent=2))
 if any(paths[k].read_bytes()!=v for k,v in raw.items()):raise ValueError('Native construction source drift during output')
 return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
