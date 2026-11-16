using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class S004BundleDrivenStartup
{
    static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static int Run(string[] args)
    {
        try
        {
            var bundle=JObject.Parse(File.ReadAllText(args[14])); var bundleInputs=(JObject)bundle["inputs"];
            foreach(var pair in new[] { ("compiled_setup",0), ("prefab_shared",2), ("authored_grid",3), ("static_blockers",5), ("prefab_s004",6), ("building_definitions",7), ("configured_building_readmodel",9), ("base_surface_roads",10), ("physical_config_graph",11), ("map_rotation",12), ("authored_loading_audit",13) })
                Need(Path.GetFullPath(args[pair.Item2])==(string)bundleInputs[pair.Item1]["path"],"Startup input differs from checked bundle: "+pair.Item1);
            var setup = SourceS004CompiledSetupInput.Read(Path.GetFullPath(args[0]), args[1]);
#if WARLINE_S004_CACHED_SETUP_GUARD_PROBE
            SourceS004SetupGuardCacheControls.Run(setup,Path.GetFullPath(args[0]),args[1],Path.GetDirectoryName(args[4]));
            Console.WriteLine("[S004SetupGuardCacheControls] result=Passed cases=8 fixtureOnly=1 missionReady=0");
#endif
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
            Entity session;bool originalSourceLaunch=false;
            if(Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_LAUNCH_DIAGNOSTIC")=="1") {
                var definition=sourceAssets.ByPath<SkirmishScenarioDefinitionConfig>((string)graph["definition_path"]);
                var payload=new SkirmishLaunchPayload {SessionId=sessionId.ToString(),CatalogId=setup.CatalogId,
                    DefinitionId=setup.DefinitionId,ContentVersion=setup.ContentVersion,AllConfigHashes=setup.SetupHash.ToString("X8"),
                    MapId=setup.OperationMapId,LayoutId=setup.LayoutId,DifficultyId=setup.DifficultyId,SizeId=setup.SizeId,Seed=setup.Seed};
                var launchType=System.Reflection.Assembly.GetExecutingAssembly().GetType("Game.Composition.SkirmishExpandedLaunchProjection");
                var launch=launchType?.GetMethod("TryQueue",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
                Need(launch!=null,"Original source launch owner required");
                originalSourceLaunch=(bool)launch.Invoke(null,new object[]{em,payload,setup,null,definition});
                Need(originalSourceLaunch,"Original source launch owner rejected S004");
                using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
                Need(sessions.CalculateEntityCount()==1,"Original source launch must create one session");session=sessions.GetSingletonEntity();
            } else {
                session = em.CreateEntity();
                em.AddComponentData(session, new SkirmishExpandedSessionComponent { SessionId = sessionId, Phase = SkirmishSessionPhase.Queued });
                em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });
            }
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
            bool[] authoredBuildings = assumedEmpty, sidewalks = assumedEmpty;
            string authoredBuildingScope = "unknown assumed empty; 348 captured static blockers applied separately";
            string sidewalkScope = "unknown assumed empty";
#if WARLINE_S004_HISTORICAL_MASKS
            SourceS004HistoricalPlacementMasks historicalPlacementMasks = null;
            string historicalPath = Environment.GetEnvironmentVariable("WARLINE_S004_HISTORICAL_PLACEMENT_INPUT");
            string historicalSha = Environment.GetEnvironmentVariable("WARLINE_S004_HISTORICAL_PLACEMENT_INPUT_SHA256");
            if (!string.IsNullOrEmpty(historicalPath) || !string.IsNullOrEmpty(historicalSha))
            {
                Need(!string.IsNullOrEmpty(historicalPath) && !string.IsNullOrEmpty(historicalSha), "Complete historical placement input pin required");
                var historicalInput = JObject.Parse(System.Text.Encoding.UTF8.GetString(
                    AutonomousWorld.PinnedFile(historicalPath, historicalSha, 16 * 1024 * 1024)),
                    new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                historicalPlacementMasks = SourceS004HistoricalPlacementMasks.Load(historicalInput["source_historical_placement_masks"], Path.GetFullPath(Directory.GetCurrentDirectory()), map);
                authoredBuildings = historicalPlacementMasks.Buildings;
                sidewalks = historicalPlacementMasks.Sidewalks;
                for (int i = 0; i < count; i++) roads[i] |= historicalPlacementMasks.Roads[i];
                historicalPlacementMasks.VerifyInputs();
                authoredBuildingScope = "HistoricalCapture_CurrentEquivalenceUnverified";
                sidewalkScope = "HistoricalCapture_CurrentEquivalenceUnverified";
            }
#endif
            using var retainedWorld = new AutonomousWorld(em, registry);
            JObject sourceRoadDiagnostic = null;
            if (Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_ROAD_DIAGNOSTIC") == "1") {
                var loadingAudit=JObject.Parse(File.ReadAllText(args[13]));
                string root=Directory.GetCurrentDirectory(), scene=Path.Combine(root,"Assets/Game/Scenes/Match.unity");
                string sceneHash=(string)loadingAudit["source_hashes"]?[scene];
                Need(!String.IsNullOrEmpty(sceneHash), "Checked source Match scene pin required");
                var sidewalk=em.AddBuffer<GridRoadSidewalk>(gridEntity);var dirt=em.AddBuffer<GridRoadDirt>(gridEntity);
                for(int i=0;i<count;i++){sidewalk.Add(default);dirt.Add(default);}
                string output=Path.GetDirectoryName(Path.GetFullPath(args[4]));
                var roadRun=new JObject { ["kind"]="SkirmishSourceSimulationRun",["schema_version"]=1,
                    ["repository_root"]=root,["source_repository_root"]=root,["output_directory"]=output,
                    ["mission_scene"]=new JObject { ["path"]=scene,["sha256"]=sceneHash } };
                sourceRoadDiagnostic=retainedWorld.PrepareS004InitialRoadDiagnostic(gridEntity,roadRun);
            }
            var pipeline = new GeneratedStartingBuildingPipeline(em, gridEntity, boundary, definitions.Definitions, true, registry,
                authoredBuildings, roads, sidewalks, assumedEmpty, 0f,
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
            retainedWorld.AttachS004DiagnosticStartup(gridEntity, surfaceEntity, session, boundary, setup, army, pipeline, definitions, mapRoot, args[0], args[1]);
            Need(ReferenceEquals(retainedWorld.Entities, em) && ReferenceEquals(retainedWorld.Buildings, registry) &&
                retainedWorld.Session == session && retainedWorld.GridEntity == gridEntity &&
                ReferenceEquals(retainedWorld.StartingPipeline, pipeline) && !retainedWorld.Initialized,
                "Retained source world did not preserve the startup owner objects or prematurely admitted the scheduler");
            bool repeatRejected=false, prematureTickRejected=false;
            try { retainedWorld.AttachS004DiagnosticStartup(gridEntity, surfaceEntity, session, boundary, setup, army, pipeline, definitions, mapRoot, args[0], args[1]); }
            catch (InvalidOperationException) { repeatRejected=true; }
            try { retainedWorld.AdvanceOneTick(); } catch (InvalidOperationException) { prematureTickRejected=true; }
            Need(repeatRejected && prematureTickRejected, "Retained source world must reject repeat attachment and premature scheduling");
            var groupBefore=em.GetBuffer<SkirmishArmyGroupRecord>(session).ToArray();
            var strategyBefore=em.GetComponentData<SkirmishEnemyStrategyComponent>(session);
            using var groundLease=sourceRoadDiagnostic!=null ? retainedWorld.LeaseS004GroundOwner() : null;
            var ownerSubset=retainedWorld.RunS004InactiveOwnerSubset(8);
            Need(retainedWorld.S004GroundOwnerRetained == (sourceRoadDiagnostic != null), "Source ground owner retention differs from road diagnostic");
            if (sourceRoadDiagnostic != null) retainedWorld.CompleteInitialRoadInputs();
            var resourcesBefore=(JObject)ownerSubset["resources_before"]; var resourcesAfter=(JObject)ownerSubset["resources_after"];
            Need(JToken.DeepEquals(resourcesBefore["materials"],resourcesAfter["materials"]) &&
                JToken.DeepEquals(resourcesBefore["building_storage"],resourcesAfter["building_storage"]) &&
                (int)resourcesBefore["fuel_state_count"]==14 && (int)resourcesBefore["fuel_state_initialized"]==0 &&
                (int)resourcesAfter["fuel_state_count"]==14 && (int)resourcesAfter["fuel_state_initialized"]==14,
                "Inactive source owner resource conservation or fuel initialization changed");
            int gameplayStates=(int)ownerSubset["runtime_gameplay_state_count_before_start"];
            var timeline=(JArray)ownerSubset["timeline"];
            double advanceSeconds=(double)ownerSubset["advance_wall_seconds"];
            var strategyAfter=em.GetComponentData<SkirmishEnemyStrategyComponent>(session);
            int pathRequests=(int)timeline[timeline.Count-1]["requests_after_enemy"];
            int pathsBeforeEnemy=(int)timeline[0]["requests_after_path"]; int pathsAfterEnemy=(int)timeline[0]["requests_after_enemy"];
            var gameplayAfter=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)).GetSingleton<RuntimeGameplayStateComponent>(); Need(gameplayAfter.SimulationActive==0 && gameplayAfter.PlayRequested==1,"Source state unexpectedly activated");
            var authoredLoading=JObject.Parse(File.ReadAllText(args[13]));
            var loadingFacts=(JObject)authoredLoading["authored_facts"];
            Need((string)authoredLoading["status"]=="PassedSerializedLoadingInputAudit" && (string)loadingFacts["source_scene_guid"]=="915718fcf0cd7422db987b388cd0d925" && (string)loadingFacts["match_building_config"]=="authored_null" && (string)loadingFacts["match_vehicle_config"]=="authored_null" && (string)loadingFacts["scene_building_placements"]=="authored_null" && (string)loadingFacts["scene_vehicle_placements"]=="authored_null", "Exact authored loading input audit required");
            bool projectedBuildingPlacement=GameplayStartCompletionPredicate.MapPlacementOwnerComplete(true,false,false,false,false,false);
            bool projectedVehiclePlacement=GameplayStartCompletionPredicate.MapPlacementOwnerComplete(true,false,false,false,false,false);
            bool projectedCitySpawned=GameplayStartCompletionPredicate.RuntimeCityOwnerSpawned(false,(int)loadingFacts["city_count"]);
            using var configs=em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
            using var initialized=em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig),typeof(InitialUnitsSpawnInitialized));
            int configCount=configs.CalculateEntityCount(), initializedCount=initialized.CalculateEntityCount();
            bool initialConfigGate=GameplayStartCompletionPredicate.InitialSpawnConfigsComplete(configCount,initializedCount);
            bool sourceWorldGate=GameplayStartCompletionPredicate.RuntimeWorldAvailable(em.World!=null,em.World!=null&&em.World.IsCreated);
            // Managed initialization, building context and optional owner facts are not observed; false is a conservative input, not a claim about native state.
            bool requestGate=GameplayStartCompletionPredicate.GameplayStartRequested(false,gameplayAfter.PlayRequested!=0);
            bool buildingGate=GameplayStartCompletionPredicate.BuildingStartupComplete(false);
            bool cityGate=GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(true,false);
            bool blockerGate=GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(true,false);
            bool decorationGate=GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(true,false);
            bool gateComplete=requestGate&&buildingGate&&cityGate&&blockerGate&&decorationGate&&sourceWorldGate&&initialConfigGate;
            bool activated=RuntimeGameplayStartTransition.TryActivateSourceHostDiagnostic(em,gateComplete);
            var afterGate=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)).GetSingleton<RuntimeGameplayStateComponent>();
            Need(!activated && afterGate.SimulationActive==0,"Unknown managed loading facts must not activate source host");
            JObject sourceGameplayDiagnostic=null;
            if(Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_GAMEPLAY_DIAGNOSTIC")=="1") {
                Need(sourceRoadDiagnostic!=null && retainedWorld.S004GroundOwnerRetained,"Prepared same-world source roads and ground owner required");
                string scenePath=Path.Combine(Directory.GetCurrentDirectory(),"Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/RuntimeBinding.unity");
                string scenePin=(string)authoredLoading["source_hashes"]?[scenePath];byte[] sceneBytes=File.ReadAllBytes(scenePath);
                Need(!String.IsNullOrEmpty(scenePin) && AutonomousWorld.Hash(sceneBytes)==scenePin,"Pinned S004 binding scene required");
                string sceneText=System.Text.Encoding.UTF8.GetString(sceneBytes);
                bool entityScene=map.PresentationKind==OperationMapPresentationKind.EntityScene &&
                    map.SourceSceneReference.AssetGUID=="915718fcf0cd7422db987b388cd0d925" &&
                    System.Text.RegularExpressions.Regex.Matches(sceneText,@"(?m)^  canonicalPresentationMode: 2\r?$").Count==1;
                bool missingBuildings=String.IsNullOrEmpty(map.BuildingPlacementsReference.AssetGUID);
                bool missingVehicles=String.IsNullOrEmpty(map.VehiclePlacementsReference.AssetGUID);
                bool sourceBuildings=GameplayStartCompletionPredicate.BuildingStartupComplete(buildingsBound && registry.Count==20 &&
                    GameplayStartCompletionPredicate.MapPlacementOwnerComplete(missingBuildings,false,false,false,false,false) &&
                    GameplayStartCompletionPredicate.MapPlacementOwnerComplete(missingVehicles,false,false,false,false,false));
                bool sourceCity=GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(
                    (int)loadingFacts["city_spawn_on_start"]!=0 && (int)loadingFacts["city_count"]>0,
                    GameplayStartCompletionPredicate.RuntimeCityOwnerSpawned(false,(int)loadingFacts["city_count"]));
                bool sourceBlockers=GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(
                    GameplayStartCompletionPredicate.LegacyPresentationEnabled(entityScene),false);
                bool sourceDecorations=GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(false,false);
                bool sourceStart=entityScene && missingBuildings && missingVehicles && sourceBuildings && sourceCity && sourceBlockers && sourceDecorations &&
                    GameplayStartCompletionPredicate.GameplayStartRequested(ReferenceEquals(retainedWorld.Entities,em),afterGate.PlayRequested!=0) &&
                    sourceWorldGate && initialConfigGate && resultingSession.SpawnComplete==1;
                Need(sourceStart,"Actual source-only start predicates did not pass");
                bool sourceActivated=RuntimeGameplayStartTransition.TryActivateSourceHostDiagnostic(em,sourceStart);
                Need(sourceActivated,"Original source-host start transition rejected admitted source state");
                var activeEnemy=retainedWorld.RunS004ActiveEnemyOwnerDiagnostic();
                Need(!em.BankBound,"S004 diagnostic unexpectedly has a canonical source storage bank");
                bool ownerInitializerRejected=false;JObject sourceBankDiagnostic=null;
                if(Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_BANK_DIAGNOSTIC")=="1") {
                    sourceBankDiagnostic=retainedWorld.BindS004FreshSourceStorage(Path.GetFullPath(args[14]),args[15]);
                    Need(em.BankBound && (bool?)sourceBankDiagnostic["before_after_rows_equal"]==true,"S004 source storage binding failed");
                    bool launchRejected=false;string ownerInitializationRejection=null;
                    bool ownerInitializationRequested=Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_OWNER_INITIALIZATION")=="1";
                    if(ownerInitializationRequested) {
                        Need(originalSourceLaunch,"Original source launch required before shared owner initialization");
                        retainedWorld.InitializeS004ActiveSourceOwners();
                        Need(retainedWorld.S004SourceOwnersInitialized,"Shared source owners did not initialize");
                    } else if(!originalSourceLaunch) {
                        try { retainedWorld.InitializeS004ActiveSourceOwners(); }
                        catch(InvalidOperationException error) {
                            if(error.Message!="Original BaseAssault launch and objective state required before owner initialization")throw;
                            launchRejected=true;ownerInitializationRejection=error.Message;
                        }
                        Need(launchRejected,"Incomplete source launch must reject shared owner initialization");
                    }
                    sourceBankDiagnostic["owner_initialization_rejected"]=launchRejected;
                    sourceBankDiagnostic["owner_initialization_rejection"]=ownerInitializationRejection;
                    sourceBankDiagnostic["source_owner_initialization_requested"]=ownerInitializationRequested;
                    sourceBankDiagnostic["source_owner_initialization_completed"]=retainedWorld.S004SourceOwnersInitialized;
                    sourceBankDiagnostic["source_owner_resource_day_seconds"]=retainedWorld.S004SourceOwnersInitialized ? (JToken)retainedWorld.S004SourceOwnerResourceDaySeconds : null;
#if WARLINE_S004_UNIT_METADATA_DIAGNOSTIC
                    string unitMetadataPath=Environment.GetEnvironmentVariable("WARLINE_S004_CHECKED_UNIT_METADATA_PATH");
                    string unitMetadataSha=Environment.GetEnvironmentVariable("WARLINE_S004_CHECKED_UNIT_METADATA_SHA256");
                    Need(!String.IsNullOrEmpty(unitMetadataPath) && !String.IsNullOrEmpty(unitMetadataSha),"Checked source unit metadata descriptor required");
#if WARLINE_S004_ROLE_METADATA_DIAGNOSTIC
                    var roleCandidate=JObject.Parse(File.ReadAllText(unitMetadataPath),new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
                    var roleUnits=(JArray)roleCandidate["units"]; var capturedKeys=(JArray)source["requested_prefab_keys"];
                    Need(roleUnits!=null && roleUnits.Count==19 && capturedKeys!=null && capturedKeys.Count==19 &&
                        roleUnits.Select(row=>(string)row["key"]).SequenceEqual(capturedKeys.Select(row=>(string)row)),
                        "Exact independently captured source role order required");
                    var stagedRoleIdentities=new Dictionary<string,JObject>(StringComparer.Ordinal);
                    var stagedRoleAssets=new Dictionary<string,SourceUnitPrefab>(StringComparer.Ordinal);
                    foreach(JObject row in roleUnits) {
                        var identity=(JObject)row["prefab"]; string key=(string)identity["key"], relative=(string)identity["path"], guid=(string)identity["guid"];
                        string root=Path.GetFullPath(Directory.GetCurrentDirectory())+Path.DirectorySeparatorChar;
                        string asset=Path.GetFullPath(Path.Combine(root,relative)); string meta=asset+".meta";
                        Need(asset.StartsWith(root,StringComparison.Ordinal) && templates.ContainsKey(key) && (string)row["key"]==key,
                            "Captured role prefab must resolve to an imported same-world template");
                        string assetPin=(string)source["source_pins"]?[asset],metaPin=(string)source["source_pins"]?[meta];
                        Need(!String.IsNullOrEmpty(assetPin) && !String.IsNullOrEmpty(metaPin) && AutonomousWorld.Hash(File.ReadAllBytes(asset))==assetPin &&
                            AutonomousWorld.Hash(File.ReadAllBytes(meta))==metaPin &&
                            System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(meta),@"(?m)^guid: "+System.Text.RegularExpressions.Regex.Escape(guid)+@"\s*$"),
                            "Independent captured role prefab source identity differs");
                        if(definitions.AssetIdentities.TryGetValue(key,out var existing)) Need(JToken.DeepEquals(existing,identity),"Conflicting source role identity");
                        else stagedRoleIdentities.Add(key,(JObject)identity.DeepClone());
                        var template=templates[key];
                        Need(template!=Entity.Null && em.Exists(template),"Imported role template must exist in the same world");
                        if(definitions.Assets.TryGetValue(key,out var existingAsset))
                            Need(existingAsset.Template==template,"Existing source role token must reference its same-world template");
                        else stagedRoleAssets.Add(key,new SourceUnitPrefab(key,template,relative,guid,(long)identity["local_id"]));
                    }
                    foreach(var pair in stagedRoleIdentities) definitions.AssetIdentities.Add(pair.Key,pair.Value);
                    foreach(var pair in stagedRoleAssets) definitions.Assets.Add(pair.Key,pair.Value);
#endif
                    sourceBankDiagnostic["source_unit_metadata_diagnostic"]=retainedWorld.BindActualUnitMetadata(unitMetadataPath,unitMetadataSha,Directory.GetCurrentDirectory());
                    Need(!retainedWorld.ActualUnitMetadata.FactoryDomainQualified,"S004 source metadata must remain unqualified");
                    sourceBankDiagnostic["source_unit_metadata_diagnostic"]["factory_domain_qualified"]=retainedWorld.ActualUnitMetadata.FactoryDomainQualified;
                    sourceBankDiagnostic["source_unit_metadata_diagnostic"]["runtime_admission"]=false;
#if WARLINE_S004_AIRPORT_SOURCE_DEFINITION
                    string airportPath=Environment.GetEnvironmentVariable("WARLINE_S004_AIRPORT_CANDIDATE_PATH");
                    string airportHash=Environment.GetEnvironmentVariable("WARLINE_S004_AIRPORT_CANDIDATE_SHA256");
                    Need(!String.IsNullOrEmpty(airportPath) && !String.IsNullOrEmpty(airportHash),"Pinned source airport candidate required");
                    sourceBankDiagnostic["source_airport_definition_probe"]=definitions.AddS004SourceOnlyAirport(airportPath,airportHash,em,registry);
#endif
#if WARLINE_S004_CATALOG_GETTER_PROBE
                    var catalogCandidate=JObject.Parse(File.ReadAllText(unitMetadataPath),new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
                    var requestMetadata=GeneratedS002CatalogAvailabilityBinding.DecodeGameplayRequestMetadata(catalogCandidate,retainedWorld.ActualUnitMetadata,unitMetadataPath);
                    var configuredCatalog=GeneratedS002CatalogAvailabilityBinding.DecodeConfiguredUnits(catalogCandidate,retainedWorld.ActualUnitMetadata,unitMetadataPath);
                    int requestRows=0;
                    foreach(var row in retainedWorld.ActualUnitMetadata.Ordered)
                        if(requestMetadata.TryGet(row.Prefab,out _))requestRows++;
                    int expectedUnitRows=retainedWorld.ActualUnitMetadata.Ordered.Count;
                    Need(requestRows==expectedUnitRows && configuredCatalog.Count==expectedUnitRows,"Complete source-only catalog getter readback required");
                    sourceBankDiagnostic["source_catalog_getter_probe"]=new JObject {
                        ["request_metadata_rows"]=requestRows,["configured_catalog_rows"]=configuredCatalog.Count,
                        ["projected_rows_native_sprite_names_observed"]=false,["catalog_runtime_admission"]=false,
                        ["scope"]="OriginalDecoder_SourceGetterProjection_LogicalSpriteReferences" };
#if WARLINE_S004_CATALOG_BINDING_PROBE && !WARLINE_S004_DISPATCHER_PROBE
                    var sourceCatalogBinding=GeneratedS002CatalogAvailabilityBinding.Bind(em,session,retainedWorld.ActualUnitMetadata,unitMetadataPath,unitMetadataSha);
                    var sourceCatalog=em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;
                    var overlayUnits=sourceCatalogBinding.Store.RuntimeOverlay.UnitSpawnPrefabs;
                    Need(sourceCatalogBinding.ConfiguredUnits.Count==retainedWorld.ActualUnitMetadata.Ordered.Count && overlayUnits!=null &&
                        overlayUnits.All(unit=>unit!=null && sourceCatalog.TryRole(unit.name,out _)),
                        "Same-world catalog overlay must retain only actual allowed roles");
                    var excluded=sourceCatalogBinding.ConfiguredUnits.Where(unit=>!sourceCatalog.TryRole(unit.name,out _)).Select(unit=>unit.name);
                    sourceBankDiagnostic["source_catalog_binding_probe"]=new JObject {
                        ["configured_units"]=sourceCatalogBinding.ConfiguredUnits.Count,["allowed_overlay_units"]=overlayUnits.Count,
                        ["excluded_without_role"]=new JArray(excluded),["same_world_session_bound"]=true,
                        ["gameplay_dispatcher_bound"]=false,["native_ui_bound"]=false,["mission_ready"]=false,
                        ["scope"]="SourceCatalogObjectOnly_NoInputOrDelivery" };
#if WARLINE_S004_ARIA_CONTEXT_PROBE
                    string canonicalGraphPath=Environment.GetEnvironmentVariable("WARLINE_S004_CANONICAL_SOURCE_GRAPH");
                    string canonicalGraphHash=Environment.GetEnvironmentVariable("WARLINE_S004_CANONICAL_SOURCE_GRAPH_SHA256");
                    Need(!String.IsNullOrEmpty(canonicalGraphPath) && !String.IsNullOrEmpty(canonicalGraphHash),"Pinned canonical source graph required");
                    var canonicalGraph=JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(canonicalGraphPath,canonicalGraphHash,16*1024*1024)),
                        new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
                    var canonicalAssets=new SourceConfigMaterializer(canonicalGraph);
                    var canonicalCatalog=canonicalAssets.ByPath<SkirmishBattleCatalogConfig>("Assets/Game/Resources/SkirmishBattleCatalog.asset");
                    Need(canonicalCatalog!=null && retainedWorld.ActualDefinition!=null,"Actual authored catalog and definition required");
                    var matrixInput=(JObject)graph["setup_matrix"];
                    string matrixText=System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)matrixInput["path"],(string)matrixInput["sha256"],4*1024*1024));
                    Need(SkirmishSetupMatrixTable.TryLoadFromText(matrixText,out var actualMatrix,out string matrixError),"Original matrix parser failed: "+matrixError);
                    var sourceManifest=new SkirmishContentManifest { RequiredFeatureIds=retainedWorld.ActualDefinition.RequiredFeatureIds };
                    var matchState=em.GetComponentData<SkirmishMatchState>(session);
                    var expandedState=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                    int sourcePresetIndex=SourceGroundMissionFamily.SourcePresetLoadIndex(setup,matchState.ScenarioIndex);
                    bool contextAccepted=GroundMissionContextFactory.TryCreateLogicalPublicData(retainedWorld.ActualDefinition,setup,sourceManifest,
                        actualMatrix,canonicalCatalog,expandedState,matchState.ScenarioIndex,sourcePresetIndex,out var groundContext,out string contextError);
                    sourceBankDiagnostic["source_aria_context_probe"]=new JObject {
                        ["context_accepted"]=contextAccepted,["error"]=contextError,["actual_catalog_id"]=setup.CatalogId,
                        ["actual_army_profile"]=setup.ArmyProfileId.ToString(),["actual_start_package"]=setup.StartPackageId.ToString(),
                        ["actual_scenario_index"]=matchState.ScenarioIndex,["source_preset_index"]=sourcePresetIndex,
                        ["planner_invoked"]=false,["input_sent"]=false,["native_readiness"]=false,
                        ["scope"]="OriginalContextFactory_SameSourceSession_NoPlannerOrInput" };
