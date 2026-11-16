using System;using System.Collections.Generic;using System.IO;using System.Linq;using Newtonsoft.Json.Linq;using Game.Components;using Game.Configs;using Game.Simulation;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;using Unity.Transforms;
namespace Game.Runtime { internal sealed partial class AutonomousWorld {
 private GroundMissionContext s073LegacyAriaContext;private bool s073LegacyAriaContextBound;private SourcePublicPlayerContactOwner s073LegacyAriaPlayerMarkers;
 internal void SyncS073LegacyDiagnosticRoads(Entity grid){if(initialRoads==null||initializationAttempted||Buildings.Count!=0)throw new NotSupportedException("Prepared road owner before actor creation required");initialRoads.Sync(Entities,grid);}
 // Bind the policy clock to a real step performed by the retained source-world scheduler.
 // This only records the same EntityManager clock; it does not change session state.
 internal void RecordS073LegacyDiagnosticTick(int frame){
  if(frame<=Tick||Entities.World==null||!Entities.World.IsCreated||!Entities.Exists(Session))throw new InvalidOperationException("Strictly increasing live S073 source-host frame required");
  var match=Entities.GetComponentData<SkirmishMatchState>(Session);
  if(match.Phase!=SkirmishPhase.Playing||match.Outcome!=SkirmishOutcome.None)throw new InvalidOperationException("ARIA clock requires original live nonterminal Playing match");
  using(var gameplay=Entities.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))if(gameplay.CalculateEntityCount()!=1||gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive==0)throw new InvalidOperationException("Unique active source-host gameplay state required");
  var time=Entities.WorldUnmanaged.Time;
  if(double.IsNaN(time.ElapsedTime)||double.IsInfinity(time.ElapsedTime)||time.ElapsedTime<=SimulationSeconds||float.IsNaN(time.DeltaTime)||float.IsInfinity(time.DeltaTime)||time.DeltaTime<=0)throw new InvalidOperationException("ARIA clock must follow a real monotonic ECS simulation step");
  Tick=frame;SimulationSeconds=time.ElapsedTime;FixedDeltaTime=time.DeltaTime;
 }
 // Candidate-only handoff: retain the actual startup manager and registry in this host.
 // Captured static masks do not establish live occupancy, active water or scheduler admission.
 internal void RetainS073LegacyDiagnosticStartup(EntityManager manager, Entity grid, Entity surface, Entity session, Entity boundary, Entity owner, IReadOnlyCollection<Entity> newlySpawned) {
  if(disposed||initializationAttempted||Initialized||!ReferenceEquals(manager,Entities))throw new NotSupportedException("Fresh same-world S073 host required");
  if(initialRoads==null)throw new NotSupportedException("Original initial-road authority required before source startup");initialRoads.RequireBeforePath();
  if(!Entities.Exists(grid)||!Entities.HasComponent<GridConfig>(grid)||!Entities.Exists(surface)||!Entities.HasComponent<MapSurfaceComponent>(surface)||!Entities.Exists(session)||!Entities.HasComponent<SkirmishMatchState>(session)||Entities.HasComponent<SkirmishExpandedSessionComponent>(session)||!Entities.Exists(owner)||!Entities.HasComponent<InitialUnitsSpawnInitialized>(owner)||!Entities.Exists(boundary)||!Entities.HasBuffer<BuildingRuntimeSpawnRequest>(boundary))throw new NotSupportedException("Completed legacy startup identities required");
  var requests=Entities.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
  if(Entities.GetComponentData<SkirmishMatchState>(session).StartupFailure!=SkirmishStartupFailureCode.None||requests.Any(r=>r.Status==BuildingRuntimeSpawnRequest.Pending)||Buildings.Count!=10||newlySpawned==null||newlySpawned.Count!=24)throw new NotSupportedException("Completed legacy startup results required");
  foreach(var e in newlySpawned)if(!Entities.Exists(e)||!Entities.HasComponent<UnitGrid>(e)||!Entities.HasComponent<Faction>(e)||!Entities.HasComponent<UnitSourcePrefabKey>(e))throw new NotSupportedException("Actual newly spawned units required");
  initializationAttempted=true;GridEntity=grid;SurfaceEntity=surface;Session=session;Boundary=boundary;InitializationStage="S073LegacyDiagnosticRetained_SchedulerAdmissionPending";
 }
 // Actual logical-public inputs for the shared S073 ARIA policy. This returns only
 // source player markers and the designated-base objective label; it does not
 // synthesize commands, camera presentation, readiness, or a mission outcome.
 internal JObject ReadS073LegacyAriaPublicFacts(SkirmishBattleCatalogConfig canonicalCatalog,int frame,float ariaTime){
  if(disposed||!initializationAttempted||Initialized||Entities==null||!Entities.Exists(Session)||
     !Entities.HasComponent<SkirmishMatchState>(Session)||Entities.HasComponent<SkirmishExpandedSessionComponent>(Session)||
     canonicalCatalog==null||frame<=0||!float.IsFinite(ariaTime))throw new NotSupportedException("Active original S073 legacy world and ordered public-observation inputs required");
  var match=Entities.GetComponentData<SkirmishMatchState>(Session);
  if(!s073LegacyAriaContextBound){if(!GroundMissionContextFactory.TryCreateS073LegacyLogicalPublicData(canonicalCatalog,match,out s073LegacyAriaContext,out string error))throw new NotSupportedException("Original S073 ARIA context rejected: "+error);s073LegacyAriaContextBound=true;s073LegacyAriaPlayerMarkers=new SourcePublicPlayerContactOwner(Entities,Session,s073LegacyAriaContext);}
  if(s073LegacyAriaContext.SessionId!=match.SessionId.ToString()||match.Phase!=SkirmishPhase.Playing||match.Outcome!=SkirmishOutcome.None||match.StartupFailure!=SkirmishStartupFailureCode.None)throw new NotSupportedException("Bound S073 ARIA context no longer matches its active legacy session");
  s073LegacyAriaPlayerMarkers.UpdateAfterFog(frame,ariaTime);var snapshot=s073LegacyAriaPlayerMarkers.ReadSnapshot();var objectives=SkirmishPublicObjectiveProjection.Read(Entities,Session);
  return new JObject{["scope"]="LogicalPublicData",["source_scenario_index"]=3,["session_id"]=match.SessionId.ToString(),["frame"]=snapshot.Frame,["aria_time"]=snapshot.AriaTime,["world_time"]=snapshot.WorldTime,["marker_identity"]=snapshot.Identity,["marker_generation"]=snapshot.Generation,["player_markers"]=new JArray(snapshot.Players.Select(p=>new JObject{["x"]=p.X,["y"]=p.Y,["z"]=p.Z})),["designated_enemy_base"] = new JObject{["alive"]=objectives.EnemyObjectiveAlive,["visible"]=objectives.EnemyTargetVisible,["position_known"]=objectives.EnemyObjectiveAlive,["position"]=objectives.EnemyObjectiveAlive?new JArray(objectives.EnemyPosition.X,objectives.EnemyPosition.Y,objectives.EnemyPosition.Z):null},["readiness_claimed"]=false,["command_issued"]=false,["outcome_claimed"]=false};
 }
 // Convert live legacy ECS facts into the common pure ARIA policy contract.
 // ExpandedSession/ExpandedBaseAssault deliberately stay false; the page-shaped
 // fields below describe only the actual legacy five-slot roster for the shared
 // AriaGroundColumnPolicy. No expanded-session identity is synthesized.
 internal AriaS002Observation ReadS073LegacyAriaPolicyObservation(SkirmishBattleCatalogConfig canonicalCatalog,int frame,float ariaTime,bool attackMode,int acceptedAttackOrderMask,out int2 advanceCell,out float3 advancePosition){
  ReadS073LegacyAriaPublicFacts(canonicalCatalog,frame,ariaTime);var match=Entities.GetComponentData<SkirmishMatchState>(Session);var snapshot=s073LegacyAriaPlayerMarkers.ReadSnapshot();var objectives=SkirmishPublicObjectiveProjection.Read(Entities,Session);
  if(!objectives.PlayerPositionKnown)throw new NotSupportedException("Original public player-base position is unavailable");
  if(objectives.EnemyObjectiveAlive&&!objectives.EnemyTargetVisible)throw new NotSupportedException("Legacy ARIA policy has no unknown enemy-health representation; visible objective sample required");
  int playerHealth=0,enemyHealth=0;bool playerAlive=Entities.Exists(match.PlayerMainBase)&&Entities.HasComponent<UnitHealth>(match.PlayerMainBase);bool enemyAlive=objectives.EnemyObjectiveAlive;
  if(playerAlive){var h=Entities.GetComponentData<UnitHealth>(match.PlayerMainBase);playerHealth=Math.Max(0,h.Current);playerAlive=h.Current>0;}
  if(enemyAlive){if(!Entities.Exists(match.EnemyMainBase)||!Entities.HasComponent<UnitHealth>(match.EnemyMainBase))throw new NotSupportedException("Live designated enemy base health is unavailable");enemyHealth=Math.Max(0,Entities.GetComponentData<UnitHealth>(match.EnemyMainBase).Current);}
  int rosterMask=0,structureMask=0,selectedMask=0,slotCount=0,infantry=SourceS073LegacyHudInfantry.Count(Entities),selectedCount=0,structureUnits=0;
  using(var query=Entities.CreateEntityQuery(typeof(SkirmishSquadMember),typeof(Faction),typeof(UnitHealth),typeof(UnitGrid),typeof(UnitMove))){using var units=query.ToEntityArray(Allocator.Temp);foreach(var unit in units){var faction=Entities.GetComponentData<Faction>(unit);var health=Entities.GetComponentData<UnitHealth>(unit);var member=Entities.GetComponentData<SkirmishSquadMember>(unit);if(faction.Id!=FactionIdentity.PlayerFactionId||health.Current<=0||Entities.HasComponent<RuntimeBuildingCombatTag>(unit)||Entities.HasComponent<StaticGridBlocker>(unit)||Entities.HasComponent<UnitTransportPassenger>(unit))continue;if(member.Slot>4)throw new NotSupportedException("Original S073 legacy roster uses a slot outside the source selector domain");int bit=1<<member.Slot;rosterMask|=bit;bool selected=Entities.HasComponent<SelectedUnitTag>(unit);if(selected){selectedMask|=bit;selectedCount++;}if(Entities.HasComponent<UnitAttack>(unit)&&Entities.GetComponentData<UnitAttack>(unit).Damage>0&&Entities.HasComponent<UnitCombat>(unit)&&Entities.GetComponentData<UnitCombat>(unit).CanAttack!=0&&Entities.HasComponent<CombatTargetPolicy>(unit)&&(Entities.GetComponentData<CombatTargetPolicy>(unit).AllowedTargets&CombatTargetDomain.Structure)!=0){structureMask|=bit;structureUnits++;}}}
  for(int slot=0;slot<5;slot++){int bit=1<<slot;if((rosterMask&bit)!=0)slotCount++;}
  int selectedSlots=0;for(int slot=0;slot<5;slot++)if((selectedMask&(1<<slot))!=0)selectedSlots++;
  var distances=enemyAlive?AriaGroundApproachPolicy.Observe(objectives.EnemyPosition,snapshot.Players,enemyHealth,selectedCount):default;
  advanceCell=default;advancePosition=default;bool advanceAvailable=false;
  if(enemyAlive&&AriaGroundApproachPolicy.AdvancePreferred(true,distances)){
   var goal=AriaGroundApproachPolicy.Approach(true,objectives.EnemyPosition,objectives.PlayerPosition,distances);advancePosition=new float3(goal.X,goal.Y,goal.Z);
   using var grids=Entities.CreateEntityQuery(typeof(GridConfig));if(grids.CalculateEntityCount()!=1)throw new NotSupportedException("Original S073 logical ARIA needs one live source grid");advanceCell=GridUtils.WorldToCell(grids.GetSingleton<GridConfig>(),advancePosition);advanceAvailable=true;
  }
  AriaS002ActionTarget Slot(int slot)=>new AriaS002ActionTarget{Action=AriaS002Action.SelectSquad,Id=slot,Slot=slot,Admitted=(rosterMask&(1<<slot))!=0};
  return new AriaS002Observation{Scope=AriaS002Scope.LogicalPublicData,SourceScenarioIndex=3,Active=match.Phase==SkirmishPhase.Playing,Finished=match.Outcome!=SkirmishOutcome.None,ExpandedSession=false,ExpandedBaseAssault=false,Frame=frame,Time=Entities.WorldUnmanaged.Time.ElapsedTime>float.MaxValue?throw new NotSupportedException("S073 simulation clock exceeds ARIA float range"): (float)Entities.WorldUnmanaged.Time.ElapsedTime,
   PlayerDesignatedAlive=playerAlive,EnemyDesignatedAlive=enemyAlive,PlayerHealth=playerHealth,EnemyHealth=enemyHealth,Infantry=infantry,SelectedCount=selectedCount,OwnStructureAttackUnits=structureUnits,
   ExpandedPageIndex=0,ExpandedPageCount=slotCount>0?1:0,ExpandedTotalGroups=slotCount,ExpandedSelectedGroups=selectedSlots,ExpandedSelectedMask=selectedMask,ExpandedAssaultMask=structureMask,ExpandedStructureMask=structureMask,ExpandedAttackOrderMask=acceptedAttackOrderMask,ExpandedRosterRevision=0,
   Squad0=Slot(0),Squad1=Slot(1),Squad2=Slot(2),Squad3=Slot(3),Squad4=Slot(4),Attack=new AriaS002ActionTarget{Action=AriaS002Action.ArmAttack,Id=0,Admitted=selectedCount>0},
   EnemyBase=default,AdvanceGround=new AriaS002ActionTarget{Action=AriaS002Action.AttackPublicTerrain,Id=0,Admitted=advanceAvailable},AttackMode=attackMode,SelectionVisible=selectedCount>0,AssaultAtBase=distances.AssaultAtBase,AssaultDistanceKnown=distances.DistanceVisible,NearestFriendlyObjectiveDistance=distances.Distance,
   PublicSnapshot=new AriaS002PublicSnapshot{Identity=snapshot.Identity,MarkerRevision=snapshot.Generation,MarkerFrame=snapshot.Frame,MarkerTime=snapshot.AriaTime,ObjectiveWorld=objectives.EnemyPosition,OwnedIdentity=true,Complete=enemyAlive,SourceScopeAdmitted=enemyAlive},
   ExpandedRetries=0};
 }
 } }

