using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ResourceExchangeStartupProjectionSystemHelperTests
{
    private const string ExchangeConfigPath =
        "Assets/Game/Configs/Scene/Game_ResourceExchange_Config.asset";
    private const string DepotConfigPath =
        "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Ammunition_Depot_Config.asset";
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string BalanceReportPath =
        "Design/BalanceConfigs/Field_Fabrication_Materials_Balance_Report.json";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ShippingConfig_UsesEmergencyMarkupAndRejectsArbitrage),
                test => test.ShippingConfig_UsesEmergencyMarkupAndRejectsArbitrage(),
                ref passed);
            RunValidationStep(
                nameof(ShippingBalanceReport_ComparisonMatchesAuthoredModel),
                test => test.ShippingBalanceReport_ComparisonMatchesAuthoredModel(),
                ref passed);
            RunValidationStep(
                nameof(ShippingMatchScene_ReferencesExchangeConfig),
                test => test.ShippingMatchScene_ReferencesExchangeConfig(),
                ref passed);
            RunValidationStep(
                nameof(Initialize_ProjectsOntoCanonicalPlayerFactionEntity),
                test => test.Initialize_ProjectsOntoCanonicalPlayerFactionEntity(),
                ref passed);
            RunValidationStep(
                nameof(Initialize_ReinitializationClearsTransientExchangeState),
                test => test.Initialize_ReinitializationClearsTransientExchangeState(),
                ref passed);
            RunValidationStep(
                nameof(ShippingEmergencyImport_RejectsCanonicalMaterialsCapacityOverflow),
                test => test.ShippingEmergencyImport_RejectsCanonicalMaterialsCapacityOverflow(),
                ref passed);
            RunValidationStep(
                nameof(Initialize_UnknownScenarioDoesNotCreateExchangeBoundary),
                test => test.Initialize_UnknownScenarioDoesNotCreateExchangeBoundary(),
                ref passed);
            RunValidationStep(
                nameof(Initialize_DuplicatePlayerEconomiesFailClosed),
                test => test.Initialize_DuplicatePlayerEconomiesFailClosed(),
                ref passed);

            Debug.Log($"[ResourceExchangeStartupProjectionValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[ResourceExchangeStartupProjectionValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ShippingBalanceReport_ComparisonMatchesAuthoredModel()
    {
        ResourceExchangeRecipeConfigSet exchangeConfig = LoadExchangeConfig();
        BuildingDefinitionAuthoringPrefabConfigAsset depotConfig =
            AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringPrefabConfigAsset>(DepotConfigPath);
        Assert.AreEqual(
            ResourceExchangeReason.None,
            ResourceExchangeMaterialsBalanceModel.Evaluate(
                exchangeConfig,
                depotConfig,
                "custom.skirmish.legacy",
                out ResourceExchangeMaterialsBalanceResult balance));

        Assert.IsTrue(File.Exists(BalanceReportPath), $"Missing balance report: {BalanceReportPath}");
        BalanceReport report = JsonUtility.FromJson<BalanceReport>(File.ReadAllText(BalanceReportPath));
        Assert.NotNull(report);
        Assert.AreEqual("custom.skirmish.legacy", report.scenarioTag);
        Assert.AreEqual(balance.LocalCreditsPerMaterial, report.localProduction.creditsPerMaterial, 0.001f);
        Assert.AreEqual(balance.ImportCreditsPerMaterial, report.repeatedImports.creditsPerMaterial, 0.001f);
        Assert.AreEqual(14.25f, report.mixedStrategy.creditsPerMaterial, 0.001f);
        Assert.AreEqual(balance.ImportCreditsPerMaterial, report.destroyedDepotRecovery.creditsPerMaterial, 0.001f);
        Assert.Less(report.localProduction.creditsPerMaterial, report.mixedStrategy.creditsPerMaterial);
        Assert.Less(report.mixedStrategy.creditsPerMaterial, report.repeatedImports.creditsPerMaterial);
        Assert.IsTrue(report.materialsRoundTripSafe);
        Assert.IsTrue(report.oilFabricationExportSafe);
    }

    [Test]
    public void ShippingMatchScene_ReferencesExchangeConfig()
    {
        try
        {
            EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
            MatchSceneView sceneView =
                UnityEngine.Object.FindFirstObjectByType<MatchSceneView>(FindObjectsInactive.Include);
            Assert.NotNull(sceneView);
            Assert.AreEqual(LoadExchangeConfig(), sceneView.ResourceExchangeConfig);
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    [Test]
    public void ShippingConfig_UsesEmergencyMarkupAndRejectsArbitrage()
    {
        ResourceExchangeRecipeConfigSet exchangeConfig = LoadExchangeConfig();
        BuildingDefinitionAuthoringPrefabConfigAsset depotConfig =
            AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringPrefabConfigAsset>(DepotConfigPath);

        Assert.NotNull(depotConfig);
        Assert.AreEqual(
            ResourceExchangeReason.None,
            ResourceExchangeRecipeConfigValidator.ValidateRecipeAndScenarioGateSet(
                exchangeConfig.Recipes,
                exchangeConfig.ScenarioGates));
        Assert.AreEqual(
            ResourceExchangeReason.None,
            ResourceExchangeMaterialsBalanceModel.Evaluate(
                exchangeConfig,
                depotConfig,
                "custom.skirmish.legacy",
                out ResourceExchangeMaterialsBalanceResult balance));
        Assert.AreEqual(10.5f, balance.LocalCreditsPerMaterial, 0.001f);
        Assert.AreEqual(18f, balance.ImportCreditsPerMaterial, 0.001f);
        Assert.GreaterOrEqual(balance.ImportMarkup, exchangeConfig.MaterialsBalance.MinimumImportMarkup);
        Assert.LessOrEqual(balance.ImportMarkup, exchangeConfig.MaterialsBalance.MaximumImportMarkup);
        Assert.LessOrEqual(
            balance.MaterialsRoundTripRetention,
            ResourceExchangeRecipeConfigValidator.MaximumRoundTripResourceRetention);
        Assert.LessOrEqual(
            balance.OilFabricateExportCreditsPerBarrel,
            balance.OilDirectExportCreditsPerBarrel);
    }

    [Test]
    public void Initialize_ProjectsOntoCanonicalPlayerFactionEntity()
    {
        using World world = new(nameof(Initialize_ProjectsOntoCanonicalPlayerFactionEntity));
        EntityManager em = world.EntityManager;
        CreateScenario(em, "custom.skirmish.legacy");
        Entity player = CreateFactionEconomy(em, FactionIdentity.PlayerFactionId);
        CreateFactionEconomy(em, FactionIdentity.EnemyFactionId);

        ResourceExchangeStartupProjectionSystemHelper.Result result =
            new ResourceExchangeStartupProjectionSystemHelper(em).Initialize(LoadExchangeConfig());

        Assert.IsTrue(result.Projected);
        Assert.AreEqual(player, result.BoundaryEntity);
        Assert.AreEqual(5, result.RecipeCount);
        Assert.AreEqual(ResourceExchangeReason.None, result.Reason);
        Assert.AreEqual(2, em.CreateEntityQuery(typeof(FactionEconomy)).CalculateEntityCount());
        ResourceExchangeEnabledComponent enabled =
            em.GetComponentData<ResourceExchangeEnabledComponent>(player);
        Assert.AreEqual(1, enabled.Enabled);
        Assert.AreEqual(FactionIdentity.PlayerFactionId, enabled.FactionId);
        Assert.AreEqual(2, enabled.MaxQueueItems);
        Assert.AreEqual("custom.skirmish.legacy", enabled.ScenarioTag.ToString());
        Assert.AreEqual(5, em.GetBuffer<ResourceExchangeRecipeComponent>(player).Length);
        Assert.IsTrue(em.HasBuffer<ResourceExchangeRequestComponent>(player));
        Assert.IsTrue(em.HasBuffer<ResourceExchangeResultComponent>(player));
        Assert.IsTrue(em.HasBuffer<ResourceExchangeEconomyEventComponent>(player));
        Assert.IsTrue(em.HasBuffer<ResourceExchangePhysicalReservationComponent>(player));
    }

    [Test]
    public void Initialize_ReinitializationClearsTransientExchangeState()
    {
        using World world = new(nameof(Initialize_ReinitializationClearsTransientExchangeState));
        EntityManager em = world.EntityManager;
        CreateScenario(em, "custom.skirmish.legacy");
        Entity player = CreateFactionEconomy(em, FactionIdentity.PlayerFactionId);
        ResourceExchangeStartupProjectionSystemHelper projection = new(em);
        Assert.IsTrue(projection.Initialize(LoadExchangeConfig()).Projected);
        em.GetBuffer<ResourceExchangeQueueComponent>(player).Add(new ResourceExchangeQueueComponent
        {
            QueueItemId = 17,
            FactionId = FactionIdentity.PlayerFactionId,
            State = ResourceExchangeQueueState.InProgress
        });
        em.GetBuffer<ResourceExchangeResultComponent>(player).Add(new ResourceExchangeResultComponent
        {
            RequestId = 9,
            FactionId = FactionIdentity.PlayerFactionId
        });

        ResourceExchangeStartupProjectionSystemHelper.Result result =
            projection.Initialize(LoadExchangeConfig());

        Assert.IsTrue(result.Projected);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(player).Length);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeResultComponent>(player).Length);
        Assert.AreEqual(5, em.GetBuffer<ResourceExchangeRecipeComponent>(player).Length);
        Assert.AreEqual(2u, em.GetComponentData<ResourceExchangeEnabledComponent>(player).Version);
    }

    [Test]
    public void ShippingEmergencyImport_RejectsCanonicalMaterialsCapacityOverflow()
    {
        using World world = new(nameof(ShippingEmergencyImport_RejectsCanonicalMaterialsCapacityOverflow));
        EntityManager em = world.EntityManager;
        CreateScenario(em, "custom.skirmish.legacy");
        Entity player = CreateFactionEconomy(em, FactionIdentity.PlayerFactionId);
        FactionTacticalMaterialsComponent materials =
            em.GetComponentData<FactionTacticalMaterialsComponent>(player);
        materials.Current = 550;
        materials.Capacity = 600;
        em.SetComponentData(player, materials);
        Assert.IsTrue(
            new ResourceExchangeStartupProjectionSystemHelper(em).Initialize(LoadExchangeConfig()).Projected);

        int requestId = ResourceExchangeRequestValidationSystem.EnqueueStartRequest(
            em,
            player,
            new FixedString128Bytes("exchange.import_materials.emergency"),
            1800,
            FactionIdentity.PlayerFactionId,
            0);
        SystemHandle system = world.CreateSystem<ResourceExchangeRequestValidationSystem>();
        world.Unmanaged.GetUnsafeSystemRef<ResourceExchangeRequestValidationSystem>(system)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(system));

        Assert.IsTrue(
            ResourceExchangeRequestValidationSystem.TryGetResult(
                em,
                player,
                requestId,
                out ResourceExchangeResultComponent result));
        Assert.AreEqual(0, result.Accepted);
        Assert.AreEqual(ResourceExchangeReason.StorageFull, result.Reason);
        Assert.AreEqual(30000000, em.GetComponentData<FactionEconomy>(player).Money);
        Assert.AreEqual(550, em.GetComponentData<FactionTacticalMaterialsComponent>(player).Current);
    }

    [Test]
    public void Initialize_UnknownScenarioDoesNotCreateExchangeBoundary()
    {
        using World world = new(nameof(Initialize_UnknownScenarioDoesNotCreateExchangeBoundary));
        EntityManager em = world.EntityManager;
        CreateScenario(em, "mission.not_configured");
        Entity player = CreateFactionEconomy(em, FactionIdentity.PlayerFactionId);

        ResourceExchangeStartupProjectionSystemHelper.Result result =
            new ResourceExchangeStartupProjectionSystemHelper(em).Initialize(LoadExchangeConfig());

        Assert.IsFalse(result.Projected);
        Assert.AreEqual(ResourceExchangeReason.InvalidScenarioGate, result.Reason);
        Assert.IsFalse(em.HasComponent<ResourceExchangeEnabledComponent>(player));
    }

    [Test]
    public void Initialize_DuplicatePlayerEconomiesFailClosed()
    {
        using World world = new(nameof(Initialize_DuplicatePlayerEconomiesFailClosed));
        EntityManager em = world.EntityManager;
        CreateScenario(em, "custom.skirmish.legacy");
        Entity first = CreateFactionEconomy(em, FactionIdentity.PlayerFactionId);
        Entity second = CreateFactionEconomy(em, FactionIdentity.PlayerFactionId);

        ResourceExchangeStartupProjectionSystemHelper.Result result =
            new ResourceExchangeStartupProjectionSystemHelper(em).Initialize(LoadExchangeConfig());

        Assert.IsFalse(result.Projected);
        Assert.AreEqual(ResourceExchangeReason.ExchangeUnavailable, result.Reason);
        Assert.IsFalse(em.HasComponent<ResourceExchangeEnabledComponent>(first));
        Assert.IsFalse(em.HasComponent<ResourceExchangeEnabledComponent>(second));
    }

    private static ResourceExchangeRecipeConfigSet LoadExchangeConfig()
    {
        ResourceExchangeRecipeConfigSet config =
            AssetDatabase.LoadAssetAtPath<ResourceExchangeRecipeConfigSet>(ExchangeConfigPath);
        Assert.NotNull(config, $"Missing shipping Exchange config: {ExchangeConfigPath}");
        return config;
    }

    private static void CreateScenario(EntityManager em, string scenarioTag)
    {
        Entity entity = em.CreateEntity(typeof(CustomGameStartupStateComponent));
        em.SetComponentData(entity, new CustomGameStartupStateComponent
        {
            GameModeId = new FixedString64Bytes(scenarioTag)
        });
    }

    private static Entity CreateFactionEconomy(EntityManager em, byte factionId)
    {
        Entity entity = em.CreateEntity(
            typeof(FactionEconomy),
            typeof(FactionTacticalMaterialsComponent));
        em.SetComponentData(entity, new FactionEconomy
        {
            FactionId = factionId,
            Money = 30000000
        });
        em.SetComponentData(entity, new FactionTacticalMaterialsComponent
        {
            FactionId = factionId,
            Current = 120,
            Capacity = 600
        });
        return entity;
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeStartupProjectionSystemHelperTests> action,
        ref int passed)
    {
        ResourceExchangeStartupProjectionSystemHelperTests test = new();
        action(test);
        passed++;
        Debug.Log($"[ResourceExchangeStartupProjectionValidation] pass={name}");
    }

    [Serializable]
    private sealed class BalanceReport
    {
        public string scenarioTag;
        public BalanceStrategy localProduction;
        public BalanceStrategy repeatedImports;
        public BalanceStrategy mixedStrategy;
        public BalanceStrategy destroyedDepotRecovery;
        public bool materialsRoundTripSafe;
        public bool oilFabricationExportSafe;
    }

    [Serializable]
    private sealed class BalanceStrategy
    {
        public float creditsPerMaterial;
    }
}
#endif