#endif
#endif
#endif
#endif
                    if(Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_AIR_OWNER_DIAGNOSTIC")=="1") {
                        Need(ownerInitializationRequested,"Shared source owner initialization required before air diagnostic");
                        sourceBankDiagnostic["source_air_owner_diagnostic"]=retainedWorld.RunS004OriginalAirOwnerDiagnostic();
                    }
                    if(Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_TRANSPORT_OWNER_DIAGNOSTIC")=="1") {
                        Need(Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_AIR_OWNER_DIAGNOSTIC")=="1", "Original air diagnostic required before transport diagnostic");
                        var type=System.Reflection.Assembly.GetExecutingAssembly().GetType("S004TransportOwnerDiagnostic");
                        var method=type?.GetMethod("Run",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
                        Need(method!=null,"Original transport diagnostic not compiled");
                        sourceBankDiagnostic["source_transport_owner_diagnostic"]=(JObject)method.Invoke(null,new object[]{em});
                    }
#if WARLINE_S004_EXPERIMENT_TICK
                    if(Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_EXPERIMENT_TICK")=="1")
                        sourceBankDiagnostic["source_experimental_tick"]=retainedWorld.RunS004ExperimentalSourceTick(templates.Keys);
#endif
#if WARLINE_S004_ORIGINAL_ARIA_STEP_PROBE
                    sourceBankDiagnostic["original_aria_same_world_probe"]=SourceS004OriginalAriaStepDiagnostic.Capture(
                        em,em.GetComponentData<SkirmishMatchState>(session),"WARLINE_S004_ORIGINAL_ARIA_STEP_PROBE");
#endif
#if WARLINE_S004_PRODUCTION_INPUT_PROBE
                    string sourceRoot=Directory.GetCurrentDirectory();
                    string placementConfig=File.ReadAllText(Path.Combine(sourceRoot,"Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset"));
                    var queueValues=System.Text.RegularExpressions.Regex.Matches(placementConfig,@"(?m)^  maxQueuedUnitProductions: (\d+)\r?$");
                    Need(queueValues.Count==1,"One source-configured production queue limit required");
                    int queueLimit=Int32.Parse(queueValues[0].Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
                    string composition=File.ReadAllText(Path.Combine(sourceRoot,"Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs"));
                    var randomValues=System.Text.RegularExpressions.Regex.Matches(composition,@"\binternal uint BuildingSpawnRandomState = 0x([0-9a-fA-F]+)u;");
                    Need(randomValues.Count==1,"One original production random initializer required");
                    uint randomInitial=UInt32.Parse(randomValues[0].Groups[1].Value,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture);
                    var transportInputs=new GeneratedS002TransportInputs(retainedWorld.ActualUnitMetadata,sourceRoot);
                    var productionCatalog=em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;
                    var unitsWithoutRole=new JArray(retainedWorld.ActualUnitMetadata.Ordered
                        .Where(row=>!productionCatalog.TryRole(row.Prefab.name,out _)).Select(row=>row.Prefab.name));
                    var producerCensus=new JArray(productionRoles.Select(pair=>new JObject {
                        ["unit_key"]=pair.Key,["producer_key"]=GeneratedProductionRecipeSource.ProducerPrefabKey(pair.Value.Producer),
                        ["producer_asset_bound"]=definitions.Assets.ContainsKey(GeneratedProductionRecipeSource.ProducerPrefabKey(pair.Value.Producer)),
                        ["producer_definition_bound"]=definitions.Definitions.Any(definition=>definition.Prefab!=null && definition.Prefab.name==GeneratedProductionRecipeSource.ProducerPrefabKey(pair.Value.Producer)) }));
                    string productionInputError=null;
                    try { retainedWorld.PrepareActualProductionInputs(queueLimit,randomInitial,transportInputs.TryMetadata); }
                    catch(NotSupportedException error) { productionInputError=error.Message; }
                    catch(InvalidOperationException error) { productionInputError=error.Message; }
                    sourceBankDiagnostic["source_production_input_probe"]=new JObject {
                        ["queue_limit_from_config"]=queueLimit,["random_initializer_from_source"]=randomInitial,
                        ["production_owner_bound"]=retainedWorld.ActualDeliveryOwner!=null,
                        ["configured_unit_keys_without_role"]=unitsWithoutRole,
                        ["source_role_producer_census"]=producerCensus,
                        ["error"]=productionInputError,["paid_request_attempted"]=false,
                        ["scope"]="SourceOnly_NoNativeReadiness" };
                    bool semanticRequestAttempted=false,semanticRequestAccepted=false;
#if WARLINE_S004_DISPATCHER_PROBE
                    if(retainedWorld.ActualDeliveryOwner==null)
                        Console.Error.WriteLine("[S004ProductionPrerequisiteFailure] "+sourceBankDiagnostic["source_production_input_probe"].ToString(Newtonsoft.Json.Formatting.None));
                    Need(retainedWorld.ActualDeliveryOwner!=null,"Actual source production owner required for dispatcher probe; initializer="+productionInputError);
                    Need(setup.OperationMapId=="opmap.skirmish.desert_base_01" && retainedWorld.SourceNumericCamera==null,
                        "Exact source map and absent source numeric camera required");
                    string clearancePath=Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_CLEARANCE_PATH");
                    string clearanceHash=Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_CLEARANCE_SHA256");
                    Need(!String.IsNullOrEmpty(clearancePath) && !String.IsNullOrEmpty(clearanceHash),"Pinned source clearance required");
                    var sourceClearance=new GeneratedS002DropClearance(clearancePath,clearanceHash);
                    retainedWorld.ActualDeliveryOwner.Helicopters=new SourceHelicopterDelivery(retainedWorld.ActualDeliveryOwner,
                        transportInputs.Templates,cameraKnown:true,hasCamera:false,actualCameraRight:float3.zero,
                        actualCameraForward:float3.zero,reserveActualStaticBounds:sourceClearance.Reserve);
#if WARLINE_S004_RETAINED_MAP_PROBE
                    retainedWorld.AttachS004RetainedMapPublication(publisher,mapRoot);
#endif
                    var presentMetadata=retainedWorld.ActualUnitMetadata.Ordered.Select(row=>row.Prefab.name).ToHashSet(StringComparer.Ordinal);
                    var missingRoleMetadata=productionRoles.Keys.Where(key=>!presentMetadata.Contains(key)).OrderBy(key=>key,StringComparer.Ordinal).ToArray();
                    var missingRoleTemplates=productionRoles.Keys.Where(key=>!templates.ContainsKey(key)).OrderBy(key=>key,StringComparer.Ordinal).ToArray();
                    var missingRoleIdentities=productionRoles.Keys.Where(key=>!definitions.AssetIdentities.ContainsKey(key)).OrderBy(key=>key,StringComparer.Ordinal).ToArray();
                    var roleOrder=setup.RoleOverlays.Where(role=>army.Allows(role.RoleId)).Select((role,index)=>new JObject {
                        ["index"]=index,["role_id"]=role.RoleId,["role_kind"]=role.RoleKind.ToString(),
                        ["prefab_key"]=SkirmishRoleCatalogConfig.RuntimePrefabKey(role.RoleKind),
                        ["template_present"]=templates.ContainsKey(SkirmishRoleCatalogConfig.RuntimePrefabKey(role.RoleKind)) });
                    string dispatcherError=null;
                    try { retainedWorld.BindActualPaidCatalog(unitMetadataPath,unitMetadataSha); }
                    catch(NotSupportedException error) { dispatcherError=error.ToString(); }
                    catch(InvalidOperationException error) { dispatcherError=error.ToString(); }
                    sourceBankDiagnostic["source_dispatcher_probe"]=new JObject {
                        ["source_headless_camera_absent_by_construction"]=true,
                        ["clearance_scope"]="SourceBakedSnapshot_NotLiveProviderParity",["clearance_sha256"]=clearanceHash,
                        ["source_helicopter_owner_bound"]=retainedWorld.ActualDeliveryOwner.Helicopters!=null,
                        ["source_dispatcher_bound"]=retainedWorld.ActualFactionDispatcher!=null,["error"]=dispatcherError,
                        ["role_prefabs_without_metadata"]=new JArray(missingRoleMetadata),["role_prefabs_without_templates"]=new JArray(missingRoleTemplates),
                        ["role_prefabs_without_independent_factory_identity"]=new JArray(missingRoleIdentities),
                        ["original_role_overlay_order"]=new JArray(roleOrder),
                        ["injected_requests"]=0,["native_camera_parity"]=false,["native_scene_validation"]=false,
                        ["mission_ready"]=false };
#if WARLINE_S004_LOGICAL_CONTEXT_CANDIDATE
                    string candidateGraphPath=Environment.GetEnvironmentVariable("WARLINE_S004_CANONICAL_SOURCE_GRAPH");
                    string candidateGraphHash=Environment.GetEnvironmentVariable("WARLINE_S004_CANONICAL_SOURCE_GRAPH_SHA256");
                    Need(!String.IsNullOrEmpty(candidateGraphPath) && !String.IsNullOrEmpty(candidateGraphHash),"Pinned authored canonical source graph required");
                    var candidateGraph=JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(candidateGraphPath,candidateGraphHash,16*1024*1024)),
                        new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
                    var candidateAssets=new SourceConfigMaterializer(candidateGraph);
                    var candidateCatalog=candidateAssets.ByPath<SkirmishBattleCatalogConfig>("Assets/Game/Resources/SkirmishBattleCatalog.asset");
                    var candidateMatrixInput=(JObject)graph["setup_matrix"];
                    string candidateMatrixText=System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)candidateMatrixInput["path"],(string)candidateMatrixInput["sha256"],4*1024*1024));
                    Need(SkirmishSetupMatrixTable.TryLoadFromText(candidateMatrixText,out var candidateMatrix,out string candidateMatrixError),"Original matrix parser failed: "+candidateMatrixError);
                    var candidateManifest=new SkirmishContentManifest { RequiredFeatureIds=retainedWorld.ActualDefinition.RequiredFeatureIds };
                    var candidateMatch=em.GetComponentData<SkirmishMatchState>(session);
                    var candidateSession=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                    int candidatePreset=SourceGroundMissionFamily.SourcePresetLoadIndex(setup,candidateMatch.ScenarioIndex);
                    bool candidateAccepted=GroundMissionContextFactory.TryCreateLogicalPublicData(retainedWorld.ActualDefinition,setup,candidateManifest,
                        candidateMatrix,candidateCatalog,candidateSession,candidateMatch.ScenarioIndex,candidatePreset,out var candidateContext,out string candidateError);
                    var wrongSeedSession=candidateSession;wrongSeedSession.Seed++;
                    bool wrongSeedAccepted=GroundMissionContextFactory.TryCreateLogicalPublicData(retainedWorld.ActualDefinition,setup,candidateManifest,
                        candidateMatrix,candidateCatalog,wrongSeedSession,candidateMatch.ScenarioIndex,candidatePreset,out _,out _);
                    bool wrongIndexAccepted=GroundMissionContextFactory.TryCreateLogicalPublicData(retainedWorld.ActualDefinition,setup,candidateManifest,
                        candidateMatrix,candidateCatalog,candidateSession,candidateMatch.ScenarioIndex-1,candidatePreset,out _,out _);
                    bool observationAccepted=false;
                    if(candidateAccepted) {
                        var actualPublic=SkirmishAriaPublicProjection.FromSession(em,session,army);
                        var sourceObservation=new Game.Simulation.AriaS002Observation {
                            Scope=Game.Simulation.AriaS002Scope.LogicalPublicData,SourceScenarioIndex=candidateMatch.ScenarioIndex,
                            ExpandedSession=candidateSession.IsLegacy==0,
                            ExpandedBaseAssault=candidateSession.ObjectiveKind==SkirmishObjectiveKind.BaseAssault,
                            AirProfile=actualPublic.AirProfile,CombinedArmsField=setup.ArmyProfileId==SkirmishArmyProfileId.CombinedArms && setup.StartPackageId==SkirmishStartPackageId.FieldBase };
                        candidateContext.RequireCompatibleObservation(sourceObservation);observationAccepted=true;
                    }
                    sourceBankDiagnostic["source_s004_logical_context_candidate"]=new JObject {
                        ["scope"]="SourceOnlyIdentityCandidate_OriginalCompilerCatalogAndSession_NoPlanner",
                        ["context_accepted"]=candidateAccepted,["error"]=candidateError,["source_observation_accepted"]=observationAccepted,
                        ["wrong_seed_rejected"]=!wrongSeedAccepted,["wrong_scenario_index_rejected"]=!wrongIndexAccepted,
                        ["actual_catalog_id"]=setup.CatalogId,["scenario_index"]=candidateMatch.ScenarioIndex,
                        ["planner_invoked"]=false,["input_sent"]=false,["full_policy_parity_proven"]=false,
                        ["native_readiness"]=false,["mission_ready"]=false };
