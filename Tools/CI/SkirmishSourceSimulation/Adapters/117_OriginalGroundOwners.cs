using System;using Unity.Entities;using Unity.Collections;using Game.Components;
namespace Game.Runtime{
 // Retained original systems, not a replacement path/movement algorithm. Actual bundle import
 // must restore/admit their complete INITIAL algorithmic private state before these phases run.
 internal sealed partial class OriginalGroundOwners:IDisposable {
  readonly EntityManager em;readonly OwnedEndSimulationBuffers end;
  UnitPathfindingSystem path;DynamicOccupancyRebuildSystem occupancy;UnitGridMovementSystem movement;
  SystemState pathState,occupancyState,movementState;bool disposed;
  internal OriginalGroundOwners(EntityManager manager,bool generatedFresh=false){em=manager??throw new ArgumentNullException();
   var gridEntity=em.CreateEntityQuery(typeof(GridConfig)).GetSingletonEntity();var g=em.GetComponentData<GridConfig>(gridEntity);long count=(long)g.Width*g.Height;
   if(g.Width<1||g.Height<1||count>4194304||!(g.CellSize>0)||float.IsNaN(g.CellSize)||float.IsInfinity(g.CellSize))throw new NotSupportedException("Finite complete initial grid required");
   if(em.GetBuffer<GridWalkable>(gridEntity).Length!=count||em.GetBuffer<GridRoad>(gridEntity).Length!=count||em.GetBuffer<GridRoadSidewalk>(gridEntity).Length!=count||em.GetBuffer<GridRoadDirt>(gridEntity).Length!=count)throw new NotSupportedException("Complete source grid buffers required");
   var blocked=em.GetComponentData<DynamicBlockerComponent>(gridEntity);var occ=em.GetComponentData<DynamicOccupancyComponent>(gridEntity);var pool=em.GetComponentData<PathPoolComponent>(gridEntity);
   if(!blocked.Blocked.IsCreated||blocked.Blocked.Length!=count||!occ.Occupied.IsCreated||occ.Occupied.Length!=count||occ.GridSize!=count||!pool.Cells.IsCreated)throw new NotSupportedException("Actual blocker/occupied/pool initialization required");
   int pendingCount=em.CreateEntityQuery(typeof(UnitPathfindingPendingStateComponent)).CalculateEntityCount(),logCount=em.CreateEntityQuery(typeof(UnitPathfindingDiagnosticLogQueueComponent),typeof(UnitPathfindingDiagnosticLogComponent)).CalculateEntityCount();
   if(generatedFresh?(pendingCount>1||logCount>1):(pendingCount!=1||logCount!=1))throw new NotSupportedException("Source path singleton/log domain differs from selected fresh/reference initialization mode");
   if(pendingCount==1&&em.CreateEntityQuery(typeof(UnitPathfindingPendingStateComponent)).GetSingleton<UnitPathfindingPendingStateComponent>().HasPendingPathJob!=0)throw new NotSupportedException("Initial active path job requires original pending input/result/handle ownership; not admitted yet");
   end=em.CreateEntityQuery(typeof(EndSimulationEntityCommandBufferSystem.Singleton)).GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().Owner??throw new NotSupportedException("Complete initial deferred owner required");
   pathState=new SystemState{EntityManager=em,Enabled=true};occupancyState=pathState;movementState=pathState;
   pathState.Begin();path.OnCreate(ref pathState);pathState.End();occupancyState.Begin();occupancy.OnCreate(ref occupancyState);occupancyState.End();movementState.Begin();movement.OnCreate(ref movementState);movementState.End();
   // Source fresh systems have no prior completed update; captured reference restore may subsequently overwrite these.
   pathState.LastSystemVersion=0;occupancyState.LastSystemVersion=0;movementState.LastSystemVersion=0;
  }
  void Alive(){if(disposed)throw new ObjectDisposedException(nameof(OriginalGroundOwners));}
  internal void PathPhase(){Alive();pathState.Begin();if(pathState.Enabled&&pathState.RequiredQueriesPresent)path.OnUpdate(ref pathState);pathState.End();}
  internal void OccupancyPhase(){Alive();occupancyState.Begin();if(occupancyState.Enabled&&occupancyState.RequiredQueriesPresent)occupancy.OnUpdate(ref occupancyState);occupancyState.End();}
  internal void MovementPhase(){Alive();movementState.Begin();if(movementState.Enabled&&movementState.RequiredQueriesPresent)movement.OnUpdate(ref movementState);movementState.End();}
  internal void EndSimulationPhase(){Alive();end.Playback(em);}
  internal bool HasPendingPath=>em.CreateEntityQuery(typeof(UnitPathfindingPendingStateComponent)).GetSingleton<UnitPathfindingPendingStateComponent>().HasPendingPathJob!=0;
  public void Dispose(){if(disposed)return;pathState.Begin();path.OnDestroy(ref pathState);occupancyState.Begin();occupancy.OnDestroy(ref occupancyState);disposed=true;}
 }
}
