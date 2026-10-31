using System;using System.Collections.Generic;using System.IO;using Game.Components;using Game.Configs;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;
namespace Game.Runtime { internal sealed partial class AutonomousWorld {
 private bool s073DiagnosticBlockerOwnersInitialized;
 internal void SyncS073LegacyDiagnosticRoads(Entity grid){if(initialRoads==null||initializationAttempted||Buildings.Count!=0)throw new NotSupportedException("Prepared road owner before actor creation required");initialRoads.Sync(Entities,grid);initialRoads.RequireBeforePath();}
 internal void InitializeS073LegacyDiagnosticBlockerOwners(Entity grid){if(initializationAttempted||Buildings.Count!=0||!Entities.Exists(grid)||!Entities.HasComponent<GridConfig>(grid)||s073DiagnosticBlockerOwnersInitialized)throw new NotSupportedException("Fresh S073 grid required before source blocker lifecycle");GridEntity=grid;storageState=FreshState();storageState.Begin();storageOwner.OnCreate(ref storageState);storageState.End();dedupState=FreshState();dedupState.Begin();dedup.OnCreate(ref dedupState);dedupState.End();staticBlockerState=FreshState();staticBlockerState.Begin();staticBlockers.OnCreate(ref staticBlockerState);staticBlockerState.End();s073DiagnosticBlockerOwnersInitialized=true;UpdateSourceBlockers();}
 internal void UpdateS073LegacyDiagnosticBlockerOwners(){if(!s073DiagnosticBlockerOwnersInitialized)throw new NotSupportedException("S073 source blocker lifecycle must be initialized first");UpdateSourceBlockers();}
 // Candidate-only handoff: retain the actual startup manager and registry in this host.
 // Captured static masks do not establish live occupancy, active water or scheduler admission.
 internal void RetainS073LegacyDiagnosticStartup(EntityManager manager, Entity grid, Entity surface, Entity session, Entity boundary, Entity owner, IReadOnlyCollection<Entity> newlySpawned) {
  if(disposed||initializationAttempted||Initialized||!ReferenceEquals(manager,Entities))throw new NotSupportedException("Fresh same-world S073 host required");
  if(initialRoads==null)throw new NotSupportedException("Original initial-road authority required before source startup");initialRoads.RequireBeforePath();
  if(!Entities.Exists(grid)||!Entities.HasComponent<GridConfig>(grid)||!Entities.Exists(surface)||!Entities.HasComponent<MapSurfaceComponent>(surface)||!Entities.Exists(session)||!Entities.HasComponent<SkirmishMatchState>(session)||Entities.HasComponent<SkirmishExpandedSessionComponent>(session)||!Entities.Exists(owner)||!Entities.HasComponent<InitialUnitsSpawnInitialized>(owner)||!Entities.Exists(boundary)||!Entities.HasBuffer<BuildingRuntimeSpawnRequest>(boundary))throw new NotSupportedException("Completed legacy startup identities required");
  if(Entities.GetComponentData<SkirmishMatchState>(session).StartupFailure!=SkirmishStartupFailureCode.None||Entities.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length!=0||Buildings.Count!=10||newlySpawned==null||newlySpawned.Count!=24)throw new NotSupportedException("Completed legacy startup results required");
  foreach(var e in newlySpawned)if(!Entities.Exists(e)||!Entities.HasComponent<UnitGrid>(e)||!Entities.HasComponent<Faction>(e)||!Entities.HasComponent<UnitSourcePrefabKey>(e))throw new NotSupportedException("Actual newly spawned units required");
  initializationAttempted=true;GridEntity=grid;SurfaceEntity=surface;Session=session;Boundary=boundary;InitializationStage="S073LegacyDiagnosticRetained_SchedulerAdmissionPending";
 }
 } }

