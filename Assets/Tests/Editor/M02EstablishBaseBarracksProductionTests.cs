using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseBarracksProductionTests
{
    private const string TentConfigPath =
        "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Tent_Regular_Config.asset";
    private const string RoadBarrierConfigPath =
        "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Road_Barrier_Config.asset";
    private const string BarracksPrefabPath =
        "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab";
    private const string HelicopterTransportPrefabPath =
        "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab";
    private const string Marker =
        "[M02EstablishBaseBarracksProductionValidation] result=Passed tests=6";

    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseBarracksProductionTests tests = new();
            tests.BarracksConfigContainsOnlyTheApprovedRifle();
            tests.BarracksAuthoringConsumesTheCanonicalProductionEntry();
            tests.ExistingProductionMetadataResolvesTheApprovedRifle();
            tests.OneBarracksOrderQueuesTheCanonicalFourSoldierSquad();
            new BuildingProductionQueueCompositionSystemHelperTests()
                .OperationMapProducerQueueConsumer_CompletesConfiguredQuantityBeforeNextRequest();
            tests.UnrelatedProducerCatalogsRemainUnchanged();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseBarracksProductionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void BarracksConfigContainsOnlyTheApprovedRifle()
    {
        BuildingDefinitionAuthoringConfig config = Load<BuildingDefinitionAuthoringConfig>(
            M02EstablishBaseConfigBuilder.BarracksConfigPath);
        GameObject expected = Load<GameObject>(M02EstablishBaseConfigBuilder.RequiredRiflePrefabPath);

        Assert.That(config.Productions, Has.Count.EqualTo(1));
        Assert.That(config.Productions[0], Is.Not.Null);
        Assert.That(config.Productions[0].SpawnUnitPrefab, Is.SameAs(expected));
        Assert.That(config.Productions[0].Quantity, Is.EqualTo(4));
        Assert.That(config.Role, Is.EqualTo(BuildingRole.MilitaryCamp));
    }

    [Test]
    public void BarracksAuthoringConsumesTheCanonicalProductionEntry()
    {
        GameObject barracksPrefab = Load<GameObject>(BarracksPrefabPath);
        BuildingDefinitionAuthoring authoring = barracksPrefab.GetComponent<BuildingDefinitionAuthoring>();
        GameObject expected = Load<GameObject>(M02EstablishBaseConfigBuilder.RequiredRiflePrefabPath);

        Assert.That(authoring, Is.Not.Null);
        Assert.That(authoring.ConfiguredProductionCount, Is.EqualTo(1));
        Assert.That(authoring.GetProductionOrDefault(0), Is.Not.Null);
        Assert.That(authoring.GetProductionOrDefault(0).spawnUnitPrefab, Is.SameAs(expected));
        Assert.That(authoring.GetProductionOrDefault(0).quantity, Is.EqualTo(4));
        Assert.That(authoring.GetProductionOrDefault(1), Is.Null);
    }

    [Test]
    public void ExistingProductionMetadataResolvesTheApprovedRifle()
    {
        GameObject riflePrefab = Load<GameObject>(M02EstablishBaseConfigBuilder.RequiredRiflePrefabPath);
        UnitGridAuthoring authoring = riflePrefab.GetComponent<UnitGridAuthoring>();
        BuildingProductionQueueCompositionSystemHelper production = new();
        production.ConfigureUnitProductionMetadataResolver(
            BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata);

        Assert.That(authoring, Is.Not.Null);
        Assert.That(production.ResolveProductionDurationSeconds(riflePrefab),
            Is.EqualTo(authoring.ProductionDurationSeconds).Within(0.0001f));
        Assert.That(authoring.ProductionDurationSeconds, Is.GreaterThan(0f));
    }

    [Test]
    public void OneBarracksOrderQueuesTheCanonicalFourSoldierSquad()
    {
        GameObject barracksPrefab = Load<GameObject>(BarracksPrefabPath);
        GameObject riflePrefab = Load<GameObject>(M02EstablishBaseConfigBuilder.RequiredRiflePrefabPath);
        GameObject helicopterPrefab = Load<GameObject>(HelicopterTransportPrefabPath);
        BuildingDefinitionPrefabSystemHelper definitions = new();
        definitions.ConfigureAuthoringMetadataResolvers(
            BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
        BuildingDefinition definition = definitions.CreateRuntimeBuildingDefinition(
            barracksPrefab, "Barracks", "", Vector2Int.one, 1, new BuildingRunwaySystem());
        RuntimeBuildingEntity building = new() { Definition = definition };
        BuildingProductionQueueCompositionSystemHelper production = new();
        production.ConfigureUnitProductionMetadataResolver(
            BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata);
        Dictionary<string, GameObject> prefabsByKey = new(definitions.UnitSpawnPrefabsByKey)
        {
            ["unit_veh_helicopter_transport"] = helicopterPrefab
        };
        BuildingProductionQueueCompositionSystemHelper.QueueContext context = new(
            new[] { riflePrefab, helicopterPrefab }, prefabsByKey,
            new BuildingProductionSlotUtilitySystemHelper(), null, null);
        using Unity.Entities.World world = new(nameof(OneBarracksOrderQueuesTheCanonicalFourSoldierSquad));

        Assert.That(production.TryQueuePlayerUnitFromBuilding(
            context, building, 0, riflePrefab, world.EntityManager, 10f), Is.True);
        Assert.That(building.PendingProductions, Has.Count.EqualTo(1));
        Assert.That(building.PendingProductions[0].TransportPrefab, Is.SameAs(helicopterPrefab),
            "The regression must exercise the managed helicopter queue used by M2.");
        Assert.That(building.PendingProductions[0].RemainingQuantity, Is.EqualTo(4));
    }

    [Test]
    public void UnrelatedProducerCatalogsRemainUnchanged()
    {
        BuildingDefinitionAuthoringConfig tent = Load<BuildingDefinitionAuthoringConfig>(TentConfigPath);
        BuildingDefinitionAuthoringConfig roadBarrier =
            Load<BuildingDefinitionAuthoringConfig>(RoadBarrierConfigPath);

        Assert.That(tent.Productions, Has.Count.EqualTo(10));
        Assert.That(roadBarrier.Productions, Is.Empty);
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.That(asset, Is.Not.Null, $"Required asset is missing: {path}");
        return asset;
    }
}
