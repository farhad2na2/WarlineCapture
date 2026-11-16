using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class S004OrderedHandoffDiagnostic
{
    static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static int Main(string[] args)
    {
        try
        {
            var setup = SourceS004CompiledSetupInput.Read(Path.GetFullPath(args[0]), args[1]);
            var source = JObject.Parse(File.ReadAllText(args[2]));
            var keys = ((JArray)source["requested_prefab_keys"]).Values<string>().ToArray();
            int next = 200000; var em = new EntityManager { LogicalAllocatorAdmitted = true };
            em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
            var imported = GeneratedReferenceTemplateImporter.Import(em, source, Path.GetDirectoryName(Path.GetFullPath(args[2])), keys);
            var otherSource = JObject.Parse(File.ReadAllText(args[6]));
            var otherKeys = ((JArray)otherSource["requested_prefab_keys"]).Values<string>().ToArray();
            var importedOther = GeneratedReferenceTemplateImporter.Import(em, otherSource, Path.GetDirectoryName(Path.GetFullPath(args[6])), otherKeys);
            var templates = new Dictionary<string, Entity>(imported.Prefabs, StringComparer.Ordinal);
            foreach (var pair in importedOther.Prefabs) templates.Add(pair.Key, pair.Value);
            var definitions = GeneratedBuildingDefinitions.LoadS004Source(Path.GetFullPath(args[7]), args[8], templates, setup, Path.GetFullPath(args[0]), args[1]);
            const int width = 2048, height = 1024, count = width * height;
            var graph = JObject.Parse(File.ReadAllText(args[11]));
            var sourceAssets = new SourceConfigMaterializer(graph);
            var map=sourceAssets.ByPath<OperationMapDefinition>("Assets/Game/Resources/SkirmishS004OperationMap.asset");
            var rotation=SourceOperationMapRotationReference.LoadPinned(Path.GetFullPath(args[12]),"d084501099c72a04ef1a2a56fe2b47781626b05ed10c8c5da6bdaf0579cc1fc4",Directory.GetCurrentDirectory());
            var publisher=new Game.Composition.OperationMapRuntimeBootstrapSceneSystemHelper(em,rotation);
            const OperationMapReadinessFlags metadata=OperationMapReadinessFlags.Metadata;
            const OperationMapReadinessFlags required=OperationMapReadinessFlags.SourceContent|OperationMapReadinessFlags.SubScene|OperationMapReadinessFlags.Metadata|OperationMapReadinessFlags.MapSurface|OperationMapReadinessFlags.AuthoredConversion|OperationMapReadinessFlags.PresentationManifest|OperationMapReadinessFlags.RequiredPresentationPreload;
            Need(publisher.TryPublish(map,new FixedString64Bytes("S004"),new FixedString64Bytes("skirmish.s004"),1,metadata,required,out var mapRoot,out var mapError),"Original map publisher rejected source: "+mapError);
            Need(em.Exists(mapRoot) && em.HasComponent<OperationMapMetadataComponent>(mapRoot),"Original metadata root missing");
            Need(em.GetComponentData<OperationMapMetadataComponent>(mapRoot).PhysicalSourceValidated == 0,"Metadata publication unexpectedly asserts physical source validation");
            var surfaceAsset = sourceAssets.ByPath<MapSurfaceDataAsset>("Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Surface.asset");
            Need(surfaceAsset.TryCreateRuntimeBlobAsset(Allocator.Persistent, out var surfaceBlob), "Source surface decode failed");
            ref var surface = ref surfaceBlob.Value;
            Need(surface.Dimensions.x == width && surface.Dimensions.y == height && surface.CellSize == 1f, "Source surface frame changed");
            var surfaceEntity = em.CreateEntity();
            em.AddComponentData(surfaceEntity, new MapSurfaceComponent { SurfaceBlob=surfaceBlob, GridOrigin=surface.GridOrigin, CellSize=surface.CellSize, Dimensions=surface.Dimensions, HasSurfaceData=1 });
            var sessionId = new FixedString64Bytes("s004-starting-grant-fixture");
            var session = em.CreateEntity();
            em.AddComponentData(session, new SkirmishExpandedSessionComponent { SessionId = sessionId, Phase = SkirmishSessionPhase.Queued });
            em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });
            var initialState = new SystemState { EntityManager = em, Enabled = true };
            var initializer = new SkirmishSessionInitializationSystem(); initializer.OnUpdate(ref initialState);
            Need(em.GetComponentData<SkirmishExpandedSessionComponent>(session).InitializationComplete == 1 && em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase == SkirmishSessionPhase.Spawning, "Original session initializer rejected source setup");
            byte[] mask = File.ReadAllBytes(args[3]);
            Need(mask.Length == count && mask.All(value => value == 0 || value == 1) && mask.Count(value => value == 0) == 22840, "Exact authored-grid projection required");
            var gridEntity = em.CreateEntity();
            em.AddComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = new float3(0, 0, 0) });
            var walkable = em.AddBuffer<GridWalkable>(gridEntity);
            for (int i = 0; i < count; i++) walkable.Add(new GridWalkable { Value = mask[i] });
            var gridRoad = em.AddBuffer<GridRoad>(gridEntity); for (int i = 0; i < count; i++) gridRoad.Add(new GridRoad { Value = 0 });
            var blocked = new NativeBitArray(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            var occupied = new NativeBitArray(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            em.AddComponentData(gridEntity, new DynamicBlockerComponent { GridSize = count, Blocked = blocked, Counts = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.ClearMemory), FriendlyPassFactionIds = new NativeArray<byte>(count, Allocator.Persistent, NativeArrayOptions.ClearMemory) });
            em.AddComponentData(gridEntity, new DynamicOccupancyComponent { GridSize = count, Occupied = occupied });
            var blockerCapture = JObject.Parse(File.ReadAllText(args[5]));
            var blockerRows = (JArray)blockerCapture["blockers"];
            Need((int?)blockerCapture["blocker_count"] == 348 && blockerRows != null && blockerRows.Count == 348, "Exact captured blocker domain required");
            var blockerEntities = new System.Collections.Generic.List<Entity>();
            foreach (JObject row in blockerRows)
            {
                Need((bool?)row["source_section_owned"] == true && (bool?)row["friendly_pass_present"] == false && (bool?)row["opening_present"] == false, "Unsupported captured physical blocker input");
                var position = (JArray)row["unit_grid"]; var size = (JArray)row["size"]; var origin = (JArray)row["origin"];
                var obstacle = em.CreateEntity();
                em.AddComponentData(obstacle, new UnitGrid { Cell = new int2((int)position[0], (int)position[1]) });
                em.AddComponentData(obstacle, new GridBlockerSize { Size = new int2((int)size[0], (int)size[1]) });
                em.AddComponentData(obstacle, new StaticGridBlocker());
                if ((bool)row["building"])
                {
                    em.AddComponentData(obstacle, new OperationMapBuildingComponent());
                    em.AddComponentData(obstacle, new RuntimeBuildingCombatInfo { OriginCell = new int2((int)origin[0], (int)origin[1]) });
                }
                Need(!em.HasComponent<Faction>(obstacle) && !em.HasComponent<UnitHealth>(obstacle), "Campaign gameplay state cannot be copied");
                blockerEntities.Add(obstacle);
            }
            var blockerState = new SystemState { EntityManager = em, Enabled = true };
            var blockerOwner = new StaticGridBlockerUpdateSystem();
            blockerState.Begin(); blockerOwner.OnCreate(ref blockerState); blockerState.End();
            blockerState.Begin();
            Need(blockerState.Enabled && blockerState.RequiredQueriesPresent, "Original static blocker owner prerequisites missing");
            blockerOwner.OnUpdate(ref blockerState); blockerState.End();
            Need(blockerEntities.All(entity => em.HasComponent<StaticBlockerPrevBounds>(entity)), "Original owner did not publish all physical blocker histories");
            var blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            int blockedCells = 0; for (int i = 0; i < count; i++) if (blockerData.Blocked.IsSet(i)) blockedCells++;
            Need(blockedCells > 0, "Original owner produced no physical blockers");
            var nativeReadmodel = JObject.Parse(File.ReadAllText(args[9]));
            var nativeRows = nativeReadmodel["entries"] as JArray;
            Need(nativeRows != null && nativeRows.Count == 9 && (string)nativeReadmodel["catalog_id"] == "S004" && (uint?)nativeReadmodel["setup_hash"] == setup.SetupHash, "Exact captured configured source metadata required");
            var definitionSource = new GeneratedConfiguredDefinitionSource();
            definitionSource.BindGeneratedDefinitions(definitions.Definitions, true);
            var order = new List<SourceUnitPrefab>();
            var entries = new Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry>();
            foreach (JObject row in nativeRows)
            {
                string key = (string)row["visual_key"];
                var definition = definitions.Definitions.Single(value => value.Prefab.name == key);
                var sourceDefinition = (JObject)row["source_definition"];
                Need(JToken.DeepEquals(sourceDefinition["prefab"], definitions.AssetIdentities[key]) && (string)sourceDefinition["display_name"] == definition.DisplayName && sourceDefinition["can_request"].Type == JTokenType.Boolean, "Current publisher source identity changed");
                order.Add(definition.Prefab);
                entries.Add(definition.Prefab, new BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry(definition.DisplayName, definition.Description, definition.Prefab, (bool)sourceDefinition["can_request"], Math.Max(0, definition.CreditsCost)));
            }
            definitionSource.BindRequestMetadata(order, entries);
            var boundary = em.CreateEntity(); em.AddComponentData(boundary, new BuildingRuntimeStateTag());
            em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
            var grid = em.GetComponentData<GridConfig>(gridEntity);
            var roadBytes = File.ReadAllBytes(args[10]);
            Need(roadBytes.Length == count && roadBytes.All(value => value == 0 || value == 1) && roadBytes.Count(value => value == 1) == 23500, "Exact source road mask required");
            bool[] roads = roadBytes.Select(value => value != 0).ToArray();
            // Explicit diagnostic assumption: missing scene masks are treated as empty, not admitted evidence.
            var assumedEmpty = new bool[count]; var registry = new Dictionary<int, RuntimeBuildingEntity>();
            var pipeline = new GeneratedStartingBuildingPipeline(em, gridEntity, boundary, definitions.Definitions, true, registry,
                assumedEmpty, roads, assumedEmpty, assumedEmpty, 0f,
                (x,z,w,h) => { var bits=em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
                    for (int y=z;y<z+h;y++) for(int xx=x;xx<x+w;xx++) if(bits.IsSet(y*width+xx)) return true; return false; });
            pipeline.BindConfiguredRequestMetadata(order, entries);
            var actual = em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
            Need(actual.Length == nativeRows.Count, "Original publisher row count differs");
            for (int i = 0; i < actual.Length; i++)
            {
                var expected = nativeRows[i]["read_model"]; var model = actual[i];
                Need(model.BuildingId.ToString() == (string)expected["BuildingId"] && model.DisplayName.ToString() == (string)expected["DisplayName"] && model.Price == (int)expected["Price"] && model.MaterialsCost == (int)expected["MaterialsCost"] && model.FootprintCells.x == (int)expected["FootprintCells"]["x"] && model.FootprintCells.y == (int)expected["FootprintCells"]["y"] && model.CanRequest == (byte)expected["CanRequest"], "Actual publisher differs from checked source capture");
            }
            bool ready = SkirmishStartingBuildingsService.Step(em, session, setup, out var reason);
            Need(!ready && reason == SkirmishReasonCode.None, "Original owner did not queue initial requests");
            var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            var tracked = em.GetBuffer<SkirmishStartingBuildingRequest>(session);
            Need(requests.Length == setup.Structures.Length && tracked.Length == setup.Structures.Length && requests.Length == 20, "Original request count differs from setup");
            using (var all=em.CreateEntityQuery(new EntityQueryDesc { Options=EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities })) {
                RuntimeActorBirthContext.BindNoNativeBirthRecordProfile(em, true, true, all.ToEntityArray(Allocator.Temp).ToArray(),
                    entity => em.HasComponentNamed(entity,"Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord"));
            }
            for (int frame=0;frame<20;frame++) {
                pipeline.ProcessRequests(frame/60f, true);
                if (requests.ToArray().All(request => request.Status != BuildingRuntimeSpawnRequest.Pending)) break;
            }
            var rows = new JArray(); int succeeded=0, failed=0, pending=0;
            for (int i=0;i<requests.Length;i++) {
                var request=requests[i];
                if(request.Status==BuildingRuntimeSpawnRequest.Succeeded) succeeded++;
                else if(request.Status==BuildingRuntimeSpawnRequest.Failed) failed++;
                else if(request.Status==BuildingRuntimeSpawnRequest.Pending) pending++;
                else throw new InvalidOperationException("Unexpected request state");
                rows.Add(new JObject { ["request_id"]=request.RequestId,["building"]=request.BuildingId.ToString(),["status"]=(int)request.Status,
                    ["result_code"]=(int)request.ResultCode,["actual_x"]=request.ActualOrigin.x,["actual_y"]=request.ActualOrigin.y,["runtime_id"]=request.BuildingRuntimeId });
            }
            Need(succeeded == 20 && failed == 0 && pending == 0 && registry.Count == 20, "Starting building placement did not complete; no actor loop attempted");
            bool buildingsBound = SkirmishStartingBuildingsService.Step(em, session, setup, out var bindReason);
            Need(buildingsBound && bindReason == SkirmishReasonCode.None && em.HasComponent<SkirmishSharedBuildingsReady>(session), "Original owner did not bind completed building requests: " + bindReason);
            var boundRows = em.GetBuffer<SkirmishStartingBuildingRequest>(session);
            Need(boundRows.Length == 20 && boundRows.ToArray().All(row => row.BoundEntity != Entity.Null && em.Exists(row.BoundEntity) && em.HasComponent<SkirmishSharedActorTag>(row.BoundEntity)), "Original owner building adoption incomplete");
            blockerState.Begin(); blockerOwner.OnUpdate(ref blockerState); blockerState.End();
            var prefabRegistry=em.CreateEntity(); var prefabEntries=em.AddBuffer<UnitPrefabRegistryEntry>(prefabRegistry);
            foreach (var entry in templates.OrderBy(pair=>pair.Key,StringComparer.Ordinal)) prefabEntries.Add(new UnitPrefabRegistryEntry { Prefab=entry.Value });
            SkirmishScenarioSpawnSystem.TrySpawn(em,session);
            var resultingSession=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            bool ledgerResult=resultingSession.SpawnComplete==1; var ledgerReason=resultingSession.FailureCode; var visualPending=resultingSession.SpawnVisualPending;
            Need(resultingSession.Phase==SkirmishSessionPhase.Playing && resultingSession.SpawnComplete==1, "Projected outer owner did not start diagnostic session");
            var army=SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            Need(army!=null, "Actual authored army profile unavailable");
            var productionRoles=setup.RoleOverlays.Where(role=>army.Allows(role.RoleId)).ToDictionary(role=>SkirmishRoleCatalogConfig.RuntimePrefabKey(role.RoleKind),role=>role,StringComparer.Ordinal);
            foreach(var key in productionRoles.Keys) Need(templates.ContainsKey(key), "Actual production prefab source missing: "+key);
            em.AddComponentObject(session,new SkirmishProductionCatalogRecord { Catalog=new SkirmishProductionCatalog(setup,productionRoles,true) });
            var groupBefore=em.GetBuffer<SkirmishArmyGroupRecord>(session).ToArray();
            var strategyBefore=em.GetComponentData<SkirmishEnemyStrategyComponent>(session);
            int gameplayStates; using(var states=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent))) gameplayStates=states.CalculateEntityCount();
            Need(gameplayStates==0,"Fixture unexpectedly contains gameplay state");
            var movementOwner=new SkirmishWorldMovementSystem(); var movementState=new SystemState {EntityManager=em,Enabled=true};
            movementState.Begin(); movementOwner.OnCreate(ref movementState); movementState.End();
            var enemyOwner=new SkirmishEnemyStrategySystem(); var enemyState=new SystemState {EntityManager=em,Enabled=true};
            enemyState.Begin(); enemyOwner.OnCreate(ref enemyState); enemyState.End();
            int CountEnemyPaths() {int total=0; using(var q=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(UnitPathRequest))) using(var es=q.ToEntityArray(Allocator.Temp)) foreach(var actor in es) {var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor); if(owned.SessionId.Equals(sessionId)&&owned.FactionId==2) total++;} return total;}
            em.WorldUnmanaged.Time=new TimeData {ElapsedTime=1.0/60.0,DeltaTime=1f/60f};
            movementState.Begin(); Need(movementState.Enabled&&movementState.RequiredQueriesPresent,"Movement owner prerequisites missing"); movementOwner.OnUpdate(ref movementState); movementState.End();
            int pathsBeforeEnemy=CountEnemyPaths();
            enemyState.Begin(); Need(enemyState.Enabled&&enemyState.RequiredQueriesPresent,"Enemy owner prerequisites missing"); enemyOwner.OnUpdate(ref enemyState); enemyState.End();
            int pathsAfterEnemy=CountEnemyPaths(); var strategyAfter=em.GetComponentData<SkirmishEnemyStrategyComponent>(session);
            em.WorldUnmanaged.Time=new TimeData {ElapsedTime=2.0/60.0,DeltaTime=1f/60f};
            movementState.Begin(); movementOwner.OnUpdate(ref movementState); movementState.End();
            int pathRequests=CountEnemyPaths();
            var groupAfter=em.GetBuffer<SkirmishArmyGroupRecord>(session).ToArray();
            int actualActiveIntents=0;
            using(var intents=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishMoveIntentComponent)))
            using(var entities=intents.ToEntityArray(Allocator.Temp)) foreach(var unit in entities) {
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(unit); var intent=em.GetComponentData<SkirmishMoveIntentComponent>(unit);
                if(owned.SessionId.Equals(sessionId) && owned.FactionId==2 && intent.Active!=0) actualActiveIntents++;
            }
            int changedOrders=0; for(int i=0;i<groupAfter.Length;i++) { var previous=groupBefore.Single(value=>value.GroupId==groupAfter[i].GroupId); if(previous.LastOrder!=groupAfter[i].LastOrder) changedOrders++; }
            int actorCount=0, groupedCount=0;
            using(var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent)))
            using(var entities=query.ToEntityArray(Allocator.Temp)) foreach(var actor in entities) {
                if(!em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).SessionId.Equals(sessionId)) continue;
                actorCount++; if(em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor)) groupedCount++;
            }
            var playerCapacity=em.GetComponentData<SkirmishCapacityComponent>(session);
            var enemyCapacity=em.HasComponent<SkirmishEnemyCapacityComponent>(session) ? em.GetComponentData<SkirmishEnemyCapacityComponent>(session) : default;
            var result=new JObject { ["status"]="DiagnosticOriginalPlacementOwnerObserved", ["request_count"]=requests.Length,
                ["succeeded"]=succeeded,["failed"]=failed,["pending"]=pending,["registered"]=registry.Count,["requests"]=rows,
                ["original_metadata_published"]=true,["metadata_before_session_and_placement"]=true,["physical_source_validated"]=false,["source_registry_root_count"]=prefabEntries.Length,["native_registry_captured"]=false,["production_catalog_source_roles"]=productionRoles.Count,["ordered_subset"]=new JArray("world_movement_tick1","enemy_strategy_tick1","world_movement_tick2"),["paths_before_enemy"]=pathsBeforeEnemy,["paths_after_enemy"]=pathsAfterEnemy,["world_movement_owner_updates"]=2,["world_movement_delta_seconds"]=1f/60f,["runtime_gameplay_state_count"]=gameplayStates,["enemy_path_requests"]=pathRequests,["enemy_active_move_intents"]=actualActiveIntents,["changed_group_orders"]=changedOrders,["enemy_last_group_id"]=strategyAfter.LastGroupId,["enemy_failed_attempts"]=strategyAfter.FailedAttempts,["enemy_owner_updates"]=1,["enemy_priority_before"]=strategyBefore.Priority.ToString(),["enemy_priority_after"]=strategyAfter.Priority.ToString(),["enemy_recruit_role"]=strategyAfter.RecruitRole.ToString(),["enemy_score"]=strategyAfter.LastScore,["outer_owner_executed"]=true,["session_phase"]=resultingSession.Phase.ToString(),["source_view_profile_assumed_absent"]=true,["full_ledger_result"]=ledgerResult,["ledger_reason"]=ledgerReason.ToString(),["visual_pending"]=(int)visualPending,
                ["player_live_infantry"]=playerCapacity.InfantryLive,["enemy_live_infantry"]=enemyCapacity.InfantryLive,
                ["materials_initialized"]=em.HasComponent<SkirmishSharedMaterialsInitialized>(session),["supply_initialized"]=em.HasComponent<SkirmishSharedSupplyInitialized>(session),
                ["original_building_owner_bound"]=buildingsBound,["bound_buildings"]=boundRows.Length,["actors_created"]=actorCount,["actors_grouped"]=groupedCount,
                ["base_road_mask_source"]=true,["scene_overlays"]="unknown assumed empty",["sidewalk"]="unknown assumed empty",["water"]="unknown assumed empty",
                ["authored_building_mask"]="unknown assumed empty; 348 captured static blockers applied separately",["source_fit_verified"]=false,["mission_ready"]=false };
            File.WriteAllText(args[4],result.ToString(Newtonsoft.Json.Formatting.Indented)+"\n");
            Console.WriteLine("[S004OrderedHandoffDiagnostic] result=Observed requests="+requests.Length+" succeeded="+succeeded+" failed="+failed+" pending="+pending+" registered="+registry.Count+" actors="+actorCount+" readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