#endif
                    if(Environment.GetEnvironmentVariable("WARLINE_S004_SEMANTIC_RIFLE_PROBE")=="1") {
                        Need(dispatcherError==null && retainedWorld.ActualAriaPaid!=null,
                            "Actual same-world semantic production boundary required");
                        var offer=retainedWorld.ActualAriaPaid.Read(SkirmishRoleKind.Rifle);
                        var probe=new JObject { ["scope"]="ExplicitSourceOnlySemanticCommand_NotAriaOrNativeInput",
                            ["role"]="Rifle",["role_present"]=offer.Present,["can_request"]=offer.CanRequest,
                            ["availability_reason"]=offer.Reason,["request_attempted"]=false,
                            ["accepted"]=false,["receipt_id"]=null,["mission_ready"]=false };
                        if(offer.Present && offer.CanRequest) {
                            semanticRequestAttempted=true;
                            var command=retainedWorld.ActualAriaPaid.Request(SkirmishRoleKind.Rifle,(float)retainedWorld.SimulationSeconds);
                            semanticRequestAccepted=command.Accepted;
                            probe["request_attempted"]=true;probe["accepted"]=command.Accepted;
                            probe["command_reason"]=command.SourceReason;
                            probe["receipt_id"]=command.ReceiptId.HasValue?(JToken)command.ReceiptId.Value:null;
                        }
                        sourceBankDiagnostic["source_semantic_rifle_probe"]=probe;
                    }
