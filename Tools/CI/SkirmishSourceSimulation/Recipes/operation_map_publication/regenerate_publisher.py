from pathlib import Path
import hashlib,json,types,re
PATHS={'ownership':'Assets/Game/Scripts/Systems/SkirmishMissionProductionInfrastructure.cs','processing':'Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs','buffers':'Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryCommands.cs','helipad':'Assets/Game/Scripts/Systems/OperationMapHelipadReadModelUtility.cs','metadata':'Assets/Game/Scripts/Systems/OperationMapMetadataUtility.cs','active':'Assets/Game/Scripts/Systems/OperationMapMetadataUtility.ActiveMetadata.cs','launch':'Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs','source_map':'Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset','source_meta':'Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset.meta'}
def generate(repo,out):
 repo=Path(repo);out=Path(out);out.mkdir(parents=True,exist_ok=True);raw={k:(repo/v).read_bytes()for k,v in PATHS.items()};text={k:v.decode().replace('\r\n','\n')for k,v in raw.items()};helper=Path(__file__).with_name('regenerate_config_metadata.py');helperbytes=helper.read_bytes();lex=types.ModuleType('publisher_native_lexical');lex.__file__=str(helper);exec(compile(helperbytes,str(helper),'exec'),lex.__dict__)
 names=['PublishFactionProductionSpawnPointsReadModel','PublishFactionProductionSpawnPointsForBuilding','TryGetGridConfig','ResolveBoundaryId'];methods={n:lex.member(text['processing'],n)for n in names};point='building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[i])'
 if sum(v.count(point)for v in methods.values())!=1:raise ValueError('Unique native production slot TransformPoint required')
 methods['PublishFactionProductionSpawnPointsForBuilding']=methods['PublishFactionProductionSpawnPointsForBuilding'].replace(point,'sourceTransformPoint(building,building.ProductionSpawnLocalPositions[i])',1)
 header='using System;using System.Collections.Generic;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;using Game.Components;using static Game.Runtime.BuildingRuntimeBoundaryBuffers;using GameObject=Game.Runtime.SourceUnitPrefab;namespace Game.Runtime{internal sealed class SourceFactionProductionSpawnPointPublisher{\n'
 fields=''' readonly Dictionary<GameObject,FixedString128Bytes> _boundaryIdsByPrefab=new();readonly Dictionary<string,FixedString128Bytes> _boundaryIdsByFallback=new();
 readonly Func<RuntimeBuildingEntity,Vector3,Vector3> sourceTransformPoint;
 internal SourceFactionProductionSpawnPointPublisher(Func<RuntimeBuildingEntity,Vector3,Vector3> point){sourceTransformPoint=point??throw new ArgumentNullException(nameof(point));}
 // Direct ORIGINAL writer invocation only. No configured/phase/cadence success is invented.
 internal void Publish(EntityManager em,Entity boundary,IReadOnlyDictionary<int,RuntimeBuildingEntity> buildings)=>PublishFactionProductionSpawnPointsReadModel(em,boundary,buildings);
'''
 code=header+fields+'\n'.join(methods.values())+'\n}}'
 source=text['buffers'];mask=lex.mask(source);marker='internal static class BuildingRuntimeBoundaryBuffers';start=source.index(marker);begin=mask.index('{',start);end=begin+1;depth=1
 while depth:
  if mask[end]=='{':depth+=1
  elif mask[end]=='}':depth-=1
  end+=1
 body=source[start:end]
 if body.count('where T : unmanaged, IBufferElementData')!=1:raise ValueError('Native buffer constraint shape')
 body=body.replace('where T : unmanaged, IBufferElementData','where T : struct, IBufferElementData',1);buffers='using Unity.Collections;using Unity.Entities;using GameObject=Game.Runtime.SourceUnitPrefab;namespace Game.Runtime{'+body+'}'
 helipad=text['helipad']
 if helipad.count('FixedList4096Bytes<int>')!=2:raise ValueError('Unsupported finite helipad scratch shape')
 helipad=helipad.replace('FixedList4096Bytes<int>','SourceHelipadBuildingIds')
 brace=helipad.index('{',helipad.index('public static class OperationMapHelipadReadModelUtility'))
 # Native FixedList capacity for int: (4096 - sizeof(ushort)2 - alignment pad2)/sizeof(int)4 = 1023.
 # Consumed domain: one local, Add there, readonly in readers only. No mutable value copies escape.
 scratch='\n        private struct SourceHelipadBuildingIds { int[] values; int length; public int Length=>length; public int Capacity=>1023; public int this[int index] { get { if((uint)index>=(uint)length)throw new System.IndexOutOfRangeException();return values[index]; } } public void Add(int value){if(length>=Capacity)throw new System.IndexOutOfRangeException();if(values==null)values=new int[Capacity];values[length++]=value;} }\n'
 helipad=helipad[:brace+1]+scratch+helipad[brace+1:]
 files={'SourceSkirmishMissionProductionInfrastructure.cs':text['ownership'],'SourceFactionProductionSpawnPointPublisher.cs':code,'SourceBuildingRuntimeBoundaryBuffers.cs':buffers,'SourceOperationMapHelipadReadModelUtility.cs':helipad,'SourceOperationMapMetadataUtility.cs':text['metadata'],'SourceOperationMapMetadataUtility.ActiveMetadata.cs':text['active']}

 constants=[]
 for name in ['MissionId','ScenarioId','OperationMapId']:
  found=re.findall(r'public const string '+name+r' = "([^"\n]+)";',text['launch'])
  if len(found)!=1:raise ValueError('Unique original native Skirmish fallback constant required')
  constants.append('internal const string '+name+'="'+found[0]+'";')
 guid=re.findall(r'^guid: ([0-9a-f]{32})$',text['source_meta'],re.M)
 if len(guid)!=1:raise ValueError('Exact legacy source map GUID required')
 files['SourceLegacyS002MapSelection.cs']='namespace Game.Runtime{internal static class SourceLegacyS002MapSelection{'+''.join(constants)+'internal const string AssetPath="'+PATHS['source_map']+'";internal const string AssetGuid="'+guid[0]+'";internal const string AssetSha256="'+hashlib.sha256(raw['source_map']).hexdigest()+'";}}\n'
 if any((repo/PATHS[k]).read_bytes()!=v for k,v in raw.items()):raise ValueError('Native publisher drift before emission')
 for name,value in files.items():(out/name).write_text(value)
 proof={'source_pins':{str(repo/PATHS[k]):hashlib.sha256(v).hexdigest()for k,v in raw.items()},'method_pins':{n:hashlib.sha256(lex.member(text['processing'],n).encode()).hexdigest()for n in names},'helper_sha256':hashlib.sha256(helperbytes).hexdigest(),'outputs':{n:hashlib.sha256(v.encode()).hexdigest()for n,v in files.items()},'bindings':['Actual canonical RuntimeBuildingEntity and exact source asset token aliases','ONE native TransformPoint read through explicit actual source-root delegate; all Instance-null/filter/order semantics unchanged','Current complete grid preference and prefab/fallback NormalizeSpawnableKey/ToFixedString identities','Original buffer creation/clear and complete original helipad/metadata validation','Direct original writer scope ONLY; no cadence/configured callback or mission readiness claim']};(out/'source-extraction.json').write_text(json.dumps(proof,indent=2)+'\n')
 if helper.read_bytes()!=helperbytes or any((repo/PATHS[k]).read_bytes()!=v for k,v in raw.items()):raise ValueError('Native/helper publisher drift after emission')
 return proof
if __name__=='__main__':generate(Path(__file__).resolve().parent/'native-source',Path(__file__).resolve().parent/'generated')
