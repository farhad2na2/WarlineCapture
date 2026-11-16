using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Isolated source fixture: an actual compiled definition/setup is passed by
// the authoring owner. No normal-input launch or physical scene is asserted.
internal static class S074SourceSessionConstructionControl
{
    internal static void Run(SkirmishResolvedSetup setup, SkirmishScenarioDefinitionConfig definition, string directory, string sourceRoot)
    {
        if (setup == null || setup.CatalogId != "S074" || setup.Seed != 104803 || setup.SetupHash != 977354998u ||
            definition == null || definition.CampaignMap == null)
            throw new InvalidOperationException("Exact actual S074 compiled setup required");
        int next = 200000;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var reference = SourceOperationMapRotationReference.LoadPinned(
            Path.Combine(sourceRoot, "Logs/SkirmishProduction/s073-operation-map-rotation-capture-20261101-r4/rotation-reference.json"),
            "56de6520a47d17ddedfa69aeaa0dcc9fefc7b29cfb04005ec79149bbeae24beb", sourceRoot);
        using var publisher = new Game.Composition.OperationMapRuntimeBootstrapSceneSystemHelper(em, reference);
        const OperationMapReadinessFlags required = OperationMapReadinessFlags.SourceContent | OperationMapReadinessFlags.SubScene |
            OperationMapReadinessFlags.Metadata | OperationMapReadinessFlags.MapSurface | OperationMapReadinessFlags.AuthoredConversion |
            OperationMapReadinessFlags.PresentationManifest | OperationMapReadinessFlags.RequiredPresentationPreload;
        if (!publisher.TryPublish(definition.CampaignMap, new FixedString64Bytes("S074"), new FixedString64Bytes("skirmish.s074"), 1,
                OperationMapReadinessFlags.Metadata, required, out var map, out var error))
            throw new InvalidOperationException("Original metadata publication failed: " + error);
        var metadata = em.GetComponentData<OperationMapMetadataComponent>(map);
        if (metadata.PhysicalSourceValidated != 0 || metadata.Blob.Value.SourceOperationMapId.ToString() != "opmap.skirmish.desert_base_01")
            throw new InvalidOperationException("Actual logical source binding changed");
        var session = GeneratedS002SessionConstruction.Create(em, setup, "s074-source-fixture", definition);
        var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
        var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
        if (state.CatalogId.ToString() != "S074" || state.SetupHash != setup.SetupHash ||
            state.Phase != SkirmishSessionPhase.Spawning || state.InitializationComplete != 1 ||
            stock.Materials != setup.MaterialsEach || stock.Oil != setup.OilEach || stock.Fuel != setup.UsableFuelEach ||
            !em.HasComponent<SkirmishEnemyPlannerV1Component>(session))
            throw new InvalidOperationException("Original session owner output differs from compiled setup");
        var planner = em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);
        if (setup.EnemyPolicy == null || planner.Initialized != 1 || planner.PolicyHash != setup.EnemyPolicy.Hash)
            throw new InvalidOperationException("Original enemy policy initialization differs");
        string requestPath = Path.Combine(sourceRoot, "T Logs/SkirmishProduction/s074-source-metadata-owner-control-20261107-r1/metadata-request.json");
        var request = JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(requestPath,
            "aa206a4657d7f40505d36277c39bd529a43140ef9f52a660a38bb74f7f9e1bda", 1024 * 1024)));
        var compatibility = SourceMetadataCompatibility.Load(request, setup, sourceRoot, sourceRoot);
        var descriptor = request["building_definitions"];
        var definitions = GeneratedBuildingDefinitions.Load((string)descriptor["path"], (string)descriptor["sha256"],
            setup.SetupHash, new Dictionary<string, Entity>(), compatibility);
        var gm = definition.CampaignMap.GridMetadata;
        var gridEntity = em.CreateEntity();
        em.AddComponentData(gridEntity, new GridConfig { Width = gm.Dimensions.x, Height = gm.Dimensions.y,
            CellSize = gm.CellSize, Origin = new float3(gm.Origin.x, gm.Origin.y, gm.Origin.z) });
        var boundary = em.CreateEntity();
        em.AddComponentData(boundary, new BuildingRuntimeStateTag());
        var configured = em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
        foreach (var d in definitions.Definitions)
            configured.Add(new BuildingConfiguredSpawnableReadModel { BuildingId = new FixedString128Bytes(d.Prefab.name),
                FootprintCells = new int2(d.FootprintCells.x, d.FootprintCells.y) });
        em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        bool ready = SkirmishStartingBuildingsService.Step(em, session, setup, out var reason);
        if (ready || reason != SkirmishReasonCode.None || !em.HasBuffer<SkirmishStartingBuildingRequest>(session))
            throw new InvalidOperationException("Original starting-building owner did not queue grants: " + reason);
        var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        var tracked = em.GetBuffer<SkirmishStartingBuildingRequest>(session);
        if (requests.Length != setup.Structures.Length || tracked.Length != setup.Structures.Length || requests.Length != 20)
            throw new InvalidOperationException("Original starting-building request count differs");
        var pending = new JArray();
        for (int i = 0; i < requests.Length; i++)
        {
            var item = requests[i];
            if (item.Status != BuildingRuntimeSpawnRequest.Pending || item.EntryIndex != i || item.PlanEntity != session ||
                item.FactionId != setup.Structures[i].FactionId || item.AuthoredStartingGrant != 1)
                throw new InvalidOperationException("Actual pending grant differs at " + i);
            if (!SkirmishStartingGrantPlacementConstraint.TryResolvePad(definition.MapLayout, setup, i, out var pad))
                throw new InvalidOperationException("Actual grant pad missing at " + i);
            var sourceDefinition = definitions.Definitions.Single(d => d.Prefab.name == item.BuildingId.ToString());
            var sourceGrid = em.GetComponentData<GridConfig>(gridEntity);
            bool Contains(int x, int z) => SkirmishStartingGrantPlacementConstraint.ContainsFootprint(definition.MapLayout,
                pad, sourceGrid, definition.CampaignMap.Bounds, new RectInt(x, z, sourceDefinition.FootprintCells.x, sourceDefinition.FootprintCells.y));
            bool nearby = false;
            for (int z = -2; z <= 2; z++) for (int x = -2; x <= 2; x++)
                nearby |= Contains(item.PreferredOrigin.x + x, item.PreferredOrigin.y + z);
            pending.Add(new JObject { ["index"] = item.EntryIndex, ["request_id"] = item.RequestId,
                ["building"] = item.BuildingId.ToString(), ["faction"] = item.FactionId,
                ["preferred_x"] = item.PreferredOrigin.x, ["preferred_z"] = item.PreferredOrigin.y,
                ["status"] = "Pending", ["pad_width"] = pad.WidthMetres, ["pad_depth"] = pad.DepthMetres,
                ["footprint_width"] = sourceDefinition.FootprintCells.x, ["footprint_depth"] = sourceDefinition.FootprintCells.y,
                ["preferred_contained"] = Contains(item.PreferredOrigin.x, item.PreferredOrigin.y),
                ["contained_within_two_cells"] = nearby });
        }
        var result = new JObject {
            ["kind"] = "S074SourceSessionConstructionControl", ["catalog_id"] = setup.CatalogId,
            ["setup_hash"] = setup.SetupHash, ["session_phase"] = state.Phase.ToString(),
            ["initialization_complete"] = state.InitializationComplete, ["materials"] = stock.Materials,
            ["oil"] = stock.Oil, ["fuel"] = stock.Fuel, ["enemy_planner_initialized"] = planner.Initialized,
            ["physical_source_validated"] = metadata.PhysicalSourceValidated, ["structure_grants"] = setup.Structures.Length,
            ["pending_original_building_requests"] = pending, ["configured_definition_count"] = definitions.Definitions.Count,
            ["fixture_manager_used"] = true, ["gameplay_world_initialized"] = false, ["normal_launch"] = false,
            ["placement_performed"] = false, ["mission_ready"] = false
        };
        File.WriteAllText(Path.Combine(directory, "s074-session-owner.json"), result.ToString() + Environment.NewLine);
        Console.WriteLine("[S074SourceSessionConstruction] result=Passed fixture=1 pending=20 placement=0 gameplay=0 readiness=0");
    }
}
