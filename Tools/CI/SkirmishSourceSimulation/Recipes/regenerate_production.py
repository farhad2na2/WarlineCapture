"""Regenerate finite production read-model/key rules from CURRENT native source.
Host Context/type bindings are explicit; no policy method body is stored here.
"""
from pathlib import Path
import argparse,hashlib,json,re
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as file:file.write(text)
NAMES=['CountRuntimeProducedUnitsForFaction','TryCountProducedUnitsFromReadModel','CountPendingProductionsForFaction','CountProducedUnitsForBuilding','CountPendingProductionsForBuilding','RuntimeProducedUnitMatchesId','ProducedReadModelUnitMatchesFactionAndBuilding','ProducedReadModelUnitMatchesId','IsProducedUnitAlive','Normalize','TryGetRuntimeBuilding']
PREFIX='using System;using System.Collections.Generic;using Game.Components;using Unity.Entities;using Unity.Collections;using GameObject=Game.Runtime.SourceUnitPrefab;\nnamespace Game.Runtime { internal sealed class GeneratedProductionReadModelSource {\n internal delegate bool Manager(out EntityManager em);internal delegate bool Boundary(EntityManager em,out Entity entity);\n internal readonly struct Context {\n  public readonly IReadOnlyDictionary<int,RuntimeBuildingEntity> RuntimeBuildings;public readonly Manager TryGetEntityManager;public readonly Boundary TryGetRuntimeBoundaryEntity;public readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;public readonly Func<string,string> NormalizeId;public readonly Func<SourceUnitPrefab,string,bool> UnitPrefabMatchesId;\n  internal Context(IReadOnlyDictionary<int,RuntimeBuildingEntity> buildings,Manager manager,Boundary boundary,BuildingProductionQueueCompositionSystemHelper production,Func<string,string> normalize,Func<SourceUnitPrefab,string,bool> unit){RuntimeBuildings=buildings;TryGetEntityManager=manager;TryGetRuntimeBoundaryEntity=boundary;ProductionSystem=production;NormalizeId=normalize;UnitPrefabMatchesId=unit;}\n }\n'

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

def initializer(text,name):
 code=mask(text);matches=list(re.finditer(r'\b'+re.escape(name)+r'\s*=',code))
 if len(matches)!=1:raise ValueError('Unique source initializer required: '+name)
 start=matches[0].end();cursor=start;stack=[]
 while cursor<len(code):
  c=code[cursor]
  if c in'([{':stack.append(c)
  elif c in')]}':
   if not stack:
    if c=='}':return text[start:cursor].strip()
    raise ValueError('Initializer ended at unexpected delimiter')
   stack.pop()
  elif c==','and not stack:return text[start:cursor].strip()
  cursor+=1
 raise ValueError('Unterminated native initializer')

def generate(repo,out):
 repo=Path(repo);out=Path(out);out.mkdir(parents=True,exist_ok=True);read=repo/'Assets/Game/Scripts/Systems/BuildingRuntimeReadModelCompositionSystemHelper.cs';catalog=repo/'Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionCatalog.cs';raw_read=read.read_bytes();raw_catalog=catalog.read_bytes();r=raw_read.decode('utf-8').replace('\r\n','\n');c=raw_catalog.decode('utf-8').replace('\r\n','\n');methods=[member(r,n)for n in NAMES]
 bound=[m.replace('RuntimeBuildingEntity.PendingProduction','SourcePendingProduction')for m in methods]
 read_output=PREFIX+'\n\n'.join(bound)+'\n}}\n';producer=member(c,'ProducerPrefabKey').replace('public static string ProducerPrefabKey','internal static string ProducerPrefabKey');ground=initializer(c,'UseProducerGroundExit');recipe='using Game.Skirmish.Contracts; namespace Game.Runtime { internal static class GeneratedProductionRecipeSource {\n'+producer+'\ninternal static bool UsesGroundExit(SkirmishRoleOverlay overlay) => '+ground+';\n}}\n'
 files={'GeneratedProductionReadModelSource.cs':read_output,'GeneratedProductionRecipeSource.cs':recipe}
 if read.read_bytes()!=raw_read or catalog.read_bytes()!=raw_catalog:raise ValueError('Native source drift during bounded production extraction')
 for name,text in files.items():write_lf(out/name,text)
 proof={'kind':'CurrentNativeProductionExtraction','source_inputs':{str(f):hashlib.sha256(raw).hexdigest()for f,raw in [(read,raw_read),(catalog,raw_catalog)]},'methods':{name:hashlib.sha256(text.encode()).hexdigest()for name,text in zip(NAMES,methods)},'bindings':['RuntimeBuildingEntity.PendingProduction -> SourcePendingProduction (type contract)','public -> internal ProducerPrefabKey visibility (metadata only)','GameObject -> SourceUnitPrefab alias (explicit authored identity)'],'source_ground_exit_initializer':ground,'outputs':{name:hashlib.sha256(text.encode()).hexdigest()for name,text in files.items()},'host_context':'Minimal readonly same-world contract only; no policy body stored in generator'};(out/'source-extraction.json').write_text(json.dumps(proof,indent=2))
 if read.read_bytes()!=raw_read or catalog.read_bytes()!=raw_catalog:raise ValueError('Native source drift while writing bounded production extraction')
 return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