namespace Game.Runtime {
 internal static class SourceS073LegacySpawnRequestAdapter {
  const string FullOriginalReadModelOptIn="WARLINE_S073_ORIGINAL_PRODUCTION_PUBLICATION";
  const string ProductionSpawnpointBridgeOptIn="WARLINE_S073_PRODUCTION_SPAWNPOINT_BRIDGE";
  internal static void RequireSharedPlacementSource(OperationMapDefinition selected) {
   if(selected==null||selected.OperationMapId!="opmap.skirmish.industrial_basin"||selected.SourceIdentityHash!="2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900")throw new NotSupportedException("Actual S073 logical operation-map source required");
   var binding=selected.SourceBinding;if(!binding.IsConfigured||binding.SourceOperationMapId!="opmap.skirmish.desert_base_01"||binding.SourceIdentityHash!=selected.SourceIdentityHash||binding.SourceContentHash!="2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c")throw new NotSupportedException("S073 must retain its reviewed Desert Base Campaign source binding");
   var grid=selected.GridMetadata;var origin=grid.Origin;if(grid.AssetGuid!="b201000000000000000000000000000b"||grid.ContentHash!="8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb"||grid.Dimensions.x!=2048||grid.Dimensions.y!=1024||grid.CellSize!=1||origin.x!=0||origin.y!=0||origin.z!=0)throw new NotSupportedException("S073 baked Campaign grid differs from the retained placement-owner capture");
   var surface=selected.SurfaceMetadata;if(surface.AssetGuid!="12f517deb32ab49698acbfdaf7c3eac7"||surface.ContentHash!="5447c779e2b68ba6b7c6f86a1146f6bf570d49b4455a676a2680ea595fa47d1d"||surface.SurfaceCount!=2048*1024||selected.MapSurfaceDataReference==null||selected.MapSurfaceDataReference.AssetGUID!=surface.AssetGuid)throw new NotSupportedException("S073 baked Campaign surface differs from the retained placement-owner capture");
   if(selected.NavigationMetadata.AuthoredSubSceneGuid!="c00140f2e94a04c3084c8dcb0c18cbd0")throw new NotSupportedException("S073 authored ECS placement subscene differs from the retained owner capture");
  }
  // This is the original BuildingPlacementAuthoredBuildingCache.AppendTo output:
  // static source-cache occupancy only, not the complete walkability/roads/water domain.
  internal static bool[] LoadOriginalAuthoredBuildingCache(OperationMapDefinition selected,string maskPath,string expectedSha,string sceneGuid) {
   RequireSharedPlacementSource(selected);
   if(string.IsNullOrEmpty(maskPath)||!Path.IsPathRooted(maskPath)||string.IsNullOrEmpty(expectedSha)||expectedSha.Length!=64||expectedSha.Any(c=>!(c>='0'&&c<='9'||c>='a'&&c<='f')))throw new InvalidDataException("Pinned S073 authored-cache mask path and SHA256 required");
   var info=new FileInfo(maskPath);if(!info.Exists||info.Length!=2048L*1024||(info.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Exact bounded S073 authored-cache mask required");
   var bytes=AutonomousWorld.PinnedFile(maskPath,expectedSha,2048L*1024);if(bytes.Length!=2048*1024||bytes.Any(v=>v>1))throw new InvalidDataException("S073 authored-cache bytes must be row-major bool01 over the selected grid");
   string manifestPath=maskPath+".json";var manifestInfo=new FileInfo(manifestPath);if(!manifestInfo.Exists||manifestInfo.Length<1||manifestInfo.Length>1024*1024||(manifestInfo.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Bounded regular S073 authored-cache manifest required");
   JObject manifest;using(var text=File.OpenText(manifestPath))using(var reader=new Newtonsoft.Json.JsonTextReader(text){MaxDepth=32,DateParseHandling=Newtonsoft.Json.DateParseHandling.None})manifest=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
   if((string)manifest["kind"]!="S073LiveOriginalAuthoredBuildingMask"||(string)manifest["authority"]!="BuildingPlacementAuthoredBuildingCache.AppendTo at original S073 placement validation"||(string)manifest["layout"]!="row-major y*width+x; one byte per cell; 1=reserved, 0=free"||(string)manifest["sha256"]!=expectedSha||(long?)manifest["byte_length"]!=2048L*1024||(int?)manifest["occupied_cell_count"]!=bytes.Count(v=>v!=0)||(int?)manifest["scenario_index"]!=3||(int?)manifest["seed"]<=0||string.IsNullOrEmpty((string)manifest["session_id"])||
      (string)manifest["selected_source_path"]!="Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"||(string)manifest["selected_operation_map_id"]!=selected.OperationMapId||(string)manifest["selected_source_identity_hash"]!=selected.SourceIdentityHash||(string)manifest["selected_content_hash"]!=selected.ContentHash||
      (string)manifest["bound_source_map_id"]!=selected.SourceBinding.SourceOperationMapId||(string)manifest["bound_source_identity_hash"]!=selected.SourceBinding.SourceIdentityHash||(string)manifest["bound_source_content_hash"]!=selected.SourceBinding.SourceContentHash||(string)manifest["source_scene_guid"]!=sceneGuid||(string)manifest["authored_subscene_guid"]!=selected.NavigationMetadata.AuthoredSubSceneGuid||
      (string)manifest["grid_guid"]!=selected.GridMetadata.AssetGuid||(string)manifest["grid_content_hash"]!=selected.GridMetadata.ContentHash||(string)manifest["surface_guid"]!=selected.SurfaceMetadata.AssetGuid||(string)manifest["surface_content_hash"]!=selected.SurfaceMetadata.ContentHash||(int?)manifest["width"]!=2048||(int?)manifest["height"]!=1024||(float?)manifest["cell_size"]!=1f||!(manifest["grid_origin"] is JArray origin)||origin.Count!=3||origin.Any(v=>v.Type!=JTokenType.Float&&v.Type!=JTokenType.Integer)||origin.Any(v=>v.Value<float>()!=0f)||
      (bool?)manifest["native_visual_readiness"]!=false||(bool?)manifest["full_mission_initialized"]!=false)throw new InvalidDataException("S073 authored-cache manifest is not bound to the selected original source/grid/surface; readiness remains unproven");
   var result=new bool[bytes.Length];for(int i=0;i<bytes.Length;i++)result[i]=bytes[i]!=0;return result;
  }
  // Retain S002 artifact provenance while admitting only the S073 logical map's exact reviewed SourceBinding.
  // The selected runtime scene reference is visual parity data; these placement owners read the shared ECS subscene.
  internal static SourcePlacementDomain LoadSharedPlacementSource(OperationMapDefinition selected,string physicalPath,string physicalSha,string waterPath,string waterSha,string startupPath,string startupSha,string currentSourceRoot,string relocationPath=null,string relocationSha=null,bool historicalInputOnly=false) {
   RequireSharedPlacementSource(selected);
   var domain=historicalInputOnly
    ?SourcePlacementDomain.LoadS002HistoricalInputOnly(physicalPath,physicalSha,waterPath,waterSha,startupPath,startupSha,currentSourceRoot,relocationPath,relocationSha)
    :SourcePlacementDomain.LoadS002SourceDiagnostic(physicalPath,physicalSha,waterPath,waterSha,startupPath,startupSha,currentSourceRoot,relocationPath,relocationSha);
   const int cells=2048*1024;
   // The S073 run binds its distinct native authored-building mask immediately
   // after this loader and materializes the final masks once in that bound domain.
   // Keep a same-build reference mode for A/B measurement of the old eager build.
   string validationMode=Environment.GetEnvironmentVariable("WARLINE_S073_PLACEMENT_MASK_LOAD_VALIDATION");
   if(string.IsNullOrEmpty(validationMode))validationMode="reference";
   if(validationMode!="optimized"&&validationMode!="reference")throw new InvalidDataException("S073 placement mask load validation mode must be optimized or reference");
   Console.Error.WriteLine("[S073PlacementMaskLoadValidation] mode="+validationMode+" grid_cells="+cells+" water_footprints="+domain.WaterFootprintCount);
   if(!domain.UsesOriginalOwnerMaskCapture||!domain.WaterDomainKnown||domain.WaterFootprintCount!=194||domain.CopyOriginalOwnerRoadMask().Length!=cells)
    throw new InvalidDataException("Complete shared Campaign placement owner inputs required");
   // Preserve per-run freshness even when avoiding discarded full-grid products.
   // The capture decoder has already admitted exact 2048x1024 authored/sidewalk
   // masks and 194 water footprints; downstream S073 binding/build rechecks their
   // frame and consumes the same captured values.
   domain.VerifyOriginalOwnerAssetPins();
   if(validationMode=="reference") {
    var masks=domain.Build(2048,1024,1,0,0,0);var activeWater=domain.BuildWaterMask(true,2048,1024,1,0,0,0);
    if(masks.AuthoredBuildings.Length!=cells||masks.Sidewalks.Length!=cells||activeWater==null||activeWater.Length!=cells)
     throw new InvalidDataException("Complete shared Campaign placement owner inputs required");
   }
   return domain;
  }
  internal static int Queue(EntityManager em, Entity plan, Entity boundary, bool exactSkirmishSite) {
   if(em==default||!em.Exists(plan)||!em.HasComponent<InitialUnitsSpawnConfig>(plan)||!em.HasBuffer<InitialUnitsFactionSpawnEntry>(plan)||!em.HasBuffer<InitialUnitsFactionBuildingSpawnEntry>(plan)||!em.Exists(boundary)||!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary))throw new NotSupportedException("Actual original legacy startup plan and runtime boundary required");
   using var factionSpawns=em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan).ToNativeArray(Allocator.Temp);
   if(!BuildingDefinitionPrefabSystemHelper.EnqueueConfiguredInitialBuildingRequests(em,boundary,plan,factionSpawns,exactSkirmishSite,message=>{throw new InvalidDataException(message);},out int count))throw new NotSupportedException("Shared original legacy building request owner rejected the source plan");
   return count;
  }
  // Execute queued legacy starting-building requests through the same runtime spawn
  // processor and registration owner used by the source host. This deliberately
  // stops before InitialUnitsSpawnInitialized: the legacy InitialUnitsSpawnSystem
  // owns that transition only after it observes all requests complete and spawns
  // the actual unit roster.
  internal static JObject ProcessOriginalLegacyStartingBuildings(EntityManager em,Entity grid,Entity boundary,
      IReadOnlyList<RuntimeDefinition> definitions,Dictionary<int,RuntimeBuildingEntity> buildings,
      bool[] authoredBuildings,bool[] roads,bool[] sidewalks,bool[] water,float buildPlaneY,
      IReadOnlyDictionary<SourceUnitPrefab,BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry> capturedMetadata=null,
      S073NativeConfiguredUnits capturedUnits=null,bool publishOriginalReadModels=false) {
   if(em==null||!em.Exists(grid)||!em.HasComponent<GridConfig>(grid)||!em.Exists(boundary)||
      !em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary)||definitions==null||definitions.Count!=6||buildings==null||
      buildings.Count!=0||float.IsNaN(buildPlaneY)||float.IsInfinity(buildPlaneY))
    throw new NotSupportedException("Fresh actual legacy request boundary, six source definitions, and source grid required");
   using(var matches=em.CreateEntityQuery(typeof(SkirmishMatchState)))
   using(var plans=em.CreateEntityQuery(typeof(CustomGameStartupStateComponent),typeof(InitialUnitsSpawnConfig))) {
    if(matches.CalculateEntityCount()!=1||plans.CalculateEntityCount()!=1||em.HasComponent<SkirmishExpandedSessionComponent>(matches.GetSingletonEntity()))
     throw new NotSupportedException("One original non-expanded legacy match and startup plan required");
    var requests=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
    if(requests.Length!=10||requests.Any(r=>r.Status!=BuildingRuntimeSpawnRequest.Pending||!GeneratedRuntimeSpawnProcessor.IsLegacyInitialRequest(em,r)))
     throw new NotSupportedException("All ten pending source legacy building requests required");
   }
   int n=checked(em.GetComponentData<GridConfig>(grid).Width*em.GetComponentData<GridConfig>(grid).Height);
   foreach(var mask in new[]{authoredBuildings,roads,sidewalks,water})if(mask==null||mask.Length!=n)
    throw new NotSupportedException("Complete source Campaign placement masks required");
   if(!em.HasComponent<BuildingRuntimeStateTag>(boundary))em.AddComponentData(boundary,new BuildingRuntimeStateTag());
   var birthIdentityCapture=SourceS073OriginalBirthIdentityCapture.Enabled?new SourceS073OriginalBirthIdentityCapture(em):null;
   Action<BuildingRuntimeSpawnRequest,RuntimeBuildingEntity> birthIdentityObserver=birthIdentityCapture==null?null:birthIdentityCapture.Observe;
   var pipeline=new GeneratedStartingBuildingPipeline(em,grid,boundary,definitions,true,buildings,
       authoredBuildings,roads,sidewalks,water,buildPlaneY,
       (x,y,w,h)=>{var blocker=em.GetComponentData<DynamicBlockerComponent>(grid);if(!blocker.Blocked.IsCreated)return false;
        for(int row=y;row<y+h;row++)for(int col=x;col<x+w;col++)if(blocker.Blocked.IsSet(row*em.GetComponentData<GridConfig>(grid).Width+col))return true;return false;},
       birthIdentityObserver:birthIdentityObserver);
   var configuredOrder=definitions.Select(d=>d.Prefab).ToArray();
   var configuredEntries=capturedMetadata??definitions.ToDictionary(d=>d.Prefab,GeneratedConfiguredDefinitionSource.BuildConfiguredSpawnableEntry);
   pipeline.BindConfiguredRequestMetadata(configuredOrder,configuredEntries);
   if(capturedUnits!=null){if(capturedMetadata==null)throw new NotSupportedException("Qualified building metadata required before captured unit publication");pipeline.PublishS073CapturedConfiguredUnits(capturedUnits);}
   for(int frame=1;frame<=64;frame++) {
    pipeline.ProcessRequests(frame/60f,true);
    var rows=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
    if(rows.All(r=>r.Status!=BuildingRuntimeSpawnRequest.Pending))break;
   }
   var completed=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
   JObject productionReadModelPublication=null;
   if(completed.All(r=>r.Status==BuildingRuntimeSpawnRequest.Succeeded)) {
    if(publishOriginalReadModels||string.Equals(Environment.GetEnvironmentVariable(FullOriginalReadModelOptIn),"1",StringComparison.Ordinal)) {
     if(capturedUnits==null||capturedMetadata==null)throw new NotSupportedException("Pinned S073 configured units and native metadata required for original full publication");
     pipeline.BindS073CapturedSourceReadModels(capturedUnits,(building,local)=> {
      if(building==null||building.CombatEntity==Entity.Null||!em.Exists(building.CombatEntity)||!em.HasComponent<LocalTransform>(building.CombatEntity))
       throw new NotSupportedException("Actual registered source producer CombatEntity transform required");
      var transform=em.GetComponentData<LocalTransform>(building.CombatEntity);
      if(!math.all(math.isfinite(transform.Position))||!math.all(math.isfinite(transform.Rotation.value)))
       throw new NotSupportedException("Finite source producer frame required");
      var world=transform.Position+math.mul(transform.Rotation,new float3(local.x,local.y,local.z));
      return new Vector3(world.x,world.y,world.z);
     });
     var readContext=new GeneratedProductionReadModelSource.Context(buildings,
      (out EntityManager manager)=>{manager=em;return true;},
      (EntityManager manager,out Entity entity)=>{entity=boundary;return ReferenceEquals(manager,em)&&em.Exists(entity);},
      null,BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey,
      (prefab,id)=>string.IsNullOrEmpty(id)||(prefab!=null&&BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefab.name)==id));
     float sourceNow=(float)em.WorldUnmanaged.Time.ElapsedTime;
     pipeline.PublishSourceReadModels(readContext,new FactionResourceCompositionSystemHelper(),sourceNow);
     JObject spawnpointBridge=null;
     if(string.Equals(Environment.GetEnvironmentVariable(ProductionSpawnpointBridgeOptIn),"1",StringComparison.Ordinal)) {
      PublishOriginalLegacyProductionSpawnPoints(em,grid,boundary,buildings);
      var bridgeRows=em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
      spawnpointBridge=new JObject {
       ["status"]=bridgeRows.Length>0?"Published":"Empty",
       ["publisher"]=nameof(PublishOriginalLegacyProductionSpawnPoints),
       ["scope"]="SameWorldRegisteredLegacyBuildings_AuthoredProductionSpawnLocalPositions_CombatEntityLocalTransform",
       ["source_registered_buildings"] = buildings.Count,
       ["production_spawnpoint_rows"] = bridgeRows.Length,
       ["source_world_mutation_scope"]="Existing production spawnpoint read-model buffer only",
       ["production_request_or_completion_claimed"]=false,
       ["mission_ready"]=false
      };
      if(bridgeRows.Length==0)throw new NotSupportedException("Opt-in S073 production spawnpoint bridge produced no source rows");
     }
     var productionRequests=BuildingRuntimeBoundaryBuffers.EnsureBoundaryBuffer<BuildingFactionUnitProductionRequest>(em,boundary);
     if(productionRequests.Length!=0)throw new NotSupportedException("Original AI production owner requires an empty source request buffer at Preparing");
     productionReadModelPublication=new JObject{["status"]="Published",
      ["owner"]="ExtractedOriginalBuildingRuntimeReadModelCadence",
      ["source_clock_seconds"]=sourceNow,["production_completion_claimed"]=false,
      ["configured_units"]=em.GetBuffer<BuildingConfiguredUnitReadModel>(boundary).Length,
      ["production_slots"]=em.GetBuffer<BuildingProductionSlotReadModel>(boundary).Length,
      ["unit_production_summaries"]=em.GetBuffer<BuildingRuntimeUnitProductionSummary>(boundary).Length,
      ["production_spawn_points"]=em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary).Length,
      ["empty_original_request_buffer_ensured"]=true,
      ["request_buffer_utility"]="BuildingRuntimeBoundaryBuffers.EnsureBoundaryBuffer",
      ["source_world_mutation_scope"]="Original read-model buffers plus empty native request buffer component; no requests seeded"};
     if(spawnpointBridge!=null)productionReadModelPublication["spawnpoint_bridge"]=spawnpointBridge;
    } else {
     PublishOriginalLegacyProductionSpawnPoints(em,grid,boundary,buildings);
    }
   }
   var result=new JObject{["status"]=completed.Any(r=>r.Status==BuildingRuntimeSpawnRequest.Pending)?"Incomplete":completed.Any(r=>r.Status!=BuildingRuntimeSpawnRequest.Succeeded)?"Rejected":"Passed",
    ["queued_requests"]=completed.Length,["registered_buildings"]=buildings.Count,["initial_spawn_completion_marked"]=false,
    ["requests"]=new JArray(completed.Select(r=>new JObject{["request_id"]=r.RequestId,["building_id"]=r.BuildingId.ToString(),
     ["status"]=r.Status,["result_code"]=r.ResultCode,["faction_id"]=r.FactionId,["preferred_origin"]=new JArray(r.PreferredOrigin.x,r.PreferredOrigin.y),
     ["actual_origin"]=new JArray(r.ActualOrigin.x,r.ActualOrigin.y),["actual_footprint"]=new JArray(r.ActualFootprint.x,r.ActualFootprint.y),["spawned_count"]=r.SpawnedCount}))};
   if(birthIdentityCapture!=null)result["original_birth_identity_capture"]=birthIdentityCapture.Snapshot();
   if(productionReadModelPublication!=null)result["original_production_read_model_publication"]=productionReadModelPublication;
   if(capturedUnits!=null){var units=em.GetBuffer<BuildingConfiguredUnitReadModel>(boundary);result["configured_units_scope"]="OriginalConfiguredUnitPublisher_AuthoredMetadata_NotRuntimeOverlayOrReadiness";result["configured_units"]=new JArray(units.Select(x=>new JObject{["unit_id"]=x.UnitId.ToString(),["display_name"]=x.DisplayName.ToString(),["materials_cost"]=x.Price,["credits_cost"]=x.CreditsCost,["can_request"]=x.CanRequest,["is_vehicle"]=x.IsVehicle}));}
   return result;
  }
  internal static void PublishOriginalLegacyProductionSpawnPoints(EntityManager em,Entity gridEntity,Entity boundary,
      IReadOnlyDictionary<int,RuntimeBuildingEntity> buildings) {
   if(em==null||!em.Exists(gridEntity)||!em.HasComponent<GridConfig>(gridEntity)||!em.Exists(boundary)||
      !em.HasComponent<BuildingRuntimeStateTag>(boundary)||buildings==null)throw new NotSupportedException("Actual same-world legacy building registry and runtime boundary required");
   if(!em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary))em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
   var rows=em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);rows.Clear();
   var grid=em.GetComponentData<GridConfig>(gridEntity);
   foreach(var pair in buildings) {
    var building=pair.Value;
    if(building==null||building.IsDestroyed||!building.HasOwnerFaction||building.Definition?.Prefab==null||
       building.ProductionSpawnLocalPositions==null||building.ProductionSpawnLocalPositions.Length==0||
       building.CombatEntity==Entity.Null||!em.Exists(building.CombatEntity)||!em.HasComponent<LocalTransform>(building.CombatEntity))continue;
    var transform=em.GetComponentData<LocalTransform>(building.CombatEntity);
    if(!math.all(math.isfinite(transform.Position))||!math.all(math.isfinite(transform.Rotation.value)))
     throw new NotSupportedException("Finite source registered building transform required");
    var id=new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(building.Definition.Prefab.name));
    for(int slot=0;slot<building.ProductionSpawnLocalPositions.Length;slot++) {
     var local=building.ProductionSpawnLocalPositions[slot];
     var world=transform.Position+math.mul(transform.Rotation,new float3(local.x,local.y,local.z));
     if(BuildingFactionProductionSpawnPointReadModelProjection.TryCreate(
        building.OwnerFactionId,id,building.Id,slot,grid,world,out var row))rows.Add(row);
    }
   }
   OperationMapHelipadReadModelUtility.Bind(em,rows,in grid);
  }
 }
}