#if WARLINE_S004_AIRCRAFT_REQUEST_PROBE
                    Need(dispatcherError==null && retainedWorld.ActualAriaPaid!=null,
                        "Actual same-world semantic aircraft production boundary required");
                    var aircraftOffer=retainedWorld.ActualAriaPaid.Read(SkirmishRoleKind.AttackHeliLight);
                    var deliveryOwner=retainedWorld.ActualDeliveryOwner;
                    string aircraftKey=SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.AttackHeliLight);
                    var transportRows=new JArray();
                    foreach(var producerRecipes in deliveryOwner.Recipes) foreach(var recipe in producerRecipes.Value) {
                        if(recipe.Unit==null || recipe.Unit.name!=aircraftKey)continue;
                        var settings=deliveryOwner.Production.ResolveProductionTransportSettings(recipe.Unit,
                            deliveryOwner.QueueContext.UnitSpawnPrefabs,deliveryOwner.QueueContext.UnitSpawnPrefabsByKey,
                            deliveryOwner.QueueContext.TryGetPrefabLocalBounds);
                        transportRows.Add(new JObject { ["producer_id"]=producerRecipes.Key,["production_index"]=recipe.ProductionIndex,
                            ["unit_prefab"]=recipe.Unit.name,["source_asset_path"]=recipe.Unit.SourceAssetPath,
                            ["source_asset_guid"]=recipe.Unit.SourceAssetGuid,["source_asset_local_id"]=recipe.Unit.SourceAssetLocalId,
                            ["transport_mode"]=settings.Mode.ToString(),["transport_prefab"]=settings.TransportPrefab?.name,
                            ["transport_is_same_prefab"]=ReferenceEquals(settings.TransportPrefab,recipe.Unit),
                            ["requires_airport_runway"]=settings.RequiresAirportRunway,
                            ["helicopter_delivery_bound"]=deliveryOwner.Helicopters!=null });
                    }
#if WARLINE_S004_AIRSELF_HELIPAD_READBACK
#if WARLINE_S004_AIRSELF_SPAWNPOINT_PUBLICATION
                    int checkedFrames=0;
                    foreach(var pair in deliveryOwner.World.Buildings) {
                        var building=pair.Value;
                        if(building==null||building.IsDestroyed||!building.HasOwnerFaction||building.Definition?.Prefab==null||
                            building.ProductionSpawnLocalPositions==null||building.ProductionSpawnLocalPositions.Length==0)continue;
                        Need(building.CombatEntity!=Entity.Null && em.Exists(building.CombatEntity) &&
                            em.HasComponent<Unity.Transforms.LocalTransform>(building.CombatEntity) &&
                            !em.HasComponent<Unity.Transforms.Parent>(building.CombatEntity) &&
                            !em.HasComponent<Unity.Transforms.PostTransformMatrix>(building.CombatEntity),
                            "Actual unparented registered production building transform required");
                        var frame=em.GetComponentData<Unity.Transforms.LocalTransform>(building.CombatEntity);
                        Need(frame.Scale==1f && math.all(math.isfinite(frame.Position)) && math.all(math.isfinite(frame.Rotation.value)),
                            "Finite unit-scale source building transform required for original source publication");
                        checkedFrames++;
                    }
                    Need(checkedFrames>0,"Actual registered production building frames required");
                    SourceS073LegacySpawnRequestAdapter.PublishOriginalLegacyProductionSpawnPoints(em,deliveryOwner.World.GridEntity,boundary,deliveryOwner.World.Buildings);
#endif
                    var resolverEvidence=new JObject { ["scope"]="ActualSourceHelipadOwnerProbe_CopiedRng_NoQueueOrDelivery",
                        ["invoked"]=false,["native_scene_or_presentation_parity"]=false,["mission_ready"]=false };
#if WARLINE_S004_AIRSELF_SPAWNPOINT_PUBLICATION
                    resolverEvidence["source_spawnpoint_publisher_invoked"]=true;
                    resolverEvidence["checked_source_building_frames"]=checkedFrames;
#else
                    resolverEvidence["source_spawnpoint_publisher_invoked"]=false;
