using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
            RunValidationStep(
                nameof(ShippingScenarioGatesKeepAIExchangeDisabled),
                test => test.ShippingScenarioGatesKeepAIExchangeDisabled(),
                ref passed);
            RunValidationStep(
                nameof(AIEnabledScenarioProjectsCanonicalNonPlayerFaction),
                test => test.AIEnabledScenarioProjectsCanonicalNonPlayerFaction(),
                ref passed);
            RunValidationStep(
                nameof(AIProjectionDuplicateFactionControlsFailClosed),
                test => test.AIProjectionDuplicateFactionControlsFailClosed(),
                ref passed);
            RunValidationStep(
                nameof(AIDisabledScenarioClearsPreviouslyProjectedBoundary),
                test => test.AIDisabledScenarioClearsPreviouslyProjectedBoundary(),
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
        Assert.AreEqual(balance.LocalMaterialsPerOil, report.localMaterialsPerOil, 0.001f);
        Assert.AreEqual(balance.ExchangeMaterialsPerOil, report.exchangeMaterialsPerOil, 0.001f);
        Assert.AreEqual(balance.ExchangeEfficiency, report.exchangeEfficiency, 0.001f);
        Assert.AreEqual(balance.MaterialsRoundTripRetention, report.materialsRoundTripRetention, 0.001f);
        Assert.Less(report.exchangeMaterialsPerOil, report.localMaterialsPerOil);
        Assert.IsTrue(report.materialsRoundTripSafe);
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
        Assert.AreEqual(5f, balance.LocalMaterialsPerOil, 0.001f);
        Assert.AreEqual(3f, balance.ExchangeMaterialsPerOil, 0.001f);
        Assert.AreEqual(0.6f, balance.ExchangeEfficiency, 0.001f);
        Assert.LessOrEqual(
            balance.MaterialsRoundTripRetention,
            ResourceExchangeRecipeConfigValidator.MaximumRoundTripResourceRetention);
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
            new FixedString128Bytes("exchange.convert_oil_materials.emergency"),
            100,
            FactionIdentity.PlayerFactionId,
            0);
        Entity oilStorage = em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(oilStorage, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = 1,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            StoredOilBarrels = 1000f,
            OilStorageCapacity = 2000
        });
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

    [Test]
    public void ShippingScenarioGatesKeepAIExchangeDisabled()
    {
        ResourceExchangeRecipeConfigSet config = LoadExchangeConfig();
        for (int i = 0; i < config.ScenarioGates.Count; i++)
            Assert.IsFalse(config.ScenarioGates[i].AllowAiExchange, config.ScenarioGates[i].ScenarioTag);

        using World world = new(nameof(ShippingScenarioGatesKeepAIExchangeDisabled));
        EntityManager em = world.EntityManager;
        CreateScenario(em, "custom.skirmish.legacy");
        CreateFactionEconomy(em, FactionIdentity.PlayerFactionId);
        Entity enemy = CreateFactionEconomy(em, FactionIdentity.EnemyFactionId);
        Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(controlEntity);
        controls.Add(new FactionControlEntry
        {
            FactionId = FactionIdentity.PlayerFactionId,
            AIControlled = 1,
            IsPlayerFaction = 1
        });
        controls.Add(new FactionControlEntry
        {
            FactionId = FactionIdentity.EnemyFactionId,
            AIControlled = 1
        });

        ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult result =
            new ResourceExchangeStartupProjectionSystemHelper(em).InitializeEligibleAIFactions(config);

        Assert.IsFalse(result.ScenarioAllowsAIExchange);
        Assert.AreEqual(1, result.EligibleFactionCount);
        Assert.AreEqual(0, result.ProjectedFactionCount);
        Assert.IsFalse(em.HasComponent<ResourceExchangeEnabledComponent>(enemy));
    }

    [Test]
    public void AIEnabledScenarioProjectsCanonicalNonPlayerFaction()
    {
        ResourceExchangeRecipeConfigSet config = CreateAIConfig(allowAIExchange: true);
        try
        {
            using World world = new(nameof(AIEnabledScenarioProjectsCanonicalNonPlayerFaction));
            EntityManager em = world.EntityManager;
            CreateScenario(em, "custom.skirmish.ai_test");
            Entity enemy = CreateFactionEconomy(em, FactionIdentity.EnemyFactionId);
            Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
            em.AddBuffer<FactionControlEntry>(controlEntity).Add(new FactionControlEntry
            {
                FactionId = FactionIdentity.EnemyFactionId,
                AIControlled = 0,
                IsPlayerFaction = 0
            });

            ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult result =
                new ResourceExchangeStartupProjectionSystemHelper(em).InitializeEligibleAIFactions(config);

            Assert.IsTrue(result.ScenarioAllowsAIExchange);
            Assert.AreEqual(1, result.EligibleFactionCount);
            Assert.AreEqual(1, result.ProjectedFactionCount);
            Assert.AreEqual(ResourceExchangeReason.None, result.Reason);
            ResourceExchangeEnabledComponent enabled =
                em.GetComponentData<ResourceExchangeEnabledComponent>(enemy);
            Assert.AreEqual(1, enabled.Enabled);
            Assert.AreEqual(1, enabled.AllowAiExchange);
            Assert.AreEqual(FactionIdentity.EnemyFactionId, enabled.FactionId);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void AIProjectionDuplicateFactionControlsFailClosed()
    {
        ResourceExchangeRecipeConfigSet config = CreateAIConfig(allowAIExchange: true);
        try
        {
            using World world = new(nameof(AIProjectionDuplicateFactionControlsFailClosed));
            EntityManager em = world.EntityManager;
            CreateScenario(em, "custom.skirmish.ai_test");
            Entity enemy = CreateFactionEconomy(em, FactionIdentity.EnemyFactionId);
            Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
            DynamicBuffer<FactionControlEntry> controls = em.AddBuffer<FactionControlEntry>(controlEntity);
            controls.Add(new FactionControlEntry { FactionId = FactionIdentity.EnemyFactionId });
            controls.Add(new FactionControlEntry { FactionId = FactionIdentity.EnemyFactionId });

            ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult result =
                new ResourceExchangeStartupProjectionSystemHelper(em).InitializeEligibleAIFactions(config);

            Assert.IsTrue(result.ScenarioAllowsAIExchange);
            Assert.AreEqual(ResourceExchangeReason.ExchangeUnavailable, result.Reason);
            Assert.AreEqual(0, result.ProjectedFactionCount);
            Assert.IsFalse(em.HasComponent<ResourceExchangeEnabledComponent>(enemy));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void AIDisabledScenarioClearsPreviouslyProjectedBoundary()
    {
        ResourceExchangeRecipeConfigSet enabledConfig = CreateAIConfig(allowAIExchange: true);
        ResourceExchangeRecipeConfigSet disabledConfig = CreateAIConfig(allowAIExchange: false);
        try
        {
            using World world = new(nameof(AIDisabledScenarioClearsPreviouslyProjectedBoundary));
            EntityManager em = world.EntityManager;
            CreateScenario(em, "custom.skirmish.ai_test");
            Entity enemy = CreateFactionEconomy(em, FactionIdentity.EnemyFactionId);
            Entity controlEntity = em.CreateEntity(typeof(FactionControlConfigTag));
            em.AddBuffer<FactionControlEntry>(controlEntity).Add(new FactionControlEntry
            {
                FactionId = FactionIdentity.EnemyFactionId,
                AIControlled = 1
            });
            ResourceExchangeStartupProjectionSystemHelper projection = new(em);
            Assert.AreEqual(1, projection.InitializeEligibleAIFactions(enabledConfig).ProjectedFactionCount);
            em.GetBuffer<ResourceExchangeRequestComponent>(enemy).Add(new ResourceExchangeRequestComponent
            {
                RequestId = 7,
                RequestKind = ResourceExchangeRequestKind.Start,
                FactionId = FactionIdentity.EnemyFactionId
            });
            em.GetBuffer<ResourceExchangeQueueComponent>(enemy).Add(new ResourceExchangeQueueComponent
            {
                QueueItemId = 8,
                FactionId = FactionIdentity.EnemyFactionId,
                State = ResourceExchangeQueueState.InProgress
            });

            ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult result =
                projection.InitializeEligibleAIFactions(disabledConfig);

            Assert.IsFalse(result.ScenarioAllowsAIExchange);
            Assert.AreEqual(1, result.EligibleFactionCount);
            Assert.AreEqual(0, result.ProjectedFactionCount);
            ResourceExchangeEnabledComponent enabled =
                em.GetComponentData<ResourceExchangeEnabledComponent>(enemy);
            Assert.AreEqual(0, enabled.Enabled);
            Assert.AreEqual(0, enabled.AllowAiExchange);
            Assert.AreEqual(0, em.GetBuffer<ResourceExchangeRecipeComponent>(enemy).Length);
            Assert.AreEqual(0, em.GetBuffer<ResourceExchangeRequestComponent>(enemy).Length);
            Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(enemy).Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(enabledConfig);
            UnityEngine.Object.DestroyImmediate(disabledConfig);
        }
    }

    private static ResourceExchangeRecipeConfigSet CreateAIConfig(bool allowAIExchange)
    {
        ResourceExchangeRecipeConfigSet config =
            ScriptableObject.CreateInstance<ResourceExchangeRecipeConfigSet>();
        var recipes = new List<ResourceExchangeRecipeConfigEntry>
        {
            new(
                "exchange.import_materials.ai_test",
                ResourceExchangeRouteType.Import,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Materials,
                inputAmountMin: 1800,
                inputAmountMax: 9000,
                inputStep: 1800,
                outputPerInput: 1f / 18f,
                feePercent: 0f,
                durationSecondsBase: 90f,
                missionTag: "custom.skirmish.ai_test")
        };
        var gates = new List<ResourceExchangeScenarioGateConfigEntry>
        {
            new(
                "custom.skirmish.ai_test",
                exchangeEnabled: true,
                maxQueueItems: 2,
                allowAiExchange: allowAIExchange)
        };
        SetPrivateField(config, "recipes", recipes);
        SetPrivateField(config, "scenarioGates", gates);
        return config;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing private field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
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
        public float localMaterialsPerOil;
        public float exchangeMaterialsPerOil;
        public float exchangeEfficiency;
        public float materialsRoundTripRetention;
        public bool materialsRoundTripSafe;
    }
}
#endif
