using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Game.Runtime.Targeting;

internal static class WorldAcquisition
{
    internal sealed class Options
    {
        public int GridWidth,GridHeight,AggroRangeCells;
        public byte AutoEngage;
        public bool AttackMove,ManualMove,PathFollow,PathRequest,HoldPosition;
        public TargetScanOrder? Scan;
        public string Recent;
    }
    internal static Options Read(WorldFrameReplay.Actor a)
    {
        var v=a.Acquisition;
        WorldJson.Shape(v,new[]{"grid_width","grid_height","aggro_range_cells","auto_engage","attack_move","manual_move","path_follow","path_request","hold_position","scan","recent"});
        var o=new Options {GridWidth=v.GetProperty("grid_width").GetInt32(),GridHeight=v.GetProperty("grid_height").GetInt32(),
            AggroRangeCells=v.GetProperty("aggro_range_cells").GetInt32(),AutoEngage=v.GetProperty("auto_engage").GetByte(),
            AttackMove=v.GetProperty("attack_move").GetBoolean(),ManualMove=v.GetProperty("manual_move").GetBoolean(),PathFollow=v.GetProperty("path_follow").GetBoolean(),
            PathRequest=v.GetProperty("path_request").GetBoolean(),HoldPosition=v.GetProperty("hold_position").GetBoolean(),Recent=v.GetProperty("recent").GetString()};
        WorldJson.Need(o.GridWidth>=1&&o.GridWidth<=512&&o.GridHeight>=1&&o.GridHeight<=512&&o.AggroRangeCells>=0&&o.AggroRangeCells<=64&&o.AutoEngage<=1,"Invalid acquisition grid/range/flag");
        WorldJson.Need(a.Radius==0&&o.HoldPosition&&!o.PathFollow&&!o.PathRequest,"Bounded stationary profile requires hold position and no path follow/request");
        WorldJson.Need(o.Recent!=null,"Recent attacker must be an ID or empty string");
        if(v.GetProperty("scan").ValueKind!=JsonValueKind.Null)
        {
            WorldJson.FactsShape(v.GetProperty("scan"),typeof(TargetScanOrder));var scan=v.GetProperty("scan").Deserialize<TargetScanOrder>(WorldJson.Json);
            WorldJson.Need(scan.HasStarted<=1&&scan.EngageDetectedTargets<=1&&scan.RadiusCells>=0&&scan.RadiusCells<=64&&
                Math.Abs((long)scan.CenterX)<=1024&&Math.Abs((long)scan.CenterZ)<=1024,"Invalid scan facts");WorldJson.Need(scan.HasStarted==0||scan.EngageDetectedTargets==0||scan.RadiusCells==0,"Active scans require native retention policy outside stationary profile");o.Scan=scan;
        }
        return o;
    }
    internal static void ValidateActor(WorldFrameReplay.Actor a) { a.Recent=Read(a).Recent; }
    internal static void ValidateWorld(List<WorldFrameReplay.Actor> actors,Dictionary<string,WorldFrameReplay.Actor> byId,float cellSize)
    {
        var mobile=actors.Where(a=>a.Mobile).ToArray();
        var grids=mobile.Select(a=>(Read(a).GridWidth,Read(a).GridHeight)).Distinct().ToArray();
        WorldJson.Need(grids.Length<=1,"Native world uses one common grid");
        foreach(var a in actors)WorldJson.Need(!a.ProtectedRecords,"Protected-record actors outside unrestricted world profile");
        foreach(var a in mobile)
        {
            var o=Read(a);WorldJson.Need(o.AggroRangeCells*cellSize>=a.Weapon.Range,"Aggro radius must conservatively cover attack range for stationary leash safety");
            if(a.HasEngage&&a.Target!="")ValidateClose(a,byId);
        }
    }
    static void ValidateClose(WorldFrameReplay.Actor a,Dictionary<string,WorldFrameReplay.Actor> byId)
    {
        WorldJson.Need(byId.TryGetValue(a.Target,out var target),"Unknown retained engagement target");
        double dx=(double)a.X-target.X,dz=(double)a.Z-target.Z;
        WorldJson.Need(a.Weapon.Range>0&&dx*dx+dz*dz<=((double)a.Weapon.Range*.5)*((double)a.Weapon.Range*.5),"Retained engagement outside conservative stationary range");
    }
    internal static void MaintainStationaryEngagement(List<WorldFrameReplay.Actor> actors,Dictionary<string,WorldFrameReplay.Actor> byId,float cellSize)
    {
        foreach(var a in actors.Where(a=>a.Mobile&&a.HasEngage&&a.Target!=""))
        {
            WorldJson.Need(byId.TryGetValue(a.Target,out var target),"Missing retained target requires native lifecycle handling");
            if(TargetAcquisitionKernel.ShouldClearUnavailableTarget(false,true,target.Health)){a.Target="";continue;}
            ValidateClose(a,byId);
        }
    }
    static TargetIdentity Id(WorldFrameReplay.Actor a)=>new TargetIdentity(a.Index,a.Version);
    internal struct Context : ITargetAcquisitionContext
    {
        public WorldFrameReplay.Actor Source;
        public Dictionary<(int,int),WorldFrameReplay.Actor> Actors;
        public Dictionary<string,WorldFrameReplay.Actor> ById;
        public Options Options;
        public Dictionary<int,List<TargetIdentity>> Cells;
        public List<TargetIdentity> Buildings,Current;
        public Dictionary<(int,int),TargetPolicyFacts> Policies;
        public Dictionary<(int,int),int> Counts;
        public int At;
        public bool SourceHasAttackMove()=>Options.AttackMove;
        public bool SourceHasManualMove()=>Options.ManualMove;
        public bool SourceHasPathFollow()=>Options.PathFollow;
        public bool SourceHasPathRequest()=>Options.PathRequest;
        public bool SourceHoldsPosition()=>Options.HoldPosition;
        public bool TryReadScan(out TargetScanOrder scan){scan=Options.Scan??default;return Options.Scan.HasValue;}
        public bool TryReadRecent(out TargetIdentity t){t=Source.Recent!=""&&ById.TryGetValue(Source.Recent,out var a)?Id(a):default;return Source.Recent!="";}
        public bool BeginCell(int key,out TargetIdentity t){At=0;if(Cells.TryGetValue(key,out Current)&&Current.Count>0){t=Current[0];return true;}t=default;return false;}
        public bool NextCell(out TargetIdentity t){At++;if(At<Current.Count){t=Current[At];return true;}t=default;return false;}
        public int BuildingCount()=>Buildings.Count;public TargetIdentity BuildingAt(int i)=>Buildings[i];
        public bool HasFaction(TargetIdentity t)=>Actors.ContainsKey((t.Index,t.Version));public byte Faction(TargetIdentity t)=>Actors[(t.Index,t.Version)].Faction;
        public bool HasTransform(TargetIdentity t)=>HasFaction(t);public TargetPosition Position(TargetIdentity t){var a=Actors[(t.Index,t.Version)];return new TargetPosition(a.X,a.Y,a.Z);}
        public bool HasHealth(TargetIdentity t)=>HasFaction(t);public int Health(TargetIdentity t)=>Actors[(t.Index,t.Version)].Health;
        public bool HasEngagement(TargetIdentity t)=>Actors.TryGetValue((t.Index,t.Version),out var a)&&a.HasEngage;
        public TargetIdentity EngagementTarget(TargetIdentity t){var a=Actors[(t.Index,t.Version)];return ById.TryGetValue(a.Target,out var other)?Id(other):default;}
        public bool ProtectedRecords(TargetIdentity t)=>Actors.TryGetValue((t.Index,t.Version),out var a)&&a.ProtectedRecords;
        public TargetPolicyFacts PolicyFacts(TargetIdentity source,TargetIdentity target)=>Policies.TryGetValue((target.Index,target.Version),out var p)?p:default;
        public int AttackerCount(TargetIdentity t)=>Counts.TryGetValue((t.Index,t.Version),out int n)?n:0;
        public TargetCell WorldToCell(TargetPosition p)
        {
            // Explicit captured native WorldToCell facts for fixed actor positions; no invented grid conversion.
            var matches=Actors.Values.Where(a=>a.X==p.X&&a.Y==p.Y&&a.Z==p.Z).ToArray();
            WorldJson.Need(matches.Length>0&&matches.All(a=>a.CellX==matches[0].CellX&&a.CellZ==matches[0].CellZ),"Missing/ambiguous captured WorldToCell facts");
            return new TargetCell(matches[0].CellX,matches[0].CellZ);
        }
    }
    static Dictionary<string,Context> Contexts(List<WorldFrameReplay.Actor> actors,Dictionary<string,WorldFrameReplay.Actor> byId,List<string> order,JsonElement supplied)
    {
        WorldJson.Shape(supplied,order.ToArray());var contexts=new Dictionary<string,Context>();
        Dictionary<int,List<TargetIdentity>> commonCells=null;List<TargetIdentity> commonBuildings=null;
        var identities=actors.ToDictionary(a=>(a.Index,a.Version));var counts=new Dictionary<(int,int),int>();
        foreach(var a in actors)if(a.HasEngage&&byId.TryGetValue(a.Target,out var t)){var key=(t.Index,t.Version);counts.TryGetValue(key,out int n);counts[key]=n+1;}
        foreach(string source in order)
        {
            var a=byId[source];var o=Read(a);var row=supplied.GetProperty(source);
            WorldJson.Shape(row,new[]{"cells","buildings","policies"});var cells=new Dictionary<int,List<TargetIdentity>>();var seen=new HashSet<string>();
            foreach(var cell in row.GetProperty("cells").EnumerateArray())
            {
                WorldJson.Shape(cell,new[]{"key","actors"});int key=cell.GetProperty("key").GetInt32();WorldJson.Need(key>=0&&key<o.GridWidth*o.GridHeight&&!cells.ContainsKey(key),"Duplicate/invalid spatial cell");
                var list=new List<TargetIdentity>();
                foreach(var idJson in cell.GetProperty("actors").EnumerateArray())
                { string name=idJson.GetString();WorldJson.Need(name!=null&&byId.ContainsKey(name)&&seen.Add(name),"Invalid/duplicate spatial actor");var actor=byId[name];
                    WorldJson.Need(actor.Mobile&&actor.Health>0&&actor.CellX>=0&&actor.CellX<o.GridWidth&&actor.CellZ>=0&&actor.CellZ<o.GridHeight&&actor.CellX+actor.CellZ*o.GridWidth==key,"Spatial membership does not match live unit facts");list.Add(Id(actor)); }
                WorldJson.Need(list.Count>0,"Empty spatial cells should be omitted");cells[key]=list;
            }
            WorldJson.Need(seen.SetEquals(actors.Where(x=>x.Mobile&&x.Health>0&&x.CellX>=0&&x.CellX<o.GridWidth&&x.CellZ>=0&&x.CellZ<o.GridHeight).Select(x=>x.Id)),"Spatial snapshot must include every live in-grid infantry actor");
            var buildings=row.GetProperty("buildings").EnumerateArray().Select(x=>x.GetString()).ToList();
            WorldJson.Need(buildings.Count==buildings.Distinct().Count()&&buildings.ToHashSet().SetEquals(actors.Where(x=>!x.Mobile).Select(x=>x.Id)),"Building enumeration must contain exactly all runtime bases");
            var policies=new Dictionary<(int,int),TargetPolicyFacts>();
            foreach(var entry in row.GetProperty("policies").EnumerateArray())
            {
                WorldJson.Shape(entry,new[]{"target","facts"});string name=entry.GetProperty("target").GetString();WorldJson.Need(name!=null&&byId.ContainsKey(name),"Unknown policy target");var target=byId[name];
                WorldJson.FactsShape(entry.GetProperty("facts"),typeof(TargetPolicyFacts));var facts=entry.GetProperty("facts").Deserialize<TargetPolicyFacts>(WorldJson.Json);
                WorldJson.Need(!facts.HasSourcePolicy&&!facts.HasTargetPolicy&&!facts.PairedFog&&facts.TargetVisible==1&&facts.SourceAllowedTargets==0&&facts.TargetDomain==0&&!facts.TargetIsAir&&!facts.TargetUsesVehicleMotion&&facts.TargetIsStructure==!target.Mobile,"Policy flags do not match bounded ground actor profile");
                WorldJson.Need(policies.TryAdd((target.Index,target.Version),facts),"Duplicate policy target");
            }
            WorldJson.Need(policies.Count==actors.Count,"Complete explicit policy/visibility facts required for every actor");
            var buildingIds=buildings.Select(x=>Id(byId[x])).ToList();
            if(commonCells!=null)
            {
                WorldJson.Need(cells.Keys.ToHashSet().SetEquals(commonCells.Keys)&&cells.All(pair=>pair.Value.Select(t=>(t.Index,t.Version)).SequenceEqual(commonCells[pair.Key].Select(t=>(t.Index,t.Version)))),"Candidate cell order must match shared native spatial snapshot for every acquirer");
                WorldJson.Need(buildingIds.Select(t=>(t.Index,t.Version)).SequenceEqual(commonBuildings.Select(t=>(t.Index,t.Version))),"Building order must match shared native query snapshot");
            }
            else {commonCells=cells;commonBuildings=buildingIds;}
            contexts[source]=new Context{Source=a,Options=o,Actors=identities,ById=byId,Counts=counts,Cells=cells,Buildings=buildings.Select(x=>Id(byId[x])).ToList(),Policies=policies};
        }
        return contexts;
    }
    internal static void ValidateOrders(List<WorldFrameReplay.Actor> actors,Dictionary<string,WorldFrameReplay.Actor> byId,List<string> order,JsonElement supplied)=>Contexts(actors,byId,order,supplied);
    internal static void Acquire(List<WorldFrameReplay.Actor> actors,Dictionary<string,WorldFrameReplay.Actor> byId,List<string> order,JsonElement supplied,float cellSize,
        List<Action> pending,List<object> events)
    {
        var contexts=Contexts(actors,byId,order,supplied);
        foreach(string id in order)
        {
            var context=contexts[id];var a=byId[id];var o=context.Options;
            var input=new TargetAcquisitionInput{Source=Id(a),Position=new TargetPosition(a.X,a.Y,a.Z),GridCell=new TargetCell(a.CellX,a.CellZ),GridWidth=o.GridWidth,GridHeight=o.GridHeight,
                AggroRangeCells=o.AggroRangeCells,CellSize=cellSize,AttackRange=a.Weapon.Range,FactionId=a.Faction,CanAttack=1,AutoEngage=o.AutoEngage};
            var result=TargetAcquisitionKernel.Acquire(ref context,input);
            string target=result.Acquired?actors.Single(x=>x.Index==result.Target.Index&&x.Version==result.Target.Version).Id:"";
            if(result.RemoveRecentAttacker)pending.Add(()=>a.Recent="");
            if(result.Acquired)
            {
                var chosen=byId[target];double dx=(double)a.X-chosen.X,dz=(double)a.Z-chosen.Z;
                WorldJson.Need(dx*dx+dz*dz<=((double)a.Weapon.Range*.5)*((double)a.Weapon.Range*.5),"Acquired target outside conservative stationary profile");
                pending.Add(()=>{a.HasEngage=true;a.Target=target;});
            }events.Add(new{actor=id,result,target});
        }
    }
}
