using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using Debug = UnityEngine.Debug;

public sealed class OperationMapCurrentAircraftRuntimeAcceptanceTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var aircraftTests = new OperationMapCurrentAircraftRuntimeAcceptanceTests();
            aircraftTests.CurrentMap_RunwayInitializesFixedWingAndTeardownRestoresCompatibility();
            aircraftTests.CurrentMap_HelipadsFeedSpawnConsumerAndTeardownRestoresCompatibility();
            new CurrentOperationMapScenarioSetupTests()
                .StandardSkirmish_UsesCurrentPhysicalMapAndTypedDeploymentAnchors();
            Debug.Log("[OperationMapCurrentAircraftRuntimeAcceptanceValidation] result=Passed tests=3");
        }
        catch (Exception exception)
        {
            Debug.LogError("[OperationMapCurrentAircraftRuntimeAcceptanceValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void CurrentMap_RunwayInitializesFixedWingAndTeardownRestoresCompatibility()
    {
        OperationMapDefinition definition = LoadDefinition();
        using World world = new("OperationMapCurrentAircraftRuntimeAcceptance.Runway");
        using OperationMapRuntimeBootstrapSceneSystemHelper bootstrap = new(world);
        PublishCurrentMap(bootstrap, definition);

        EntityManager entityManager = world.EntityManager;
        GridConfig grid = CreateGrid(definition);
        Entity boundary = entityManager.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingFactionRunwayReadModel> runways =
            entityManager.AddBuffer<BuildingFactionRunwayReadModel>(boundary);

        Assert.That(OperationMapRunwayReadModelUtility.TryAppendRunways(
            entityManager,
            runways,
            in grid,
            out FixedList512Bytes<byte> mapFactions,
            out bool hasActiveMap,
            out string error), Is.True, error);
        Assert.That(hasActiveMap, Is.True);
        OperationMapRunwayReadModelUtility.RemoveBuildingFallbacks(runways, runways.Length, in mapFactions);
        Assert.That(runways.Length, Is.EqualTo(1));

        BuildingFactionRunwayReadModel runway = runways[0];
        Entity aircraft = entityManager.CreateEntity(
            typeof(UnitAirMovement),
            typeof(UnitAirComponent),
            typeof(UnitMove),
            typeof(UnitGrid),
            typeof(Faction),
            typeof(UnitSourcePrefabKey),
            typeof(LocalTransform));
        entityManager.SetComponentData(aircraft, new UnitAirMovement { CruiseHeight = 20f, RunwayTaxiSpeed = 5f });
        entityManager.SetComponentData(aircraft, new UnitMove { Speed = 18f, WalkSpeed = 18f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        entityManager.SetComponentData(aircraft, new UnitGrid { Cell = runway.TakeoffCell });
        entityManager.SetComponentData(aircraft, new Faction { Id = runway.FactionId });
        entityManager.SetComponentData(aircraft, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Air_Strike_Jet") });
        entityManager.SetComponentData(aircraft, LocalTransform.FromPosition(runway.TakeoffPosition));

        SystemHandle system = world.CreateSystem<FixedWingRunwayHomeInitializationSystem>();
        system.Update(world.Unmanaged);

        UnitAirComponent air = entityManager.GetComponentData<UnitAirComponent>(aircraft);
        Assert.That(air.HomeInitialized, Is.EqualTo(1));
        Assert.That(air.UsesRunway, Is.EqualTo(1));
        Assert.That(air.RunwayTakeoffCell, Is.EqualTo(runway.TakeoffCell));
        Assert.That(air.RunwayLandingCell, Is.EqualTo(runway.LandingCell));
        Assert.That(math.distancesq(air.RunwayTakeoffPosition, runway.TakeoffPosition), Is.LessThan(0.0001f));
        Assert.That(math.distancesq(air.RunwayLandingPosition, runway.LandingPosition), Is.LessThan(0.0001f));

        bootstrap.ClearPublishedState();
        runways = entityManager.GetBuffer<BuildingFactionRunwayReadModel>(boundary);
        runways.Clear();
        runways.Add(new BuildingFactionRunwayReadModel { FactionId = 2, BuildingRuntimeId = 901 });
        Assert.That(OperationMapRunwayReadModelUtility.TryAppendRunways(
            entityManager,
            runways,
            in grid,
            out _,
            out hasActiveMap,
            out error), Is.True, error);
        Assert.That(hasActiveMap, Is.False);
        Assert.That(runways.Length, Is.EqualTo(1));
        Assert.That(runways[0].BuildingRuntimeId, Is.EqualTo(901));
        Assert.That(OperationMapRunwayReadModelUtility.ResolveGenerationSignature(entityManager), Is.Zero);
    }

    [Test]
    public void CurrentMap_HelipadsFeedSpawnConsumerAndTeardownRestoresCompatibility()
    {
        OperationMapDefinition definition = LoadDefinition();
        using World world = new("OperationMapCurrentAircraftRuntimeAcceptance.Helipad");
        using OperationMapRuntimeBootstrapSceneSystemHelper bootstrap = new(world);
        PublishCurrentMap(bootstrap, definition);

        EntityManager entityManager = world.EntityManager;
        GridConfig grid = CreateGrid(definition);
        Entity boundary = entityManager.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            entityManager.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
        List<OperationMapAnchorConfig> helipads = GetHelipads(definition);
        Assert.That(helipads.Count, Is.EqualTo(3));

        for (int index = 0; index < helipads.Count; index++)
        {
            float3 center = helipads[index].Position;
            spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
            {
                FactionId = (byte)helipads[index].FactionId,
                BuildingId = new FixedString128Bytes("building_helipad"),
                BuildingRuntimeId = 100 + index,
                SlotIndex = 0,
                Cell = GridUtils.WorldToCell(in grid, center),
                WorldPosition = center
            });
        }

        spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
        {
            FactionId = (byte)helipads[0].FactionId,
            BuildingId = new FixedString128Bytes("building_helipad"),
            BuildingRuntimeId = 999,
            SlotIndex = 0,
            Cell = new int2(1, 1),
            WorldPosition = GridUtils.CellToWorldCenter(in grid, new int2(1, 1))
        });

        Assert.That(OperationMapHelipadReadModelUtility.TryBind(
            entityManager,
            spawnPoints,
            in grid,
            out bool hasActiveMap,
            out string error), Is.True, error);
        Assert.That(hasActiveMap, Is.True);
        Assert.That(spawnPoints.Length, Is.EqualTo(helipads.Count));

        bool TryGetBoundary(EntityManager _, out Entity result)
        {
            result = boundary;
            return true;
        }

        BuildingSpawnCompositionSystemHelper.Context context = new(
            null,
            default,
            null,
            default,
            default,
            null,
            null,
            null,
            TryGetBoundary);
        BuildingSpawnCompositionSystemHelper spawnSystem = new();
        for (int index = 0; index < helipads.Count; index++)
        {
            Assert.That(spawnSystem.TryGetFactionProductionSpawnPoint(
                context,
                (byte)helipads[index].FactionId,
                "Building_Helipad",
                index,
                entityManager,
                grid,
                out int2 cell,
                out float3 position), Is.True);
            Assert.That(cell, Is.EqualTo(GridUtils.WorldToCell(in grid, (float3)helipads[index].Position)));
            Assert.That(math.distancesq(position, (float3)helipads[index].Position), Is.LessThan(0.0001f));
        }

        bootstrap.ClearPublishedState();
        spawnPoints = entityManager.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
        spawnPoints.Clear();
        spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
        {
            FactionId = 2,
            BuildingId = new FixedString128Bytes("building_helipad"),
            BuildingRuntimeId = 902,
            SlotIndex = 0,
            Cell = new int2(2, 2),
            WorldPosition = GridUtils.CellToWorldCenter(in grid, new int2(2, 2))
        });
        Assert.That(OperationMapHelipadReadModelUtility.TryBind(
            entityManager,
            spawnPoints,
            in grid,
            out hasActiveMap,
            out error), Is.True, error);
        Assert.That(hasActiveMap, Is.False);
        Assert.That(spawnPoints.Length, Is.EqualTo(1));
        Assert.That(spawnPoints[0].BuildingRuntimeId, Is.EqualTo(902));
    }

    private static OperationMapDefinition LoadDefinition()
    {
        OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            OperationMapCurrentCompatibilityDefinitionBuilder.DefinitionPath);
        Assert.That(definition, Is.Not.Null);
        Assert.That(definition.TryValidateMetadata(out string error), Is.True, error);
        return definition;
    }

    private static void PublishCurrentMap(
        OperationMapRuntimeBootstrapSceneSystemHelper bootstrap,
        OperationMapDefinition definition)
    {
        FixedString64Bytes scenarioId = new("scenario.skirmish.desert_base_standard");
        FixedString64Bytes missionId = new("skirmish");
        Assert.That(bootstrap.TryPublish(
            definition,
            in scenarioId,
            in missionId,
            1,
            OperationMapReadinessFlags.Metadata,
            OperationMapReadinessFlags.Metadata,
            out _,
            out string error), Is.True, error);
    }

    private static GridConfig CreateGrid(OperationMapDefinition definition)
    {
        OperationMapGridMetadataConfig metadata = definition.GridMetadata;
        return new GridConfig
        {
            Width = metadata.Dimensions.x,
            Height = metadata.Dimensions.y,
            CellSize = metadata.CellSize,
            Origin = metadata.Origin
        };
    }

    private static List<OperationMapAnchorConfig> GetHelipads(OperationMapDefinition definition)
    {
        List<OperationMapAnchorConfig> result = new(3);
        foreach (OperationMapAnchorConfig anchor in definition.Anchors)
        {
            if (anchor.Kind == OperationMapAnchorKind.Helipad)
                result.Add(anchor);
        }

        result.Sort((left, right) => left.LaneIndex.CompareTo(right.LaneIndex));
        return result;
    }
}
