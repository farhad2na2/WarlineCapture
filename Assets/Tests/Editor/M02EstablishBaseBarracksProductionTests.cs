using System;
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
    private const string Marker =
        "[M02EstablishBaseBarracksProductionValidation] result=Passed tests=4";

    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseBarracksProductionTests tests = new();
            tests.BarracksConfigContainsOnlyTheApprovedRifle();
            tests.BarracksAuthoringConsumesTheCanonicalProductionEntry();
            tests.ExistingProductionMetadataResolvesTheApprovedRifle();
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
