# Finite CURRENT original initial-road sync binding. Nonempty footprint domain rejects.
from pathlib import Path
import hashlib,json,re,argparse

def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as f:f.write(text)

def member(text,signature):
 if text.count(signature)!=1:raise ValueError('Unique original road member required: '+signature)
 start=text.index(signature);brace=text.find('{',start)
 if brace<0 or '=>' in text[start:brace]:raise ValueError('Block-only original road member required')
 depth=0
 for i in range(brace,len(text)):
  if text[i]=='{':depth+=1
  elif text[i]=='}':
   depth-=1
   if depth==0:return text[start:i+1]
 raise ValueError('Unclosed road member')

def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True)
 names=['Assets/Game/Scripts/Systems/RoadGridProjectionSystem.cs','Assets/Game/Scripts/Configs/GameplayConfigModels.cs','Assets/Game/Scripts/Systems/RoadBuildCompositionSystemHelper.cs','Assets/Game/Scripts/Systems/RoadBuildCompositionSourceCompositionSystemHelper.cs','Assets/Game/Scripts/Systems/RoadBuildCompositionLifecycleCompositionSystemHelper.cs','Assets/Game/Scripts/Environment/RuntimeCityStartupSystemHelper.cs','Assets/Game/Scripts/Systems/RoadNetworkCompositionSystemHelper.cs']
 raw={name:(repo/name).read_bytes()for name in names};s=raw[names[0]].decode().replace('\r\n','\n');models=raw[names[1]].decode().replace('\r\n','\n')
 composition=member(raw[names[2]].decode().replace('\r\n','\n'),'public Result Initialize(')
 if composition.count('new RoadBuildCompositionSourceCompositionSystemHelper()')!=1:raise ValueError('Fresh native road source construction changed')
 if raw[names[3]].decode().replace('\r\n','\n').count('readonly RoadNetworkCompositionSystemHelper RoadNetworkCompositionSystemHelper = new();')!=1:raise ValueError('Fresh native road network ownership changed')
 lifecycle=member(raw[names[4]].decode().replace('\r\n','\n'),'public void Init(')
 for text in [composition,lifecycle]:
  if any(x in text for x in ['RestoreSnapshot','RestoreRoad','CreateStroke(','RoadTiles[','.RoadTiles.Add','LoadRoad']):raise ValueError('New initial-road population entrance requires original owner closure')
 city=models[models.index('public class RuntimeCitySpawnerSystemConfig'):];getter=re.search(r'public int CityCount\s*=>\s*([^;]+);',city)
 if not getter:raise ValueError('Current native CityCount expression required')
 roadmodels=models[models.index('public class RoadBuildSystemConfig'):models.index('public class RuntimeCitySpawnerSystemConfig')]
 roadgetter=re.search(r'public float RoadGridSize\s*=>\s*([^;]+);',roadmodels)
 if not roadgetter:raise ValueError('Current native RoadGridSize expression required')
 evaluate=member(raw[names[5]].decode().replace('\r\n','\n'),'public RuntimeCityStartupSystemHelper.Result Evaluate(')
 gate='if (context.CityCount <= 0)\n                return RuntimeCityStartupSystemHelper.Result.None;'
 if gate not in evaluate or evaluate.index(gate)>evaluate.index('context.PlayRequested'):raise ValueError('Original empty-city startup gate changed')
 oncreate=member(s,'protected override void OnCreate()')
 expected=[('ReadOnly','GridConfig'),('ReadOnly','GridRoad'),('ReadOnly','DynamicBlockerComponent'),('ReadOnly','GridConfig'),('ReadWrite','GridRoad'),('ReadOnly','GridConfig'),('ReadWrite','GridRoad'),('ReadWrite','GridRoadSidewalk'),('ReadWrite','GridRoadDirt')]
 if re.findall(r'ComponentType\.(ReadOnly|ReadWrite)<([^>]+)>\(\)',oncreate)!=expected:raise ValueError('Changed native road query domain requires explicit owner closure')
 methods=[member(s,'private struct RoadBuffersData'),member(s,'protected override void OnCreate()').replace('protected override void OnCreate()','internal SourceFreshRoadProjection(EntityManager manager)').replace('{','{\n this.manager=manager;',1),member(s,'public void SyncRoadCellsToEcs(Context context)'),member(s,'private bool TryGetRoadBuffers(out RoadBuffersData roadBuffers)'),member(s,'private static void ClearRoadBuffers(RoadBuffersData roadBuffers)'),member(s,'public static bool IsGridCellCenterInsideBounds('),member(s,'private static void GetGridBounds(')]
 text='''using System;using System.Collections.Generic;using Unity.Entities;using Unity.Mathematics;using UnityEngine;using Game.Components;
namespace Game.Runtime{internal sealed class SourceFreshRoadProjection:IDisposable{
 private readonly EntityManager manager;private EntityManager EntityManager=>manager;private bool Enabled;private EntityQuery _gridDataQuery,_roadBufferQuery,_roadBuffersQuery;
 internal enum RoadFootprintKind{Dirt,Sidewalk} internal readonly struct UnadmittedFootprintState{}
 internal readonly struct Context{internal readonly IReadOnlyDictionary<Vector2Int,RoadNetworkCompositionSystemHelper.RoadTileData> RoadTiles;internal readonly float RoadGridSize;internal readonly UnadmittedFootprintState FootprintState;internal Context(RoadNetworkCompositionSystemHelper network,float size){RoadTiles=network.RoadTiles;RoadGridSize=size;FootprintState=default;}}
 private bool TryGetEntityManager(out EntityManager em){em=manager;return manager!=null;}
 private static void ForEachRoadWorldFootprintKind(UnadmittedFootprintState state,Vector2Int cell,RoadNetworkCompositionSystemHelper.RoadTileData tile,Func<Vector3,Vector3,RoadFootprintKind,bool> visitor){throw new NotSupportedException("Nonempty road footprints require original source inputs/owner; no fake footprint callback");}
 public void Dispose(){_gridDataQuery.Dispose();_roadBufferQuery.Dispose();_roadBuffersQuery.Dispose();}
'''+ '\n'.join(methods)+'\ninternal static int NativeCityCount(int cityCount)=>'+getter.group(1)+';\ninternal static float NativeRoadGridSize(float roadGridSize)=>'+roadgetter.group(1)+';\n}}\n'
 network=raw[names[6]].decode().replace('\r\n','\n')
 if network.count('public sealed class RoadNetworkCompositionSystemHelper')!=1:raise ValueError('Unique complete original network required')
 network='extern alias ActualCore;using Quaternion = ActualCore::UnityEngine.Quaternion;\n'+network
 write_lf(output/'SourceNativeRoadNetwork.cs',network)
 write_lf(output/'SourceFreshRoadProjection.cs',text)
 if any((repo/name).read_bytes()!=b for name,b in raw.items()):raise ValueError('Road source drift during extraction')
 proof={'kind':'CurrentOriginalInitialRoadProjectionBinding','source_inputs':{str(repo/n):hashlib.sha256(b).hexdigest()for n,b in raw.items()},'output':hashlib.sha256(text.encode()).hexdigest(),'complete_original_network_body':True,'network_output':hashlib.sha256(network.encode()).hexdigest(),'complete_source_members':['RoadBuffersData','OnCreate','SyncRoadCellsToEcs','TryGetRoadBuffers','ClearRoadBuffers','IsGridCellCenterInsideBounds','GetGridBounds'],'bindings':['OnCreate becomes captured same-EntityManager constructor; current query definitions retained','World lookup binds actual captured manager; no default World invented','Nonempty footprint resolver rejects; never returns guessed bounds or success','Current CityCount getter expression regenerated from original schema'],'nonempty_road_capability_admitted':False};(output/'initial-roads-source-extraction.json').write_text(json.dumps(proof,indent=2)+'\n');return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
