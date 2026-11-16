using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class S004StartingGrantSubmissionControl
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
            var grid = em.GetComponentData<GridConfig>(gridEntity); var unusedMasks = new bool[count];
            // Constructor-only mask placeholders: this diagnostic invokes the publisher and request owner only, never placement or mask reads.
            var publisher = new GeneratedRuntimeSpawnProcessor(new GeneratedRoadMask(grid, unusedMasks, unusedMasks, unusedMasks), new GeneratedBuildingMask(grid, unusedMasks));
            publisher.PublishConfiguredSpawnablesReadModel(definitionSource, em, boundary);
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
            var rows = new JArray();
            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i]; var structure = setup.Structures[i];
                Need(request.RequestId > 0 && request.EntryIndex == i && request.PlanEntity == session && request.FactionId == structure.FactionId && request.HasOwnerFaction == 1 && request.AuthoredStartingGrant == 1 && request.Status == BuildingRuntimeSpawnRequest.Pending && tracked[i].RequestId == request.RequestId, "Original request identity or status changed");
                rows.Add(new JObject { ["request_id"] = request.RequestId, ["structure_index"] = i, ["building"] = request.BuildingId.ToString(), ["faction"] = request.FactionId, ["preferred_x"] = request.PreferredOrigin.x, ["preferred_y"] = request.PreferredOrigin.y, ["typed_pad_required"] = request.RequireTypedPad, ["status"] = "Pending" });
            }
            var result = new JObject { ["status"] = "OriginalStartingGrantRequestsPending", ["request_count"] = requests.Length, ["requests"] = rows, ["configured_rows_compared"] = actual.Length,
                ["static_blockers_applied"] = blockerRows.Count, ["owner_blocked_cells"] = blockedCells, ["placement_attempted"] = false, ["mask_placeholders_read"] = false,
                ["buildings_registered"] = 0, ["mission_world_created"] = false, ["mission_ready"] = false };
            File.WriteAllText(args[4], result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Console.WriteLine("[S004StartingGrantSubmissionControl] result=Passed requests=20 configured=9 blockers=348 placement=0 mission=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