namespace Game.Runtime {
 // Runs only the original InitialUnitsSpawnSystem against the caller's existing
 // EntityManager. It does not create runtime prerequisites or completion markers.
 internal sealed class OriginalS073InitialUnitsSpawnOwner {
  readonly EntityManager em;InitialUnitsSpawnSystem owner;SystemState state;bool disposed,occupancyRebuilt;
  internal OriginalS073InitialUnitsSpawnOwner(EntityManager manager) {
   em=manager??throw new ArgumentNullException(nameof(manager));
   using(var plans=em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig)))
   using(var boundaries=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingConfiguredSpawnableReadModel),typeof(BuildingFactionProductionSpawnPointReadModel),typeof(BuildingRuntimeSpawnRequest)))
   using(var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent)))
   using(var gameplay=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent),typeof(RuntimeCameraFocusRequestComponent)))
    if(plans.CalculateEntityCount()!=1||boundaries.CalculateEntityCount()!=1||grids.CalculateEntityCount()!=1||gameplay.CalculateEntityCount()!=1)
     throw new NotSupportedException("Unique original S073 unit-spawn plan, same-world source boundary, baked grid and composition-owned gameplay state required");
   state=new SystemState{EntityManager=em,Enabled=true};state.Begin();owner.OnCreate(ref state);state.End();state.LastSystemVersion=0;
  }
  internal JObject Step() {
   if(disposed)throw new ObjectDisposedException(nameof(OriginalS073InitialUnitsSpawnOwner));
   using var plans=em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
   Entity plan=plans.GetSingletonEntity();
   int before=CountSpawnedUnits();
   bool requiredQueriesPresent;
   bool ownerUpdateInvoked=false;
   state.Begin();requiredQueriesPresent=state.RequiredQueriesPresent;if(state.Enabled&&requiredQueriesPresent){owner.OnUpdate(ref state);ownerUpdateInvoked=true;}state.End();
   int after=CountSpawnedUnits();
   bool completed=em.HasComponent<InitialUnitsSpawnInitialized>(plan);
   int boundaryCount,gridCount,gameplayCount,occupancyCount,activeConfigCount,progressCount;
   bool playRequested;JObject cameraFocus=null;
   using(var boundaries=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingConfiguredSpawnableReadModel),typeof(BuildingFactionProductionSpawnPointReadModel),typeof(BuildingRuntimeSpawnRequest)))boundaryCount=boundaries.CalculateEntityCount();
   using(var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent)))gridCount=grids.CalculateEntityCount();
   using(var gameplay=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent),typeof(RuntimeCameraFocusRequestComponent))){gameplayCount=gameplay.CalculateEntityCount();playRequested=gameplayCount==1&&gameplay.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested!=0;if(gameplayCount==1){var focus=gameplay.GetSingleton<RuntimeCameraFocusRequestComponent>();cameraFocus=new JObject{["requested"]=focus.Requested!=0,["world"]=new JArray(focus.World.x,focus.World.y,focus.World.z),["smooth"]=focus.Smooth!=0,["use_tactical_reveal_zoom"]=focus.UseTacticalRevealZoom,["use_explicit_yaw"]=focus.UseExplicitYaw!=0,["yaw_degrees"]=focus.YawDegrees,["use_explicit_perspective"]=focus.UseExplicitPerspective!=0,["perspective"]=new JArray(focus.Perspective.x,focus.Perspective.y,focus.Perspective.z,focus.Perspective.w),["smooth_seconds"]=focus.SmoothTimeSeconds,["scope"]="ActualSourceWorldReadback_NoCameraConsumptionOrReadiness"};}}
   using(var occupancy=em.CreateEntityQuery(typeof(DynamicOccupancyComponent)))occupancyCount=occupancy.CalculateEntityCount();
   using(var active=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<InitialUnitsSpawnConfig>()},None=new[]{ComponentType.ReadOnly<InitialUnitsSpawnInitialized>()}}))activeConfigCount=active.CalculateEntityCount();
   using(var progress=em.CreateEntityQuery(typeof(InitialUnitsSpawnProgress)))progressCount=progress.CalculateEntityCount();
   string firstUnsatisfiedGate=completed?null:!requiredQueriesPresent?"SystemState.RequiredQueriesPresent":boundaryCount==0?"BuildingRuntimeBoundary":gridCount==0?"BakedGridContext":occupancyCount==0?"DynamicOccupancyComponent":gameplayCount==0?"RuntimeGameplayStateComponent":!playRequested?"RuntimeGameplayStateComponent.PlayRequested":activeConfigCount==0?"ActiveInitialUnitsSpawnConfig":null;
   int occupiedCells=-1;
   if(completed&&!occupancyRebuilt) {
    var occupancyState=new SystemState{EntityManager=em,Enabled=true};var occupancyOwner=new DynamicOccupancyRebuildSystem();
    occupancyState.Begin();occupancyOwner.OnCreate(ref occupancyState);occupancyState.End();
    try {occupancyState.Begin();if(occupancyState.Enabled&&occupancyState.RequiredQueriesPresent)occupancyOwner.OnUpdate(ref occupancyState);occupancyState.End();}
    finally {occupancyState.Begin();occupancyOwner.OnDestroy(ref occupancyState);}
    occupancyRebuilt=true;
   }
   if(occupancyRebuilt) {
    using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(DynamicOccupancyComponent));var grid=grids.GetSingletonEntity();var occupancy=em.GetComponentData<DynamicOccupancyComponent>(grid);
    if(occupancy.Occupied.IsCreated) {occupiedCells=0;for(int i=0;i<occupancy.Occupied.Length;i++)if(occupancy.Occupied.IsSet(i))occupiedCells++;}
   }
   return new JObject{["status"]=completed?"Completed":after>before?"Progressed":"Pending",["original_owner_invoked"]=true,
    ["owner_update_invoked"]=ownerUpdateInvoked,["required_queries_present"]=requiredQueriesPresent,["first_unsatisfied_gate"]=firstUnsatisfiedGate,
    ["boundary_query_count"]=boundaryCount,["grid_context_query_count"]=gridCount,["gameplay_state_query_count"]=gameplayCount,
    ["dynamic_occupancy_query_count"]=occupancyCount,["active_spawn_config_count"]=activeConfigCount,["progress_component_count"]=progressCount,
    ["play_requested_readback"]=playRequested,["camera_focus_request_readback"]=cameraFocus,
    ["units_before"]=before,["units_after"]=after,["units_spawned_this_step"]=after-before,["owner_marked_completion"]=completed,
    ["original_dynamic_occupancy_owner_invoked"]=occupancyRebuilt,["occupied_grid_cells"]=occupiedCells};
  }
  int CountSpawnedUnits(){using var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitSourcePrefabKey>()},None=new[]{ComponentType.ReadOnly<Prefab>(),ComponentType.ReadOnly<Disabled>(),ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),ComponentType.ReadOnly<StaticGridBlocker>()}});return query.CalculateEntityCount();}
 }
}
