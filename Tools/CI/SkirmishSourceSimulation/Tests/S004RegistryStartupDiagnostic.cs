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

internal static class S004RegistryStartupDiagnostic
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
            var sessionId = new FixedString64Bytes("s004-starting-grant-fixture");
            var session = em.CreateEntity();
            em.AddComponentData(session, new SkirmishExpandedSessionComponent { SessionId = sessionId, Phase = SkirmishSessionPhase.Queued });
            em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });
            var initialState = new SystemState { EntityManager = em, Enabled = true };
            var initializer = new SkirmishSessionInitializationSystem(); initializer.OnUpdate(ref initialState);
            Need(em.GetComponentData<SkirmishExpandedSessionComponent>(session).InitializationComplete == 1 && em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase == SkirmishSessionPhase.Spawning, "Original session initializer rejected source setup");
            const int width = 2048, height = 1024, count = width * height;
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
            var graph = JObject.Parse(File.ReadAllText(args[11]));
            var sourceAssets = new SourceConfigMaterializer(graph);
            var surfaceAsset = sourceAssets.ByPath<MapSurfaceDataAsset>("Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Surface.asset");
            Need(surfaceAsset.TryCreateRuntimeBlobAsset(Allocator.Persistent, out var surfaceBlob), "Source surface decode failed");
            ref var surface = ref surfaceBlob.Value;
            Need(surface.Dimensions.x == width && surface.Dimensions.y == height && surface.CellSize == grid.CellSize, "Source surface frame changed");
            var surfaceEntity = em.CreateEntity();
            em.AddComponentData(surfaceEntity, new MapSurfaceComponent { SurfaceBlob=surfaceBlob, GridOrigin=surface.GridOrigin, CellSize=surface.CellSize, Dimensions=surface.Dimensions, HasSurfaceData=1 });
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
            // The original spawn-ledger owner invokes this after starting buildings complete.
            SkirmishMaterialsService.Initialize(em, session, setup);
            Need(em.HasComponent<SkirmishSharedMaterialsInitialized>(session), "Materials owner did not initialize");
            var playerBank = SkirmishMaterialsService.FindEconomy(em, 1); var enemyBank = SkirmishMaterialsService.FindEconomy(em, 2);
            Need(playerBank != Entity.Null && enemyBank != Entity.Null && playerBank != enemyBank, "Distinct original faction banks required");
            Need(SkirmishMaterialsService.Read(em, session, 1) == setup.MaterialsEach && SkirmishMaterialsService.Read(em, session, 2) == setup.MaterialsEach, "Actual material budgets differ from source setup");
            SkirmishMaterialsService.Initialize(em, session, setup);
            Need(SkirmishMaterialsService.FindEconomy(em, 1) == playerBank && SkirmishMaterialsService.FindEconomy(em, 2) == enemyBank, "Materials initialization changed bank identity on repeat");
            SkirmishArmyGroupSystem.EnsureSession(em, session, setup);
            // Source-host registry assembled from imported capture roots; it is not a native registry capture.
            var prefabRegistry=em.CreateEntity(); var prefabEntries=em.AddBuffer<UnitPrefabRegistryEntry>(prefabRegistry);
            foreach (var entry in templates.OrderBy(pair=>pair.Key,StringComparer.Ordinal)) prefabEntries.Add(new UnitPrefabRegistryEntry { Prefab=entry.Value });
            var actualLookup=S004OriginalPrefabLookup.BuildPrefabEntityLookup(em);
            foreach (var force in setup.Forces) Need(actualLookup.TryGetValue(force.RuntimePrefabKey,out var root) && root==templates[force.RuntimePrefabKey], "Original registry lookup did not resolve actual configured force source: "+force.RuntimePrefabKey);
            var forces=setup.Forces.Select(force => new Game.Simulation.SkirmishBootstrapForce(force.RoleId,force.FactionId,force.RoleKind,force.Quantity,force.SupplyCost,force.RuntimePrefabKey)).ToArray();
            var opening=new Game.Simulation.SkirmishMissionBootstrapKernel(forces);
            var actors=new JArray(); string firstFailure=null;
            while(opening.TryBeginMember(out var member)) {
                var force=setup.Forces[member.ForceIndex];
                var actor=GeneratedTypedOpeningActorSource.CreateForceMember(em,sessionId,force,member.ForceIndex,member.MemberIndex,member.PerMemberSupply,member.SourceKey,actualLookup,setup);
                if(actor==Entity.Null || !em.Exists(actor)) { firstFailure=member.StableId; opening.Abort(); break; }
                uint group=opening.RequiresNewGroup ? SkirmishArmyGroupSystem.OpenGroup(em,session,member.Faction,member.Role,member.Category) : opening.OpenGroupId;
                SkirmishArmyGroupSystem.BindMember(em,session,actor,group);
                Need(em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor) && em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId==group, "Actor group binding failed");
                var cell=em.GetComponentData<UnitGrid>(actor).Cell;
                opening.RecordCreatedAndBound(group);
                actors.Add(new JObject { ["actor_id"]=member.StableId,["faction"]=member.Faction,["prefab"]=member.SourceKey,["cell_x"]=cell.x,["cell_y"]=cell.y,["group"]=group });
            }
            Need(opening.Completed && opening.CompletedMembers==setup.Forces.Sum(force=>Math.Max(1,force.Quantity)), "Original actor opening did not complete");
            S004OriginalLedgerTail.Apply(em,session,setup,opening,out var visualPending);
            float[] oilBefore = new float[2], fuelBefore = new float[2];
            using (var stores=em.CreateEntityQuery(typeof(BuildingResourceStorageComponent),typeof(SkirmishAttemptOwnedComponent)))
            using (var entities=stores.ToEntityArray(Allocator.Temp)) foreach (var store in entities) {
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(store); var data=em.GetComponentData<BuildingResourceStorageComponent>(store);
                if (owned.SessionId==sessionId && data.OwnerFactionId>=1 && data.OwnerFactionId<=2) {oilBefore[data.OwnerFactionId-1]+=data.StoredOilBarrels; fuelBefore[data.OwnerFactionId-1]+=data.StoredFuelBarrels;}
            }
            bool supplyReady=SkirmishStartingSupplyService.Initialize(em,session,setup);
            float[] oilAfter = new float[2], fuelAfter = new float[2]; int[] storeCount = new int[2];
            using (var stores=em.CreateEntityQuery(typeof(BuildingResourceStorageComponent),typeof(SkirmishAttemptOwnedComponent)))
            using (var entities=stores.ToEntityArray(Allocator.Temp)) foreach (var store in entities) {
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(store); var data=em.GetComponentData<BuildingResourceStorageComponent>(store);
                if (owned.SessionId==sessionId && data.OwnerFactionId>=1 && data.OwnerFactionId<=2) {int f=data.OwnerFactionId-1; oilAfter[f]+=data.StoredOilBarrels; fuelAfter[f]+=data.StoredFuelBarrels; storeCount[f]++;}
            }
            Need(supplyReady==em.HasComponent<SkirmishSharedSupplyInitialized>(session), "Original supply result inconsistent with completion marker");
            if (!supplyReady) Need(oilBefore.SequenceEqual(oilAfter) && fuelBefore.SequenceEqual(fuelAfter), "Failed original supply preflight mutated stocks");
            else { Need(oilAfter[0]-oilBefore[0]==setup.OilEach && oilAfter[1]-oilBefore[1]==setup.OilEach && fuelAfter[0]-fuelBefore[0]==setup.UsableFuelEach && fuelAfter[1]-fuelBefore[1]==setup.UsableFuelEach, "Original supply grant differs from authored budget"); SkirmishFogService.Project(em,session); }
            var playerCapacity=em.GetComponentData<SkirmishCapacityComponent>(session);
            var enemyCapacity=em.GetComponentData<SkirmishEnemyCapacityComponent>(session);
            var result=new JObject { ["status"]="DiagnosticOriginalPlacementOwnerObserved", ["request_count"]=requests.Length,
                ["succeeded"]=succeeded,["failed"]=failed,["pending"]=pending,["registered"]=registry.Count,["requests"]=rows,
                ["source_registry_root_count"]=prefabEntries.Length,["original_registry_lookup_count"]=actualLookup.Count,["native_registry_captured"]=false,["supply_owner_result"]=supplyReady,["supply_owned_stores"]=new JArray(storeCount),["oil_before"]=new JArray(oilBefore),["oil_after"]=new JArray(oilAfter),["fuel_before"]=new JArray(fuelBefore),["fuel_after"]=new JArray(fuelAfter),["fog_owner_called"]=supplyReady,["ledger_tail_executed"]=true,["visual_pending"]=(int)visualPending,["player_live_infantry"]=playerCapacity.InfantryLive,["enemy_live_infantry"]=enemyCapacity.InfantryLive,["player_materials"]=SkirmishMaterialsService.Read(em,session,1),["enemy_materials"]=SkirmishMaterialsService.Read(em,session,2),["materials_initialized_by_original_owner"]=true,["original_building_owner_bound"]=buildingsBound,["bound_buildings"]=boundRows.Length,["actors_created_and_grouped"]=opening.CompletedMembers,["actors_expected"]=setup.Forces.Sum(force=>Math.Max(1,force.Quantity)),["first_failed_actor"]=firstFailure,["actors"]=actors,
                ["base_road_mask_source"]=true,["scene_overlays"]="unknown assumed empty",["sidewalk"]="unknown assumed empty",["water"]="unknown assumed empty",
                ["authored_building_mask"]="unknown assumed empty; 348 captured static blockers applied separately",["source_fit_verified"]=false,["mission_ready"]=false };
            File.WriteAllText(args[4],result.ToString(Newtonsoft.Json.Formatting.Indented)+"\n");
            Console.WriteLine("[S004RegistryStartupDiagnostic] result=Observed requests="+requests.Length+" succeeded="+succeeded+" failed="+failed+" pending="+pending+" registered="+registry.Count+" actors="+opening.CompletedMembers+" readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
