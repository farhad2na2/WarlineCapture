using System;
using System.IO;
using System.Reflection;
using Game.Authoring;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class BuildingMaterialsCostConfigProjectionTests
{
    private const string ConfigRoot = "Assets/Game/Configs/Prefabs/";
    private const string PrefabRoot = "Assets/Game/Prefabs/Buildings/";
    private const string MatchConfigPath =
        "Assets/Game/Configs/Scene/MatchSubScene_InitialUnitsSpawner_Config.asset";

    private readonly struct ExpectedBuilding
    {
        public readonly string ConfigPath;
        public readonly string PrefabPath;
        public readonly int Price;
        public readonly int MaterialsCost;

        public ExpectedBuilding(string configName, string prefabName, int price, int materialsCost)
        {
            ConfigPath = ConfigRoot + configName + "_Config.asset";
            PrefabPath = PrefabRoot + prefabName + ".prefab";
            Price = price;
            MaterialsCost = materialsCost;
        }
    }

    private static readonly ExpectedBuilding[] ExpectedBuildings =
    {
        new("Prefab_BuildingDefinition_Airport", "Building_Airport", 120000, 300),
        new("Prefab_BuildingDefinition_Ammunition_Depot", "Building_Ammunition_Depot", 45000, 100),
        new("Prefab_BuildingDefinition_Building_Barrack", "Building_Barrack", 40000, 90),
        new("Prefab_BuildingDefinition_Building_Satelite_Dish", "Building_Satelite_Dish", 20000, 45),
        new("Prefab_BuildingDefinition_Fuel_Bladder", "Building_Fuel_Bladder", 18000, 40),
        new("Prefab_BuildingDefinition_GuardTower_Big", "Building_GuardTower_Big", 30000, 70),
        new("Prefab_BuildingDefinition_GuardTower", "Building_GuardTower", 22000, 50),
        new("Prefab_BuildingDefinition_Hall", "Building_Hall", 50000, 110),
        new("Prefab_BuildingDefinition_Helipad", "Building_Helipad", 35000, 80),
        new("Prefab_BuildingDefinition_House", "Building_House", 9000, 20),
        new("Prefab_BuildingDefinition_OilPump", "Building_OilPump", 50000, 100),
        new("Prefab_BuildingDefinition_OilRefinery_Big", "Building_Refinery_Big", 140000, 260),
        new("Prefab_BuildingDefinition_OilRefinery", "Building_Refinery", 80000, 160),
        new("Prefab_BuildingDefinition_Portaloo_", "Portaloo", 1000, 2),
        new("Prefab_BuildingDefinition_Road_Barrier", "Building_Road_Barrier", 6000, 15),
        new("Prefab_BuildingDefinition_Shop", "Building_Shop", 14000, 30),
        new("Prefab_BuildingDefinition_Tent_Contractor", "Tent_Contractor", 8000, 20),
        new("Prefab_BuildingDefinition_Tent_Expert", "Tent_Expert", 10000, 25),
        new("Prefab_BuildingDefinition_Tent_Refugee", "Tent_Refugee", 6000, 15),
        new("Prefab_BuildingDefinition_Tent_Regular", "Tent_Regular", 12000, 25),
        new("Prefab_BuildingDefinition_Wall_Dirt_Straight", "Wall_Dirt_Straight", 10000, 15),
        new("Prefab_BuildingDefinition_Wall_Fence_Straight", "Wall_Fence_Straight", 7000, 10),
        new("Prefab_BuildingDefinition_WaterTank", "Building_WaterTank", 14000, 30)
    };

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new BuildingMaterialsCostConfigProjectionTests();
            tests.AuthoredBuildingCosts_ProjectExactlyThroughRuntimeCatalogAndClone();
            tests.MatchConfig_AuthorsRequestedStartingMaterialsAndCapacity();
            tests.ZeroMaterialsCost_RemainsValidAcrossConfigMetadataRuntimeAndCatalog();
            tests.FootprintClone_PreservesEveryFieldExceptTheAuthorizedOverride();
            tests.FootprintCloneBoundary_MapsEveryCurrentFieldExactlyOnce();
            Debug.Log("[BuildingMaterialsCostConfigProjectionValidation] result=Passed tests=5");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[BuildingMaterialsCostConfigProjectionValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AuthoredBuildingCosts_ProjectExactlyThroughRuntimeCatalogAndClone()
    {
        GameObject[] prefabs = new GameObject[ExpectedBuildings.Length];
        var definitionSystem = new BuildingDefinitionPrefabSystemHelper();
        definitionSystem.ConfigureAuthoringMetadataResolvers(
            BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);

        for (int i = 0; i < ExpectedBuildings.Length; i++)
        {
            ExpectedBuilding expected = ExpectedBuildings[i];
            BuildingDefinitionAuthoringConfig config =
                AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(expected.ConfigPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expected.PrefabPath);

            Assert.IsNotNull(config, $"Missing config: {expected.ConfigPath}");
            Assert.IsNotNull(prefab, $"Missing prefab: {expected.PrefabPath}");
            Assert.AreEqual(expected.Price, config.Price, $"Credits price changed: {expected.ConfigPath}");
            Assert.AreEqual(expected.MaterialsCost, config.MaterialsCost, expected.ConfigPath);
            Assert.IsTrue(
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata(
                    prefab,
                    out BuildingDefinitionPrefabSystemHelper.BuildingDefinitionMetadata metadata),
                expected.PrefabPath);
            Assert.AreEqual(expected.Price, metadata.Price, $"Metadata Credits price changed: {expected.PrefabPath}");
            Assert.AreEqual(expected.MaterialsCost, metadata.MaterialsCost, expected.PrefabPath);
            BuildingDefinition runtimeDefinition = definitionSystem.CreateRuntimeBuildingDefinition(
                prefab,
                prefab.name,
                string.Empty,
                Vector2Int.one,
                1,
                null);
            Assert.AreEqual(expected.Price, runtimeDefinition.CreditsCost, expected.PrefabPath);
            Assert.AreEqual(expected.MaterialsCost, runtimeDefinition.MaterialsCost, expected.PrefabPath);
            prefabs[i] = prefab;
        }

        definitionSystem.RebuildSpawnablesLookup(prefabs, null);
        definitionSystem.RebuildConfiguredSpawnableDefinitions(null, UnityEngine.Object.DestroyImmediate);
        using var world = new World("BuildingMaterialsCostConfigProjectionTests");
        EntityManager em = world.EntityManager;
        Entity boundary = em.CreateEntity();

        try
        {
            var publisher = new BuildingRuntimeProcessingCompositionSystemHelper();
            publisher.PublishConfiguredSpawnablesReadModel(definitionSystem, em, boundary);
            DynamicBuffer<BuildingConfiguredSpawnableReadModel> readModels =
                em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary, true);

            Assert.AreEqual(ExpectedBuildings.Length, definitionSystem.ConfiguredSpawnableCount);
            Assert.AreEqual(ExpectedBuildings.Length, readModels.Length);
            for (int i = 0; i < ExpectedBuildings.Length; i++)
            {
                ExpectedBuilding expected = ExpectedBuildings[i];
                Assert.IsTrue(definitionSystem.TryGetConfiguredDefinition(i, out BuildingDefinition definition));
                Assert.IsTrue(definitionSystem.TryGetConfiguredSpawnable(i, out var catalogEntry));
                Assert.AreEqual(expected.Price, catalogEntry.Price, $"Catalog Credits price changed: {expected.PrefabPath}");
                Assert.AreEqual(expected.Price, definition.CreditsCost, expected.PrefabPath);
                Assert.AreEqual(expected.MaterialsCost, definition.MaterialsCost, expected.PrefabPath);
                Assert.AreEqual(expected.Price, readModels[i].Price, $"Read-model Credits price changed: {expected.PrefabPath}");
                Assert.AreEqual(expected.MaterialsCost, readModels[i].MaterialsCost, expected.PrefabPath);
            }

            Assert.IsTrue(definitionSystem.TryGetConfiguredDefinition(0, out BuildingDefinition source));
            BuildingDefinition clone = BuildingRuntimeSpawnCompositionSystemHelper.CloneDefinitionWithFootprint(
                source,
                new Vector2Int(7, 9));
            Assert.IsNotNull(clone);
            Assert.AreEqual(source.CreditsCost, clone.CreditsCost);
            Assert.AreEqual(source.MaterialsCost, clone.MaterialsCost);
            Assert.AreEqual(new Vector2Int(7, 9), clone.FootprintCells);
        }
        finally
        {
            definitionSystem.ClearConfiguredSpawnableDefinitions(UnityEngine.Object.DestroyImmediate);
        }
    }

    [Test]
    public void MatchConfig_AuthorsRequestedStartingMaterialsAndCapacity()
    {
        InitialUnitsSpawnerAuthoringConfig config =
            AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(MatchConfigPath);

        Assert.IsNotNull(config, $"Missing config: {MatchConfigPath}");
        Assert.AreEqual(120, config.InitialMaterials);
        Assert.AreEqual(600, config.MaterialsCapacity);
        Assert.AreEqual(655, config.InitialAiMaterials);
        Assert.AreEqual(655, config.AiMaterialsCapacity);
    }

    [Test]
    public void ZeroMaterialsCost_RemainsValidAcrossConfigMetadataRuntimeAndCatalog()
    {
        BuildingDefinitionAuthoringConfig config = ScriptableObject.CreateInstance<BuildingDefinitionAuthoringConfig>();
        var prefab = new GameObject("ZeroMaterialsCostBuilding");
        var authoring = prefab.AddComponent<BuildingDefinitionAuthoring>();
        var definitionSystem = new BuildingDefinitionPrefabSystemHelper();

        try
        {
            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("materialsCost").intValue = -1;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreEqual(0, config.MaterialsCost);

            var serializedAuthoring = new SerializedObject(authoring);
            serializedAuthoring.FindProperty("config").objectReferenceValue = config;
            serializedAuthoring.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata(
                    prefab,
                    out BuildingDefinitionPrefabSystemHelper.BuildingDefinitionMetadata metadata));
            Assert.AreEqual(0, metadata.MaterialsCost);

            definitionSystem.ConfigureAuthoringMetadataResolvers(
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            definitionSystem.RebuildSpawnablesLookup(new[] { prefab }, null);
            definitionSystem.RebuildConfiguredSpawnableDefinitions(null, UnityEngine.Object.DestroyImmediate);
            Assert.IsTrue(definitionSystem.TryGetConfiguredDefinition(0, out BuildingDefinition definition));
            Assert.AreEqual(config.Price, definition.CreditsCost);
            Assert.AreEqual(0, definition.MaterialsCost);

            BuildingDefinition clone = BuildingRuntimeSpawnCompositionSystemHelper.CloneDefinitionWithFootprint(
                definition,
                Vector2Int.one);
            Assert.IsNotNull(clone);
            Assert.AreEqual(definition.CreditsCost, clone.CreditsCost);
            Assert.AreEqual(0, clone.MaterialsCost);
        }
        finally
        {
            definitionSystem.ClearConfiguredSpawnableDefinitions(UnityEngine.Object.DestroyImmediate);
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void FootprintClone_PreservesEveryFieldExceptTheAuthorizedOverride()
    {
        Texture2D texture = new(2, 2);
        Sprite selectionPortrait = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
        Sprite cardPortrait = Sprite.Create(texture, new Rect(1, 1, 1, 1), Vector2.zero);
        var source = new BuildingDefinition
        {
            DisplayName = "SourceDefinition",
            Description = "Preserve every field",
            MaxHealth = 731,
            SelectionPortraitSprite = selectionPortrait,
            CardPortraitSprite = cardPortrait,
            FootprintCells = new Vector2Int(2, 3),
            CreditsCost = 4400,
            MaterialsCost = 57,
            ProductionDurationSeconds = 12.5f,
            AttackTraceColor = new Color(0.1f, 0.2f, 0.3f, 0.4f),
            LocalBounds = new Bounds(new Vector3(1, 2, 3), new Vector3(4, 5, 6)),
            ProductionSpawnLocalPositions = new[] { new Vector3(7, 8, 9) },
            RunwayLocalRotation = Quaternion.Euler(0, 35, 0),
            RunwayHalfExtents = new Vector3(10, 2, 3)
        };
        var overrideFootprint = new Vector2Int(7, 9);

        try
        {
            BuildingDefinition clone = BuildingDefinitionFootprintCloneSystemHelper.Clone(
                source,
                overrideFootprint);

            Assert.IsNotNull(clone);
            foreach (FieldInfo field in typeof(BuildingDefinition).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.Name == nameof(BuildingDefinition.FootprintCells))
                {
                    Assert.AreEqual(overrideFootprint, field.GetValue(clone));
                    continue;
                }

                object expected = field.GetValue(source);
                object actual = field.GetValue(clone);
                if (field.FieldType.IsValueType)
                    Assert.AreEqual(expected, actual, field.Name);
                else
                    Assert.AreSame(expected, actual, field.Name);
            }
            Assert.AreEqual(new Vector2Int(2, 3), source.FootprintCells);
            Assert.IsNull(BuildingDefinitionFootprintCloneSystemHelper.Clone(null, overrideFootprint));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(selectionPortrait);
            UnityEngine.Object.DestroyImmediate(cardPortrait);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    [Test]
    public void FootprintCloneBoundary_MapsEveryCurrentFieldExactlyOnce()
    {
        const string HelperPath = "Assets/Game/Scripts/Systems/BuildingDefinitionFootprintCloneSystemHelper.cs";
        const string CallerPath = "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.cs";
        string helperSource = File.ReadAllText(HelperPath);
        string callerSource = File.ReadAllText(CallerPath);

        Assert.AreEqual(72, File.ReadAllLines(HelperPath).Length);
        Assert.AreEqual(4230, new FileInfo(HelperPath).Length);
        foreach (FieldInfo field in typeof(BuildingDefinition).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            string mapping = field.Name == nameof(BuildingDefinition.FootprintCells)
                ? "FootprintCells = footprintCells"
                : $"{field.Name} = definition.{field.Name}";
            Assert.AreEqual(1, CountOccurrences(helperSource, mapping), field.Name);
        }

        Assert.AreEqual(1, CountOccurrences(
            callerSource,
            "BuildingDefinitionFootprintCloneSystemHelper.Clone(definition, footprintCells)"));
        Assert.That(helperSource, Does.Not.Contain("new GameObject"));
        Assert.That(helperSource, Does.Not.Contain("Instantiate("));
        Assert.That(helperSource, Does.Not.Contain("Destroy("));
        Assert.That(helperSource, Does.Not.Contain("World."));
        Assert.That(helperSource, Does.Not.Contain("EntityManager"));
        Assert.That(helperSource, Does.Not.Contain("Allocator."));
    }

    private static int CountOccurrences(string source, string value)
    {
        return source.Split(new[] { value }, StringSplitOptions.None).Length - 1;
    }
}
