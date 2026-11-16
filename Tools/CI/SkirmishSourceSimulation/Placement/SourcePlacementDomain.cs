using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using Game.Components;
using Game.Simulation;

namespace Game.Runtime
{
    // One source environment, read once at startup. No entity allocator/bank/parser
    // replacement: the existing packed geometry owner remains the packed owner.
    public sealed partial class SourcePlacementDomain
    {
        internal struct Bounds { internal float3 Center, Extents; }
        readonly SourcePackedCampaignGeometry packed;
        readonly Bounds[] additional;
        readonly SharedSidewalkGeometryKernel.Footprint[] sidewalks;
        readonly SharedWaterGeometryKernel.Footprint[] water;
        readonly System.Collections.Generic.Dictionary<string,string> sourceAssetPins;
        public readonly string WaterReferenceSha256;
        public readonly int WaterQueryCount;
        public bool WaterDomainKnown=>water!=null;
        public int WaterFootprintCount=>water==null?0:water.Length;
        internal SharedWaterGeometryKernel.Footprint[] CopyWater()=>water==null?null:(SharedWaterGeometryKernel.Footprint[])water.Clone();
        internal readonly SharedSidewalkGeometryKernel.MeshName[] Meshes;
        public readonly string ReferenceSha256;
        internal int AdditionalBoundsCount=>additional.Length;
        internal int SidewalkCount=>sidewalks.Length;
        internal SharedSidewalkGeometryKernel.Footprint[] CopySidewalks()=>(SharedSidewalkGeometryKernel.Footprint[])sidewalks.Clone();
        SourcePlacementDomain(SourcePackedCampaignGeometry p,Bounds[] b,SharedSidewalkGeometryKernel.Footprint[] s,SharedSidewalkGeometryKernel.MeshName[] m,string hash,SharedWaterGeometryKernel.Footprint[] w,int queryCount,System.Collections.Generic.Dictionary<string,string> assetPins,string waterHash)
        {packed=p;additional=b;sidewalks=s;Meshes=m;ReferenceSha256=hash;water=w;WaterQueryCount=queryCount;sourceAssetPins=assetPins;WaterReferenceSha256=waterHash;}
        static int Int(JToken t,int lo,int hi){if(t==null||t.Type!=JTokenType.Integer)throw new FormatException("Source integer required");int n=t.Value<int>();if(n<lo||n>hi)throw new FormatException("Source integer domain");return n;}
        static float F(JToken t){if(t==null||t.Type!=JTokenType.Integer&&t.Type!=JTokenType.Float)throw new FormatException("Source scalar required");float n=t.Value<float>();if(!math.isfinite(n))throw new FormatException("Finite source scalar required");return n;}
        static float3 V(JToken t){var a=t as JArray??throw new FormatException("Source vector required");if(a.Count!=3)throw new FormatException("Exact3axes required");return new float3(F(a[0]),F(a[1]),F(a[2]));}
        static float4x4 M(JToken t){var a=t as JArray??throw new FormatException("Source matrix required");if(a.Count!=16)throw new FormatException("Exact16column-major fields required");return new float4x4(new float4(F(a[0]),F(a[1]),F(a[2]),F(a[3])),new float4(F(a[4]),F(a[5]),F(a[6]),F(a[7])),new float4(F(a[8]),F(a[9]),F(a[10]),F(a[11])),new float4(F(a[12]),F(a[13]),F(a[14]),F(a[15])));}
        public static SourcePlacementDomain Load(string packedPath,string packedSha256,string expectedBakeSha256,string referencePath,string referenceSha256)
        {
            var p=SourcePackedCampaignGeometry.Load(packedPath,packedSha256,expectedBakeSha256);
            if(p.OperationMapId!="opmap.skirmish.desert_base_01")throw new NotSupportedException("First source profile is existing S002 Campaign environment");
            if(SourcePackedCampaignGeometry.HashFile(referencePath,32*1024*1024)!=referenceSha256)throw new InvalidOperationException("Success-pinned authored reference bundle changed");
            JObject j;using(var text=File.OpenText(referencePath))using(var reader=new JsonTextReader(text){MaxDepth=64,DateParseHandling=DateParseHandling.None})j=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            if(Int(j["schema_version"],1,1)!=1||(string)j["kind"]!="AuthoredSkirmishReferenceData"||(string)j["authority"]!="ActualSourceAssetsAndNativeBakers_NotMissionState"||
                (string)j["mesh_database_sha256"]!=expectedBakeSha256||j["fresh_world_allocator_exported"]?.Type!=JTokenType.Boolean||(bool)j["fresh_world_allocator_exported"]||
                j["full_mission_initialized"]?.Type!=JTokenType.Boolean||(bool)j["full_mission_initialized"])
                throw new FormatException("Exact actual source-only reference authority required");
            var scenes=j["source_scenes"] as JArray??throw new FormatException("Source scene domain required");
            string[] expected={"Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity","Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity","Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/opmap_skirmish_desert_base_01_runtime.unity"};
            if(scenes.Count!=3||!scenes.Select(x=>(string)x).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(expected.OrderBy(x=>x,StringComparer.Ordinal)))throw new FormatException("Exact selected source scenes required");
            var meshRows=j["meshes"] as JArray??throw new FormatException("Actual mesh domain required");
            var boundsRows=j["authored_bounds"] as JArray??throw new FormatException("Complete original Count/Collect bounds required");
            var sidewalkRows=j["sidewalks"] as JArray??throw new FormatException("Complete baked sidewalk domain required");
            if(meshRows.Count!=447||boundsRows.Count>65536||sidewalkRows.Count>262144||Int(j["authored_bounds_count"],0,65536)!=boundsRows.Count||Int(j["sidewalk_owner_count"],1,16)!=1)
                throw new FormatException("Complete bounded actual source domain mismatch");
            // Admit every cardinality before arrays. Names are source Mesh.name,
            // including Quad10210; no FBX filename or builtin inference.
            var meshes=new SharedSidewalkGeometryKernel.MeshName[meshRows.Count];var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<meshes.Length;i++)
            {
                var row=meshRows[i];if(Int(row["index"],0,446)!=i||row["meshPresent"]?.Type!=JTokenType.Boolean||row["localId"]?.Type!=JTokenType.Integer||row["assetGuid"]?.Type!=JTokenType.String)throw new FormatException("Exact ordered source mesh identity");
                string identity=(string)row["assetGuid"]+":"+row["localId"].Value<long>();if(!ids.Add(identity))throw new FormatException("Duplicate mesh source identity");
                bool present=(bool)row["meshPresent"];string name=(string)row["name"];if(present&&string.IsNullOrEmpty(name))throw new FormatException("Actual present mesh needs its name");
                meshes[i]=new SharedSidewalkGeometryKernel.MeshName{Known=true,Present=present,Name=name};
            }
            var bounds=new Bounds[boundsRows.Count];for(int i=0;i<bounds.Length;i++){var row=boundsRows[i];var center=V(row["center"]);var extents=V(row["extents"]);if(math.any(extents<0))throw new FormatException("Negative actual bounds extents");bounds[i]=new Bounds{Center=center,Extents=extents};}
            var sidewalks=new SharedSidewalkGeometryKernel.Footprint[sidewalkRows.Count];for(int i=0;i<sidewalks.Length;i++){var row=sidewalkRows[i];sidewalks[i]=new SharedSidewalkGeometryKernel.Footprint{WorldToLocal=M(row["world_to_local"]),LocalMin=V(row["local_min"]),LocalMax=V(row["local_max"]),WorldMin=V(row["world_min"]),WorldMax=V(row["world_max"])};}
            int waterQueryCount;
            var water=ReadWater(j,false,out waterQueryCount);
            return new SourcePlacementDomain(p,bounds,sidewalks,meshes,referenceSha256,water,waterQueryCount,ReadAssetPins(j),water==null?null:referenceSha256);
        }
        static System.Collections.Generic.Dictionary<string,string> ReadAssetPins(JObject j)
        {
            var rows=j["source_pins"] as JObject??throw new FormatException("Source asset pins required");
            if(rows.Count>65536)throw new FormatException("Finite source asset pin domain required");
            var result=new System.Collections.Generic.Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var row in rows.Properties())
            {
                string path=row.Name;
                // Gameplay C# edits do not invalidate immutable asset data. Actual
                // source scenes/prefabs/meshes/config assets and their metadata do.
                if(!(path.StartsWith("Assets/",StringComparison.Ordinal)||path.IndexOf("/Assets/",StringComparison.Ordinal)>=0)||path.EndsWith(".cs",StringComparison.Ordinal)||path.EndsWith(".cs.meta",StringComparison.Ordinal)||path.EndsWith(".asmdef",StringComparison.Ordinal)||path.EndsWith(".asmdef.meta",StringComparison.Ordinal))continue;
                string hash=(string)row.Value;if(hash==null||hash.Length!=64||hash.Any(c=>!(c>='0'&&c<='9'||c>='a'&&c<='f')))throw new FormatException("Exact source asset digest required");
                result.Add(path,hash);
            }
            if(result.Count==0)throw new FormatException("Actual source asset dependency domain required");
            return result;
        }
        static SharedWaterGeometryKernel.Footprint[] ReadWater(JObject j,bool required,out int count)
        {
            count=-1;
            bool any=j["water_query_count"]!=null||j["water_footprint_count"]!=null||j["water_footprints"]!=null;
            if(!any){if(required)throw new FormatException("Actual water query not exported");return null;}
            count=Int(j["water_query_count"],0,1048576);
            var rows=j["water_footprints"] as JArray??throw new FormatException("Complete actual water query rows required");
            if(rows.Count>262144||Int(j["water_footprint_count"],0,262144)!=rows.Count||rows.Count>count)throw new FormatException("Exact bounded complete water query count required");
            var result=new SharedWaterGeometryKernel.Footprint[rows.Count];
            for(int i=0;i<result.Length;i++){var row=rows[i];result[i]=new SharedWaterGeometryKernel.Footprint{WorldToLocal=M(row["world_to_local"]),LocalCenter=V(row["local_bounds_center"]),LocalExtents=V(row["local_bounds_extents"]),WorldCenter=V(row["world_bounds_center"]),WorldExtents=V(row["world_bounds_extents"])};}
            return result;
        }
        public SourcePlacementDomain WithWaterReference(string path,string sha256)
        {
            if(SourcePackedCampaignGeometry.HashFile(path,32*1024*1024)!=sha256)throw new InvalidOperationException("Success-pinned source water reference changed");
            JObject j;using(var text=File.OpenText(path))using(var reader=new JsonTextReader(text){MaxDepth=64,DateParseHandling=DateParseHandling.None})j=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            if(Int(j["schema_version"],1,1)!=1||(string)j["kind"]!="AuthoredSkirmishReferenceData"||(string)j["authority"]!="ActualSourceAssetsAndNativeBakers_NotMissionState"||(string)j["mesh_database_sha256"]!=packed.SourceAssetSha256||j["full_mission_initialized"]?.Type!=JTokenType.Boolean||(bool)j["full_mission_initialized"]||j["fresh_world_allocator_exported"]?.Type!=JTokenType.Boolean||(bool)j["fresh_world_allocator_exported"])
                throw new FormatException("Exact same source geometry authority required for water");
            var assetPins=ReadAssetPins(j);
            if(assetPins.Count!=sourceAssetPins.Count||sourceAssetPins.Any(pair=>!assetPins.TryGetValue(pair.Key,out string hash)||hash!=pair.Value))throw new InvalidOperationException("Physical source asset dependency changed between geometry and water bundles");
            int count;var rows=ReadWater(j,true,out count);
            // Keep the original013e geometry arrays and its identity, importing
            // ONLY the new actual water query. No actor/template state is read.
            return new SourcePlacementDomain(packed,additional,sidewalks,Meshes,ReferenceSha256,rows,count,sourceAssetPins,sha256);
        }
        public sealed class Masks
        {
            public readonly bool[] AuthoredBuildings,Sidewalks;
            public readonly int PackedCells,AuthoredBuildingCells,SidewalkCells,AdditionalBounds,SidewalkFootprints;
            public readonly long AdditionalRasterVisits;
            internal Masks(bool[] b,bool[] s,int packed,int additions,int footprints,long work){AuthoredBuildings=b;Sidewalks=s;PackedCells=packed;AuthoredBuildingCells=b.Count(x=>x);SidewalkCells=s.Count(x=>x);AdditionalBounds=additions;SidewalkFootprints=footprints;AdditionalRasterVisits=work;}
        }
        // Public host seam uses only scalar/array CLR values. A generated host
        // does not need another Unity/Game.Components type or math shim binding.
        public Masks Build(int width,int height,float cellSize,float originX,float originY,float originZ)
            =>Build(new GridConfig{Width=width,Height=height,CellSize=cellSize,Origin=new float3(originX,originY,originZ)});
        public bool[] BuildWaterMask(bool skirmishMatchPresent,int width,int height,float cellSize,float originX,float originY,float originZ)
        {
            if(!skirmishMatchPresent)return null;
            if(water==null)throw new InvalidOperationException("Actual complete source water query has not been admitted; missing geometry never defaults to empty");
            if(capturedBuildings!=null)RequireCapturedFrame(width,height,cellSize,originX,originY,originZ);else if(width!=2048||height!=1024||cellSize!=1||originX!=0||originY!=0||originZ!=0)throw new NotSupportedException("First qualified source water grid is exact2048x1024/cell1/origin0");
            return SharedWaterGeometryKernel.BuildMask(true,water,width,height,cellSize,new float3(originX,originY,originZ));
        }
        internal static long AdmitAdditionalBounds(Bounds[] values,GridConfig grid)
        {
            long work=0;
            foreach(var bounds in values)
            {
                // Same reserve envelope including original one-metre gap; clamp
                // before int conversion for safe finite admission at grid edges.
                int2 min=(int2)math.clamp(math.floor((bounds.Center.xz-bounds.Extents.xz-1f-grid.Origin.xz)/grid.CellSize),0,new float2(grid.Width,grid.Height));
                int2 max=(int2)math.clamp(math.ceil((bounds.Center.xz+bounds.Extents.xz+1f-grid.Origin.xz)/grid.CellSize),0,new float2(grid.Width,grid.Height));
                work+=(long)math.max(0,max.x-min.x)*math.max(0,max.y-min.y);
                if(work>67108864)throw new NotSupportedException("Additional source bounds raster work exceeds finite admission before mask allocation");
            }
            return work;
        }
        internal Masks Build(GridConfig grid)
        {
            if(capturedBuildings!=null){RequireCapturedFrame(grid.Width,grid.Height,grid.CellSize,grid.Origin.x,grid.Origin.y,grid.Origin.z);return new Masks((bool[])capturedBuildings.Clone(),(bool[])capturedSidewalks.Clone(),capturedBuildings.Count(v=>v),0,0,0);}
            if(grid.Width!=2048||grid.Height!=1024||grid.CellSize!=1||math.any(grid.Origin!=float3.zero))throw new NotSupportedException("First qualified source grid is exact2048x1024/cell1/origin0");
            long work=AdmitAdditionalBounds(additional,grid);
            var originalPacked=packed.BuildAuthoredMask(grid);
            var buildings=originalPacked.Cells;
            foreach(var bounds in additional)SharedPlacementGeometry.Reserve(buildings,grid.Width,grid.Height,grid.CellSize,grid.Origin,bounds.Center,bounds.Extents);
            var sidewalkMask=SharedSidewalkGeometryKernel.BuildMask(sidewalks,grid.Width,grid.Height,grid.CellSize,grid.Origin);
            return new Masks(buildings,sidewalkMask,originalPacked.OccupiedCells,additional.Length,sidewalks.Length,work);
        }
    }
}
