using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Security.Cryptography;
using System.Buffers.Binary;
using Game.Components;
using Game.Runtime.Navigation;
using Game.Runtime.Pathfinding;
using Game.Runtime.Movement;

internal sealed class MovingWorldMap
{
    internal int Width,Height;
    internal bool FullMap;
    internal sealed class BodyPolicy
    {
        internal int FootprintX,FootprintZ; internal bool Vehicle,Manual;
        internal bool[] CellSurface,FootprintSurface;internal int[] Cardinal,Diagonal;
        internal string Provenance;
    }
    readonly Dictionary<(int,int,bool,bool),BodyPolicy> bodyPolicies=new();
    BodyPolicy legacyOrdinaryPolicy,legacyManualPolicy;
    // Profile facts are explicit, complete evaluated native-policy inputs. A spawn
    // admission bitmap is never registered here as movement/traversal authority.
    internal void RegisterBodyPolicy(BodyPolicy policy)
    {
        WorldJson.Need(policy!=null&&policy.FootprintX>=1&&policy.FootprintZ>=1&&policy.FootprintX<=64&&policy.FootprintZ<=64&&!string.IsNullOrWhiteSpace(policy.Provenance),"Explicit body path policy required");
        int n=checked(Width*Height);WorldJson.Need(policy.CellSurface?.Length==n&&policy.FootprintSurface?.Length==n&&policy.Cardinal?.Length==n&&policy.Diagonal?.Length==n,"Complete body path policy arrays required");
        WorldJson.Need(policy.Cardinal.All(x=>x>=1&&x<=10000)&&policy.Diagonal.All(x=>x>=1&&x<=10000),"Invalid body traversal costs");
        WorldJson.Need(bodyPolicies.Count<16&&(long)(bodyPolicies.Count+1)*n*10<=256L*1024*1024,"Body path policy memory budget exceeded");
        WorldJson.Need(bodyPolicies.TryAdd((policy.FootprintX,policy.FootprintZ,policy.Vehicle,policy.Manual),policy),"Duplicate body path policy");
    }
    internal BodyPolicy PolicyFor(MovingWorldReplay.Unit unit)
    {
        if(bodyPolicies.TryGetValue((unit.Footprint.x,unit.Footprint.y,unit.Vehicle,unit.Manual),out var policy))return policy;
        WorldJson.Need(unit.Footprint.x==1&&unit.Footprint.y==1&&!unit.Vehicle&&(!FullMap||!unit.Manual),"Explicit footprint/vehicle/manual movement surface and traversal policy required");
        WorldJson.Need(CellSurface!=null&&FootprintSurface!=null&&Cardinal!=null&&Diagonal!=null,"Movement policy facts absent");
        var cached=unit.Manual?legacyManualPolicy:legacyOrdinaryPolicy;if(cached!=null)return cached;
        var created=new BodyPolicy{FootprintX=1,FootprintZ=1,Vehicle=false,Manual=unit.Manual,CellSurface=CellSurface,FootprintSurface=FootprintSurface,Cardinal=Cardinal,Diagonal=Diagonal,Provenance=FullMap?"Complete ordinary1x1 full-map policy":"Legacy bounded ordinary/manual1x1 policy"};
        if(unit.Manual)legacyManualPolicy=created;else legacyOrdinaryPolicy=created;return created;
    }
    internal GridCoordinateFrame Frame;
    internal byte[] Walkable,Friendly,Sidewalk,Dirt;
    internal bool[] Blocked,CellSurface,FootprintSurface,Occupied;
    internal int[] Cardinal,Diagonal,Counts;
    internal readonly Dictionary<string,OccupancyBounds> Membership=new Dictionary<string,OccupancyBounds>();
    internal readonly List<PlacementCell> Pool=new List<PlacementCell>();
    internal int NextEpoch,ReservationGeneration,ReservationGridSize;
    internal int[] ReservedEpochs;
    internal long EstimatedRetainedBytes=>(long)Width*Height*(24+10*bodyPolicies.Count);
    internal void PrepareReservations(){var decision=PathGoalReservationKernel.DecidePrepare(ReservedEpochs!=null,ReservationGridSize,ReservationGeneration,Width*Height);if(decision.Reallocate)ReservedEpochs=new int[Width*Height];else if(decision.Clear)Array.Clear(ReservedEpochs);ReservationGeneration=decision.Generation;ReservationGridSize=decision.RememberedGridSize;}
    // Full-grid admission is deliberately opt-in only for the multi-unit caller. These
    // are complete evaluated production-policy facts, never a raw surface admission
    // mask substituted for footprint legality or traversal costs.
    internal static MovingWorldMap Read(JsonElement j,bool allowFullMap=false,string artifactRoot=null)
    {
        bool full=j.TryGetProperty("full_map_policy",out var policy);
        var fields=new[]{"width","height","coordinates","provenance","walkable","dynamic_blocked","friendly_pass","sidewalk","dirt_road","surface_cell","surface_footprint","cardinal_cost","diagonal_cost"};
        WorldJson.Shape(j,full?fields.Concat(new[]{"full_map_policy"}).ToArray():fields);
        var m=new MovingWorldMap{Width=j.GetProperty("width").GetInt32(),Height=j.GetProperty("height").GetInt32(),FullMap=full};
        long count=(long)m.Width*m.Height;
        WorldJson.Need(m.Width>0&&m.Height>0&&count<=16777216,"Moving grid must contain 1..16777216 cells");
        WorldJson.Need(full||m.Width<=64&&m.Height<=64,"Moving profile requires grid dimensions 1..64 without full_map_policy");
        if(full)
        {
            WorldJson.Need(allowFullMap,"Full-map facts require the multi-unit ordinary infantry profile");
            WorldJson.Shape(policy,new[]{"profile","complete","footprint_x","footprint_z","manual","vehicle","hierarchical","surface_policy","traversal_policy"});
            WorldJson.Need(policy.GetProperty("profile").GetString()=="complete_cell_facts_ordinary_1x1_infantry_v1"&&policy.GetProperty("complete").GetBoolean()&&
                policy.GetProperty("footprint_x").GetInt32()==1&&policy.GetProperty("footprint_z").GetInt32()==1&&!policy.GetProperty("manual").GetBoolean()&&
                !policy.GetProperty("vehicle").GetBoolean()&&!policy.GetProperty("hierarchical").GetBoolean(),"Unsupported or incomplete full-map policy");
            WorldJson.Need(!string.IsNullOrWhiteSpace(policy.GetProperty("surface_policy").GetString())&&!string.IsNullOrWhiteSpace(policy.GetProperty("traversal_policy").GetString()),"Production surface/traversal policy provenance required");
            WorldJson.Need(!string.IsNullOrWhiteSpace(artifactRoot),"Captured artifact root required for full-map facts");
        }
        WorldJson.Need(!string.IsNullOrWhiteSpace(j.GetProperty("provenance").GetString()),"Static map/surface provenance required; synthetic inputs must say synthetic");
        WorldJson.FactsShape(j.GetProperty("coordinates"),typeof(GridCoordinateFrame));m.Frame=j.GetProperty("coordinates").Deserialize<GridCoordinateFrame>(WorldJson.Json);WorldJson.FiniteFields(m.Frame);
        WorldJson.Need(m.Frame.CellSize>=.01f&&m.Frame.CellSize<=100,"Invalid grid cell size");int n=(int)count;long artifactBytes=0;
        byte[] Artifact(JsonElement value,string encoding,int stride,string name)
        {
            WorldJson.Need(artifactRoot!=null,"Captured artifact root required");
            WorldJson.Shape(value,new[]{"path","sha256","bytes","encoding"});
            string root=Path.GetFullPath(artifactRoot),supplied=value.GetProperty("path").GetString();
            WorldJson.Need(!string.IsNullOrWhiteSpace(supplied),"Artifact path required");
            string path=Path.GetFullPath(Path.IsPathRooted(supplied)?supplied:Path.Combine(root,supplied));
            WorldJson.Need(path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.Ordinal),"Artifact escape");
            // Reject symlinks even within the captured directory: closure cannot depend
            // on a link target changing between staging and execution.
            for(var item=new FileInfo(path) as FileSystemInfo;item!=null;item=item is FileInfo file?file.Directory:((DirectoryInfo)item).Parent)
                WorldJson.Need((item.Attributes&FileAttributes.ReparsePoint)==0,"Artifact symlink unsupported");
            long expected=(long)n*stride;artifactBytes+=expected;
            WorldJson.Need(artifactBytes<=512L*1024*1024&&value.GetProperty("encoding").GetString()==encoding&&value.GetProperty("bytes").GetInt64()==expected,"Artifact shape/encoding differs: "+name);
            string hash=value.GetProperty("sha256").GetString();
            WorldJson.Need(hash!=null&&hash.Length==64&&hash.All(c=>"0123456789abcdef".Contains(c)),"Artifact SHA256 required");
            WorldJson.Need(new FileInfo(path).Length==expected,"Artifact length differs: "+name);
            byte[] bytes=File.ReadAllBytes(path);
            WorldJson.Need(bytes.LongLength==expected&&Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()==hash,"Artifact bytes differ: "+name);
            return bytes;
        }
        byte[] Bytes(string name,int max)
        {
            var value=j.GetProperty(name);byte[] result;
            if(value.ValueKind==JsonValueKind.Array){WorldJson.Need(!full&&value.GetArrayLength()==n,"Complete captured artifacts required: "+name);result=new byte[n];int i=0;foreach(var v in value.EnumerateArray())result[i++]=v.GetByte();}
            else result=Artifact(value,"u8",1,name);
            WorldJson.Need(result.All(x=>x<=max),"Invalid "+name);return result;
        }
        int[] Costs(string name)
        {
            var value=j.GetProperty(name);var result=new int[n];
            if(value.ValueKind==JsonValueKind.Array){WorldJson.Need(!full&&value.GetArrayLength()==n,"Complete captured artifacts required: "+name);int i=0;foreach(var v in value.EnumerateArray())result[i++]=v.GetInt32();}
            else{var bytes=Artifact(value,"i32-le",4,name);for(int i=0;i<n;i++)result[i]=BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(i*4,4));}
            WorldJson.Need(result.All(x=>x>=1&&x<=10000),"Invalid "+name);return result;
        }
        m.Walkable=Bytes("walkable",1);m.Blocked=Array.ConvertAll(Bytes("dynamic_blocked",1),x=>x!=0);m.Friendly=Bytes("friendly_pass",255);
        m.Sidewalk=Bytes("sidewalk",1);m.Dirt=Bytes("dirt_road",1);m.CellSurface=Array.ConvertAll(Bytes("surface_cell",1),x=>x!=0);m.FootprintSurface=Array.ConvertAll(Bytes("surface_footprint",1),x=>x!=0);m.Cardinal=Costs("cardinal_cost");m.Diagonal=Costs("diagonal_cost");m.Counts=new int[n];m.Occupied=new bool[n];return m;
    }
    internal GridCellValue Cell(float x,float z)=>GridCoordinateKernel.WorldToCell(Frame,x,z);
    internal bool InBounds(int x,int z)=>(uint)x<(uint)Width&&(uint)z<(uint)Height;
    struct Storage:IGridOccupancyStorage {public MovingWorldMap Map;public int GetCount(int i)=>Map.Counts[i];public void SetCount(int i,int n)=>Map.Counts[i]=n;public void SetOccupied(int i,bool v)=>Map.Occupied[i]=v;}
    internal void RefreshOccupancy(List<MovingWorldReplay.Unit> units)
    {
        var active=units.Where(u=>u.Actor.Mobile&&u.MovementOccupancyEligible&&!u.Destroyed&&!u.Dying).ToDictionary(u=>u.Actor.Id);var storage=new Storage{Map=this};
        foreach(string id in Membership.Keys.ToArray())if(!active.ContainsKey(id)){GridOccupancyKernel.Remove(Width,ref storage,Membership[id]);Membership.Remove(id);}
        foreach(var pair in active){var a=pair.Value.Actor;var bounds=GridOccupancyKernel.Bounds(Width,Height,a.CellX,a.CellZ,pair.Value.Footprint.x,pair.Value.Footprint.y);if(Membership.TryGetValue(pair.Key,out var previous))GridOccupancyKernel.Update(Width,ref storage,previous,bounds);else GridOccupancyKernel.Add(Width,ref storage,bounds);Membership[pair.Key]=bounds;}
    }
    internal struct Context:IInfantryPathContext,IPathSearchContext,IEngagedMovementContext,IPathGoalIdentityContext,IPathGoalCandidateContext,IPathGoalReservationStorage
    {
        public MovingWorldMap Map;
        public MovingWorldReplay.Unit Self;
        public MovingWorldReplay.Unit[] Live;
        public PlacementCell[] LiveCells;
        public Dictionary<string,MovingWorldReplay.Unit> ById;
        public int[] Epochs,Parents,Scores,Paths;public byte[] ClosedCells;public long[] OpenCells;
        public List<PathSearchCell> Output;
        public bool CanUseGoal(PlacementCell cell){var copy=this;return PathGoalAssignmentKernel.CanUseGoal(ref copy,cell,Self.Footprint,Map.ReservationGeneration);}
        public bool CanPlaceGoal(PlacementCell cell){var copy=this;return PathPlacementKernel.CanPlaceForPathing(ref copy,new PlacementIdentity(Self.Actor.Index,Self.Actor.Version),cell,Self.Footprint,new PlacementCell(Self.Actor.CellX,Self.Actor.CellZ),Self.Vehicle,false,Self.Actor.Faction);}
        public bool CanTraverseGoalSurface(PlacementCell cell)=>IsSurfaceFootprintPathable(cell.x,cell.y);
        public int ReservedEpoch(int index)=>Map.ReservedEpochs[index];
        void IPathGoalReservationStorage.SetEpoch(int index,int generation)=>Map.ReservedEpochs[index]=generation;
        public void ReserveGoal(PlacementCell cell){var copy=this;PathGoalReservationKernel.ReserveFootprint(ref copy,Width,Map.ReservationGeneration,cell,Self.Footprint);}
        public int Width=>Map.Width;public int Height=>Map.Height;public bool HasDynamicBlocked=>true;public bool HasOccupied=>true;public bool HasFriendlyPass=>true;public int FriendlyPassLength=>Map.Friendly.Length;
        public int UnitCount=>Live.Length;public int ManualGroupCount=>Live.Length;
        public byte Walkable(int i)=>Map.Walkable[i];public bool DynamicBlocked(int i)=>Map.Blocked[i];public bool Occupied(int i)=>Map.Occupied[i];public byte FriendlyPass(int i)=>Map.Friendly[i];
        public PlacementIdentity UnitEntity(int i)=>new PlacementIdentity(Live[i].Actor.Index,Live[i].Actor.Version);public PlacementCell UnitCell(int i)=>LiveCells[i];public PlacementCell UnitSize(int i)=>Live[i].Footprint;public byte ManualGroup(int i)=>(byte)(Live[i].GroupMember?1:0);
        public int PoolLength=>Map.Pool.Count;public PlacementCell PathCell(int i)=>Map.Pool[i];public PlacementCell WorldToCell(float x,float y,float z){var c=Map.Cell(x,z);return new PlacementCell(c.X,c.Z);}public void CellToWorld(PlacementCell c,out float x,out float z){var p=GridCoordinateKernel.CellToWorldCenter(Map.Frame,c.x,c.y);x=p.X;z=p.Z;}
        public byte Sidewalk(int i)=>Map.Sidewalk[i];public bool BoardingTransportOccupies(PlacementIdentity p,int i)=>false;
        public bool IsWalkable(int i)=>Map.Walkable[i]!=0;public bool IsSurfaceCellPathable(int x,int z)=>Map.InBounds(x,z)&&Map.PolicyFor(Self).CellSurface[z*Width+x];public bool IsSurfaceFootprintPathable(int x,int z)=>Map.InBounds(x,z)&&Map.PolicyFor(Self).FootprintSurface[z*Width+x];
        public bool CanPlace(int x,int z,int cx,int cz){var copy=this;return PathPlacementKernel.CanPlaceForPathing(ref copy,new PlacementIdentity(Self.Actor.Index,Self.Actor.Version),new PlacementCell(x,z),Self.Footprint,new PlacementCell(cx,cz),Self.Vehicle,Self.Manual,Self.Actor.Faction);}
        public int TraversalCost(int i,bool diagonal){var policy=Map.PolicyFor(Self);return diagonal?policy.Diagonal[i]:policy.Cardinal[i];}
        public int Epoch(int i)=>Epochs[i];public void SetEpoch(int i,int v)=>Epochs[i]=v;public int CameFrom(int i)=>Parents[i];public void SetCameFrom(int i,int v)=>Parents[i]=v;public int GScore(int i)=>Scores[i];public void SetGScore(int i,int v)=>Scores[i]=v;public byte Closed(int i)=>ClosedCells[i];public void SetClosed(int i,byte v)=>ClosedCells[i]=v;public void SetInOpen(int i,byte v){}public long Open(int i)=>OpenCells[i];public void SetOpen(int i,long v)=>OpenCells[i]=v;public int Path(int i)=>Paths[i];public void SetPath(int i,int v)=>Paths[i]=v;public void WriteCell(int x,int z)=>Output.Add(new PathSearchCell(x,z));
        public bool DebugTargetForSource()=>false;public bool TargetIsStatic()=>false;public bool TargetHasHealth()=>ById.ContainsKey(Self.Actor.Target)&&!ById[Self.Actor.Target].Destroyed;public int TargetHealth()=>ById[Self.Actor.Target].Actor.Health;public bool HoldingPosition()=>WorldAcquisition.Read(Self.Actor).HoldPosition;
        public bool TryReadScan(out EngagedScanOrder scan){scan=default;return false;}
        public bool TryTargetFootprint(out EngagedCell size){size=default;if(!TargetHasHealth()||!ById[Self.Actor.Target].Actor.Mobile)return false;var target=ById[Self.Actor.Target];size=new EngagedCell(target.Footprint.x,target.Footprint.y);return true;}
        public EngagedCell WorldToCell(EngagedPosition p){var c=Map.Cell(p.X,p.Z);return new EngagedCell(c.X,c.Z);}public bool PreferredSurface(bool vehicle,int i)=>vehicle?Map.Dirt[i]!=0:Map.Sidewalk[i]!=0;
        public bool CanMoveIntoCell(EngagedCell target,EngagedCell size,EngagedCell current,byte faction){var copy=this;return InfantryPathLifecycleKernel.CanOccupyMovementTarget(ref copy,new PlacementCell(target.X,target.Z),new PlacementCell(size.X,size.Z),faction);}
    }
    internal Context Make(MovingWorldReplay.Unit self,List<MovingWorldReplay.Unit> units,Dictionary<string,MovingWorldReplay.Unit> byId,bool search=false)
    {
        int n=checked(Width*Height);WorldJson.Need(n>0&&n<=16777216&&(!search||EstimatedRetainedBytes+(long)n*25<=512L*1024*1024),"Map/search workspace memory budget exceeded");var live=units.Where(u=>u.Actor.Mobile&&!u.Destroyed).ToArray();return new Context{Map=this,Self=self,Live=live,LiveCells=live.Select(u=>new PlacementCell(u.Actor.CellX,u.Actor.CellZ)).ToArray(),ById=byId,Epochs=search?new int[n]:null,Parents=search?new int[n]:null,Scores=search?new int[n]:null,Paths=search?new int[n]:null,ClosedCells=search?new byte[n]:null,OpenCells=search?new long[n]:null,Output=new List<PathSearchCell>()};
    }
}
