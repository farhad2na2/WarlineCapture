using System;using System.Collections.Generic;using System.IO;using Game.Components;using Game.Configs;using Newtonsoft.Json.Linq;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;
namespace Game.Runtime { internal sealed partial class AutonomousWorld {
 private bool s073DiagnosticBlockerOwnersInitialized;
 private bool s073LegacySchedulerInitialized;
 private int s073LegacySchedulerStepCount;
 private AIEconomySystem s073EconomyOwner; private SystemState s073EconomyState;
 private AIFactionControlSystem s073FactionControlOwner; private SystemState s073FactionControlState;
 private AIBuildPlannerSystem s073BuildPlannerOwner; private SystemState s073BuildPlannerState;
 private AIProductionSystem s073ProductionOwner; private SystemState s073ProductionState;
 internal void SyncS073LegacyDiagnosticRoads(Entity grid){if(initialRoads==null||initializationAttempted||Buildings.Count!=0)throw new NotSupportedException("Prepared road owner before actor creation required");initialRoads.Sync(Entities,grid);initialRoads.RequireBeforePath();}
 internal void InitializeS073LegacyDiagnosticBlockerOwners(Entity grid){if(initializationAttempted||Buildings.Count!=0||!Entities.Exists(grid)||!Entities.HasComponent<GridConfig>(grid)||s073DiagnosticBlockerOwnersInitialized)throw new NotSupportedException("Fresh S073 grid required before source blocker lifecycle");GridEntity=grid;storageState=FreshState();storageState.Begin();storageOwner.OnCreate(ref storageState);storageState.End();dedupState=FreshState();dedupState.Begin();dedup.OnCreate(ref dedupState);dedupState.End();staticBlockerState=FreshState();staticBlockerState.Begin();staticBlockers.OnCreate(ref staticBlockerState);staticBlockerState.End();s073DiagnosticBlockerOwnersInitialized=true;UpdateSourceBlockers();}
 internal void UpdateS073LegacyDiagnosticBlockerOwners(){if(!s073DiagnosticBlockerOwnersInitialized)throw new NotSupportedException("S073 source blocker lifecycle must be initialized first");UpdateSourceBlockers();}
 internal JObject InitializeS073LegacyOwnerScheduler(){
  if(disposed||!initializationAttempted||Initialized||s073LegacySchedulerInitialized||InitializationStage!="S073LegacyLoadingGateAdmitted_SchedulerCompositionPending")throw new NotSupportedException("Admitted retained S073 legacy world required before scheduler setup");
  var match=Entities.GetComponentData<SkirmishMatchState>(Session);using(var gameplayQuery=Entities.CreateEntityQuery(typeof(RuntimeGameplayStateComponent))){if(match.Phase!=SkirmishPhase.Playing||gameplayQuery.CalculateEntityCount()!=1||gameplayQuery.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive==0)throw new NotSupportedException("Actual playing legacy match and active gameplay required");}
  if(ground!=null||deferred!=null||Entities.CreateEntityQuery(typeof(EndSimulationEntityCommandBufferSystem.Singleton)).CalculateEntityCount()!=0)throw new NotSupportedException("Fresh S073 scheduler-owned ground and deferred resources required");
  deferred=new OwnedEndSimulationBuffers();var deferredEntity=Entities.CreateEntity();Entities.AddComponentData(deferredEntity,new EndSimulationEntityCommandBufferSystem.Singleton{Owner=deferred});UnitMoveOrderRequestSystem.EnsureQueueEntity(Entities);ground=new OriginalGroundOwners(Entities,generatedFresh:true);
  s073EconomyOwner=new AIEconomySystem();s073EconomyState=FreshState();s073EconomyState.Begin();s073EconomyOwner.OnCreate(ref s073EconomyState);s073EconomyState.LastSystemVersion=0;s073EconomyState.End();
  s073FactionControlOwner=new AIFactionControlSystem();s073FactionControlState=FreshState();s073FactionControlState.Begin();s073FactionControlOwner.OnCreate(ref s073FactionControlState);s073FactionControlState.LastSystemVersion=0;s073FactionControlState.End();
  s073BuildPlannerOwner=new AIBuildPlannerSystem();s073BuildPlannerState=FreshState();s073BuildPlannerState.Begin();s073BuildPlannerOwner.OnCreate(ref s073BuildPlannerState);s073BuildPlannerState.LastSystemVersion=0;s073BuildPlannerState.End();
  s073ProductionOwner=new AIProductionSystem();s073ProductionState=FreshState();s073ProductionState.Begin();s073ProductionOwner.OnCreate(ref s073ProductionState);s073ProductionState.LastSystemVersion=0;s073ProductionState.End();
  s073LegacySchedulerInitialized=true;InitializationStage="S073LegacySchedulerAdmitted_CoreOwnersBound";
  return new JObject{["initialized"]=true,["source_system_order"]=new JArray("AIEconomySystem","AIFactionControlSystem","AIBuildPlannerSystem","AIProductionSystem"),["required_queries_present"]=new JObject{["AIEconomySystem"]=s073EconomyState.RequiredQueriesPresent,["AIFactionControlSystem"]=s073FactionControlState.RequiredQueriesPresent,["AIBuildPlannerSystem"]=s073BuildPlannerState.RequiredQueriesPresent,["AIProductionSystem"]=s073ProductionState.RequiredQueriesPresent},["downstream_owners_pending"]=new JArray("BuildingRuntimeProcessingCompositionSystemHelper.UnitProductionQueue","AISquadSystem","AITargetingSystem","AICombatOrderSystem"),["downstream_owner_gate"]= "Original AIProduction decision owner is running; the queue/delivery feedback owner and eligible squad/objective inputs are still pending",["initialization_stage"]=InitializationStage,["original_ground_owners_initialized"]=true,["autonomous_aria"]=false,["full_mission"]=false};
 }
 private static JObject RunS073LegacyOwner(string name,bool ready,Action update){bool ran=ready;if(ran)update();return new JObject{["owner"]=name,["required_queries_present"]=ready,["updated"]=ran};}
 internal JObject AdvanceS073LegacyOwnerTick(float deltaTime){
  if(disposed||!s073LegacySchedulerInitialized||ground==null||!(deltaTime>0)||float.IsNaN(deltaTime)||float.IsInfinity(deltaTime))throw new NotSupportedException("Initialized S073 original owner scheduler and finite positive step required");
  var match=Entities.GetComponentData<SkirmishMatchState>(Session);using(var gameplayQuery=Entities.CreateEntityQuery(typeof(RuntimeGameplayStateComponent))){if(match.Phase!=SkirmishPhase.Playing||gameplayQuery.CalculateEntityCount()!=1||gameplayQuery.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive==0)throw new NotSupportedException("Actual S073 legacy gameplay stopped before owner tick");}
  TimeData time=Entities.WorldUnmanaged.Time;time.ElapsedTime+=deltaTime;time.DeltaTime=deltaTime;Entities.WorldUnmanaged.Time=time;
  var owners=new JArray();
  owners.Add(RunS073LegacyOwner("AIEconomySystem",s073EconomyState.Enabled&&s073EconomyState.RequiredQueriesPresent,()=>{s073EconomyState.Begin();try{s073EconomyOwner.OnUpdate(ref s073EconomyState);}finally{s073EconomyState.End();}}));
  owners.Add(RunS073LegacyOwner("AIFactionControlSystem",s073FactionControlState.Enabled&&s073FactionControlState.RequiredQueriesPresent,()=>{s073FactionControlState.Begin();try{s073FactionControlOwner.OnUpdate(ref s073FactionControlState);}finally{s073FactionControlState.End();}}));
  owners.Add(RunS073LegacyOwner("AIBuildPlannerSystem",s073BuildPlannerState.Enabled&&s073BuildPlannerState.RequiredQueriesPresent,()=>{s073BuildPlannerState.Begin();try{s073BuildPlannerOwner.OnUpdate(ref s073BuildPlannerState);}finally{s073BuildPlannerState.End();}}));
  owners.Add(RunS073LegacyOwner("AIProductionSystem",s073ProductionState.Enabled&&s073ProductionState.RequiredQueriesPresent,()=>{s073ProductionState.Begin();try{s073ProductionOwner.OnUpdate(ref s073ProductionState);}finally{s073ProductionState.End();}}));
  UnitMoveOrderRequestSystem.ProcessPendingRequests(Entities);ground.OccupancyPhase();ground.PathPhase();ground.MovementPhase();ground.EndSimulationPhase();s073LegacySchedulerStepCount++;InitializationStage="S073LegacyAIAndGroundOwnerPrefix";
  return new JObject{["step"]=s073LegacySchedulerStepCount,["delta_seconds"]=deltaTime,["elapsed_seconds"]=Entities.WorldUnmanaged.Time.ElapsedTime,["source_ai_owner_updates"]=owners,["original_move_request_owner_invoked"]=true,["original_occupancy_path_movement_owners_invoked"]=true,["end_simulation_buffers_played_back"]=true,["actual_outcome"]=Entities.GetComponentData<SkirmishMatchState>(Session).Outcome.ToString(),["full_tick"]=false,["autonomous_aria"]=false,["full_mission"]=false};
 }
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
 internal void MarkS073LegacyLoadingGateAdmitted(){
  if(disposed||!initializationAttempted||Initialized||Session==Entity.Null||!Entities.World.IsCreated||
     !Entities.Exists(Session)||!Entities.HasComponent<SkirmishMatchState>(Session)||
     Entities.GetComponentData<SkirmishMatchState>(Session).Phase!=SkirmishPhase.Playing)
   throw new NotSupportedException("Actual S073 legacy rules transition must follow loading-gate admission");
  using var gameplayQuery=Entities.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
  if(gameplayQuery.CalculateEntityCount()!=1)throw new NotSupportedException("Unique actual gameplay state required after S073 gate admission");
  var gameplay=Entities.GetComponentData<RuntimeGameplayStateComponent>(gameplayQuery.GetSingletonEntity());
  if(gameplay.PlayRequested==0||gameplay.SimulationActive==0)throw new NotSupportedException("Actual gameplay start request must be active after gate admission");
  InitializationStage="S073LegacyLoadingGateAdmitted_SchedulerCompositionPending";
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
  internal static SourcePlacementDomain LoadSharedPlacementSource(OperationMapDefinition selected,string physicalPath,string physicalSha,string waterPath,string waterSha,string startupPath,string startupSha,string currentSourceRoot,string capturedAssetRoot) {
   RequireSharedPlacementSource(selected);
   var domain=SourcePlacementDomain.LoadS002SourceDiagnostic(physicalPath,physicalSha,waterPath,waterSha,startupPath,startupSha,currentSourceRoot,capturedAssetRoot);
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
