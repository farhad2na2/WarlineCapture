using System;using System.Linq;using System.Collections.Generic;using Game.Components;using Game.Runtime;using Unity.Entities;using Unity.Collections;using Unity.Mathematics;using Unity.Transforms;
// A source-linked native caller boundary. It returns deferred commands; it does not invent a World scheduler.
internal static class CapturedVehicleCaller {
 internal sealed class Authority {public string InputEvidenceSha256;public bool CompleteComponentValues,ExactMoveQueryMembership,ExactCleanupQueryMembership,ImmutableOccupancyAtGridDispatch,CompleteLiveQuerySnapshot,CompleteLookupPresence,ActualClockInput,CleanupAfterMoveBarrier;public int MoveQueryIndex=-1,CleanupQueryIndex=-1;public bool RequireEcbPlayback;}
 internal sealed class State {public Entity Entity;public LocalTransform Transform;public UnitGrid Grid;public UnitPathFollow Follow;public UnitVehicleKinematics Kinematics;public UnitMove Move;public UnitPathRange Range;public UnitFootprint Footprint;public UnitMovementBehavior Behavior;public UnitVehicleMovement VehicleMovement;public Faction Faction;}
 internal sealed class Snapshot {public GridConfig Grid;public float DeltaTime;public int2[] Pool;public byte[] Walkable,Friendly,Roads,Sidewalk,Dirt;public bool[] Blocked,Occupied;public Entity[] LiveEntities;public UnitGrid[] LiveGrids;public UnitFootprint[] LiveFootprints;
 public HashSet<Entity> KnownEntities;public Dictionary<Entity,AutoWanderMoveTag> AutoWander;public Dictionary<Entity,UnitTarget> Targets;public Dictionary<Entity,UnitLongDistanceMove> LongDistance;public Dictionary<Entity,ManualMoveGroupMemberTag> ManualGroup;public Dictionary<Entity,ManualMoveOrderTag> Manual;public Dictionary<Entity,CampaignMissionGuidedMoveInProgressTag> Guided;public Dictionary<Entity,UnitTransportBoardingTarget> Boarding;}
 internal sealed class Result {public State State;public DeferredVehicleCommand[] Deferred;public bool NativeParity=false,FrameAdvanced=false,PlaybackApplied=false;}
 static void Need(bool x,string message){if(!x)throw new NotSupportedException(message);}
 static bool Finite(float x)=>float.IsFinite(x)&&Math.Abs(x)<=1048576;
 static bool Point(float3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
 static ComponentLookup<T> Lookup<T>(Snapshot s,Dictionary<Entity,T> present)where T:struct {Need(present!=null,"Explicit present/absent lookup facts required: "+typeof(T).Name);return new ComponentLookup<T>(s.KnownEntities,present);}
 internal static Result Step(Authority authority,Snapshot map,State before){
 Need(authority!=null&&authority.CompleteComponentValues&&authority.ExactMoveQueryMembership&&authority.ExactCleanupQueryMembership&&authority.ImmutableOccupancyAtGridDispatch&&authority.CompleteLiveQuerySnapshot&&authority.CompleteLookupPresence&&authority.ActualClockInput&&authority.CleanupAfterMoveBarrier,"Missing native query/snapshot/clock/cleanup-barrier facts; no inferred admission");
 Need(authority.InputEvidenceSha256!=null&&System.Text.RegularExpressions.Regex.IsMatch(authority.InputEvidenceSha256,"^[0-9a-f]{64}$"),"Exact before-input artifact SHA required");
 Need(!authority.RequireEcbPlayback,"Global EndSimulation ECB ordering/playback not shared; deferred commands only");
 Need(authority.MoveQueryIndex>=0&&authority.MoveQueryIndex<512&&authority.CleanupQueryIndex>=0&&authority.CleanupQueryIndex<1024,"Actual distinct move/cleanup query indices required");
 Need(map!=null&&before!=null&&before.Behavior.UsesVehicleMotion!=0,"Actual ground vehicle caller only; no infantry/air/chase fallback");
 int cells=checked(map.Grid.Width*map.Grid.Height);Need(map.Grid.Width>0&&map.Grid.Height>0&&cells<=16777216&&Finite(map.Grid.CellSize)&&map.Grid.CellSize>0&&Point(map.Grid.Origin),"Bounded exact grid required");
 Need(float.IsFinite(map.DeltaTime)&&map.DeltaTime>0&&map.DeltaTime<=1,"Actual finite positive grid movement delta <=1 required; pause gate stays outside caller");
 Need(map.Pool!=null&&map.Pool.Length<=1048576&&map.Walkable?.Length==cells&&map.Friendly?.Length==cells&&map.Roads?.Length==cells&&map.Sidewalk?.Length==cells&&map.Dirt?.Length==cells&&map.Blocked?.Length==cells&&map.Occupied?.Length==cells,"Complete immutable native grid buffers/pool required");
 Need(map.LiveEntities!=null&&map.LiveEntities.Length<=512&&map.LiveGrids?.Length==map.LiveEntities.Length&&map.LiveFootprints?.Length==map.LiveEntities.Length&&map.KnownEntities!=null&&map.KnownEntities.Contains(before.Entity),"Complete global live query snapshot and lookup presence census required");
 Need(map.LiveEntities.Distinct().Count()==map.LiveEntities.Length,"Unique live identities required");
 foreach(var e in map.LiveEntities)Need(map.KnownEntities.Contains(e),"Presence census omits live entity");
 foreach(var fp in map.LiveFootprints)Need(fp.Size.x>=1&&fp.Size.y>=1&&fp.Size.x<=16&&fp.Size.y<=16,"Live footprint work bound1..16 exceeded");
 Need(before.Footprint.Size.x>=1&&before.Footprint.Size.y>=1&&before.Footprint.Size.x<=16&&before.Footprint.Size.y<=16&&Point(before.Transform.Position)&&Finite(before.Transform.Scale)&&before.Transform.Scale>0&&math.all(math.isfinite(before.Transform.Rotation.value)),"Finite real body/rotation and bounded footprint required");
 Need(Finite(before.Move.Speed)&&Finite(before.Move.WalkSpeed)&&Finite(before.Move.RoadSpeedMultiplier)&&Finite(before.Move.ArriveDistance)&&Finite(before.Kinematics.CurrentSpeed)&&Finite(before.Kinematics.StallSeconds)&&Finite(before.VehicleMovement.TurnSpeedDegrees)&&Finite(before.VehicleMovement.Acceleration)&&Finite(before.VehicleMovement.Braking)&&Finite(before.VehicleMovement.RearPivotOffset),"All actual movement tuning and kinematics required");
 foreach(var cell in map.Pool)Need(GridUtils.InBounds(cell,map.Grid.Width,map.Grid.Height),"Path-pool cells must be in native grid");
 // State is copied transactionally; rejection never commits a partial actor or command batch.
 var state=new State{Entity=before.Entity,Transform=before.Transform,Grid=before.Grid,Follow=before.Follow,Kinematics=before.Kinematics,Move=before.Move,Range=before.Range,Footprint=before.Footprint,Behavior=before.Behavior,VehicleMovement=before.VehicleMovement,Faction=before.Faction};
 var commands=new DeferredVehicleCommands();
 var job=new UnitGridMoveJob{DeltaTime=map.DeltaTime,Grid=map.Grid,Ecb=new EntityCommandBuffer.ParallelWriter(commands,"grid_move"),Pool=new(map.Pool),Walkable=new(map.Walkable.Length,i=>new GridWalkable{Value=map.Walkable[i]}),DynamicBlocked=new(map.Blocked),FriendlyPassFactionIds=new(map.Friendly),Occupied=new(map.Occupied),Roads=new(map.Roads.Length,i=>new GridRoad{Value=map.Roads[i]}),Sidewalks=new(map.Sidewalk.Length,i=>new GridRoadSidewalk{Value=map.Sidewalk[i]}),DirtRoads=new(map.Dirt.Length,i=>new GridRoadDirt{Value=map.Dirt[i]}),LiveUnitEntities=new(map.LiveEntities),LiveUnitGrids=new(map.LiveGrids),LiveUnitFootprints=new(map.LiveFootprints),AutoWanderLookup=Lookup(map,map.AutoWander),UnitTargetLookup=Lookup(map,map.Targets),LongDistanceMoveLookup=Lookup(map,map.LongDistance),ManualMoveGroupLookup=Lookup(map,map.ManualGroup),ManualMoveLookup=Lookup(map,map.Manual),CampaignGuidedMoveLookup=Lookup(map,map.Guided),BoardingTargetLookup=Lookup(map,map.Boarding)};
 job.Execute(authority.MoveQueryIndex,state.Entity,ref state.Transform,ref state.Grid,ref state.Follow,ref state.Kinematics,state.Move,state.Range,state.Footprint,state.Behavior,state.VehicleMovement,state.Faction);
 // Native cleanup is scheduled after the move handle and sees derived UnitGrid;
 // deferred commands from either job have not played back, so lookup presence is unchanged.
 var gridValues=new Dictionary<Entity,UnitGrid>{{state.Entity,state.Grid}};
 var cleanup=new UnitPathFollowCleanupJob{Ecb=new EntityCommandBuffer.ParallelWriter(commands,"path_cleanup"),UnitGridLookup=Lookup(map,gridValues),LongDistanceMoveLookup=Lookup(map,map.LongDistance),ManualMoveLookup=Lookup(map,map.Manual),CampaignGuidedMoveLookup=Lookup(map,map.Guided)};
 cleanup.Execute(authority.CleanupQueryIndex,state.Entity,ref state.Kinematics,state.Follow,state.Range);
 return new Result{State=state,Deferred=commands.Values.ToArray()};
 }
}