#endif
                    var matchingRecipes=deliveryOwner.Recipes.SelectMany(pair=>pair.Value.Select(recipe=>new {producer=pair.Key,recipe}))
                        .Where(row=>row.recipe.Unit!=null && row.recipe.Unit.name==aircraftKey &&
                            deliveryOwner.World.Buildings.TryGetValue(row.producer,out var building) && building!=null &&
                            !building.IsDestroyed && building.HasOwnerFaction && building.OwnerFactionId==1).ToArray();
                    var recent=em.HasBuffer<BuildingRecentSpawnReservation>(boundary) ? em.GetBuffer<BuildingRecentSpawnReservation>(boundary,true).ToArray() : Array.Empty<BuildingRecentSpawnReservation>();
                    float resolverTime=deliveryOwner.World.Now;
                    bool expired=recent.Any(row=>row.ExpiresAt<=resolverTime);
                    resolverEvidence["matching_player_recipes"]=matchingRecipes.Length;
                    resolverEvidence["recent_reservations_before"]=recent.Length;
                    resolverEvidence["expired_reservations_present"]=expired;
                    resolverEvidence["runtime_building_id_matcher_bound"]=deliveryOwner.World.SpawnContext.RuntimeBuildingMatchesId!=null;
                    resolverEvidence["spawnpoint_buffer_present"]=em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
                    if(em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary)) {
                        var points=em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary,true);
                        var projected=new JArray();foreach(var point in points)projected.Add(new JObject {
                            ["faction"]=(int)point.FactionId,["building_id"]=point.BuildingId.ToString(),
                            ["building_runtime_id"]=point.BuildingRuntimeId,["slot_index"]=point.SlotIndex,
                            ["cell"]=new JArray(point.Cell.x,point.Cell.y),
                            ["position"]=new JArray(point.WorldPosition.x,point.WorldPosition.y,point.WorldPosition.z) });
                        resolverEvidence["spawnpoint_rows"]=projected;
                    } else resolverEvidence["spawnpoint_rows"]=null;
                    if(matchingRecipes.Length==1 && !expired && em.Exists(matchingRecipes[0].recipe.Unit.Template) &&
                        em.HasComponent<UnitFootprint>(matchingRecipes[0].recipe.Unit.Template)) {
                        var row=matchingRecipes[0];var template=row.recipe.Unit.Template;
                        var size=em.GetComponentData<UnitFootprint>(template).Size;
                        uint beforeRandom=deliveryOwner.RandomState, localRandom=beforeRandom;
                        uint versionBefore=em.ChangeVersion;int structuralBefore=em.StructuralVersion;
                        resolverEvidence["invoked"]=true;resolverEvidence["producer_id"]=row.producer;
                        resolverEvidence["footprint"]=new JArray(size.x,size.y);
                        resolverEvidence["rng_before"]=beforeRandom;
                        try {
                            var ground=deliveryOwner.World;
                            bool found=ground.TryResolveAvailableFactionHelipadSpawn(ground.SpawnContext,1,ground.Buildings[row.producer],em,
                                ground.GridEntity,ground.Grid,em.GetComponentData<DynamicBlockerComponent>(ground.GridEntity),size,
                                ref localRandom,out var resolvedCell,out var resolvedPosition);
                            resolverEvidence["completed"]=true;resolverEvidence["found"]=found;
                            if(found) { resolverEvidence["cell"]=new JArray(resolvedCell.x,resolvedCell.y);
                                resolverEvidence["world_position"]=new JArray(resolvedPosition.x,resolvedPosition.y,resolvedPosition.z);
#if WARLINE_S004_AIRSELF_SLOT_BINDING
                                bool slotBound=ground.TryResolveProductionSlotAtCell(ground.SpawnContext,em,ground.Grid,resolvedCell,size,
                                    out var slotBuilding,out int slotIndex);
                                resolverEvidence["original_slot_resolver_invoked"]=true;
                                resolverEvidence["slot_bound"]=slotBound;
                                resolverEvidence["slot_building_id"]=slotBound?(int?)slotBuilding.Id:null;
                                resolverEvidence["slot_index"]=slotBound?(int?)slotIndex:null;
#if WARLINE_S004_AIRSELF_LAUNCH_GEOMETRY
                                var launchSettings=deliveryOwner.Production.ResolveProductionTransportSettings(row.recipe.Unit,
                                    deliveryOwner.QueueContext.UnitSpawnPrefabs,deliveryOwner.QueueContext.UnitSpawnPrefabsByKey,
                                    deliveryOwner.QueueContext.TryGetPrefabLocalBounds);
                                bool launchInputsKnown=slotBound && deliveryOwner.Helicopters!=null && !deliveryOwner.Helicopters.AnyActive &&
                                    launchSettings.Mode==BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.AirSelf &&
                                    ReferenceEquals(launchSettings.TransportPrefab,row.recipe.Unit) && launchSettings.MaxConcurrent>0;
                                resolverEvidence["source_launch_geometry_inputs_known"]=launchInputsKnown;
                                if(launchInputsKnown) {
                                    var touchdown=new float3(resolvedPosition.x,math.max(.5f,resolvedPosition.y),resolvedPosition.z);
                                    var launch=SourceAirSelfLaunchGeometry.Resolve(touchdown,false,float3.zero,float3.zero,0,launchSettings.MaxConcurrent);
                                    resolverEvidence["source_launch_geometry"]=new JObject {
                                        ["scope"]="SourceHeadlessAbsentCamera_NoActiveTransport_ReadOnly_NoLaunch",
                                        ["lane_index"]=0,["max_concurrent"]=launchSettings.MaxConcurrent,
                                        ["arrival_seconds"]=launchSettings.ArrivalSeconds,
                                        ["touchdown"]=new JArray(launch.Touchdown.x,launch.Touchdown.y,launch.Touchdown.z),
                                        ["entry"]=new JArray(launch.Entry.x,launch.Entry.y,launch.Entry.z),
                                        ["hover"]=new JArray(launch.Hover.x,launch.Hover.y,launch.Hover.z),
                                        ["rotation"]=new JArray(launch.Rotation.value.x,launch.Rotation.value.y,launch.Rotation.value.z,launch.Rotation.value.w),
                                        ["launched"]=false,["native_camera_or_flight_parity"]=false };
                                }
#endif
#endif
                            }
                        } catch(Exception error) {
                            resolverEvidence["completed"]=false;resolverEvidence["error_type"]=error.GetType().FullName;
                            resolverEvidence["error"]=error.Message;resolverEvidence["error_stack"]=error.ToString();
                        }
                        resolverEvidence["rng_after_local"]=localRandom;
                        resolverEvidence["owner_rng_unchanged"]=deliveryOwner.RandomState==beforeRandom;
                        resolverEvidence["change_version_unchanged"]=em.ChangeVersion==versionBefore;
                        resolverEvidence["structural_version_unchanged"]=em.StructuralVersion==structuralBefore;
                        var after=em.HasBuffer<BuildingRecentSpawnReservation>(boundary) ? em.GetBuffer<BuildingRecentSpawnReservation>(boundary,true).ToArray() : Array.Empty<BuildingRecentSpawnReservation>();
                        resolverEvidence["recent_reservations_unchanged"]=JToken.DeepEquals(JArray.FromObject(recent),JArray.FromObject(after));
                    } else resolverEvidence["reason"]="Unique player recipe, source template footprint and no expired reservations required";
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[4]),"airself-helipad-owner-readback.json"),resolverEvidence.ToString(Newtonsoft.Json.Formatting.Indented));
#if WARLINE_S004_AIRSELF_DELIVERY
                    Need(matchingRecipes.Length==1 && (bool?)resolverEvidence["completed"]==true &&
                        (bool?)resolverEvidence["source_launch_geometry_inputs_known"]==true,
                        "Actual same-world AirSelf launch inputs required before paid admission");
                    string airSelfReport=Environment.GetEnvironmentVariable("WARLINE_S004_AIRSELF_TRANSFORM_REPORT");
                    string airSelfReportSha=Environment.GetEnvironmentVariable("WARLINE_S004_AIRSELF_TRANSFORM_REPORT_SHA256");
                    Need(!String.IsNullOrEmpty(airSelfReport) && !String.IsNullOrEmpty(airSelfReportSha), "Pinned source pose inventory required");
                    deliveryOwner.AirSelf=new SourceS004AirSelfDelivery(deliveryOwner,matchingRecipes[0].recipe.Unit,Directory.GetCurrentDirectory(),airSelfReport,airSelfReportSha);
#if WARLINE_S004_LOGICAL_ARIA
                    SourceAriaS002Controller logicalAria=null;
                    var logicalAriaTrace=new JArray();
                    var logicalAriaStatus=new JObject { ["scope"]="S004CandidateSharedPolicy_LogicalSourceCommandsOnly_NotOriginalNativeParity",
                        ["constructed"]=false,["stopped"]=false,["error"]=null,["steps"]=0,["actual_accepted_commands"]=0,
                        ["native_input_claimed"]=false,["full_policy_parity_proven"]=false,["mission_ready"]=false };
                    Need(candidateAccepted && observationAccepted && !wrongSeedAccepted && !wrongIndexAccepted,"Validated same-world S004 source context required");
                    try {
                        logicalAria=new SourceAriaS002Controller(em,session,army,retainedWorld.WorldId,retainedWorld.ActualAriaPaid,
                            candidatePreset,candidateContext,null);
                        logicalAriaStatus["constructed"]=true;
                    } catch(Exception error) {
                        logicalAriaStatus["stopped"]=true;logicalAriaStatus["error"]=error.ToString();
                    }
                    sourceBankDiagnostic["source_logical_aria_candidate"]=logicalAriaStatus;
#endif
#endif
#endif
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[4]),"aircraft-transport-preflight.json"),
                        new JObject { ["scope"]="ReadOnlyActualSourceRecipeTransportSettings_BeforeRequest",
                            ["role"]="AttackHeliLight",["role_key"]=aircraftKey,["recipes"]=transportRows,
                            ["request_made_at_capture"]=false,["native_parity"]=false,["mission_ready"]=false }.ToString(Newtonsoft.Json.Formatting.Indented));
                    var aircraftProbe=new JObject { ["scope"]=
#if WARLINE_S004_AIRCRAFT_READONLY
                        "SourceOnlyAircraftAvailabilityRead_NoCommand",
#else
                        "ExplicitSourceOnlySemanticCommand_NotAriaOrNativeInput",
#endif
                        ["role"]="AttackHeliLight",["role_present"]=aircraftOffer.Present,
                        ["can_request"]=aircraftOffer.CanRequest,["availability_reason"]=aircraftOffer.Reason,
                        ["request_attempted"]=false,["accepted"]=false,["receipt_id"]=null,["mission_ready"]=false };
#if WARLINE_S004_AIRSELF_DELIVERY
                    uint observedAirSelfReceipt=0;
                    aircraftProbe["source_transaction_before_request"]=SourceS004AirSelfTransactionReadback.Capture(em,session,boundary,deliveryOwner,matchingRecipes[0].producer,0);
#endif
#if !WARLINE_S004_AIRCRAFT_READONLY
                    if(aircraftOffer.Present && aircraftOffer.CanRequest) {
                        semanticRequestAttempted=true;
                        var aircraftCommand=retainedWorld.ActualAriaPaid.Request(SkirmishRoleKind.AttackHeliLight,(float)retainedWorld.SimulationSeconds);
                        semanticRequestAccepted=aircraftCommand.Accepted;
                        aircraftProbe["request_attempted"]=true;aircraftProbe["accepted"]=aircraftCommand.Accepted;
                        aircraftProbe["command_reason"]=aircraftCommand.SourceReason;
                        aircraftProbe["receipt_id"]=aircraftCommand.ReceiptId.HasValue?(JToken)aircraftCommand.ReceiptId.Value:null;
#if WARLINE_S004_AIRSELF_DELIVERY
                        if(aircraftCommand.ReceiptId.HasValue) observedAirSelfReceipt=aircraftCommand.ReceiptId.Value;
                        aircraftProbe["source_transaction_after_request"]=SourceS004AirSelfTransactionReadback.Capture(em,session,boundary,deliveryOwner,matchingRecipes[0].producer,observedAirSelfReceipt);
#endif
                    }
#endif
                    sourceBankDiagnostic["source_semantic_aircraft_probe"]=aircraftProbe;
#endif
#if WARLINE_S004_AIR_OPENING_SEGMENT
                    Need(dispatcherError==null && retainedWorld.ActualAriaPaid!=null,
                        "Actual same-world semantic production boundary required");
                    var segment=new SourceS004AirOpeningSegment(em,session,army,retainedWorld.ActualAriaPaid);
                    var segmentResult=segment.Step();
                    semanticRequestAttempted=segmentResult.RequestAttempted;
                    semanticRequestAccepted=segmentResult.RequestAttempted && segmentResult.Receipt.Accepted;
                    sourceBankDiagnostic["source_air_opening_segment"]=new JObject {
                        ["scope"]="SharedAirOpeningSegmentOnly_HigherPriorityPlannerStepsNotRun",
                        ["role_present"]=segmentResult.RolePresent,["can_request"]=segmentResult.CanRequest,
                        ["availability_reason"]=segmentResult.AvailabilityReason,["policy_action"]=segmentResult.Action.ToString(),
                        ["observed_air_profile"]=segmentResult.Facts.AirProfile,["observed_infantry"]=segmentResult.Facts.Infantry,
                        ["observed_source_time"]=segmentResult.Facts.Time,["observed_rifle_pending"]=segmentResult.Facts.RifleRecruitPending,
                        ["observed_rifle_affordable"]=segmentResult.Facts.CanAffordRifle,
                        ["resulting_opening_until"]=segmentResult.State.OpeningUntil,["resulting_assault_started"]=(int)segmentResult.State.AssaultStarted,
                        ["request_attempted"]=segmentResult.RequestAttempted,["accepted"]=semanticRequestAccepted,
                        ["receipt_id"]=segmentResult.Receipt.ReceiptId.HasValue?(JToken)segmentResult.Receipt.ReceiptId.Value:null,
                        ["command_reason"]=segmentResult.RequestAttempted?segmentResult.Receipt.SourceReason:null,
                        ["full_aria_step_invoked"]=false,["native_input_claimed"]=false,["mission_ready"]=false };
