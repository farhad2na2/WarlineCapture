using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Game.Components;
using Game.Runtime.Navigation;
using Game.Runtime.Pathfinding;
using Game.Runtime.Movement;

internal sealed class MovingWorldMap
{
    internal int Width,Height;
    internal GridCoordinateFrame Frame;
    internal byte[] Walkable,Friendly,Sidewalk,Dirt;
    internal bool[] Blocked,CellSurface,FootprintSurface,Occupied;
    internal int[] Cardinal,Diagonal,Counts;
    internal readonly Dictionary<string,OccupancyBounds> Membership=new Dictionary<string,OccupancyBounds>();
    internal readonly List<PlacementCell> Pool=new List<PlacementCell>();
    internal int NextEpoch;
    internal static MovingWorldMap Read(JsonElement j)
    {
        WorldJson.Shape(j,new[]{"width","height","coordinates","provenance","walkable","dynamic_blocked","friendly_pass","sidewalk","dirt_road","surface_cell","surface_footprint","cardinal_cost","diagonal_cost"});
        var m=new MovingWorldMap{Width=j.GetProperty("width").GetInt32(),Height=j.GetProperty("height").GetInt32()};
        WorldJson.Need(m.Width>0&&m.Width<=64&&m.Height>0&&m.Height<=64,"Moving profile requires grid dimensions 1..64");
        WorldJson.Need(!string.IsNullOrWhiteSpace(j.GetProperty("provenance").GetString()),"Static map/surface provenance required; synthetic inputs must say synthetic");
        WorldJson.FactsShape(j.GetProperty("coordinates"),typeof(GridCoordinateFrame));m.Frame=j.GetProperty("coordinates").Deserialize<GridCoordinateFrame>(WorldJson.Json);WorldJson.FiniteFields(m.Frame);
        WorldJson.Need(m.Frame.CellSize>=.01f&&m.Frame.CellSize<=100,"Invalid grid cell size");int n=m.Width*m.Height;
        int[] Values(string name,int min,int max){var a=j.GetProperty(name);WorldJson.Need(a.ValueKind==JsonValueKind.Array&&a.GetArrayLength()==n,"Incomplete map array: "+name);return a.EnumerateArray().Select(v=>{int x=v.GetInt32();WorldJson.Need(x>=min&&x<=max,"Invalid "+name);return x;}).ToArray();}
        m.Walkable=Values("walkable",0,1).Select(x=>(byte)x).ToArray();m.Blocked=Values("dynamic_blocked",0,1).Select(x=>x!=0).ToArray();m.Friendly=Values("friendly_pass",0,255).Select(x=>(byte)x).ToArray();
        m.Sidewalk=Values("sidewalk",0,1).Select(x=>(byte)x).ToArray();m.Dirt=Values("dirt_road",0,1).Select(x=>(byte)x).ToArray();m.CellSurface=Values("surface_cell",0,1).Select(x=>x!=0).ToArray();m.FootprintSurface=Values("surface_footprint",0,1).Select(x=>x!=0).ToArray();m.Cardinal=Values("cardinal_cost",1,10000);m.Diagonal=Values("diagonal_cost",1,10000);m.Counts=new int[n];m.Occupied=new bool[n];return m;
    }
    internal GridCellValue Cell(float x,float z)=>GridCoordinateKernel.WorldToCell(Frame,x,z);
    internal bool InBounds(int x,int z)=>(uint)x<(uint)Width&&(uint)z<(uint)Height;
    struct Storage:IGridOccupancyStorage {public MovingWorldMap Map;public int GetCount(int i)=>Map.Counts[i];public void SetCount(int i,int n)=>Map.Counts[i]=n;public void SetOccupied(int i,bool v)=>Map.Occupied[i]=v;}
    internal void RefreshOccupancy(List<MovingWorldReplay.Unit> units)
    {
        var active=units.Where(u=>u.Actor.Mobile&&!u.Destroyed&&!u.Dying).ToDictionary(u=>u.Actor.Id);var storage=new Storage{Map=this};
        foreach(string id in Membership.Keys.ToArray())if(!active.ContainsKey(id)){GridOccupancyKernel.Remove(Width,ref storage,Membership[id]);Membership.Remove(id);}
        foreach(var pair in active){var a=pair.Value.Actor;var bounds=GridOccupancyKernel.Bounds(Width,Height,a.CellX,a.CellZ,1,1);if(Membership.TryGetValue(pair.Key,out var previous))GridOccupancyKernel.Update(Width,ref storage,previous,bounds);else GridOccupancyKernel.Add(Width,ref storage,bounds);Membership[pair.Key]=bounds;}
    }
    internal struct Context:IInfantryPathContext,IPathSearchContext,IEngagedMovementContext,IPathGoalIdentityContext
    {
        public MovingWorldMap Map;
        public MovingWorldReplay.Unit Self;
        public MovingWorldReplay.Unit[] Live;
        public PlacementCell[] LiveCells;
        public Dictionary<string,MovingWorldReplay.Unit> ById;
        public int[] Epochs,Parents,Scores,Paths;public byte[] ClosedCells;public long[] OpenCells;
        public List<PathSearchCell> Output;
        public HashSet<int> Reserved;
        public bool CanUseGoal(PlacementCell cell)=>!Reserved.Contains(cell.y*Width+cell.x)&&CanPlace(cell.x,cell.y,Self.Actor.CellX,Self.Actor.CellZ)&&IsSurfaceFootprintPathable(cell.x,cell.y);
        public void ReserveGoal(PlacementCell cell)=>Reserved.Add(cell.y*Width+cell.x);
        public int Width=>Map.Width;public int Height=>Map.Height;public bool HasDynamicBlocked=>true;public bool HasOccupied=>true;public bool HasFriendlyPass=>true;public int FriendlyPassLength=>Map.Friendly.Length;
        public int UnitCount=>Live.Length;public int ManualGroupCount=>Live.Length;
        public byte Walkable(int i)=>Map.Walkable[i];public bool DynamicBlocked(int i)=>Map.Blocked[i];public bool Occupied(int i)=>Map.Occupied[i];public byte FriendlyPass(int i)=>Map.Friendly[i];
        public PlacementIdentity UnitEntity(int i)=>new PlacementIdentity(Live[i].Actor.Index,Live[i].Actor.Version);public PlacementCell UnitCell(int i)=>LiveCells[i];public PlacementCell UnitSize(int i)=>new PlacementCell(1,1);public byte ManualGroup(int i)=>0;
        public int PoolLength=>Map.Pool.Count;public PlacementCell PathCell(int i)=>Map.Pool[i];public PlacementCell WorldToCell(float x,float y,float z){var c=Map.Cell(x,z);return new PlacementCell(c.X,c.Z);}public void CellToWorld(PlacementCell c,out float x,out float z){var p=GridCoordinateKernel.CellToWorldCenter(Map.Frame,c.x,c.y);x=p.X;z=p.Z;}
        public byte Sidewalk(int i)=>Map.Sidewalk[i];public bool BoardingTransportOccupies(PlacementIdentity p,int i)=>false;
        public bool IsWalkable(int i)=>Map.Walkable[i]!=0;public bool IsSurfaceCellPathable(int x,int z)=>Map.CellSurface[z*Width+x];public bool IsSurfaceFootprintPathable(int x,int z)=>Map.FootprintSurface[z*Width+x];
        public bool CanPlace(int x,int z,int cx,int cz){var copy=this;return PathPlacementKernel.CanPlaceForPathing(ref copy,new PlacementIdentity(Self.Actor.Index,Self.Actor.Version),new PlacementCell(x,z),new PlacementCell(1,1),new PlacementCell(cx,cz),false,Self.Manual,Self.Actor.Faction);}
        public int TraversalCost(int i,bool diagonal)=>diagonal?Map.Diagonal[i]:Map.Cardinal[i];
        public int Epoch(int i)=>Epochs[i];public void SetEpoch(int i,int v)=>Epochs[i]=v;public int CameFrom(int i)=>Parents[i];public void SetCameFrom(int i,int v)=>Parents[i]=v;public int GScore(int i)=>Scores[i];public void SetGScore(int i,int v)=>Scores[i]=v;public byte Closed(int i)=>ClosedCells[i];public void SetClosed(int i,byte v)=>ClosedCells[i]=v;public void SetInOpen(int i,byte v){}public long Open(int i)=>OpenCells[i];public void SetOpen(int i,long v)=>OpenCells[i]=v;public int Path(int i)=>Paths[i];public void SetPath(int i,int v)=>Paths[i]=v;public void WriteCell(int x,int z)=>Output.Add(new PathSearchCell(x,z));
        public bool DebugTargetForSource()=>false;public bool TargetIsStatic()=>false;public bool TargetHasHealth()=>ById.ContainsKey(Self.Actor.Target)&&!ById[Self.Actor.Target].Destroyed;public int TargetHealth()=>ById[Self.Actor.Target].Actor.Health;public bool HoldingPosition()=>WorldAcquisition.Read(Self.Actor).HoldPosition;
        public bool TryReadScan(out EngagedScanOrder scan){scan=default;return false;}
        public bool TryTargetFootprint(out EngagedCell size){size=new EngagedCell(1,1);return TargetHasHealth()&&ById[Self.Actor.Target].Actor.Mobile;}
        public EngagedCell WorldToCell(EngagedPosition p){var c=Map.Cell(p.X,p.Z);return new EngagedCell(c.X,c.Z);}public bool PreferredSurface(bool vehicle,int i)=>vehicle?Map.Dirt[i]!=0:Map.Sidewalk[i]!=0;
        public bool CanMoveIntoCell(EngagedCell target,EngagedCell size,EngagedCell current,byte faction){var copy=this;return InfantryPathLifecycleKernel.CanOccupyMovementTarget(ref copy,new PlacementCell(target.X,target.Z),new PlacementCell(size.X,size.Z),faction);}
    }
    internal Context Make(MovingWorldReplay.Unit self,List<MovingWorldReplay.Unit> units,Dictionary<string,MovingWorldReplay.Unit> byId,bool search=false)
    {
        int n=Width*Height;var live=units.Where(u=>u.Actor.Mobile&&!u.Destroyed).ToArray();return new Context{Map=this,Self=self,Live=live,LiveCells=live.Select(u=>new PlacementCell(u.Actor.CellX,u.Actor.CellZ)).ToArray(),ById=byId,Epochs=search?new int[n]:null,Parents=search?new int[n]:null,Scores=search?new int[n]:null,Paths=search?new int[n]:null,ClosedCells=search?new byte[n]:null,OpenCells=search?new long[n]:null,Output=new List<PathSearchCell>(),Reserved=new HashSet<int>()};
    }
}