namespace Game.Runtime {
 internal static class SourceS073LegacySpawnRequestAdapter {
  internal static void RequireSharedPlacementSource(OperationMapDefinition selected) {
   if(selected==null||selected.OperationMapId!="opmap.skirmish.industrial_basin"||selected.SourceIdentityHash!="2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900")throw new NotSupportedException("Actual S073 logical operation-map source required");
   var binding=selected.SourceBinding;if(!binding.IsConfigured||binding.SourceOperationMapId!="opmap.skirmish.desert_base_01"||binding.SourceIdentityHash!=selected.SourceIdentityHash||binding.SourceContentHash!="2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c")throw new NotSupportedException("S073 must retain its reviewed Desert Base Campaign source binding");
   var grid=selected.GridMetadata;var origin=grid.Origin;if(grid.AssetGuid!="b201000000000000000000000000000b"||grid.ContentHash!="8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb"||grid.Dimensions.x!=2048||grid.Dimensions.y!=1024||grid.CellSize!=1||origin.x!=0||origin.y!=0||origin.z!=0)throw new NotSupportedException("S073 baked Campaign grid differs from the retained placement-owner capture");
   var surface=selected.SurfaceMetadata;if(surface.AssetGuid!="12f517deb32ab49698acbfdaf7c3eac7"||surface.ContentHash!="5447c779e2b68ba6b7c6f86a1146f6bf570d49b4455a676a2680ea595fa47d1d"||surface.SurfaceCount!=2048*1024||selected.MapSurfaceDataReference==null||selected.MapSurfaceDataReference.AssetGUID!=surface.AssetGuid)throw new NotSupportedException("S073 baked Campaign surface differs from the retained placement-owner capture");
   if(selected.NavigationMetadata.AuthoredSubSceneGuid!="c00140f2e94a04c3084c8dcb0c18cbd0")throw new NotSupportedException("S073 authored ECS placement subscene differs from the retained owner capture");
  }
  // Retain S002 artifact provenance while admitting only the S073 logical map's exact reviewed SourceBinding.
  // The selected runtime scene reference is visual parity data; these placement owners read the shared ECS subscene.
  internal static SourcePlacementDomain LoadSharedPlacementSource(OperationMapDefinition selected,string physicalPath,string physicalSha,string waterPath,string waterSha,string startupPath,string startupSha,string currentSourceRoot) {
   RequireSharedPlacementSource(selected);
   var domain=SourcePlacementDomain.LoadS002SourceDiagnostic(physicalPath,physicalSha,waterPath,waterSha,startupPath,startupSha,currentSourceRoot);
   var masks=domain.Build(2048,1024,1,0,0,0);var activeWater=domain.BuildWaterMask(true,2048,1024,1,0,0,0);
   if(!domain.UsesOriginalOwnerMaskCapture||!domain.WaterDomainKnown||domain.WaterFootprintCount!=194||masks.AuthoredBuildings.Length!=2048*1024||masks.Sidewalks.Length!=2048*1024||domain.CopyOriginalOwnerRoadMask().Length!=2048*1024||activeWater==null||activeWater.Length!=2048*1024)throw new InvalidDataException("Complete shared Campaign placement owner inputs required");
   return domain;
  }
  internal static int Queue(EntityManager em, Entity plan, Entity boundary, bool exactSkirmishSite) {
   if(em==default||!em.Exists(plan)||!em.HasComponent<InitialUnitsSpawnConfig>(plan)||!em.HasBuffer<InitialUnitsFactionSpawnEntry>(plan)||!em.HasBuffer<InitialUnitsFactionBuildingSpawnEntry>(plan)||!em.Exists(boundary)||!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary))throw new NotSupportedException("Actual original legacy startup plan and runtime boundary required");
   using var factionSpawns=em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan).ToNativeArray(Allocator.Temp);
   if(!BuildingDefinitionPrefabSystemHelper.EnqueueConfiguredInitialBuildingRequests(em,boundary,plan,factionSpawns,exactSkirmishSite,message=>{throw new InvalidDataException(message);},out int count))throw new NotSupportedException("Shared original legacy building request owner rejected the source plan");
   return count;
  }
 }
}