#endif
#if WARLINE_S004_SUPPLY_SEGMENT
                    Need(dispatcherError==null && retainedWorld.ActualAriaPaid!=null,
                        "Actual same-world semantic production boundary required");
#if WARLINE_S004_PUBLIC_THREAT_PROBE
                    SourceS004PublicThreatControls.Run(em,session);
#endif
                    var supplySegment=new SourceS004SupplySegment(em,session,army,retainedWorld.ActualAriaPaid);
                    var supplyResult=supplySegment.Step();
                    JObject ReadSourceAirFacts() {
                        var v=SkirmishAriaPublicProjection.FromSession(em,session,army);
                        var airOffer=retainedWorld.ActualAriaPaid.Read(SkirmishRoleKind.AttackHeliLight);
                        return new JObject { ["source_time"]=(double)em.WorldUnmanaged.Time.ElapsedTime,
                            ["air_profile"]=v.AirProfile,["readiness_eligible"]=v.ReadinessEligible,
                            ["pad_present"]=v.PadPresent,["can_build_air_pad"]=v.CanBuildAirPad,
                            ["air_queue_offered"]=v.AirQueueOffered,["air_recruit_pending"]=v.AirRecruitPending,
                            ["own_attack_air_live"]=v.OwnAttackAirLive,["own_attack_air_active"]=v.OwnAttackAirActive,
                            ["own_attack_air_landed"]=v.OwnAttackAirLanded,["own_air_fuel"]=v.OwnAirFuel,
                            ["visible_hostile_air"]=v.VisibleHostileAir,["antiair_committed"]=v.AntiAirCommitted,
                            ["can_afford_antiair"]=v.CanAffordAntiAir,
                            ["attack_helicopter_role_present"]=airOffer.Present,["attack_helicopter_can_request"]=airOffer.CanRequest,
                            ["attack_helicopter_availability_reason"]=airOffer.Reason,
                            ["native_control_visibility_known"]=false };
                    }
                    semanticRequestAttempted=supplyResult.RequestAttempted;
                    semanticRequestAccepted=supplyResult.RequestAttempted && supplyResult.Receipt.Accepted;
                    sourceBankDiagnostic["source_supply_segment"]=new JObject {
                        ["scope"]="SharedSupplySegmentOnly_HigherPriorityPlannerStepsNotRun",
                        ["policy_invoked"]=!supplyResult.OpeningDefensePriorityUnresolved && !supplyResult.PublicThreatPresent,
                        ["policy_action"]=supplyResult.OpeningDefensePriorityUnresolved || supplyResult.PublicThreatPresent ? null : supplyResult.Action.ToString(),
                        ["observed_air_profile"]=supplyResult.Facts.AirProfile,
                        ["observed_infantry"]=supplyResult.Facts.Infantry,
                        ["observed_own_defense_towers"]=supplyResult.OwnDefenseTowers,
                        ["opening_defense_priority_unresolved"]=supplyResult.OpeningDefensePriorityUnresolved,
                        ["source_public_home_threat_checked"]=supplyResult.PublicThreatChecked,
                        ["isolated_threat_identity_negative_controls_passed"]=supplyResult.PublicThreatChecked,
                        ["isolated_visible_hidden_distance_and_defense_controls_passed"]=supplyResult.PublicThreatChecked,
#if WARLINE_S004_CACHED_SETUP_GUARD_PROBE
                        ["cached_guard_differential_controls_passed"]=true,
#endif
                        ["source_public_home_threat_present"]=supplyResult.PublicThreatChecked ? (JToken)supplyResult.PublicThreatPresent : null,
                        ["observed_source_time"]=supplyResult.Facts.Time,
                        ["observed_logistics_committed"]=supplyResult.Facts.LogisticsTruckCommitted,
                        ["observed_tankers_committed"]=supplyResult.Facts.TankersCommitted,
                        ["observed_tanker_affordable"]=supplyResult.Facts.CanAffordTanker,
                        ["logistics_present"]=supplyResult.Logistics.Present,["logistics_can_request"]=supplyResult.Logistics.CanRequest,
                        ["tanker_present"]=supplyResult.Tanker.Present,["tanker_can_request"]=supplyResult.Tanker.CanRequest,
                        ["tanker_availability_reason"]=supplyResult.Tanker.Reason,
                        ["resulting_assault_started"]=(int)supplyResult.State.AssaultStarted,
                        ["public_air_facts_after_decision"]=ReadSourceAirFacts(),
                        ["request_attempted"]=supplyResult.RequestAttempted,["accepted"]=semanticRequestAccepted,
                        ["receipt_id"]=supplyResult.Receipt.ReceiptId.HasValue?(JToken)supplyResult.Receipt.ReceiptId.Value:null,
                        ["command_reason"]=supplyResult.RequestAttempted?supplyResult.Receipt.SourceReason:null,
                        ["full_aria_step_invoked"]=false,["native_input_claimed"]=false,["mission_ready"]=false };
#endif
#endif
                    string productionTicksText=Environment.GetEnvironmentVariable("WARLINE_S004_PRODUCTION_DIAGNOSTIC_TICKS");
                    if(!String.IsNullOrEmpty(productionTicksText)) {
#if WARLINE_S004_DISPATCHER_PROBE
                        Need(dispatcherError==null,"Incomplete source dispatcher binding cannot advance production ticks");
#endif
                        int sourceTickLimit=Environment.GetEnvironmentVariable("WARLINE_S004_TERMINAL_DIAGNOSTIC")=="1"?66000:60000;
                        Need(Int32.TryParse(productionTicksText,out int productionTicks) && productionTicks>=1 && productionTicks<=sourceTickLimit,
                            "Bounded source production ticks required");
                        Need(retainedWorld.ActualDeliveryOwner!=null,"Source production owner must bind before advancing");
                        int completed=0,paidPhases=0,factionPhases=0,maxRequestRows=0; var requestVariants=new HashSet<string>(StringComparer.Ordinal); var requestSamples=new JArray(); var plannerSamples=new JArray();
                        bool profileOwnerPhases=Environment.GetEnvironmentVariable("WARLINE_S004_OWNER_PHASE_TOTALS")=="1";
                        var ownerPhaseTotals=new Dictionary<string,double>(StringComparer.Ordinal);
                        JObject ReadPlannerSample() {
                            var sample=new JObject { ["tick"]=retainedWorld.Tick,["world_elapsed_seconds"]=em.WorldUnmanaged.Time.ElapsedTime };
                            sample["enemy_policy_present"]=setup.EnemyPolicy!=null;
                            bool activeClock=SkirmishEnemyPlannerService.TryReadActiveClock(em,session,out float enemyClock);
                            sample["enemy_active_clock_read"]=activeClock;
                            sample["enemy_active_clock_seconds"]=activeClock ? (JToken)enemyClock : null;
                            if(em.HasComponent<SkirmishObjectiveClockComponent>(session)) {
                                var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                                sample["objective_clock"]=new JObject { ["elapsed_seconds"]=clock.ElapsedSeconds,["playing"]=(int)clock.Playing,["paused"]=(int)clock.Paused };
                            } else sample["objective_clock"]=null;
                            if(em.HasComponent<SkirmishEnemyPlannerV1Component>(session)) {
                                var planner=em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);
                                sample["enemy_planner"]=new JObject { ["initialized"]=(int)planner.Initialized,["decision_count"]=planner.DecisionCount,
                                    ["current_plan"]=planner.CurrentPlan.ToString(),["next_decision_at"]=planner.NextDecisionAt,
                                    ["last_decision_at"]=planner.LastDecisionAt,["opening_until"]=planner.OpeningUntil };
                            } else sample["enemy_planner"]=null;
                            if(em.HasComponent<SkirmishEnemyStrategyComponent>(session)) {
                                var strategy=em.GetComponentData<SkirmishEnemyStrategyComponent>(session);
                                sample["enemy_fallback_strategy"]=new JObject { ["priority"]=strategy.Priority.ToString(),
                                    ["recruit_role"]=strategy.RecruitRole.ToString(),["last_score"]=strategy.LastScore,
                                    ["failed_attempts"]=strategy.FailedAttempts,["last_group_id"]=strategy.LastGroupId,
                                    ["counter_committed"]=(int)strategy.CounterCommitted };
                            } else sample["enemy_fallback_strategy"]=null;
                            if(em.HasComponent<SkirmishEnemyProductionIntentV1Component>(session)) {
                                var intent=em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session);
                                sample["production_intent"]=new JObject { ["state"]=intent.State.ToString(),["role"]=intent.Role.ToString(),
                                    ["request_id"]=intent.RequestId,["next_request_id"]=intent.NextRequestId,["last_reject_code"]=(int)intent.LastRejectCode };
                            } else sample["production_intent"]=null;
                            if(em.HasBuffer<SkirmishArmyGroupRecord>(session)) {
                                var groups=em.GetBuffer<SkirmishArmyGroupRecord>(session,true);
                                var groupRows=new JArray();
                                for(int j=0;j<groups.Length;j++) {
                                    var group=groups[j];
                                    groupRows.Add(new JObject { ["group_id"]=group.GroupId,["faction"]=(int)group.FactionId,
                                        ["role"]=group.Role.ToString(),["domain"]=group.Domain.ToString(),
                                        ["members"]=group.MemberCount,["alive"]=group.AliveCount,
                                        ["last_order"]=group.LastOrder.ToString() });
                                }
                                sample["army_groups"]=groupRows;
                            } else sample["army_groups"]=null;
                            return sample;
                        }
                        plannerSamples.Add(ReadPlannerSample());bool firstFailureCaptured=false;
#if WARLINE_S004_ENEMY_BREAKDOWN_PROBE
                        SourceS004EnemyTiming.Reset();
#endif
                        var stopwatch=System.Diagnostics.Stopwatch.StartNew();
                        string progressPath=Path.Combine(Path.GetDirectoryName(args[4]),"source-progress.jsonl");
                        using var progress=new StreamWriter(new FileStream(progressPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read)) { AutoFlush=true };
#if WARLINE_S004_VALIDATOR_SNAPSHOTS_PROBE
                        var validator=new SourceMissionValidatorSnapshot(retainedWorld);
                        string validatorPath=Path.Combine(Path.GetDirectoryName(args[4]),"source-validator-snapshots.jsonl");
                        using var validatorOutput=new StreamWriter(new FileStream(validatorPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read)) { AutoFlush=true };
                        int validatorRows=0;
                        void WriteValidator() { validatorOutput.WriteLine(validator.Capture().ToString(Newtonsoft.Json.Formatting.None));validatorRows++; }
                        WriteValidator();
#endif
                        int progressRows=0;
#if WARLINE_S004_FUEL_DRIFT_READBACK
                        string fuelDriftPath=Path.Combine(Path.GetDirectoryName(args[4]),"source-fuel-drift.jsonl");
                        using var fuelDriftOutput=new StreamWriter(new FileStream(fuelDriftPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read)) { AutoFlush=true };
                        int fuelDriftRows=0;
#endif
                        void WriteSourceProgress(string reason) {
                            var liveMatch=em.GetComponentData<SkirmishMatchState>(session);
                            var liveObjective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
                            var row=new JObject { ["scope"]="SourceOwnerSubset_NotNativeOrFullScheduler",["reason"]=reason,
                                ["tick"]=retainedWorld.Tick,["simulation_seconds"]=retainedWorld.SimulationSeconds,
                                ["phase"]=liveMatch.Phase.ToString(),["match_outcome"]=liveMatch.Outcome.ToString(),
                                ["objective_outcome"]=liveObjective.Outcome.ToString(),["terminal"]=liveObjective.Terminal!=0,
                                ["explicit_semantic_requests"]=semanticRequestAttempted?1:0,
                                ["accepted_semantic_requests"]=semanticRequestAccepted?1:0,
                                ["delivered_members"]=retainedWorld.ActualDeliveryOwner.DeliveredMembers,
                                ["delivery_event_count"]=retainedWorld.ActualDeliveryOwner.DeliveryEvents.Count,
                                ["native_input_claimed"]=false,["mission_ready"]=false };
                            progress.WriteLine(row.ToString(Newtonsoft.Json.Formatting.None));progressRows++;
                        }
                        WriteSourceProgress("BeforeAdvance");
