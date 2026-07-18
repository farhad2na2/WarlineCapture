using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class UiBuildDrawerDualCostReadModelTests
{
    private readonly List<Object> _createdObjects = new();

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(test => test.WriteReadModel_BuildingDisplaysGroupedMaterialsAndEmptyFuel());
            RunCase(test => test.WriteReadModel_UnitDisplaysGroupedMaterialsAndEmptyFuel());
            RunCase(test => test.WriteReadModel_AvailableBuildingEnablesCatalogAndSelectedDetail());
            RunCase(test => test.WriteReadModel_InsufficientCreditsDisablesCatalogAndSelectedDetail());
            RunCase(test => test.WriteReadModel_InsufficientMaterialsDisablesCatalogAndSelectedDetail());
            RunCase(test => test.WriteReadModel_InsufficientCreditsAndMaterialsDisablesCatalogAndSelectedDetail());
            Debug.Log("[UiBuildDrawerDualCostReadModelValidation] result=Passed tests=6");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[UiBuildDrawerDualCostReadModelValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<UiBuildDrawerDualCostReadModelTests> testCase)
    {
        var tests = new UiBuildDrawerDualCostReadModelTests();
        try
        {
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        UiBuildDrawerReadModelSource.Clear();
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
        {
            if (_createdObjects[i] != null)
                Object.DestroyImmediate(_createdObjects[i]);
        }

        _createdObjects.Clear();
    }

    [Test]
    public void WriteReadModel_BuildingDisplaysGroupedMaterialsAndEmptyFuel()
    {
        BuildingPlacementSystemConfig buildings = CreateAsset<BuildingPlacementSystemConfig>();
        GameObject building = CreateBuilding("Field Fabricator", 1234, 5678);
        buildings.Spawnables.Add(building);

        var query = ConfigureQuery();
        var items = new List<BuildDrawerCatalogItem>();
        query.Collect(null, buildings, BuildDrawerCategory.Buildings, items);

        Assert.AreEqual(1, items.Count);
        Assert.AreEqual(5678, items[0].MaterialsCost);
        Assert.Zero(items[0].FuelCost);

        UiBuildDrawerReadModelSource.Configure(
            null,
            buildings,
            new AvailableBuildingUiCommand(),
            null,
            Game.Composition.UiCatalogAuthoringMetadataUiSystemHelper.TryGetBuildingMetadata,
            Game.Composition.UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata);

        using World world = new(nameof(WriteReadModel_BuildingDisplaysGroupedMaterialsAndEmptyFuel));
        EntityManager entityManager = world.EntityManager;
        Entity boundary = CreateBoundary(entityManager);
        UiBuildDrawerStateComponent state = new() { ActiveCategory = BuildDrawerCategory.Buildings };

        UiBuildDrawerReadModelSource.WriteReadModel(
            entityManager,
            boundary,
            ref state,
            entityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary),
            entityManager.GetBuffer<UiBuildDrawerQueueRowComponent>(boundary));

        DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog =
            entityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
        UiBuildDrawerDetailComponent detail =
            entityManager.GetComponentData<UiBuildDrawerDetailComponent>(boundary);
        Assert.AreEqual(1, catalog.Length);
        Assert.AreEqual("5,678", catalog[0].MaterialsText.ToString());
        Assert.IsEmpty(catalog[0].FuelText.ToString());
        Assert.AreEqual("5,678", detail.MaterialsCostText.ToString());
        Assert.IsEmpty(detail.FuelCostText.ToString());
    }

    [Test]
    public void WriteReadModel_UnitDisplaysGroupedMaterialsAndEmptyFuel()
    {
        UnitPrefabRegistryAuthoringConfig units = CreateAsset<UnitPrefabRegistryAuthoringConfig>();
        GameObject unit = CreateUnit("Field Contractor", 5678);
        units.UnitSpawnPrefabs.Add(unit);

        var query = ConfigureQuery();
        var items = new List<BuildDrawerCatalogItem>();
        query.Collect(units, null, BuildDrawerCategory.Soldiers, items);

        Assert.AreEqual(1, items.Count);
        Assert.AreEqual(12, items[0].MaterialsCost);
        Assert.Zero(items[0].FuelCost);

        UiBuildDrawerReadModelSource.Configure(
            units,
            null,
            new AvailableBuildingUiCommand(),
            null,
            Game.Composition.UiCatalogAuthoringMetadataUiSystemHelper.TryGetBuildingMetadata,
            Game.Composition.UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata);

        using World world = new(nameof(WriteReadModel_UnitDisplaysGroupedMaterialsAndEmptyFuel));
        EntityManager entityManager = world.EntityManager;
        Entity boundary = CreateBoundary(entityManager);
        UiBuildDrawerStateComponent state = new() { ActiveCategory = BuildDrawerCategory.Soldiers };

        UiBuildDrawerReadModelSource.WriteReadModel(
            entityManager,
            boundary,
            ref state,
            entityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary),
            entityManager.GetBuffer<UiBuildDrawerQueueRowComponent>(boundary));

        DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog =
            entityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
        UiBuildDrawerDetailComponent detail =
            entityManager.GetComponentData<UiBuildDrawerDetailComponent>(boundary);
        Assert.AreEqual(1, catalog.Length);
        Assert.AreEqual("12", catalog[0].MaterialsText.ToString());
        Assert.IsEmpty(catalog[0].FuelText.ToString());
        Assert.AreEqual("12", detail.MaterialsCostText.ToString());
        Assert.IsEmpty(detail.FuelCostText.ToString());
    }

    [Test]
    public void WriteReadModel_AvailableBuildingEnablesCatalogAndSelectedDetail()
    {
        AssertBuildingAvailability(
            BuildingUiCommandFailure.None,
            expectedEnabled: true);
    }

    [Test]
    public void WriteReadModel_InsufficientCreditsDisablesCatalogAndSelectedDetail()
    {
        AssertBuildingAvailability(
            BuildingUiCommandFailure.InsufficientCredits,
            expectedEnabled: false);
    }

    [Test]
    public void WriteReadModel_InsufficientMaterialsDisablesCatalogAndSelectedDetail()
    {
        AssertBuildingAvailability(
            BuildingUiCommandFailure.InsufficientMaterials,
            expectedEnabled: false);
    }

    [Test]
    public void WriteReadModel_InsufficientCreditsAndMaterialsDisablesCatalogAndSelectedDetail()
    {
        AssertBuildingAvailability(
            BuildingUiCommandFailure.InsufficientCreditsAndMaterials,
            expectedEnabled: false);
    }

    private void AssertBuildingAvailability(
        BuildingUiCommandFailure failure,
        bool expectedEnabled)
    {
        BuildingPlacementSystemConfig buildings = CreateAsset<BuildingPlacementSystemConfig>();
        GameObject building = CreateBuilding("Field Fabricator", 1234, 5678);
        buildings.Spawnables.Add(building);

        UiBuildDrawerReadModelSource.Configure(
            null,
            buildings,
            new ConfigurableBuildingUiCommand(failure),
            null,
            Game.Composition.UiCatalogAuthoringMetadataUiSystemHelper.TryGetBuildingMetadata,
            Game.Composition.UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata);

        using World world = new($"{nameof(AssertBuildingAvailability)}_{failure}");
        EntityManager entityManager = world.EntityManager;
        Entity boundary = CreateBoundary(entityManager);
        UiBuildDrawerStateComponent state = new()
        {
            ActiveCategory = BuildDrawerCategory.Buildings,
            SelectedCatalogSlot = 0
        };

        UiBuildDrawerReadModelSource.WriteReadModel(
            entityManager,
            boundary,
            ref state,
            entityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary),
            entityManager.GetBuffer<UiBuildDrawerQueueRowComponent>(boundary));

        DynamicBuffer<UiBuildDrawerCatalogItemComponent> catalog =
            entityManager.GetBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
        UiBuildDrawerDetailComponent detail =
            entityManager.GetComponentData<UiBuildDrawerDetailComponent>(boundary);

        Assert.AreEqual(1, catalog.Length);
        Assert.AreEqual(expectedEnabled ? (byte)1 : (byte)0, catalog[0].Enabled);
        Assert.AreEqual(failure, catalog[0].DisabledReason);
        Assert.AreEqual(expectedEnabled ? (byte)1 : (byte)0, detail.BuildEnabled);
        Assert.AreEqual(failure, detail.DisabledReason);
    }

    private static BuildDrawerCatalogQueryUiSystemHelper ConfigureQuery()
    {
        var query = new BuildDrawerCatalogQueryUiSystemHelper();
        query.ConfigureMetadataResolvers(
            Game.Composition.UiCatalogAuthoringMetadataUiSystemHelper.TryGetBuildingMetadata,
            Game.Composition.UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata);
        return query;
    }

    private static Entity CreateBoundary(EntityManager entityManager)
    {
        Entity boundary = entityManager.CreateEntity(
            typeof(UiBuildDrawerDetailComponent),
            typeof(UiBuildDrawerActiveProductionComponent));
        entityManager.AddBuffer<UiBuildDrawerCatalogItemComponent>(boundary);
        entityManager.AddBuffer<UiBuildDrawerQueueRowComponent>(boundary);
        return boundary;
    }

    private T CreateAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        _createdObjects.Add(asset);
        return asset;
    }

    private GameObject CreateBuilding(string displayName, int price, int materialsCost)
    {
        GameObject prefab = new(displayName);
        _createdObjects.Add(prefab);
        BuildingDefinitionAuthoring authoring = prefab.AddComponent<BuildingDefinitionAuthoring>();
        SerializedObject serialized = new(authoring);
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = "Fabricates field structures.";
        serialized.FindProperty("canRequest").boolValue = true;
        serialized.FindProperty("price").intValue = price;
        serialized.FindProperty("materialsCost").intValue = materialsCost;
        serialized.FindProperty("footprintCells").vector2IntValue = new Vector2Int(2, 2);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return prefab;
    }

    private GameObject CreateUnit(string displayName, int price)
    {
        GameObject prefab = new(displayName);
        _createdObjects.Add(prefab);
        UnitGridAuthoring authoring = prefab.AddComponent<UnitGridAuthoring>();
        SerializedObject serialized = new(authoring);
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = "Field fabrication unit.";
        serialized.FindProperty("canRequest").boolValue = true;
        serialized.FindProperty("price").intValue = price;
        serialized.FindProperty("footprintCells").vector2IntValue = Vector2Int.one;
        serialized.FindProperty("productionDurationSeconds").floatValue = 42f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return prefab;
    }

    private sealed class AvailableBuildingUiCommand : ConfigurableBuildingUiCommand
    {
        public AvailableBuildingUiCommand()
            : base(BuildingUiCommandFailure.None)
        {
        }
    }

    private class ConfigurableBuildingUiCommand : IBuildingUiCommand
    {
        private readonly BuildingUiCommandFailure _failure;

        public ConfigurableBuildingUiCommand(BuildingUiCommandFailure failure)
        {
            _failure = failure;
        }

        public int CurrentDollars => int.MaxValue;
        public bool HasPendingBuildingPlacement => false;
        public bool CanConfirmBuildingPlacement => false;
        public string PlacementStatusText => string.Empty;
        public int ActivePlacementCost => 0;
        public float ActivePlacementDurationSeconds => 0f;
        public int MaxQueuedUnitProductions => 25;

        public BuildingUiCommandFailure GetCampRequestFailure(
            GameObject prefab,
            int price,
            out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            return _failure;
        }

        public BuildingUiCommandFailure TryRequestCampItem(
            GameObject prefab,
            int price,
            out string requiredBuildingDisplayName,
            bool focusProducerOnSuccess)
        {
            requiredBuildingDisplayName = string.Empty;
            return _failure;
        }

        public bool CancelProduction(int buildingId, int pendingProductionIndex) => false;
        public bool ConfirmBuildingPlacement() => false;
        public void CancelBuildingPlacement() { }
        public bool RotateBuildingPlacement() => false;
    }
}
