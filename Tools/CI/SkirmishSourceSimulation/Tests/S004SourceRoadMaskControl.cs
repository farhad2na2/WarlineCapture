using System;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
internal static class S004SourceRoadMaskControl {
 static int Main(string[] args) { try {
  var graph=JObject.Parse(File.ReadAllText(args[0]));
  var assets=new SourceConfigMaterializer(graph);
  var data=assets.ByPath<MapSurfaceDataAsset>("Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Surface.asset");
  if(!data.TryCreateRuntimeBlobAsset(Allocator.Persistent,out var blob)) throw new Exception("Surface decode failed");
  ref var surface=ref blob.Value;
  if(surface.Dimensions.x!=2048||surface.Dimensions.y!=1024||surface.CellSize!=1f||surface.GridOrigin.x!=0||surface.GridOrigin.z!=0) throw new Exception("Surface frame mismatch");
  var em=new EntityManager { LogicalAllocatorAdmitted=true }; int id=200000; em.AllocateLogicalIdentity=()=>new Entity {Index=id++,Version=1};
  var entity=em.CreateEntity();
  em.AddComponentData(entity,new MapSurfaceComponent {SurfaceBlob=blob,GridOrigin=surface.GridOrigin,CellSize=surface.CellSize,Dimensions=surface.Dimensions,HasSurfaceData=1});
  var grid=new GridConfig {Width=2048,Height=1024,CellSize=1f,Origin=new float3(0,0,0)};
  var mask=new bool[2048*1024]; BuildingPlacementRoadSurfaceMask.Append(em,entity,grid,mask);
  var bytes=mask.Select(v=>(byte)(v?1:0)).ToArray(); int count=bytes.Count(v=>v==1);
  if(count!=23500) throw new Exception("Base surface road count changed: "+count);
  File.WriteAllBytes(args[1],bytes); blob.Dispose();
  Console.WriteLine("[S004SourceRoadMaskControl] result=Passed roadCells="+count+" sceneOverlay=unavailable sidewalk=unknown readiness=0"); return 0;
 } catch(Exception e) {Console.Error.WriteLine(e); return 1;} }
}