#if WARLINE_S004_AVAILABILITY_TIMING
                        SourceS004AvailabilityTiming.Reset();
#endif
#if WARLINE_S004_SCOPE_TIMING
                        SourceS004ScopeTiming.Reset();
#endif
                        for(int i=0;i<productionTicks;i++) {
                            var step=retainedWorld.AdvanceConstructedSourceOwners(); completed++;
#if WARLINE_S004_LOGICAL_ARIA
                            if(logicalAria!=null && (bool?)logicalAriaStatus["stopped"]!=true) {
                                try {
                                    var decision=logicalAria.Step(retainedWorld.Tick,UnityEngine.Time.frameCount,UnityEngine.Time.unscaledTime,UnityEngine.Time.time);
                                    logicalAriaTrace.Add(decision);
                                    logicalAriaStatus["steps"]=(int)logicalAriaStatus["steps"]+1;
                                    if((bool?)(decision["receipt"] as JObject)?["accepted"]==true) logicalAriaStatus["actual_accepted_commands"]=(int)logicalAriaStatus["actual_accepted_commands"]+1;
                                    if((bool?)decision["stopped"]==true) logicalAriaStatus["stopped"]=true;
                                } catch(Exception error) {
                                    logicalAriaStatus["stopped"]=true;logicalAriaStatus["error_tick"]=retainedWorld.Tick;
                                    logicalAriaStatus["error"]=error.ToString();
                                }
                            }
#endif
#if WARLINE_S004_FUEL_DRIFT_READBACK
                            if(retainedWorld.Tick>=800 && retainedWorld.Tick<=910) {
                                fuelDriftOutput.WriteLine(SourceS004FuelDriftReadback.Capture(em,session,retainedWorld.Tick).ToString(Newtonsoft.Json.Formatting.None));fuelDriftRows++;
                            }
#endif
                            if(profileOwnerPhases) {
                                var phaseWall=step["phase_wall_seconds"] as JObject;
                                Need(phaseWall!=null,"Actual owner phase timings required");
                                foreach(var property in phaseWall.Properties()) {
                                    double seconds=(double)property.Value;
                                    Need(!Double.IsNaN(seconds) && !Double.IsInfinity(seconds) && seconds>=0,"Finite owner phase timing required");
                                    ownerPhaseTotals[property.Name]=ownerPhaseTotals.TryGetValue(property.Name,out var total)?total+seconds:seconds;
                                }
                            }
                            if(step["phase_wall_seconds"]?["paid_production"]!=null) paidPhases++;
                            if(step["phase_wall_seconds"]?["faction_requests"]!=null) factionPhases++;
                            if(em.HasBuffer<BuildingFactionUnitProductionRequest>(boundary)) {
                                var requestsNow=em.GetBuffer<BuildingFactionUnitProductionRequest>(boundary,true);
                                maxRequestRows=Math.Max(maxRequestRows,requestsNow.Length);
                                for(int j=0;j<requestsNow.Length;j++) {
                                    var request=requestsNow[j];string variant=request.FactionId+"/"+request.UnitId+"/"+request.Status+"/"+request.ResultCode+"/"+request.ResourcesReserved;
                                    if(requestVariants.Add(variant) && requestSamples.Count<32)
                                        requestSamples.Add(new JObject { ["tick"]=retainedWorld.Tick,["request_id"]=request.RequestId,
                                            ["faction"]=request.FactionId,["unit_id"]=request.UnitId.ToString(),["status"]=(int)request.Status,
                                            ["result_code"]=(int)request.ResultCode,["resources_reserved"]=(int)request.ResourcesReserved });
                                }
                            }
                            if(!firstFailureCaptured && em.HasComponent<SkirmishEnemyStrategyComponent>(session) &&
                                em.GetComponentData<SkirmishEnemyStrategyComponent>(session).FailedAttempts>0) {
                                var failureSample=ReadPlannerSample();failureSample["sample_reason"]="FirstObservedFailure";
                                plannerSamples.Add(failureSample);firstFailureCaptured=true;
                            }
                            if((i+1)%60==0 || i+1==productionTicks)plannerSamples.Add(ReadPlannerSample());
                            var liveObjective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
                            if((i+1)%300==0 || liveObjective.Terminal!=0 || i+1==productionTicks)
                                WriteSourceProgress(liveObjective.Terminal!=0?"Terminal":i+1==productionTicks?"Limit":"Interval");
#if WARLINE_S004_VALIDATOR_SNAPSHOTS_PROBE
                            if((i+1)%300==0 || liveObjective.Terminal!=0 || i+1==productionTicks) WriteValidator();
#endif
                            if(liveObjective.Terminal!=0) break;
                        }
                        stopwatch.Stop();
#if WARLINE_S004_FUEL_DRIFT_READBACK
                        sourceBankDiagnostic["source_fuel_drift_readback"]=new JObject { ["scope"]="PassiveAfterOwnerAndControllerTicks_800To910_SourceOnly",["path"]=fuelDriftPath,["rows"]=fuelDriftRows,["native_parity"]=false,["mission_ready"]=false };
#endif
#if WARLINE_S004_AVAILABILITY_TIMING
                        sourceBankDiagnostic["source_availability_timing"]=SourceS004AvailabilityTiming.Snapshot();
#endif
#if WARLINE_S004_SCOPE_TIMING
                        sourceBankDiagnostic["source_availability_scope_timing"]=SourceS004ScopeTiming.Snapshot();
#endif
                        if((int?)plannerSamples.Last?["tick"]!=retainedWorld.Tick)plannerSamples.Add(ReadPlannerSample());
                        var afterProduction=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
#if WARLINE_S004_LOGICAL_ARIA
                        logicalAriaStatus["trace"]=logicalAriaTrace;
                        logicalAriaStatus["owner_ticks_after_controller_stopped"]=(bool?)logicalAriaStatus["stopped"]==true ? (JToken)(completed-(int)logicalAriaStatus["steps"]) : 0;
#endif
#if WARLINE_S004_AIRSELF_DELIVERY
                        aircraftProbe["source_transaction_after_owner_ticks"]=SourceS004AirSelfTransactionReadback.Capture(em,session,boundary,deliveryOwner,matchingRecipes[0].producer,observedAirSelfReceipt);
#if WARLINE_S004_AIRCRAFT_RETURN_READBACK
                        aircraftProbe["source_aircraft_return_readback"]=SourceS004AircraftReturnReadback.Capture(em,session,boundary,matchingRecipes[0].producer);
#endif
#endif
#if WARLINE_S004_SUPPLY_FOLLOWUP_PROBE
                        var afterMatch=em.GetComponentData<SkirmishMatchState>(session);
                        if(afterProduction.Terminal==0 && afterMatch.Phase==SkirmishPhase.Playing && afterMatch.Outcome==SkirmishOutcome.None) {
                            var followup=supplySegment.Step();
                            sourceBankDiagnostic["source_supply_followup"]=new JObject {
                                ["scope"]="SameSupplyStateAfterOriginalOwnerSubsetTicks_NotFullPlanner",
                                ["source_tick"]=retainedWorld.Tick,["source_time"]=followup.Facts.Time,
                                ["observed_tankers_committed"]=followup.Facts.TankersCommitted,
                                ["observed_logistics_committed"]=followup.Facts.LogisticsTruckCommitted,
                                ["source_public_home_threat_checked"]=followup.PublicThreatChecked,
                                ["source_public_home_threat_present"]=followup.PublicThreatPresent,
                                ["opening_defense_priority_unresolved"]=followup.OpeningDefensePriorityUnresolved,
                                ["policy_invoked"]=!followup.PublicThreatPresent && !followup.OpeningDefensePriorityUnresolved,
                                ["policy_action"]=followup.PublicThreatPresent || followup.OpeningDefensePriorityUnresolved ? null : followup.Action.ToString(),
                                ["request_attempted"]=followup.RequestAttempted,
                                ["accepted"]=followup.RequestAttempted && followup.Receipt.Accepted,
                                ["receipt_id"]=followup.Receipt.ReceiptId.HasValue?(JToken)followup.Receipt.ReceiptId.Value:null,
                                ["command_reason"]=followup.RequestAttempted?followup.Receipt.SourceReason:null,
                                ["owner_ticks_after_followup"]=0,["native_input_claimed"]=false,["mission_ready"]=false };
                            sourceBankDiagnostic["source_supply_followup"]["public_air_facts_after_decision"]=ReadSourceAirFacts();
                        } else sourceBankDiagnostic["source_supply_followup"]=new JObject {
                            ["scope"]="NotInvoked_SessionNoLongerPlaying",["source_tick"]=retainedWorld.Tick,
                            ["phase"]=afterMatch.Phase.ToString(),["outcome"]=afterMatch.Outcome.ToString(),
                            ["native_input_claimed"]=false,["mission_ready"]=false };
#endif
                        sourceBankDiagnostic["source_production_ticks"]=new JObject {
                            ["requested_ticks"]=productionTicks,["completed_ticks"]=completed,["paid_owner_phases_observed"]=paidPhases,["faction_request_phases_observed"]=factionPhases,
                            ["max_faction_request_buffer_rows"]=maxRequestRows,["distinct_request_state_variants"]=requestVariants.Count,
                            ["request_state_samples"]=requestSamples,["request_sample_limit"]=32,
                            ["enemy_planner_samples"]=plannerSamples,["planner_sample_interval_ticks"]=60,
                            ["wall_seconds"]=stopwatch.Elapsed.TotalSeconds,["source_tick_after"]=retainedWorld.Tick,
                            ["objective_outcome_after"]=afterProduction.Outcome.ToString(),["terminal_after"]=afterProduction.Terminal!=0,
                            ["faction_dispatcher_bound"]=retainedWorld.ActualFactionDispatcher!=null,
                            ["helicopter_delivery_bound"]=retainedWorld.ActualDeliveryOwner.Helicopters!=null,
                            ["delivered_members"]=retainedWorld.ActualDeliveryOwner.DeliveredMembers,
                            ["delivery_events"]=JArray.FromObject(retainedWorld.ActualDeliveryOwner.DeliveryEvents),
                            ["progress_path"]=progressPath,["progress_rows"]=progressRows,["progress_interval_ticks"]=300,
                            ["owner_phase_timing_scope"]=profileOwnerPhases?"InstrumentedSourceOwnerSubset_NotFullScheduler":null,
                            ["owner_phase_totals_seconds"]=profileOwnerPhases?JObject.FromObject(ownerPhaseTotals):null,
                            ["injected_requests"]=semanticRequestAttempted?1:0,["scope"]="SourceOwnerSubset_NoNativeInputOrReadiness" };
#if WARLINE_S004_VALIDATOR_SNAPSHOTS_PROBE
                        sourceBankDiagnostic["source_production_ticks"]["validator_snapshot_rows"]=validatorRows;
                        sourceBankDiagnostic["source_production_ticks"]["validator_snapshot_scope"]="SelectedActorSessionReservationFields_NotFullWorld";
#endif
#if WARLINE_S004_ENEMY_BREAKDOWN_PROBE
                        sourceBankDiagnostic["source_production_ticks"]["enemy_subcall_timing_seconds"]=JObject.FromObject(SourceS004EnemyTiming.Seconds);
                        sourceBankDiagnostic["source_production_ticks"]["enemy_subcall_counts"]=JObject.FromObject(SourceS004EnemyTiming.Calls);
                        sourceBankDiagnostic["source_production_ticks"]["enemy_subcall_timing_nested"]=true;
#endif
                    }
