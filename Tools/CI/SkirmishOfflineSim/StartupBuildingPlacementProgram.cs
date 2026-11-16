// Standalone S002 placement executable; compiled separately from StartupPlacementProgram.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using Game.Runtime;
using Game.Components;
using UnityEngine;
using Unity.Entities;
internal static class StartupBuildingPlacementProgram {
 static JsonElement P(JsonElement e,string n)=>e.GetProperty(n);
 static int I(JsonElement e,string n)=>P(e,n).GetInt32();
 static bool B(JsonElement e,string n)=>P(e,n).GetBoolean();
 static Vector2Int Pair(JsonElement e)=>new(e[0].GetInt32(),e[1].GetInt32());
 static Vector2Int XY(JsonElement e)=>new(I(e,"x"),I(e,"y"));
 static string artifactRoot;
 static Dictionary<string,byte[]> cache=new();
 const long MaximumFileBytes=128L*1024*1024,MaximumCacheBytes=512L*1024*1024;
 static long cacheBytes;
 static FileInfo RegularFile(string path,long maximum) {var info=new FileInfo(path);if(!info.Exists||(info.Attributes&(FileAttributes.ReparsePoint|FileAttributes.Directory))!=0||info.Length<0||info.Length>maximum)throw new Exception("Bounded regular file required: "+path);return info;}
 static byte[] Artifact(JsonElement a,string encoding) {
  string name=P(a,"artifact").GetString();
  if(string.IsNullOrEmpty(name)||name=="."||name==".."||name.IndexOfAny(new[]{'/','\\'})>=0||Path.GetFileName(name)!=name||P(a,"encoding").GetString()!=encoding)throw new Exception("Invalid artifact name/encoding");
  string path=Path.GetFullPath(Path.Combine(artifactRoot,name));
  long declared=P(a,"bytes").GetInt64();if(declared<0||declared>MaximumFileBytes)throw new Exception("Artifact byte bound exceeded");
  if(!path.StartsWith(artifactRoot+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new Exception("Artifact escaped root");
  var info=RegularFile(path,MaximumFileBytes);if(info.Length!=declared)throw new Exception("Artifact file bytes differ before read");
  if(!cache.TryGetValue(path,out var bytes)){if(cacheBytes>MaximumCacheBytes-info.Length)throw new Exception("Artifact cache byte bound exceeded");bytes=File.ReadAllBytes(path);cacheBytes+=bytes.LongLength;cache.Add(path,bytes);}
  if(bytes.LongLength!=P(a,"bytes").GetInt64()||Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()!=P(a,"sha256").GetString())throw new Exception("Artifact hash/bytes mismatch "+name);
  return bytes;
 }
 static Dictionary<string,byte[]> Masks(JsonElement array,int cells) {
  var result=new Dictionary<string,byte[]>();foreach(var a in array.EnumerateArray()) {var bytes=Artifact(a,"u8");if(bytes.Length!=cells||bytes.Any(x=>x>1))throw new Exception("Noncomplete u8 mask");result.Add(P(a,"role").GetString(),bytes);}return result;
 }
 static BuildingDefinition Def(JsonElement d) {
  if(B(d,"HasRunway")||B(d,"IsWall"))throw new Exception("Unsupported runway/wall");
  var def=new BuildingDefinition{FootprintCells=Pair(P(d,"FootprintCells")),DisplayName=P(d,"DisplayName").GetString(),Prefab=new Prefab{name=P(P(d,"Prefab"),"name").GetString()}};
  if(def.FootprintCells.x<=0||def.FootprintCells.y<=0||def.FootprintCells.x>2048||def.FootprintCells.y>2048||BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(def))throw new Exception("Unsupported footprint/gate");return def;
 }
 static bool MaskOverlap(byte[] mask,RectInt r,GridConfig g) {
  if(!BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(r.position,r.size,g))return true;
  for(int y=r.yMin;y<r.yMax;y++)for(int x=r.xMin;x<r.xMax;x++)if(mask[y*g.Width+x]!=0)return true;return false;
 }
 static void Require(bool value,string why){if(!value)throw new Exception(why);}
 static bool Same(Vector2Int a,Vector2Int b)=>a==b;
 static int[] PairOut(Vector2Int p)=>new[]{p.x,p.y};
 static List<RectInt> Occupancy(JsonElement descriptor,GridConfig grid) {
  using var document=JsonDocument.Parse(Artifact(descriptor,"utf8-json"));var root=document.RootElement;
  Require(I(root,"schema_version")==1&&B(root,"complete")&&I(root,"grid_width")==grid.Width&&I(root,"grid_height")==grid.Height,"Incomplete occupancy");
  Require(P(root,"query").GetString()=="All(UnitGrid,UnitFootprint);Options=Default"&&P(root,"order").GetString()=="native_chunk_then_entity_unsorted","Wrong occupancy query");
  var entities=P(root,"entities");Require(entities.GetArrayLength()==I(root,"count")&&entities.GetArrayLength()<=1000000,"Occupancy count");
  var rects=new List<RectInt>();var ids=new HashSet<string>();int ordinal=0;
  foreach(var e in entities.EnumerateArray()) {
   Require(I(e,"ordinal")==ordinal++&&B(e,"has_unit_grid")&&B(e,"has_unit_footprint"),"Missing required occupancy input");
   Require(ids.Add(P(e,"entity").GetRawText()),"Duplicate occupancy entity");
   bool op=B(e,"has_operation_map_building"),proxy=B(e,"has_runtime_building_combat_tag")&&!op;
   if(B(e,"has_prefab")||(B(e,"has_static_grid_blocker")&&!op)||proxy)continue;
   Vector2Int min,size;
   if(op&&B(e,"has_runtime_building_combat_info")) {var info=P(e,"runtime_building_combat_info");min=Pair(P(info,"origin_cell"));size=Pair(P(info,"footprint_cells"));size=new(Math.Max(1,size.x),Math.Max(1,size.y));}
   else {size=Pair(P(e,"unit_footprint_size"));size=new(Math.Max(1,size.x),Math.Max(1,size.y));var center=Pair(P(e,"unit_grid_cell"));min=new(center.x-(size.x-1)/2,center.y-(size.y-1)/2);}
   Require(size.x<=2048&&size.y<=2048,"Unbounded live footprint");rects.Add(new(min,size));
  }return rects;
 }
 static object Run(JsonElement journal,bool partial) {
  Require(I(journal,"schema_version")==1,"Journal schema");var transactions=P(journal,"transactions");
  Require(partial?transactions.GetArrayLength()>=1:transactions.GetArrayLength()==18,"Expected eighteen completed transactions");
  var output=new List<object>();var prior=new Dictionary<int,(Vector2Int origin,Vector2Int size)>();int ordinal=0;
  foreach(var row in transactions.EnumerateArray()) {
   Require(I(row,"ordinal")==ordinal++,"Transaction ordinal");var request=P(row,"request");
   Require(I(request,"RequestKind")==0&&I(request,"AuthoredStartingGrant")==1&&I(request,"RotateVertical")==0&&I(request,"RequirePreferredOrigin")==0&&I(request,"AllowExistingWallOverlap")==0,"Unsupported request policy");
   var gj=P(row,"grid");var grid=new GridConfig{Width=I(gj,"Width"),Height=I(gj,"Height"),CellSize=P(gj,"CellSize").GetSingle(),Origin=new Vector3(P(P(gj,"Origin"),"x").GetSingle(),P(P(gj,"Origin"),"y").GetSingle(),P(P(gj,"Origin"),"z").GetSingle())};
   int cells=checked(grid.Width*grid.Height);Require(grid.Width>0&&grid.Width<=2048&&grid.Height>0&&grid.Height<=1024&&cells<=2097152&&float.IsFinite(grid.CellSize)&&grid.CellSize>0,"Bounded grid");
   var def=Def(P(row,"selected_request_definition"));var size=def.FootprintCells;
   var before=Masks(P(row,"masks_before"),cells);var original=P(row,"original_placement_inputs");Require(B(row,"original_placement_cache_inputs_complete"),"Missing original cache facts");var masks=Masks(P(original,"masks"),cells);
   var roads=new DynamicBuffer<GridRoad>{Values=before["grid_roads_before"].Select(x=>new GridRoad{Value=x}).ToArray()};var blockers=new DynamicBlockerComponent{Blocked=new BitArray{Values=before["blocked_before"]}};
   bool hasPrefix=B(original,"has_invalid_prefix");int pw=I(original,"prefix_width"),ph=I(original,"prefix_height");int[] prefix=null;
   if(hasPrefix){var raw=Artifact(P(original,"invalid_prefix"),"i32-le");Require(pw==grid.Width+1&&ph==grid.Height+1&&(long)pw*ph*4==raw.Length,"Incomplete prefix");prefix=new int[raw.Length/4];for(int j=0;j<prefix.Length;j++)prefix[j]=System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(j*4,4));}
   else Require(pw==0&&ph==0,"Unexpected absent-prefix dimensions");
   var occupied=Occupancy(P(row,"pre_placement_live_occupancy"),grid);int runtimeCount=0;
   foreach(var existing in P(row,"existing_runtime_buildings_before").EnumerateArray()) {
    int id=I(existing,"runtime_id");if(B(existing,"is_destroyed"))continue;var existingDef=Def(P(existing,"definition"));var origin=Pair(P(existing,"origin"));
    if(prior.TryGetValue(id,out var derived)){Require(origin==derived.origin&&existingDef.FootprintCells==derived.size,"Native prior runtime reservation assertion mismatch");origin=derived.origin;}
    occupied.Add(new(origin,existingDef.FootprintCells));runtimeCount++;
   }
   bool Cached(Vector2Int p,Vector2Int s)=>hasPrefix&&BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(prefix,pw,ph,p,s);
   int checks=0,originalCalls=0;Vector2Int? firstOriginal=null;
   var context=new BuildingRuntimeSpawnCompositionSystemHelper.Context();
   context.GetPlacementFootprint=(d,rot)=>{Require(!rot,"Rotation unsupported");return d.FootprintCells;};
   context.GetEffectivePlacementRect=(d,p,g,rot)=>{Require(!rot,"Rotation unsupported");return new(p,d.FootprintCells);};
   context.HasCachedInvalidCellInFootprint=Cached;
   context.IsPlacementValid=(d,p,s,rot,g,rs,bs)=>{
    checks++;var r=new RectInt(p,d.FootprintCells);
    if(MaskOverlap(before["authored_buildings"],r,g)||MaskOverlap(before["authored_roads_and_sidewalks"],r,g)||MaskOverlap(before["water"],r,g)||MaskOverlap(before["sidewalks"],r,g))return false;
    originalCalls++;firstOriginal??=p;
    if(MaskOverlap(masks["original_authored_buildings"],r,g)||MaskOverlap(masks["original_water"],r,g)||MaskOverlap(masks["original_authored_roads_and_sidewalks"],r,g))return false;
    return BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(r,g,rs,bs,hasPrefix,prefix,pw,ph,
      (x,y,w,h)=>masks["runtime_blocker_exceptions"][y*w+x]!=0,
      (gg,o,ss)=>MaskOverlap(masks["original_authored_roads_and_sidewalks"],new(o,ss),gg)||MaskOverlap(masks["original_startup_roads"],new(o,ss),gg),
      rr=>occupied.Any(x=>rr.Overlaps(x)));
   };
   bool found=new BuildingRuntimeSpawnCompositionSystemHelper().TryFindValidInitialBuildingOrigin(context,def,XY(P(request,"PreferredOrigin")),false,grid,roads,blockers,out var chosen);
   Require(found,"No derived valid placement");Require(firstOriginal.HasValue&&firstOriginal.Value==Pair(P(original,"first_validated_candidate_assertion")),"First original candidate assertion mismatch");
   var assertedRect=P(original,"effective_rect_assertion");Require(assertedRect.GetArrayLength()==4&&assertedRect[0].GetInt32()==firstOriginal.Value.x&&assertedRect[1].GetInt32()==firstOriginal.Value.y&&assertedRect[2].GetInt32()==size.x&&assertedRect[3].GetInt32()==size.y,"Effective rectangle assertion mismatch");
   Require(size==Pair(P(row,"placement_footprint_assertion"))&&size==Pair(P(original,"footprint_assertion")),"Derived footprint assertion mismatch");
   var factory=P(row,"factory_inputs");var factoryDefinition=Def(P(factory,"selected_definition"));Require(factoryDefinition.FootprintCells==size&&factoryDefinition.Prefab.name==def.Prefab.name,"Selected request/factory geometry differs");Require(chosen==Pair(P(factory,"origin_assertion")),"Derived origin vs factory-input assertion mismatch");
   int runtimeId=I(factory,"runtime_id_assertion");Require(!prior.ContainsKey(runtimeId),"Duplicate startup runtime identity");prior.Add(runtimeId,(chosen,size));
   int[] xyzBits=null;
   if(row.TryGetProperty("factory_center_inputs",out var centerInput)) {
    float plane=P(centerInput,"build_plane_y").GetSingle();Require(float.IsFinite(plane),"Nonfinite build plane");
    Require(chosen==Pair(P(centerInput,"origin_assertion"))&&size==Pair(P(centerInput,"footprint_assertion")),"Center input assertions mismatch");
    var center=new BuildingPlacementGridCameraSystemHelper().GetFootprintCenter(chosen,size,grid,plane);xyzBits=new[]{BitConverter.SingleToInt32Bits(center.x),BitConverter.SingleToInt32Bits(center.y),BitConverter.SingleToInt32Bits(center.z)};
    if(row.TryGetProperty("factory_result_assertion",out var actor)) {var transform=P(P(actor,"components"),"LocalTransform");var pos=P(transform,"Position");var expected=new[]{P(pos,"x").GetSingle(),P(pos,"y").GetSingle(),P(pos,"z").GetSingle()};for(int axis=0;axis<3;axis++)Require(xyzBits[axis]==BitConverter.SingleToInt32Bits(expected[axis]),"Factory XYZ bits mismatch");}
   }
   bool completed=row.TryGetProperty("placement_result_assertion",out var result);
   if(!partial)Require(completed&&row.TryGetProperty("factory_center_inputs",out _)&&row.TryGetProperty("factory_result_assertion",out _),"Incomplete completed placement/factory evidence");
   if(completed)Require(I(result,"Status")==1&&chosen==XY(P(result,"ActualOrigin"))&&size==XY(P(result,"ActualFootprint")),"Native placement result assertion mismatch");
   output.Add(new {ordinal=ordinal-1,prefab=def.Prefab.name,derived_origin=PairOut(chosen),derived_footprint=PairOut(size),checks,originalCalls,live_and_runtime_rectangles=occupied.Count,runtimeCount,placement_result_asserted=completed,derived_factory_xyz_bits=xyzBits});
  }
  return new{scope=partial?"SyntheticSinglePlacementControl":"EighteenPlacementOriginsFromCapturedBeforeInputs",count=output.Count,placements=output,mask_transitions_derived=false,factory_xyz_derived=!partial,full_physical_startup_derived=false,player_ready=false};
 }
 static int Main(string[] args) {try{artifactRoot=Path.GetFullPath(args[1]).TrimEnd(Path.DirectorySeparatorChar);string journalPath=Path.GetFullPath(args[0]);RegularFile(journalPath,MaximumFileBytes);using var document=JsonDocument.Parse(File.ReadAllBytes(journalPath));var result=Run(document.RootElement,args.Length>2&&args[2]=="--synthetic-first");Console.WriteLine(JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));return 0;}catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}

namespace Unity.Collections {}
namespace Unity.Mathematics { public struct int2 { public int x,y; public int2(int x,int y){this.x=x;this.y=y;} } }
namespace Unity.Entities { public struct DynamicBuffer<T> { public T[] Values; public T this[int i]=>Values[i]; } }
namespace UnityEngine {
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
 public struct Vector2Int { public int x,y; public Vector2Int(int x,int y){this.x=x;this.y=y;} public static Vector2Int one=>new(1,1); public static Vector2Int operator+(Vector2Int a,Vector2Int b)=>new(a.x+b.x,a.y+b.y); public static bool operator==(Vector2Int a,Vector2Int b)=>a.x==b.x&&a.y==b.y; public static bool operator!=(Vector2Int a,Vector2Int b)=>!(a==b); public override bool Equals(object o)=>o is Vector2Int v&&this==v; public override int GetHashCode()=>HashCode.Combine(x,y); }
 public struct RectInt { public int x,y,width,height; public RectInt(Vector2Int p,Vector2Int s){x=p.x;y=p.y;width=s.x;height=s.y;} public Vector2Int position=>new(x,y);public Vector2Int size=>new(width,height);public int xMin=>x;public int yMin=>y;public int xMax=>x+width;public int yMax=>y+height;public bool Overlaps(RectInt b)=>xMin<b.xMax&&xMax>b.xMin&&yMin<b.yMax&&yMax>b.yMin; }
 public static class Mathf {public static int Max(params int[] a)=>System.Linq.Enumerable.Max(a);public static int Min(params int[] a)=>System.Linq.Enumerable.Min(a);public static int Clamp(int x,int a,int b)=>Math.Min(Math.Max(x,a),b);public static int Abs(int x)=>Math.Abs(x);}
 public static class Debug {public static void Log(object x){}public static void LogError(object x){} public static void LogWarning(object x){} }
}
namespace Game.Components {
 public struct GridConfig {public int Width,Height;public float CellSize;public UnityEngine.Vector3 Origin;}
 public struct GridRoad {public byte Value;}
 public struct BitArray {public byte[] Values;public bool IsCreated=>Values!=null;public bool IsSet(int i)=>Values[i]!=0;}
 public struct DynamicBlockerComponent {public BitArray Blocked;}
 public static class GridUtils {public static int CellToIndex(Unity.Mathematics.int2 p,int width)=>p.y*width+p.x;}
}
namespace Game.Runtime {
 using UnityEngine;using Game.Components;using Unity.Entities;
 internal sealed class Prefab {public string name;}
 internal sealed class BuildingDefinition {public Vector2Int FootprintCells;public Prefab Prefab;public string DisplayName;}
 internal sealed class RuntimeBuildingEntity {public Vector2Int OriginCell;public BuildingDefinition Definition;}
 internal sealed partial class BuildingRuntimeSpawnCompositionSystemHelper {
  internal sealed class Context {
   public Func<BuildingDefinition,bool,Vector2Int> GetPlacementFootprint;
   public Func<BuildingDefinition,Vector2Int,GridConfig,bool,RectInt> GetEffectivePlacementRect;
   public Func<BuildingDefinition,Vector2Int,Vector2Int,bool,GridConfig,DynamicBuffer<GridRoad>,DynamicBlockerComponent,bool> IsPlacementValid;
   public Func<Vector2Int,Vector2Int,bool> HasCachedInvalidCellInFootprint;
   public WallContext WallValidationContext=new();
  }
  internal sealed class WallContext {public Dictionary<int,RuntimeBuildingEntity> RuntimeBuildings=new();}
 }
}
