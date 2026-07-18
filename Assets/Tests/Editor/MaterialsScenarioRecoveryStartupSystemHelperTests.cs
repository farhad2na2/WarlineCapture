using Game.Components;
using Game.Configs;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class MaterialsScenarioRecoveryStartupSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new MaterialsScenarioRecoveryStartupSystemHelperTests();
            tests.MaterialsFreeScenarioDoesNotRequireRecovery();
            tests.MaterialsCapacityMustFitOneAuthoredConstruction();
            tests.PolicyNormalizesNonPositiveMinimumToOneMaterial();
            tests.StartingReserveMustCoverOneAuthoredConstruction();
            tests.SeededChainRequiresDepotOilSourceAndHauler();
            tests.RebuildPathRequiresEveryComponentAndAffordability();
            tests.ExchangePathRecoversOtherwiseDeadlockedFaction();
            tests.PolicyAccumulatesEveryAvailableRecoveryPath();
            tests.AIExchangeRecoveryRequiresExplicitScenarioPermission();
            tests.ShippingStartingReservesValidateForPlayerAndAI();
            tests.DuplicateFactionFailsClosed();
            tests.AIDeadlockDecisionIsDeterministicAndAllocationFreeAfterWarmup();
            Debug.Log("[MaterialsScenarioRecoveryFocusedValidation] result=Passed tests=12");
        }
        catch (Exception exception)
        {
            Debug.LogError("[MaterialsScenarioRecoveryFocusedValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void MaterialsFreeScenarioDoesNotRequireRecovery()
    {
        MaterialsScenarioRecoveryValidationResult result = Evaluate(
            materialsRequired: false,
            minimumRequiredMaterials: 0);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(MaterialsScenarioRecoveryPathCode.MaterialsNotRequired, result.Paths);
    }

    [Test]
    public void MaterialsCapacityMustFitOneAuthoredConstruction()
    {
        MaterialsScenarioRecoveryValidationResult result = Evaluate(
            minimumRequiredMaterials: 25,
            startingMaterials: 25,
            materialsCapacity: 24,
            exchangeImportEnabled: true);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(MaterialsScenarioRecoveryValidationCode.MissingMaterialsCapacity, result.Code);
    }

    [Test]
    public void PolicyNormalizesNonPositiveMinimumToOneMaterial()
    {
        MaterialsScenarioRecoveryValidationResult noCapacity = Evaluate(
            minimumRequiredMaterials: 0,
            startingMaterialsRequirement: 0,
            materialsCapacity: 0,
            exchangeImportEnabled: true);
        MaterialsScenarioRecoveryValidationResult oneMaterial = Evaluate(
            minimumRequiredMaterials: 0,
            startingMaterialsRequirement: 0,
            startingMaterials: 1,
            materialsCapacity: 1);

        Assert.IsFalse(noCapacity.IsValid);
        Assert.AreEqual(MaterialsScenarioRecoveryValidationCode.MissingMaterialsCapacity, noCapacity.Code);
        Assert.IsTrue(oneMaterial.IsValid);
        Assert.AreEqual(MaterialsScenarioRecoveryPathCode.StartingMaterials, oneMaterial.Paths);
    }

    [Test]
    public void StartingReserveMustCoverOneAuthoredConstruction()
    {
        MaterialsScenarioRecoveryValidationResult insufficient = Evaluate(
            minimumRequiredMaterials: 25,
            startingMaterials: 24,
            materialsCapacity: 100);
        MaterialsScenarioRecoveryValidationResult sufficient = Evaluate(
            minimumRequiredMaterials: 25,
            startingMaterials: 25,
            materialsCapacity: 100);
        MaterialsScenarioRecoveryValidationResult incompleteAIPlan = Evaluate(
            minimumRequiredMaterials: 25,
            startingMaterialsRequirement: 100,
            startingMaterials: 99,
            materialsCapacity: 100);

        Assert.IsFalse(insufficient.IsValid);
        Assert.IsTrue(sufficient.IsValid);
        Assert.AreEqual(MaterialsScenarioRecoveryPathCode.StartingMaterials, sufficient.Paths);
        Assert.IsFalse(incompleteAIPlan.IsValid);
    }

    [Test]
    public void SeededChainRequiresDepotOilSourceAndHauler()
    {
        MaterialsScenarioRecoveryValidationResult missingHauler = Evaluate(
            hasSeededDepot: true,
            hasSeededOilSource: true);
        MaterialsScenarioRecoveryValidationResult complete = Evaluate(
            hasSeededDepot: true,
            hasSeededOilSource: true,
            hasSeededOilHauler: true);

        Assert.IsFalse(missingHauler.IsValid);
        Assert.IsTrue(complete.IsValid);
        Assert.AreEqual(MaterialsScenarioRecoveryPathCode.SeededFabricationChain, complete.Paths);
    }

    [Test]
    public void RebuildPathRequiresEveryComponentAndAffordability()
    {
        MaterialsScenarioRecoveryValidationResult circular = Evaluate(
            canRebuildDepot: true,
            canRebuildOilSource: true,
            canAcquireOilHauler: true,
            canAffordRebuildChain: false);
        MaterialsScenarioRecoveryValidationResult viable = Evaluate(
            canRebuildDepot: true,
            canRebuildOilSource: true,
            canAcquireOilHauler: true,
            canAffordRebuildChain: true);

        Assert.IsFalse(circular.IsValid);
        Assert.IsTrue(viable.IsValid);
        Assert.AreEqual(MaterialsScenarioRecoveryPathCode.RebuildableFabricationChain, viable.Paths);
    }

    [Test]
    public void ExchangePathRecoversOtherwiseDeadlockedFaction()
    {
        MaterialsScenarioRecoveryValidationResult result = Evaluate(exchangeImportEnabled: true);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(MaterialsScenarioRecoveryPathCode.ExchangeImport, result.Paths);
    }

    [Test]
    public void PolicyAccumulatesEveryAvailableRecoveryPath()
    {
        MaterialsScenarioRecoveryValidationResult result = Evaluate(
            startingMaterials: 10,
            hasSeededDepot: true,
            hasSeededOilSource: true,
            hasSeededOilHauler: true,
            canRebuildDepot: true,
            canRebuildOilSource: true,
            canAcquireOilHauler: true,
            canAffordRebuildChain: true,
            exchangeImportEnabled: true);
        MaterialsScenarioRecoveryPathCode expected =
            MaterialsScenarioRecoveryPathCode.StartingMaterials |
            MaterialsScenarioRecoveryPathCode.SeededFabricationChain |
            MaterialsScenarioRecoveryPathCode.RebuildableFabricationChain |
            MaterialsScenarioRecoveryPathCode.ExchangeImport;

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(expected, result.Paths);
        Assert.AreEqual(FactionIdentity.EnemyFactionId, result.FactionId);
        Assert.AreEqual(1, result.ValidatedFactionCount);
    }

    [Test]
    public void AIExchangeRecoveryRequiresExplicitScenarioPermission()
    {
        using World world = CreateScenarioWorld(
            initialPlayerMaterials: 0,
            initialAiMaterials: 0,
            controls: new[]
            {
                new FactionControlEntry
                {
                    FactionId = FactionIdentity.PlayerFactionId,
                    IsPlayerFaction = 1
                },
                new FactionControlEntry
                {
                    FactionId = FactionIdentity.EnemyFactionId,
                    AIControlled = 1
                }
            });
        ResourceExchangeRecipeConfigSet playerOnly = CreateExchangeConfig(allowAIExchange: false);
        ResourceExchangeRecipeConfigSet aiEnabled = CreateExchangeConfig(allowAIExchange: true);
        try
        {
            var validation = new MaterialsScenarioRecoveryStartupSystemHelper(world.EntityManager);
            MaterialsScenarioRecoveryValidationResult denied = validation.Validate(playerOnly);
            MaterialsScenarioRecoveryValidationResult allowed = validation.Validate(aiEnabled);

            Assert.IsFalse(denied.IsValid);
            Assert.AreEqual(FactionIdentity.EnemyFactionId, denied.FactionId);
            Assert.AreEqual(MaterialsScenarioRecoveryValidationCode.NoRecoveryPath, denied.Code);
            Assert.IsTrue(allowed.IsValid);
            Assert.IsTrue((allowed.Paths & MaterialsScenarioRecoveryPathCode.ExchangeImport) != 0);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(aiEnabled);
            UnityEngine.Object.DestroyImmediate(playerOnly);
        }
    }

    [Test]
    public void ShippingStartingReservesValidateForPlayerAndAI()
    {
        using World world = CreateScenarioWorld(
            initialPlayerMaterials: 120,
            initialAiMaterials: 655,
            controls: new[]
            {
                new FactionControlEntry
                {
                    FactionId = FactionIdentity.PlayerFactionId,
                    IsPlayerFaction = 1
                },
                new FactionControlEntry
                {
                    FactionId = FactionIdentity.EnemyFactionId,
                    AIControlled = 1
                }
            });

        MaterialsScenarioRecoveryValidationResult result =
            new MaterialsScenarioRecoveryStartupSystemHelper(world.EntityManager).Validate(null);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(2, result.ValidatedFactionCount);
        Assert.IsTrue((result.Paths & MaterialsScenarioRecoveryPathCode.StartingMaterials) != 0);
    }

    [Test]
    public void DuplicateFactionFailsClosed()
    {
        using World world = CreateScenarioWorld(
            initialPlayerMaterials: 120,
            initialAiMaterials: 655,
            controls: new[]
            {
                new FactionControlEntry
                {
                    FactionId = FactionIdentity.PlayerFactionId,
                    IsPlayerFaction = 1
                },
                new FactionControlEntry
                {
                    FactionId = FactionIdentity.PlayerFactionId,
                    AIControlled = 1
                }
            });

        MaterialsScenarioRecoveryValidationResult result =
            new MaterialsScenarioRecoveryStartupSystemHelper(world.EntityManager).Validate(null);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(MaterialsScenarioRecoveryValidationCode.DuplicateFaction, result.Code);
        Assert.AreEqual(FactionIdentity.PlayerFactionId, result.FactionId);
    }

    [Test]
    public void AIDeadlockDecisionIsDeterministicAndAllocationFreeAfterWarmup()
    {
        MaterialsScenarioRecoveryValidationInput input = CreateInput(
            factionId: FactionIdentity.EnemyFactionId,
            minimumRequiredMaterials: 100,
            startingMaterials: 99,
            materialsCapacity: 655);
        for (int i = 0; i < 64; i++)
            _ = MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Evaluate(input);

        long before = GC.GetAllocatedBytesForCurrentThread();
        MaterialsScenarioRecoveryValidationResult first = default;
        bool deterministic = true;
        for (int i = 0; i < 512; i++)
        {
            MaterialsScenarioRecoveryValidationResult result =
                MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Evaluate(input);
            if (i == 0)
                first = result;
            else
            {
                deterministic &= first.IsValid == result.IsValid;
                deterministic &= first.Code == result.Code;
                deterministic &= first.Paths == result.Paths;
                deterministic &= first.FactionId == result.FactionId;
            }
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsFalse(first.IsValid);
        Assert.AreEqual(MaterialsScenarioRecoveryValidationCode.NoRecoveryPath, first.Code);
        Assert.IsTrue(deterministic);
        Assert.AreEqual(0L, allocatedBytes);
    }

    private static MaterialsScenarioRecoveryValidationResult Evaluate(
        byte factionId = FactionIdentity.EnemyFactionId,
        bool materialsRequired = true,
        int minimumRequiredMaterials = 10,
        int startingMaterialsRequirement = -1,
        int startingMaterials = 0,
        int materialsCapacity = 100,
        bool hasSeededDepot = false,
        bool hasSeededOilSource = false,
        bool hasSeededOilHauler = false,
        bool canRebuildDepot = false,
        bool canRebuildOilSource = false,
        bool canAcquireOilHauler = false,
        bool canAffordRebuildChain = false,
        bool exchangeImportEnabled = false)
    {
        return MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Evaluate(
            CreateInput(
                factionId,
                materialsRequired,
                minimumRequiredMaterials,
                startingMaterialsRequirement,
                startingMaterials,
                materialsCapacity,
                hasSeededDepot,
                hasSeededOilSource,
                hasSeededOilHauler,
                canRebuildDepot,
                canRebuildOilSource,
                canAcquireOilHauler,
                canAffordRebuildChain,
                exchangeImportEnabled));
    }

    private static MaterialsScenarioRecoveryValidationInput CreateInput(
        byte factionId = FactionIdentity.EnemyFactionId,
        bool materialsRequired = true,
        int minimumRequiredMaterials = 10,
        int startingMaterialsRequirement = -1,
        int startingMaterials = 0,
        int materialsCapacity = 100,
        bool hasSeededDepot = false,
        bool hasSeededOilSource = false,
        bool hasSeededOilHauler = false,
        bool canRebuildDepot = false,
        bool canRebuildOilSource = false,
        bool canAcquireOilHauler = false,
        bool canAffordRebuildChain = false,
        bool exchangeImportEnabled = false)
    {
        return new MaterialsScenarioRecoveryValidationInput(
            factionId,
            materialsRequired,
            minimumRequiredMaterials,
            startingMaterialsRequirement < 0 ? minimumRequiredMaterials : startingMaterialsRequirement,
            startingMaterials,
            materialsCapacity,
            hasSeededDepot,
            hasSeededOilSource,
            hasSeededOilHauler,
            canRebuildDepot,
            canRebuildOilSource,
            canAcquireOilHauler,
            canAffordRebuildChain,
            exchangeImportEnabled);
    }

    private static World CreateScenarioWorld(
        int initialPlayerMaterials,
        int initialAiMaterials,
        params FactionControlEntry[] controls)
    {
        World world = new(nameof(MaterialsScenarioRecoveryStartupSystemHelperTests));
        EntityManager entityManager = world.EntityManager;

        Entity startupEntity = entityManager.CreateEntity(
            typeof(CustomGameStartupStateComponent),
            typeof(InitialUnitsSpawnConfig));
        entityManager.SetComponentData(startupEntity, new CustomGameStartupStateComponent
        {
            GameModeId = new FixedString64Bytes("custom.skirmish.legacy")
        });
        entityManager.SetComponentData(startupEntity, new InitialUnitsSpawnConfig
        {
            InitialDollars = 30000000,
            InitialMaterials = initialPlayerMaterials,
            MaterialsCapacity = 600,
            InitialAiMaterials = initialAiMaterials,
            AiMaterialsCapacity = 655
        });
        entityManager.AddBuffer<InitialUnitsFactionBuildingSpawnEntry>(startupEntity);
        entityManager.AddBuffer<CustomGameFactionUnitSourceSpawnEntry>(startupEntity);

        Entity controlEntity = entityManager.CreateEntity(typeof(FactionControlConfigTag));
        DynamicBuffer<FactionControlEntry> controlBuffer =
            entityManager.AddBuffer<FactionControlEntry>(controlEntity);
        for (int i = 0; i < controls.Length; i++)
            controlBuffer.Add(controls[i]);
        for (int i = 0; i < controls.Length; i++)
        {
            Entity economyEntity = entityManager.CreateEntity(typeof(FactionEconomy));
            entityManager.SetComponentData(economyEntity, new FactionEconomy
            {
                FactionId = controls[i].FactionId,
                Money = 30000000
            });
        }

        Entity catalogEntity = entityManager.CreateEntity(typeof(BuildingRuntimeStateTag));
        entityManager.AddBuffer<BuildingConfiguredSpawnableReadModel>(catalogEntity);
        entityManager.AddBuffer<BuildingConfiguredUnitReadModel>(catalogEntity);
        DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables =
            entityManager.GetBuffer<BuildingConfiguredSpawnableReadModel>(catalogEntity);
        spawnables.Add(new BuildingConfiguredSpawnableReadModel
        {
            BuildingId = new FixedString128Bytes("Wall_Dirt_Straight"),
            CanRequest = 1,
            Price = 10000,
            MaterialsCost = 15
        });

        for (int i = 0; i < controls.Length; i++)
        {
            if (controls[i].AIControlled == 0)
                continue;

            Entity planEntity = entityManager.CreateEntity(typeof(AIBuildPlan));
            entityManager.SetComponentData(planEntity, new AIBuildPlan
            {
                FactionId = controls[i].FactionId,
                Enabled = 1
            });
            DynamicBuffer<AIBuildPlanEntry> entries = entityManager.AddBuffer<AIBuildPlanEntry>(planEntity);
            entries.Add(new AIBuildPlanEntry
            {
                BuildingId = new FixedString64Bytes("Wall_Dirt_Straight")
            });
        }

        return world;
    }

    private static ResourceExchangeRecipeConfigSet CreateExchangeConfig(bool allowAIExchange)
    {
        ResourceExchangeRecipeConfigSet config =
            ScriptableObject.CreateInstance<ResourceExchangeRecipeConfigSet>();
        var recipes = new List<ResourceExchangeRecipeConfigEntry>
        {
            new(
                "exchange.import_materials.test",
                ResourceExchangeRouteType.Import,
                ResourceExchangeResourceKind.Oil,
                ResourceExchangeResourceKind.Materials,
                inputAmountMin: 1800,
                inputAmountMax: 1800,
                inputStep: 1800,
                outputPerInput: 0.05555556f,
                feePercent: 0f,
                durationSecondsBase: 90f,
                missionTag: "custom.skirmish.legacy")
        };
        var gates = new List<ResourceExchangeScenarioGateConfigEntry>
        {
            new(
                "custom.skirmish.legacy",
                exchangeEnabled: true,
                maxQueueItems: 2,
                allowAiExchange: allowAIExchange)
        };
        SetPrivateField(config, "recipes", recipes);
        SetPrivateField(config, "scenarioGates", gates);
        return config;
    }

    private static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Field `{fieldName}` was not found on {instance.GetType().Name}.");
        field.SetValue(instance, value);
    }
}
#endif