#endif
                } else {
                    try { retainedWorld.InitializeS004ActiveSourceOwners(); }
                    catch(InvalidOperationException error) {
                        if(error.Message!="Canonical source storage bank required before owner initialization")throw;
                        ownerInitializerRejected=true;
                    }
                    Need(ownerInitializerRejected && !em.BankBound,"Unbound source storage must reject owner initialization");
                }
                sourceGameplayDiagnostic=new JObject { ["scope"]="SourceHostOnly_ManagedNativeReadinessUnverified",["entity_scene"]=entityScene,
                    ["scene_sha256"]=scenePin,["building_placement_reference_missing"]=missingBuildings,["vehicle_placement_reference_missing"]=missingVehicles,
                    ["source_building_gate"]=sourceBuildings,["source_city_gate"]=sourceCity,["source_blocker_gate"]=sourceBlockers,
                    ["decorations_excluded_as_presentation"]=true,["source_start_predicates_passed"]=sourceStart,
                    ["source_start_transition_activated"]=sourceActivated,["enemy_owner"]=activeEnemy,
                    ["canonical_source_bank_bound"]=em.BankBound,["source_bank_diagnostic"]=sourceBankDiagnostic,["owner_initialization_rejected_without_bank"]=ownerInitializerRejected,["native_input_claimed"]=false,
                    ["physical_source_validated"]=false,["native_readiness_claimed"]=false,["full_scheduler_executed"]=false,["mission_ready"]=false };
                afterGate=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)).GetSingleton<RuntimeGameplayStateComponent>();
            }
            var groupAfter=em.GetBuffer<SkirmishArmyGroupRecord>(session).ToArray();
            int actualActiveIntents=0;
            using(var intents=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishMoveIntentComponent)))
            using(var entities=intents.ToEntityArray(Allocator.Temp)) foreach(var unit in entities) {
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(unit); var intent=em.GetComponentData<SkirmishMoveIntentComponent>(unit);
                if(owned.SessionId.Equals(sessionId) && owned.FactionId==2 && intent.Active!=0) actualActiveIntents++;
            }
            var beforeByGroup=groupBefore.ToDictionary(value=>value.GroupId);
            var afterByGroup=groupAfter.ToDictionary(value=>value.GroupId);
            int changedOrders=0,addedGroups=0,removedGroups=0;
            foreach(var current in groupAfter) {
                if(!beforeByGroup.TryGetValue(current.GroupId,out var previous)) { addedGroups++; continue; }
                if(previous.LastOrder!=current.LastOrder) changedOrders++;
            }
            foreach(var previous in groupBefore) if(!afterByGroup.ContainsKey(previous.GroupId)) removedGroups++;
            int actorCount=0, groupedCount=0;
            using(var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent)))
            using(var entities=query.ToEntityArray(Allocator.Temp)) foreach(var actor in entities) {
                if(!em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).SessionId.Equals(sessionId)) continue;
                actorCount++; if(em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor)) groupedCount++;
            }
            var playerCapacity=em.GetComponentData<SkirmishCapacityComponent>(session);
            var enemyCapacity=em.HasComponent<SkirmishEnemyCapacityComponent>(session) ? em.GetComponentData<SkirmishEnemyCapacityComponent>(session) : default;
            bool groundRetained=retainedWorld.S004GroundOwnerRetained;
            groundLease?.Dispose();
            bool groundReleased=!retainedWorld.S004GroundOwnerRetained;
            Need(groundReleased && !retainedWorld.S004SourceOwnersInitialized,"Retained source owners did not release");
            var result=new JObject { ["status"]="DiagnosticOriginalPlacementOwnerObserved", ["request_count"]=requests.Length,
                ["succeeded"]=succeeded,["failed"]=failed,["pending"]=pending,["registered"]=registry.Count,["requests"]=rows,
                ["added_group_count"]=addedGroups,["removed_group_count"]=removedGroups,
                ["source_bundle_preflight_passed_before_world"]=true,["original_metadata_published"]=true,["metadata_before_session_and_placement"]=true,["physical_source_validated"]=false,["source_registry_root_count"]=prefabEntries.Length,["native_registry_captured"]=false,["production_catalog_source_roles"]=productionRoles.Count,["loading_gate"]=new JObject { ["authored_null_building_projection_complete"]=projectedBuildingPlacement,["authored_null_vehicle_projection_complete"]=projectedVehiclePlacement,["authored_city_count"]=(int)loadingFacts["city_count"],["zero_city_projection_spawned"]=projectedCitySpawned,["projection_scope"]="conditional authored inputs; live managed resolution unknown",["gameplay_initialized"]="unknown",["building_startup_complete"]="unknown",["city_owner_state"]="unknown",["blocker_owner_state"]="unknown",["decoration_owner_state"]="unknown",["unknown_inputs_treated_as_false"]=true,["initial_config_count"]=configCount,["initialized_config_count"]=initializedCount,["initial_config_gate"]=initialConfigGate,["source_world_gate"]=sourceWorldGate,["request_gate"]=requestGate,["gate_complete"]=gateComplete,["source_activation_result"]=activated,["native_readiness"]=false },["ordered_subset"]=new JArray("combat_ensure","fabrication","validation","occupancy","acquisition","synchronization","path","chase","grid_movement","fuel","attack","defense","death","world_movement","enemy_strategy","ecb"),["timeline"]=timeline,["fixed_tick_count"]=8,["advance_wall_seconds"]=advanceSeconds,["source_play_requested"]=(int)gameplayAfter.PlayRequested,["source_simulation_active"]=(int)afterGate.SimulationActive,["paths_before_enemy"]=pathsBeforeEnemy,["paths_after_enemy"]=pathsAfterEnemy,["world_movement_owner_updates"]=8,["world_movement_delta_seconds"]=1f/60f,["runtime_gameplay_state_count"]=gameplayStates,["enemy_path_requests"]=pathRequests,["enemy_active_move_intents"]=actualActiveIntents,["changed_group_orders"]=changedOrders,["enemy_last_group_id"]=strategyAfter.LastGroupId,["enemy_failed_attempts"]=strategyAfter.FailedAttempts,["enemy_owner_updates"]=8,["enemy_priority_before"]=strategyBefore.Priority.ToString(),["enemy_priority_after"]=strategyAfter.Priority.ToString(),["enemy_recruit_role"]=strategyAfter.RecruitRole.ToString(),["enemy_score"]=strategyAfter.LastScore,["outer_owner_executed"]=true,["session_phase"]=resultingSession.Phase.ToString(),["source_view_profile_assumed_absent"]=true,["full_ledger_result"]=ledgerResult,["ledger_reason"]=ledgerReason.ToString(),["visual_pending"]=(int)visualPending,
                ["same_world_startup_retained"]=true,["source_launch_owner_invoked"]=originalSourceLaunch,["repeat_attachment_rejected"]=repeatRejected,["premature_scheduler_tick_rejected"]=prematureTickRejected,["retained_world_scheduler_admitted"]=retainedWorld.Initialized,["source_initial_road_diagnostic"]=sourceRoadDiagnostic,["source_gameplay_diagnostic"]=sourceGameplayDiagnostic,["source_ground_owner_retained_after_subset"]=groundRetained,["source_ground_owner_released_before_exit"]=groundReleased,
                ["resources_before"]=ownerSubset["resources_before"],["resources_after"]=ownerSubset["resources_after"],
                ["direct_resource_owner_diagnostic"]=ownerSubset["direct_resource_owner_diagnostic"],
                ["resource_cadence_diagnostic"]=ownerSubset["resource_cadence_diagnostic"],
                ["owner_ran_flag_scope"]="Wrapper entered original OnUpdate; its internal gameplay gates may still return without work",
                ["player_live_infantry"]=playerCapacity.InfantryLive,["enemy_live_infantry"]=enemyCapacity.InfantryLive,
                ["materials_initialized"]=em.HasComponent<SkirmishSharedMaterialsInitialized>(session),["supply_initialized"]=em.HasComponent<SkirmishSharedSupplyInitialized>(session),
                ["original_building_owner_bound"]=buildingsBound,["bound_buildings"]=boundRows.Length,["actors_created"]=actorCount,["actors_grouped"]=groupedCount,
                ["base_road_mask_source"]=true,["scene_overlays"]="unknown assumed empty",["sidewalk"]=sidewalkScope,["water"]="unknown assumed empty",
                ["authored_building_mask"]=authoredBuildingScope,["source_fit_verified"]=false,["mission_ready"]=false };
#if WARLINE_S004_HISTORICAL_MASKS
            historicalPlacementMasks?.VerifyInputs();
#endif
            File.WriteAllText(args[4],result.ToString(Newtonsoft.Json.Formatting.Indented)+"\n");
            Console.WriteLine("[S004BundleDrivenStartup] result=Observed requests="+requests.Length+" succeeded="+succeeded+" failed="+failed+" pending="+pending+" registered="+registry.Count+" actors="+actorCount+" readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}

internal static class S004BundleDrivenEntry {
 public static int Main(string[] args) {
  try {
   if(args.Length!=4)throw new ArgumentException("usage: repo bundle sha256 fresh-output-directory");
   string root=Path.GetFullPath(args[0]), bundlePath=Path.GetFullPath(args[1]), output=Path.GetFullPath(args[3]);
   if(root!=Directory.GetCurrentDirectory() || !Directory.Exists(output))throw new InvalidDataException("Exact source root and prepared output required");
   string preflight=Path.Combine(output,"bundle-preflight.json");
   var timing=System.Diagnostics.Stopwatch.StartNew();
#if WARLINE_S004_CURRENT_EPOCH
   if(SourceS004CurrentEpochPreflight.Main(new[] {root,bundlePath,args[2],preflight})!=0)throw new InvalidDataException("Strict current-epoch bundle preflight failed before world creation");
#else
   if(SourceS004BundlePreflight.Main(new[] {root,bundlePath,args[2],preflight})!=0)throw new InvalidDataException("Strict bundle preflight failed before world creation");
#endif
   timing.Stop(); Console.WriteLine("[S004Timing] source_bundle_preflight_seconds="+timing.Elapsed.TotalSeconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
   var bundle=JObject.Parse(File.ReadAllText(bundlePath)); var input=(JObject)bundle["inputs"];
   string PathOf(string name)=>(string)input[name]["path"];
   string HashOf(string name)=>(string)input[name]["sha256"];
   if(Environment.GetEnvironmentVariable("WARLINE_S004_SOURCE_BANK_DIAGNOSTIC")=="1") {
    var binding=System.Reflection.Assembly.GetExecutingAssembly().GetType("S004StandardSetupExportSourceBinding");
    var entry=binding?.GetMethod("Main",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
    if(entry==null)throw new InvalidDataException("Exact source layout replay adapter required");
    string graph=PathOf("physical_config_graph"),matrix=Path.Combine(root,"Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv");
    string original=Path.Combine(root,"T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/original-compiler-result.json");
    string replay=Path.Combine(root,"T Logs/SkirmishProduction/s004-current-layout-replay-20261104-T1/replayed-layout.json");
    if((int)entry.Invoke(null,new object[]{new[]{graph,matrix,"104733",original,replay,"1130726651"}})!=0)
     throw new InvalidDataException("S004 source layout replay bootstrap failed");
   }
   string[] forwarded={PathOf("compiled_setup"),HashOf("compiled_setup"),PathOf("prefab_shared"),PathOf("authored_grid"),Path.Combine(output,"requests.json"),PathOf("static_blockers"),PathOf("prefab_s004"),PathOf("building_definitions"),HashOf("building_definitions"),PathOf("configured_building_readmodel"),PathOf("base_surface_roads"),PathOf("physical_config_graph"),PathOf("map_rotation"),PathOf("authored_loading_audit"),bundlePath,args[2],preflight};
   timing.Restart(); int result=S004BundleDrivenStartup.Run(forwarded); timing.Stop(); Console.WriteLine("[S004Timing] world_diagnostic_seconds="+timing.Elapsed.TotalSeconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture)); return result;
  } catch(Exception e) {Console.Error.WriteLine(e);return 1;}
 }
}

