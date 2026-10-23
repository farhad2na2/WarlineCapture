using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Game.Runtime.Navigation;
using Game.Runtime.Pathfinding;

internal static class NavigationContext
{
    internal sealed class Member
    {
        public OccupancyBounds Bounds;
        public PlacementIdentity Identity;
        public PlacementCell Cell, Size;
        public byte ManualGroup;
    }
    internal sealed class Map
    {
        public byte[] Walkable, FriendlyPass;
        public bool[] Blocked;
        public Scratch Search;
    }
    internal struct Context : IPathPlacementContext
    {
        public int GridWidth, GridHeight;
        public Map Facts;
        public bool[] OccupiedCells;
        public Member[] Members;
        public int Width => GridWidth;
        public int Height => GridHeight;
        public bool HasDynamicBlocked => true;
        public bool HasOccupied => true;
        public bool HasFriendlyPass => true;
        public int FriendlyPassLength => Facts.FriendlyPass.Length;
        public int UnitCount => Members.Length;
        public int ManualGroupCount => Members.Length;
        public byte Walkable(int i) => Facts.Walkable[i];
        public bool DynamicBlocked(int i) => Facts.Blocked[i];
        public bool Occupied(int i) => OccupiedCells[i];
        public byte FriendlyPass(int i) => Facts.FriendlyPass[i];
        public PlacementIdentity UnitEntity(int i) => Members[i].Identity;
        public PlacementCell UnitCell(int i) => Members[i].Cell;
        public PlacementCell UnitSize(int i) => Members[i].Size;
        public byte ManualGroup(int i) => Members[i].ManualGroup;
    }
    internal static Map ReadMap(JsonElement value, int count)
    {
        NavigationProgram.Shape(value, new[] { "walkable", "dynamic_blocked", "friendly_pass" });
        byte[] ReadBytes(string name, int max)
        {
            var items = value.GetProperty(name);
            NavigationProgram.Need(items.ValueKind == JsonValueKind.Array && items.GetArrayLength() == count, name + " must cover every grid cell");
            return items.EnumerateArray().Select(item => { int x = item.GetInt32(); NavigationProgram.Need(x >= 0 && x <= max, name + " value outside range"); return (byte)x; }).ToArray();
        }
        return new Map { Search = new Scratch(count), Walkable = ReadBytes("walkable", 1), Blocked = ReadBytes("dynamic_blocked", 1).Select(x => x != 0).ToArray(), FriendlyPass = ReadBytes("friendly_pass", 255) };
    }
    internal static object Query(JsonElement step, ref Context context, Dictionary<string, Member> members)
    {
        NavigationProgram.Shape(step, new[] { "kind", "entity", "target_x", "target_z", "vehicle", "manual_move", "faction" }, "ignored_entity", "surface", "search_padding", "max_expansions", "heuristic_weight", "routing");
        string entity = step.GetProperty("entity").GetString();
        NavigationProgram.Need(entity != null && members.ContainsKey(entity), "Navigation entity must be registered");
        var self = members[entity];
        var target = new PlacementCell(NavigationProgram.Integer(step, "target_x", -1024, 1024), NavigationProgram.Integer(step, "target_z", -1024, 1024));
        bool vehicle = step.GetProperty("vehicle").GetBoolean(), manual = step.GetProperty("manual_move").GetBoolean();
        byte faction = (byte)NavigationProgram.Integer(step, "faction", 0, 255);
        Member ignored = null;
        if (step.TryGetProperty("ignored_entity", out var ignoredJson))
        {
            string id = ignoredJson.GetString();
            NavigationProgram.Need(id != null && id != entity && members.TryGetValue(id, out ignored), "Ignored entity must be a different registered unit");
        }
        if (step.GetProperty("kind").GetString() == "path" || step.GetProperty("kind").GetString() == "route")
            return Search(step, context, self, ignored, target, vehicle, manual, faction);
        NavigationProgram.Need(!step.TryGetProperty("surface", out _) && !step.TryGetProperty("search_padding", out _) &&
            !step.TryGetProperty("max_expansions", out _) && !step.TryGetProperty("heuristic_weight", out _) && !step.TryGetProperty("routing", out _), "Search-only fields on placement query");
        NavigationProgram.ChargeQuery((long)(self.Size.x + 2) * (self.Size.y + 2) * (context.Members.Length + 1), 0);
        bool allowed = PathPlacementKernel.CanPlaceForPathing(ref context, self.Identity, target, self.Size, self.Cell, vehicle, manual, faction,
            ignored?.Identity ?? default, ignored?.Cell ?? default, ignored?.Size ?? default);
        return new { kind = "placement", allowed };
    }
    internal struct RoutingObserver : IPathRoutingObserver
    {
        public List<PathRoutingAttempt> Attempts;
        public void OnAttempt(in PathRoutingAttempt attempt) => Attempts.Add(attempt);
    }
    internal sealed class Scratch
    {
        public int[] Epoch, CameFrom, GScore, Path;
        public byte[] Closed, InOpen;
        public long[] Open;
        public int NextEpoch;
        public Scratch(int n) { Epoch = new int[n]; CameFrom = new int[n]; GScore = new int[n]; Path = new int[n]; Closed = new byte[n]; InOpen = new byte[n]; Open = new long[n]; }
    }
    internal struct SearchContext : IPathSearchContext
    {
        public Context Placement;
        public Member Self, Ignored;
        public bool Vehicle, Manual, MeterRouting;
        public long PlacementWork;
        public byte Faction;
        public bool[] CellSurface, FootprintSurface;
        public int[] CardinalCost, DiagonalCost;
        public Scratch Buffer;
        public List<PathSearchCell> Cells;
        public bool IsWalkable(int i) { if (MeterRouting) NavigationProgram.ChargeQuery(1, 0); return Placement.Walkable(i) != 0; }
        public bool IsSurfaceCellPathable(int x,int z) => CellSurface[z*Placement.Width+x];
        public bool IsSurfaceFootprintPathable(int x,int z) => FootprintSurface[z*Placement.Width+x];
        public bool CanPlace(int x,int z,int cx,int cz)
        {
            if (MeterRouting) NavigationProgram.ChargeQuery(PlacementWork, 0);
            return PathPlacementKernel.CanPlaceForPathing(ref Placement,
            Self.Identity,new PlacementCell(x,z),Self.Size,new PlacementCell(cx,cz),Vehicle,Manual,Faction,
            Ignored?.Identity ?? default,Ignored?.Cell ?? default,Ignored?.Size ?? default);
        }
        public int TraversalCost(int i,bool diagonal) => diagonal ? DiagonalCost[i] : CardinalCost[i];
        public int Epoch(int i) => Buffer.Epoch[i]; public void SetEpoch(int i,int v) => Buffer.Epoch[i]=v;
        public int CameFrom(int i) => Buffer.CameFrom[i]; public void SetCameFrom(int i,int v) => Buffer.CameFrom[i]=v;
        public int GScore(int i) => Buffer.GScore[i]; public void SetGScore(int i,int v) => Buffer.GScore[i]=v;
        public byte Closed(int i) => Buffer.Closed[i]; public void SetClosed(int i,byte v) => Buffer.Closed[i]=v;
        public void SetInOpen(int i,byte v) => Buffer.InOpen[i]=v;
        public long Open(int i) => Buffer.Open[i]; public void SetOpen(int i,long v) => Buffer.Open[i]=v;
        public int Path(int i) => Buffer.Path[i]; public void SetPath(int i,int v) => Buffer.Path[i]=v;
        public void WriteCell(int x,int z) { if (MeterRouting) NavigationProgram.ChargeQuery(0, 1); Cells.Add(new PathSearchCell(x,z)); }
    }
    static object Search(JsonElement step, Context placement, Member self, Member ignored, PlacementCell target, bool vehicle, bool manual, byte faction)
    {
        var surface = step.GetProperty("surface");
        NavigationProgram.Shape(surface,new[] {"provenance","cell_pathable","footprint_pathable","cardinal_cost","diagonal_cost"});
        string provenance = surface.GetProperty("provenance").GetString();
        NavigationProgram.Need(!string.IsNullOrWhiteSpace(provenance),"Surface provenance is required (synthetic controls must say synthetic)");
        int n=placement.Width*placement.Height;
        int[] Values(string name,int low,int high)
        {
            var values=surface.GetProperty(name);
            NavigationProgram.Need(values.ValueKind==JsonValueKind.Array && values.GetArrayLength()==n,name+" must cover every grid cell for this requester footprint");
            return values.EnumerateArray().Select(v=> {int x=v.GetInt32();NavigationProgram.Need(x>=low&&x<=high,name+" value outside range");return x;}).ToArray();
        }
        bool routing = step.GetProperty("kind").GetString() == "route";
        NavigationProgram.Need(routing || !step.TryGetProperty("routing", out _), "Routing fields require route query");
        int expansionLimit=NavigationProgram.Integer(step,"max_expansions",0,30000);
        long onePlacement=(long)(self.Size.x+2)*(self.Size.y+2)*(placement.Members.Length+1);
        if (!routing) NavigationProgram.ChargeQuery((n + (long)System.Math.Min(n, expansionLimit + 1) * 24) * onePlacement, n);
        var context=new SearchContext { MeterRouting=routing, PlacementWork=onePlacement, Placement=placement, Self=self, Ignored=ignored, Vehicle=vehicle, Manual=manual, Faction=faction,
            CellSurface=Values("cell_pathable",0,1).Select(x=>x!=0).ToArray(), FootprintSurface=Values("footprint_pathable",0,1).Select(x=>x!=0).ToArray(),
            CardinalCost=Values("cardinal_cost",1,10000), DiagonalCost=Values("diagonal_cost",1,10000), Buffer=placement.Facts.Search, Cells=new List<PathSearchCell>() };
        var request=new PathSearchRequest { Width=placement.Width,Height=placement.Height,GridSize=n,StartX=self.Cell.x,StartZ=self.Cell.y,GoalX=target.x,GoalZ=target.y,
            SearchBoundsPadding=NavigationProgram.Integer(step,"search_padding",0,512),MaxExpansions=expansionLimit,
            HeuristicWeight=NavigationProgram.Integer(step,"heuristic_weight",1,1000),SearchEpoch=++context.Buffer.NextEpoch,IsVehicle=vehicle };
        if (routing)
        {
            NavigationProgram.Need(request.HeuristicWeight == 1, "Route owns normal weight 1 and detailed retry weight 2");
            var route = step.GetProperty("routing");
            NavigationProgram.Shape(route, new[] { "desired_goal_x", "desired_goal_z", "goal_search_radius", "max_candidate_attempts",
                "cheap_mode", "has_modes", "skip_alternate_search", "initial_state" });
            var initial = route.GetProperty("initial_state");
            NavigationProgram.FactsShape(initial, typeof(PathRoutingState));
            var state = initial.Deserialize<PathRoutingState>(NavigationProgram.Json);
            NavigationProgram.FiniteFields(state);
            NavigationProgram.Need(state.GoalX == target.x && state.GoalZ == target.y, "Initial assigned goal must match search target");
            NavigationProgram.Need(state.FailureCode >= 0 && state.FailureCode <= 12 && state.ExpansionCount >= 0 &&
                state.AlternateAttempts >= 0 && state.AlternateAttempts <= 32, "Invalid initial routing metadata");
            var routeRequest = new PathRoutingRequest { Search = request, ManualMove = manual,
                DesiredGoalX = NavigationProgram.Integer(route,"desired_goal_x",-1024,1024), DesiredGoalZ = NavigationProgram.Integer(route,"desired_goal_z",-1024,1024),
                GoalSearchRadius = NavigationProgram.Integer(route,"goal_search_radius",0,20), MaxCandidateAttempts = NavigationProgram.Integer(route,"max_candidate_attempts",0,32),
                CheapMode = (byte)NavigationProgram.Integer(route,"cheap_mode",0,255), HasModes = route.GetProperty("has_modes").GetBoolean(),
                SkipAlternateSearch = route.GetProperty("skip_alternate_search").GetBoolean() };
            // Match native disjoint 128-epoch request slots; retries own offsets within a slot.
            context.Buffer.NextEpoch += 127;
            var observer = new RoutingObserver { Attempts = new List<PathRoutingAttempt>() };
            GridPathRoutingKernel.Route(ref context, routeRequest, ref state, ref observer);
            return new { kind = "route", state, cells = context.Cells, attempts = observer.Attempts, surface_provenance = provenance, epoch = request.SearchEpoch };
        }
        int failure=GridPathSearchKernel.ValidateStart(ref context,request);
        var result=failure==0 ? GridPathSearchKernel.TrySearch(ref context,request) : new PathSearchResult { FailureCode=failure };
        return new { kind="path", result, cells=context.Cells, surface_provenance=provenance, epoch=request.SearchEpoch };
    }

}
