#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class M02EstablishBaseResourceTests
{
    private const string Marker = "[M02EstablishBaseResourceValidation] result=Passed tests=9";
    private const string MissionId = "saga.ch01.m02.establish_base";
    private const string ScenarioId = "scenario.ch01.m02.establish_base";
    private const string MapId = "opmap.ch01.forward_post_01";
    private const int StartingCredits = 55000;
    private const int StartingMaterials = 120;

    [MenuItem("Game/Validation/Run M02 Establish Base Resource Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseResourceTests tests = new();
            tests.CanonicalProjectionCarriesAttemptResources();
            tests.InitializerAppliesExactResourcesAndZerosLogistics();
            tests.InitializerIsIdempotentWithinAttempt();
            tests.RetryLaunchRearmsInitializerAndRestoresResources();
            tests.DisabledMissionRuntimeLeavesResourcesUntouched();
            tests.AmbiguousPlayerResourceOwnerFailsClosed();
            tests.M02HudShowsCreditsAndMaterialsWhileHidingLogistics();
            tests.M02HidesUnrelatedSquadAndSupportControls();
            tests.AttemptInitializerDoesNotReferencePersistence();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseResourceValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void CanonicalProjectionCarriesAttemptResources()
    {
        MissionDefinitionConfig mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(
            M02EstablishBaseConfigBuilder.MissionPath);
        ScenarioSetupConfig scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(
            M02EstablishBaseConfigBuilder.ScenarioPath);
        OperationMapCatalogConfig maps = AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(
            M02EstablishBaseConfigBuilder.OperationMapCatalogPath);
        using World world = new(nameof(CanonicalProjectionCarriesAttemptResources));
        Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(
            world.EntityManager, mission, scenario, maps, 1, out Entity root, out string error), error);
        CampaignMissionCatalogComponent catalog =
            world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[0];
        Assert.AreEqual(1, definition.MissionRuntimeEnabled);
        Assert.AreEqual(StartingCredits, definition.StartingCredits);
        Assert.AreEqual(StartingMaterials, definition.StartingMaterials);
        Assert.IsTrue(CampaignMissionSpawnSystem.HasRequiredRestrictions(ref definition));
        Assert.IsTrue(world.EntityManager.HasComponent<
            CampaignMissionAttemptResourceInitializationComponent>(root));
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void InitializerAppliesExactResourcesAndZerosLogistics()
    {
        using World world = CreateRuntimeWorld(enabled: true, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            Entity resources = CreatePlayerResources(world.EntityManager, 12, 88, 600, 7u, 14f, 23f);
            UpdateResources(world);
            FactionEconomy economy = world.EntityManager.GetComponentData<FactionEconomy>(resources);
            FactionTacticalMaterialsComponent materials =
                world.EntityManager.GetComponentData<FactionTacticalMaterialsComponent>(resources);
            Assert.AreEqual(StartingCredits, economy.Money);
            Assert.AreEqual(0f, economy.Oil);
            Assert.AreEqual(0f, economy.Fuel);
            Assert.AreEqual(StartingMaterials, materials.Current);
            Assert.AreEqual(StartingMaterials, materials.Capacity);
            Assert.AreEqual(8u, materials.Version);
            Assert.AreEqual(0, materials.LifetimeSpent);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void InitializerIsIdempotentWithinAttempt()
    {
        using World world = CreateRuntimeWorld(enabled: true, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            Entity resources = CreatePlayerResources(world.EntityManager, 1, 1, 10, 1u, 0f, 0f);
            UpdateResources(world);
            FactionEconomy spentEconomy = world.EntityManager.GetComponentData<FactionEconomy>(resources);
            spentEconomy.Money = 5000;
            world.EntityManager.SetComponentData(resources, spentEconomy);
            FactionTacticalMaterialsComponent spentMaterials =
                world.EntityManager.GetComponentData<FactionTacticalMaterialsComponent>(resources);
            spentMaterials.Current = 10;
            spentMaterials.Version++;
            world.EntityManager.SetComponentData(resources, spentMaterials);

            UpdateResources(world);
            Assert.AreEqual(5000, world.EntityManager.GetComponentData<FactionEconomy>(resources).Money);
            Assert.AreEqual(10, world.EntityManager.GetComponentData<
                FactionTacticalMaterialsComponent>(resources).Current);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void RetryLaunchRearmsInitializerAndRestoresResources()
    {
        using World world = CreateRuntimeWorld(enabled: true, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            EntityManager entityManager = world.EntityManager;
            Entity root = entityManager.CreateEntityQuery(typeof(CampaignMissionRootComponent)).GetSingletonEntity();
            Entity resources = CreatePlayerResources(entityManager, 5000, 10, 120, 4u, 0f, 0f);
            entityManager.AddBuffer<CampaignMissionLaunchRequestElement>(root).Add(new CampaignMissionLaunchRequestElement
            {
                SchemaVersion = MissionLaunchPayloadFactory.CurrentSchemaVersion,
                MissionId = MissionId,
                ScenarioId = ScenarioId,
                OperationMapId = MapId,
                LaunchOrigin = MissionLaunchOriginKind.CampaignOperations,
                RunKind = MissionRunKind.Retry,
                Guidance = NarrativeGuidanceMode.Contextual,
                TransitionToken = 2,
                SessionToken = "m02-resource-retry",
                AttemptOrdinal = 1,
                DeterministicSeed = 2002001
            });
            entityManager.AddBuffer<CampaignMissionLaunchResultElement>(root);
            entityManager.AddBuffer<CampaignMissionActionRequestElement>(root);
            entityManager.AddBuffer<CampaignMissionActionResultElement>(root);
            entityManager.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            entityManager.AddBuffer<CampaignMissionSettlementResultElement>(root);
            entityManager.AddBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            entityManager.CreateEntity(typeof(ActiveOperationMapComponent));
            Entity activeMap = entityManager.CreateEntityQuery(typeof(ActiveOperationMapComponent)).GetSingletonEntity();
            entityManager.SetComponentData(activeMap, new ActiveOperationMapComponent
            {
                MissionId = MissionId,
                ScenarioId = ScenarioId,
                OperationMapId = MapId
            });
            entityManager.CreateEntity(typeof(OperationMapReadinessComponent));

            UpdateLaunch(world);
            CampaignMissionAttemptResourceInitializationComponent pending =
                entityManager.GetComponentData<CampaignMissionAttemptResourceInitializationComponent>(root);
            Assert.AreEqual(0, pending.Applied);
            Assert.AreEqual(1, pending.AttemptOrdinal);
            UpdateResources(world);
            Assert.AreEqual(StartingCredits, entityManager.GetComponentData<FactionEconomy>(resources).Money);
            Assert.AreEqual(StartingMaterials, entityManager.GetComponentData<
                FactionTacticalMaterialsComponent>(resources).Current);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void DisabledMissionRuntimeLeavesResourcesUntouched()
    {
        using World world = CreateRuntimeWorld(enabled: false, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            Entity resources = CreatePlayerResources(world.EntityManager, 91, 37, 80, 3u, 5f, 6f);
            UpdateResources(world);
            Assert.AreEqual(91, world.EntityManager.GetComponentData<FactionEconomy>(resources).Money);
            Assert.AreEqual(37, world.EntityManager.GetComponentData<
                FactionTacticalMaterialsComponent>(resources).Current);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void AmbiguousPlayerResourceOwnerFailsClosed()
    {
        using World world = CreateRuntimeWorld(enabled: true, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        try
        {
            Entity first = CreatePlayerResources(world.EntityManager, 91, 37, 80, 3u, 5f, 6f);
            CreatePlayerResources(world.EntityManager, 92, 38, 81, 4u, 7f, 8f);
            UpdateResources(world);
            Assert.AreEqual(91, world.EntityManager.GetComponentData<FactionEconomy>(first).Money);
            Entity root = world.EntityManager.CreateEntityQuery(
                typeof(CampaignMissionRootComponent)).GetSingletonEntity();
            Assert.AreEqual(0, world.EntityManager.GetComponentData<
                CampaignMissionAttemptResourceInitializationComponent>(root).Applied);
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void M02HudShowsCreditsAndMaterialsWhileHidingLogistics()
    {
        World previous = World.DefaultGameObjectInjectionWorld;
        using World world = CreateRuntimeWorld(enabled: true, out BlobAssetReference<CampaignMissionCatalogBlob> blob);
        GameObject headerRoot = new("M02ResourceHeader");
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            CreatePlayerResources(world.EntityManager, StartingCredits, StartingMaterials, StartingMaterials, 1u, 0f, 0f);
            Entity boundary = world.EntityManager.CreateEntity(
                typeof(UiShellRootComponent), typeof(UiMatchHudHeaderComponent));
            SystemHandle readModel = world.CreateSystem<UiMatchHudResourceReadModelSystem>();
            UpdateSystem<UiMatchHudResourceReadModelSystem>(world, readModel);

            GameObject materials = CreateSlot(headerRoot.transform, "MaterialsSlot", out TMP_Text materialsLabel, out TMP_Text materialsValue);
            GameObject oil = CreateSlot(headerRoot.transform, "OilSlot", out TMP_Text oilLabel, out TMP_Text oilValue);
            GameObject credits = CreateSlot(headerRoot.transform, "FuelSlot", out TMP_Text creditsLabel, out TMP_Text creditsValue);
            CreateSlot(headerRoot.transform, "CivilianRiskSlot", out TMP_Text civilianLabel, out TMP_Text civilianValue);
            MatchHudResourceHeaderPresentation presentation = new();
            presentation.Bind(oil, materialsLabel, materialsValue, oilLabel, oilValue,
                creditsLabel, creditsValue, civilianLabel, civilianValue, 0f);

            Assert.IsTrue(materials.activeSelf);
            Assert.IsFalse(oil.activeSelf);
            Assert.IsTrue(credits.activeSelf);
            Assert.AreEqual("Materials", materialsLabel.text);
            Assert.AreEqual("120/120", materialsValue.text);
            Assert.AreEqual("Credits", creditsLabel.text);
            Assert.AreEqual("#55K", creditsValue.text);
            Assert.AreEqual("120/120", world.EntityManager.GetComponentData<
                UiMatchHudHeaderComponent>(boundary).MaterialsText.ToString());
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            blob.Dispose();
            UnityEngine.Object.DestroyImmediate(headerRoot);
        }
    }

    [Test]
    public void M02HidesUnrelatedSquadAndSupportControls()
    {
        GameObject railRoot = new("M02RightRail", typeof(RectTransform), typeof(MatchHudRightQuickRailView));
        GameObject trayRoot = new("M02SquadTray", typeof(RectTransform));
        trayRoot.SetActive(false);
        try
        {
            Button build = CreateButton(railRoot.transform, "BuildCommand");
            Button support = CreateButton(railRoot.transform, "SupportCommand");
            typeof(MatchHudRightQuickRailView).GetField(
                "buildButton", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
                railRoot.GetComponent<MatchHudRightQuickRailView>(), build);
            MatchHudRightQuickRailView rail = railRoot.GetComponent<MatchHudRightQuickRailView>();
            rail.ApplyMissionRestrictionVisibility(false, true, hideUnrelatedControls: true);
            Assert.IsTrue(build.gameObject.activeSelf);
            Assert.IsFalse(support.gameObject.activeSelf);

            MatchHudSquadTrayView tray = trayRoot.AddComponent<MatchHudSquadTrayView>();
            MatchHudSquadTrayView.Card[] cards = new MatchHudSquadTrayView.Card[5];
            for (int index = 0; index < cards.Length; index++)
            {
                Button button = CreateButton(trayRoot.transform, $"Card{index}");
                cards[index] = new MatchHudSquadTrayView.Card
                {
                    Button = button,
                    FrameImage = button.GetComponent<Image>()
                };
            }
            typeof(MatchHudSquadTrayView).GetField(
                "cards", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(tray, cards);
            trayRoot.SetActive(true);
            tray.ApplyMissionRestrictionVisibility(false, true, true, hideUnrelatedControls: true);
            Assert.IsTrue(cards[0].Button.gameObject.activeSelf);
            for (int index = 1; index < cards.Length; index++)
                Assert.IsFalse(cards[index].Button.gameObject.activeSelf, $"Card {index} must stay hidden in M02.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(railRoot);
            UnityEngine.Object.DestroyImmediate(trayRoot);
        }
    }

    [Test]
    public void AttemptInitializerDoesNotReferencePersistence()
    {
        string source = File.ReadAllText(
            "Assets/Game/Scripts/Runtime/Missions/CampaignMissionAttemptResourceInitializationSystem.cs");
        Assert.That(source, Does.Not.Contain("SaveService"));
        Assert.That(source, Does.Not.Contain("CampaignMissionProgressStore"));
        Assert.That(source, Does.Not.Contain("Profile"));
    }

    private static World CreateRuntimeWorld(
        bool enabled,
        out BlobAssetReference<CampaignMissionCatalogBlob> blob)
    {
        World world = new($"M02 resources enabled={enabled}");
        EntityManager entityManager = world.EntityManager;
        Entity root = entityManager.CreateEntity(
            typeof(CampaignMissionRootComponent),
            typeof(CampaignMissionCatalogComponent),
            typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent),
            typeof(CampaignMissionLaunchQueueComponent),
            typeof(CampaignMissionAttemptResourceInitializationComponent));
        using BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
        missions[0].MissionId = MissionId;
        missions[0].ScenarioId = ScenarioId;
        missions[0].OperationMapId = MapId;
        missions[0].MissionRuntimeEnabled = enabled ? (byte)1 : (byte)0;
        missions[0].StartingCredits = enabled ? StartingCredits : 0;
        missions[0].StartingMaterials = enabled ? StartingMaterials : 0;
        missions[0].TransportDisabled = 1;
        missions[0].AirDisabled = 1;
        blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
        entityManager.SetComponentData(root, new CampaignMissionCatalogComponent
        {
            Blob = blob,
            SourceVersion = 1
        });
        entityManager.SetComponentData(root, new CampaignMissionRuntimeComponent
        {
            MissionId = MissionId,
            ScenarioId = ScenarioId,
            OperationMapId = MapId,
            SessionToken = "m02-resource-attempt",
            Phase = MissionPhaseKind.Preparing,
            LaunchOrigin = MissionLaunchOriginKind.CampaignOperations,
            RunKind = MissionRunKind.FirstClear,
            Guidance = NarrativeGuidanceMode.Contextual,
            Version = 1,
            SourceVersion = 1,
            DeterministicSeed = 2002001
        });
        entityManager.SetComponentData(root, new CampaignMissionAttemptResourceInitializationComponent
        {
            SessionToken = "m02-resource-attempt",
            AttemptOrdinal = 0
        });
        return world;
    }

    private static Entity CreatePlayerResources(
        EntityManager entityManager,
        int credits,
        int materials,
        int capacity,
        uint version,
        float oil,
        float fuel)
    {
        Entity entity = entityManager.CreateEntity(
            typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent));
        entityManager.SetComponentData(entity, new FactionEconomy
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Money = credits,
            Oil = oil,
            Fuel = fuel
        });
        entityManager.SetComponentData(entity, new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Current = materials,
            Capacity = capacity,
            LifetimeSpent = 99,
            Version = version
        });
        return entity;
    }

    private static void UpdateResources(World world)
    {
        SystemHandle system = world.CreateSystem<CampaignMissionAttemptResourceInitializationSystem>();
        UpdateSystem<CampaignMissionAttemptResourceInitializationSystem>(world, system);
        world.DestroySystem(system);
    }

    private static void UpdateLaunch(World world)
    {
        SystemHandle system = world.CreateSystem<CampaignMissionLaunchSystem>();
        UpdateSystem<CampaignMissionLaunchSystem>(world, system);
        world.DestroySystem(system);
    }

    private static void UpdateSystem<T>(World world, SystemHandle system) where T : unmanaged, ISystem
    {
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(system);
        world.Unmanaged.GetUnsafeSystemRef<T>(system).OnUpdate(ref state);
        state.Dependency.Complete();
        world.EntityManager.CompleteAllTrackedJobs();
    }

    private static Button CreateButton(Transform parent, string name)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        return buttonObject.GetComponent<Button>();
    }

    private static GameObject CreateSlot(
        Transform parent,
        string name,
        out TMP_Text label,
        out TMP_Text value)
    {
        GameObject slot = new(name, typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(parent, false);
        GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(slot.transform, false);
        GameObject valueObject = new("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        valueObject.transform.SetParent(slot.transform, false);
        label = labelObject.GetComponent<TMP_Text>();
        value = valueObject.GetComponent<TMP_Text>();
        return slot;
    }

    private static void DisposeCatalog(EntityManager entityManager, Entity root)
    {
        CampaignMissionCatalogComponent catalog =
            entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);
        entityManager.SetComponentData(root, catalog);
    }
}
#endif
