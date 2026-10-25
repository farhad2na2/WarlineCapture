"""Finite CURRENT queue transport/cache/slot policy extraction; no stored gameplay bodies."""
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
 code=mask(text);pattern=r'(?m)^[ \t]*(?:public|private|internal|protected)\s+(?:(?:static|readonly|unsafe|virtual|override|async|sealed|partial)\s+)*[^\n{};=]+?\b'+re.escape(name)+r'(?:<[^>]+>)?\s*\('
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

PATHS={'queue':'Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs','metadata':'Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs','recipe':'Assets/Game/Scripts/Configs/BuildingProductionRecipe.cs','slots':'Assets/Game/Scripts/Systems/BuildingProductionSlotUtilitySystemHelper.cs'}
METHODS=['ConfigureProductionRecipes','HasAvailableProductionSlot','TryReserveProductionSlot','TryFindAvailableProductionSlot','IsProductionSlotOccupiedByReadModel','ResolveProductionDurationSeconds','ResolveProductionTransportSettings','PrewarmProductionTransportSettings','EnsureProductionTransportCache','IsSameBoundsResolver','CacheProductionTransportSettings','ConfigureUnitProductionMetadataResolver','TryGetUnitProductionMetadata','IsSameUnitProductionMetadataResolver','IsHelicopterUnitPrefab','FindNextReadyTransportPending','FindNextSoonTransportPending','RemovePendingProduction','PruneProducedUnits','IsProducedUnitAlive','TryResolveDefaultProductionTransportPrefab','TryResolveConfiguredUnitPrefab','IsLikelyGroundVehiclePrefab','IsLikelyAirUnitPrefab','ResolveEffectiveProductionFootprintCells','NormalizeLookupKey']
CONSTANTS=['HelicopterTransportPrefabName','HelicopterTransportLookupKey','PlaneTransportPrefabName','PlaneTransportLookupKey','DefaultPoolPrewarmCount']
DELEGATES=['TryGetPrefabLocalBoundsDelegate','TryGetUnitProductionMetadataDelegate','TryGetRuntimeBoundaryEntityDelegate','RuntimeBuildingMatchesIdDelegate']
FIELDS=['_productionTransportSettingsByPrefab','_producerGroundExitPrefabs'];CACHE=['_cachedTransportUnitSpawnPrefabs','_cachedTransportUnitSpawnPrefabsByKey','_cachedTransportUnitSpawnPrefabCount','_cachedTransportUnitSpawnPrefabKeyCount','_cachedTransportBoundsResolver','_cachedDefaultHelicopterTransportPrefab','_cachedDefaultPlaneTransportPrefab','_tryGetUnitProductionMetadata']
PREFIX='extern alias ActualCore;using System;using System.Collections.Generic;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;using UnityEngine;using Game.Components;using Bounds=ActualCore::UnityEngine.Bounds;using GameObject=Game.Runtime.SourceUnitPrefab;namespace Game.Runtime { public sealed partial class BuildingProductionQueueCompositionSystemHelper {\n'
def declaration(text,name):
 pattern=(r'(?m)^        (?:public|internal) delegate[^\n;]*\b'+re.escape(name)+r'\s*\([^\n;]*;'if name in DELEGATES else r'(?m)^        (?:private|public|internal)[^\n;]*\b'+re.escape(name)+r'\s*(?:=[^\n;]*)?;')
 matches=list(re.finditer(pattern,mask(text)))
 if len(matches)!=1:raise ValueError('Unique native queue field/delegate required: '+name)
 a=matches[0];return text[a.start():a.end()]
def structure(text,name):
 code=mask(text);matches=list(re.finditer(r'(?m)^        (?:public|internal) readonly struct '+re.escape(name)+r'\s*',code))
 if len(matches)!=1:raise ValueError('Unique native queue struct required: '+name)
 start=matches[0].start();brace=code.index('{',matches[0].end());cursor=brace+1;depth=1
 while depth:
  if cursor>=len(code):raise ValueError('Unclosed native queue struct')
  if code[cursor]=='{':depth+=1
  elif code[cursor]=='}':depth-=1
  cursor+=1
 return text[start:cursor]
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as f:f.write(text)
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);paths={k:repo/v for k,v in PATHS.items()};raw={k:v.read_bytes()for k,v in paths.items()};text={k:v.decode('utf-8').replace('\r\n','\n')for k,v in raw.items()};queue=text['queue'];native_methods=[member(queue,n)for n in METHODS];methods=native_methods;methods=[v.replace('private static bool TryReserveProductionSlot(','internal static bool TryReserveProductionSlot(',1)if n=='TryReserveProductionSlot'else v for n,v in zip(METHODS,methods)];header='\n'.join(declaration(queue,n)for n in CONSTANTS)+'\n\n'+'\n'.join(declaration(queue,n)for n in DELEGATES+FIELDS)+'\n\n'+'\n'.join(declaration(queue,n)for n in CACHE)+'\n';delivery=PREFIX+header+structure(queue,'QueueContext')+'\n\n'+'\n'.join(methods)+'\n'+structure(text['metadata'],'ProductionTransportSettings')+'\n\n}}\n'
 if text['recipe'].count('using UnityEngine;')!=1:raise ValueError('Current recipe object type import changed')
 recipe=text['recipe'].replace('using UnityEngine;','using GameObject=Game.Runtime.SourceUnitPrefab;',1);slots=text['slots'].replace('RuntimeBuildingEntity.PendingProduction','SourcePendingProduction')
 files={'BuildingProductionQueueCompositionSystemHelper.delivery.cs':delivery,'BuildingProductionRecipe.cs':recipe,'BuildingProductionSlotUtilitySystemHelper.cs':slots}
 if any(paths[k].read_bytes()!=v for k,v in raw.items()):raise ValueError('Native queue source drift during extraction')
 for name,value in files.items():write_lf(output/name,value)
 proof={'kind':'CurrentNativeDeliveryQueuePolicy','source_inputs':{str(paths[k]):hashlib.sha256(v).hexdigest()for k,v in raw.items()},'method_inputs':{n:hashlib.sha256(v.encode()).hexdigest()for n,v in zip(METHODS,native_methods)},'outputs':{n:hashlib.sha256(v.encode()).hexdigest()for n,v in files.items()},'bindings':['Actual managed Core Bounds; explicit source-asset SourceUnitPrefab tokens replace GameObject references, never GameObject behavior','Native pending type maps to existing same-owner SourcePendingProduction','TryReserveProductionSlot private -> internal visibility only for same canonical source delivery owner','Complete current QueueContext/cache fields/transport settings and26policy methods retained','Recipe object reference alias only; slot helper complete native policy retained','Metadata resolver is ORIGINAL native callback; captured authored getter schema validation remains separate and live session policy is not sampled','LF output, raw input bytes pinned and rechecked'],'no_fake_delivery':True};(output/'source-extraction.json').write_text(json.dumps(proof,indent=2))
 if any(paths[k].read_bytes()!=v for k,v in raw.items()):raise ValueError('Native queue source drift during output')
 return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
