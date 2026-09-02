#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
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

public sealed class M02EstablishBaseCampaignUiTests
{
    private const string M01 = "saga.ch01.m01.first_contact";
    private const string M02 = "saga.ch01.m02.establish_base";
    private const string M02Scenario = "scenario.ch01.m02.establish_base";
    private const string M02Map = "opmap.ch01.forward_post_01";
    private const string Barracks = "Building_Barrack";
    private const string Rifle = "Unit_Chr_Soldier_Male_02_Alt_04";
    private const string CampaignPrefab =
        "Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab";
    private const string BriefingPrefab =
        "Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab";

    [MenuItem("Game/Validation/Run M02 Establish Base Campaign UI Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseCampaignUiTests tests = new();
            tests.CanonicalBriefingProjectsEveryM02UiField();
            tests.LockedM02SelectionFailsClosed();
            tests.LegacyM01CompletionUnlocksAndDefaultsToM02();
            tests.UnlockedM02SelectionProjectsTheExactCardAndBriefing();
            tests.CampaignPrefabExposesTwoTypedMissionNodes();
            tests.CampaignCardShowsM02WithoutM01CopyOrRawKeys();
            tests.MissionBriefingShowsObjectivesResourcesRestrictionsMapAndThreeRewards();
            tests.ResolverOverridesM02CopyWithoutLeakingKeys();
            tests.ViewsAndBinderKeepSingleEventDrivenUiOwnership();
            tests.PlayableReviewCaptureSelectsExactM02Mission();
            Debug.Log("[M02EstablishBaseCampaignUiValidation] result=Passed tests=10");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBaseCampaignUiValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void PlayableReviewCaptureSelectsExactM02Mission()
    {
        Assert.That(MobileVisualQualityPlayModeCapture.ResolveCaptureMissionId("m02"), Is.EqualTo(M02));
        Assert.That(MobileVisualQualityPlayModeCapture.ResolveCaptureMissionId(M02), Is.EqualTo(M02));
        Assert.That(MobileVisualQualityPlayModeCapture.ResolveCaptureMissionId("m01"), Is.EqualTo(M01));
        Assert.Throws<InvalidOperationException>(() =>
            MobileVisualQualityPlayModeCapture.ResolveCaptureMissionId("mission-two"));
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Campaign UI Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(RunFocusedValidation);
            RunValidation(M01FirstContactCampaignUiTests.RunFocusedValidation);
            RunValidation(M01FirstContactMissionBriefingTests.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log("[M02EstablishBaseCampaignUiRegressionValidation] result=Passed suites=4");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBaseCampaignUiRegressionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void CanonicalBriefingProjectsEveryM02UiField()
    {
        using World world = ProjectChapter(out Entity root);
        ref CampaignMissionDefinitionBlob definition = ref FindDefinition(world.EntityManager, root, M02);
        UiCampaignOperationsComponent operations = new()
        {
            Version = 1,
            SelectedMissionId = new FixedString64Bytes(M02),
            ScenarioId = new FixedString64Bytes(M02Scenario),
            OperationMapId = new FixedString64Bytes(M02Map),
            Available = 1,
            LastAttemptOrdinal = -1
        };

        UiMissionBriefingComponent briefing = UiCampaignMissionProjectionSystem.ProjectBriefing(
            ref definition, in operations, false, default);
        Assert.That(briefing.MissionId.ToString(), Is.EqualTo(M02));
        Assert.That(briefing.OperationMapId.ToString(), Is.EqualTo(M02Map));
        Assert.That(briefing.StartingCredits, Is.EqualTo(55000));
        Assert.That(briefing.StartingMaterials, Is.EqualTo(120));
        Assert.That(briefing.AllowedBuildingConfigId.ToString(), Is.EqualTo(Barracks));
        Assert.That(briefing.AllowedBuildingCount, Is.EqualTo(1));
        Assert.That(briefing.BuildingDisabled, Is.Zero);
        Assert.That(briefing.ProductionDisabled, Is.Zero);
        Assert.That(briefing.TransportDisabled, Is.EqualTo(1));
        Assert.That(briefing.AirDisabled, Is.EqualTo(1));
        Assert.That(briefing.HostileUnitCount, Is.Zero);
        Assert.That(briefing.Objectives.Length, Is.EqualTo(2));
        Assert.That(CampaignMissionSpawnSystem.ShouldSpawnForceGroup(
            ref definition,
            definition.DelayedWaveUnitGroupId), Is.False,
            "M2 has no defense objective, so its reserved patrol data must not create runtime enemies.");
        Assert.That(briefing.Objectives[0].TargetConfigId.ToString(), Is.EqualTo(Barracks));
        Assert.That(briefing.Objectives[1].TargetConfigId.ToString(), Is.EqualTo(Rifle));
        Assert.That(briefing.Objectives[1].RequiredCount, Is.EqualTo(1),
            "One production order delivers the canonical four-soldier rifle squad.");
        Assert.That(briefing.Rewards[0].Amount, Is.EqualTo(320));
        Assert.That(briefing.Rewards[1].Amount, Is.EqualTo(1500));
        Assert.That(briefing.Rewards[2].Amount, Is.EqualTo(1));
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void LockedM02SelectionFailsClosed()
    {
        using ProjectionFixture fixture = CreateFixture(unlockM02: false);
        UpdateProjection(fixture.World);
        DynamicBuffer<UiCampaignMissionActionRequestElement> requests =
            fixture.World.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(fixture.UiRoot);
        requests.Add(new UiCampaignMissionActionRequestElement
        {
            Action = UiCampaignMissionActionKind.Select,
            MissionId = new FixedString64Bytes(M02)
        });
        UpdateProjection(fixture.World);
        Assert.That(fixture.World.EntityManager.GetComponentData<UiCampaignOperationsComponent>(fixture.UiRoot)
            .SelectedMissionId.ToString(), Is.EqualTo(M01));
    }

    [Test]
    public void UnlockedM02SelectionProjectsTheExactCardAndBriefing()
    {
        using ProjectionFixture fixture = CreateFixture(unlockM02: true);
        UpdateProjection(fixture.World);
        DynamicBuffer<UiCampaignMissionActionRequestElement> requests =
            fixture.World.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(fixture.UiRoot);
        requests.Add(new UiCampaignMissionActionRequestElement
        {
            Action = UiCampaignMissionActionKind.Select,
            MissionId = new FixedString64Bytes(M02)
        });
        UpdateProjection(fixture.World);

        UiCampaignOperationsComponent card =
            fixture.World.EntityManager.GetComponentData<UiCampaignOperationsComponent>(fixture.UiRoot);
        UiMissionBriefingComponent briefing = ReadBriefing(fixture.World.EntityManager);
        Assert.That(card.SelectedMissionId.ToString(), Is.EqualTo(M02));
        Assert.That(card.DisplayName.ToString(), Is.EqualTo("M02 - ESTABLISH THE BASE"));
        Assert.That(card.PrimaryAction, Is.EqualTo(UiCampaignMissionPrimaryActionKind.Start));
        Assert.That(briefing.MissionId.ToString(), Is.EqualTo(M02));
        Assert.That(briefing.OperationMapId.ToString(), Is.EqualTo(M02Map));
        Assert.That(briefing.Rewards.Length, Is.EqualTo(3));
    }

    [Test]
    public void LegacyM01CompletionUnlocksAndDefaultsToM02()
    {
        using ProjectionFixture fixture = CreateFixture(unlockM02: false);
        CampaignMissionProgressStore store = fixture.World.EntityManager
            .GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(fixture.Root).Store;
        Assert.That(store.Settle(M01, "legacy-m01-complete", 0, true, 3, 60000, null), Is.True);

        UpdateProjection(fixture.World);

        UiCampaignOperationsComponent card =
            fixture.World.EntityManager.GetComponentData<UiCampaignOperationsComponent>(fixture.UiRoot);
        CampaignMissionProgressSaveData m02 = store.ReadAll().Single(entry => entry.missionId == M02);
        Assert.That(m02.available, Is.True);
        Assert.That(card.SelectedMissionId.ToString(), Is.EqualTo(M02));
        Assert.That(card.PrimaryAction, Is.EqualTo(UiCampaignMissionPrimaryActionKind.Start));
        Assert.That(ReadBriefing(fixture.World.EntityManager).MissionId.ToString(), Is.EqualTo(M02));
    }

    [Test]
    public void CampaignPrefabExposesTwoTypedMissionNodes()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampaignPrefab);
        Assert.NotNull(prefab);
        CampaignOperationsScreenView view =
            prefab.GetComponentInChildren<CampaignOperationsScreenView>(true);
        Assert.NotNull(view);
        Assert.That(view.MissionNodeButtons, Has.Length.EqualTo(5));
        Assert.NotNull(view.MissionNodeButtons[0]);
        Assert.NotNull(view.MissionNodeButtons[1]);
        Assert.NotNull(prefab.GetComponentInChildren<CampaignMissionScreenBinder>(true));
    }

    [Test]
    public void CampaignCardShowsM02WithoutM01CopyOrRawKeys()
    {
        GameObject instance = UnityEngine.Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<GameObject>(CampaignPrefab));
        try
        {
            CampaignOperationsScreenView view =
                instance.GetComponentInChildren<CampaignOperationsScreenView>(true);
            view.Apply(M02CampaignModel());
            string text = AllText(instance);
            Assert.That(text, Does.Contain("MISSION SELECT"));
            Assert.That(text, Does.Contain("M02"));
            Assert.That(text, Does.Contain("ESTABLISH THE BASE"));
            Assert.That(text, Does.Contain("BUILD"));
            Assert.That(text, Does.Contain("BARRACK"));
            Assert.That(text, Does.Contain("PRODUCE"));
            Assert.That(text, Does.Contain("SQUAD"));
            Assert.That(text, Does.Contain("HOLD"));
            Assert.That(text, Does.Contain("PERIMETER"));
            Assert.That(text, Does.Contain("1,500 CREDITS"));
            Assert.That(text, Does.Contain("BARRACKS UNLOCK"));
            Assert.That(text, Does.Not.Contain("BLACKOUT AT SAHRIN"));
            Assert.That(text, Does.Not.Contain("RESTORE THE RELAY"));
            Assert.That(text, Does.Not.Contain("mission.m02"));
            Assert.That(view.MissionPreviewImage.texture.name,
                Does.Contain("scn13_operation_preview_sahrin_v02"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MissionBriefingShowsObjectivesResourcesRestrictionsMapAndThreeRewards()
    {
        GameObject instance = UnityEngine.Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<GameObject>(BriefingPrefab));
        try
        {
            MissionBriefingScreenView view =
                instance.GetComponentInChildren<MissionBriefingScreenView>(true);
            view.Apply(M02BriefingModel());
            string text = AllText(instance);
            Assert.That(text, Does.Contain("MISSION BRIEFING"));
            Assert.That(text, Does.Contain("M02"));
            Assert.That(text, Does.Contain("ESTABLISH THE BASE"));
            Assert.That(text, Does.Contain("RESTORE COMMAND POST"));
            Assert.That(text, Does.Contain("BUILD BARRACK"));
            Assert.That(text, Does.Contain("PRODUCE RIFLE SQUAD"));
            Assert.That(text, Does.Contain("HOLD PERIMETER"));
            Assert.That(text, Does.Contain("CIVILIAN RISK"));
            Assert.That(text, Does.Contain("INTEL CONFIDENCE"));
            Assert.That(text, Does.Contain("TUTORIAL CELL"));
            Assert.That(text, Does.Contain("LIGHT VEHICLES"));
            Assert.That(text, Does.Contain("AIR THREAT"));
            Assert.That(text, Does.Contain("+320"));
            Assert.That(text, Does.Contain("+1,500"));
            Assert.That(text, Does.Contain("BARRACK"));
            Assert.That(text, Does.Contain("UNLOCK"));
            Assert.That(text, Does.Contain("BUILD UNDER 5:00"));
            Assert.That(text, Does.Contain("NO BASE BREACH"));
            Assert.That(text, Does.Not.Contain("MISSION 01"));
            Assert.That(text, Does.Not.Contain("FIRST CONTACT"));
            Assert.That(text, Does.Not.Contain("OLD MARKET"));
            Assert.That(text, Does.Not.Contain("mission.m02"));
            Assert.That(view.MissionArtImage.texture.name, Does.Contain("SCN06_ForwardPost_V3"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ResolverOverridesM02CopyWithoutLeakingKeys()
    {
        GameObject instance = UnityEngine.Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<GameObject>(BriefingPrefab));
        try
        {
            MissionBriefingScreenView view =
                instance.GetComponentInChildren<MissionBriefingScreenView>(true);
            view.BindGameTextResolver(new DictionaryResolver(new Dictionary<string, string>
            {
                ["mission.m02.name"] = "LOCALIZED BASE",
                ["mission.m02.summary"] = "LOCALIZED SITUATION",
                ["mission.m02.location"] = "LOCALIZED FORWARD POST",
                ["mission.m02.objective.restore_command_post"] = "LOCALIZED RESTORE",
                ["mission.m02.objective.build_forward_barracks"] = "LOCALIZED BUILD",
                ["mission.m02.objective.produce_rifle_squad"] = "LOCALIZED PRODUCE",
                ["mission.m02.objective.defend_forward_post"] = "LOCALIZED DEFEND",
                ["mission.m02.resources.value"] = "LOCALIZED RESOURCES",
                ["mission.m02.restrictions.value"] = "LOCALIZED ACCESS",
                ["mission.m02.enemy_intel"] = "LOCALIZED INTEL",
                ["mission.reward.commander_xp"] = "LOCALIZED XP",
                ["mission.reward.credits"] = "LOCALIZED CREDITS",
                ["mission.m02.reward.barracks_unlock"] = "LOCALIZED UNLOCK"
            }));
            view.Apply(M02BriefingModel());
            string text = AllText(instance);
            Assert.That(text, Does.Contain("LOCALIZED BASE"));
            Assert.That(text, Does.Contain("LOCALIZED SITUATION"));
            Assert.That(text, Does.Contain("LOCALIZED RESTORE"));
            Assert.That(text, Does.Contain("LOCALIZED BUILD"));
            Assert.That(text, Does.Contain("LOCALIZED PRODUCE"));
            Assert.That(text, Does.Contain("LOCALIZED DEFEND"));
            Assert.That(text, Does.Contain("LOCALIZED INTEL"));
            Assert.That(text, Does.Contain("LOCALIZED XP"));
            Assert.That(text, Does.Contain("LOCALIZED CREDITS"));
            Assert.That(text, Does.Contain("LOCALIZED UNLOCK"));
            Assert.That(text, Does.Not.Contain("mission.m02"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ViewsAndBinderKeepSingleEventDrivenUiOwnership()
    {
        Assert.That(typeof(CampaignMissionScreenBinder).GetMethod(
            "Update", System.Reflection.BindingFlags.Instance |
                      System.Reflection.BindingFlags.Public |
                      System.Reflection.BindingFlags.NonPublic), Is.Null);
        string sources = File.ReadAllText("Assets/Game/Scripts/UI/Screens/CampaignMissionScreenBinder.cs") +
                         File.ReadAllText("Assets/Game/Scripts/UI/Screens/CampaignOperationsScreenView.cs") +
                         File.ReadAllText("Assets/Game/Scripts/UI/Screens/MissionBriefingScreenView.cs");
        Assert.That(sources, Does.Not.Contain("GameObject.Find"));
        Assert.That(sources, Does.Not.Contain("Resources.Load"));
        Assert.That(sources, Does.Not.Contain("AssetDatabase"));
    }

    private static UiCampaignOperationsModel M02CampaignModel()
    {
        UiCampaignMissionModel mission = new(
            M02, M02Scenario, M02Map, "M02 - ESTABLISH THE BASE",
            true, false, false, 0, 0, 0,
            UiCampaignMissionPrimaryActionKind.Start, "START OPERATION");
        return new UiCampaignOperationsModel(1, 1, 1, mission, string.Empty, false);
    }

    private static UiMissionBriefingModel M02BriefingModel() => new(
        1, M02, M02Scenario, M02Map,
        "mission.m02.name", "mission.m02.summary", "mission.m02.location",
        new[]
        {
            new UiMissionObjectiveModel(
                "obj.ch01.m02.build_forward_barracks",
                "mission.m02.objective.build_forward_barracks", string.Empty, Barracks,
                UiMissionObjectiveRuleKind.BuildStructure, 1, false),
            new UiMissionObjectiveModel(
                "obj.ch01.m02.produce_rifle_squad",
                "mission.m02.objective.produce_rifle_squad", string.Empty, Rifle,
                UiMissionObjectiveRuleKind.ProduceUnit, 1, false),
            new UiMissionObjectiveModel(
                "obj.ch01.m02.defend_forward_post",
                "mission.m02.objective.defend_forward_post", "role.friendly.forward_post", string.Empty,
                UiMissionObjectiveRuleKind.DefendMissionRole, 1, true)
        },
        new[]
        {
            new UiMissionRewardModel(UiMissionRewardKind.None, "reward.commander_xp",
                "mission.reward.commander_xp", 320),
            new UiMissionRewardModel(UiMissionRewardKind.Credits, string.Empty,
                "mission.reward.credits", 1500),
            new UiMissionRewardModel(UiMissionRewardKind.None, "reward.ch01.m02.production_unlock",
                "mission.m02.reward.barracks_unlock", 1)
        },
        3, 55000, 120, Barracks, 1,
        buildingDisabled: false, productionDisabled: false, economyDisabled: false,
        transportDisabled: true, airDisabled: true,
        replay: false, replayAllowed: true, replayTutorialEnabled: false,
        replayTutorialToggleVisible: false, deployQueued: false);

    private static ProjectionFixture CreateFixture(bool unlockM02)
    {
        World world = ProjectChapter(out Entity root);
        string saveRoot = Path.Combine(
            Path.GetTempPath(), "WarlineCapture", "M02CampaignUiTests", Guid.NewGuid().ToString("N"));
        CampaignMissionProgressStore store = new(new SaveService(new JsonSaveRepository(saveRoot)));
        if (unlockM02)
            Assert.That(store.Settle(M01, "m01-ui-unlock", 0, true, 3, 60000, M02), Is.True);
        world.EntityManager.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store = store;
        Entity uiRoot = world.EntityManager.CreateEntity(typeof(UiShellRootComponent));
        world.EntityManager.AddBuffer<UiShellRouteRequestComponent>(uiRoot);
        return new ProjectionFixture(world, root, uiRoot, saveRoot);
    }

    private static World ProjectChapter(out Entity root)
    {
        World world = new("m02-campaign-ui");
        MissionDefinitionCatalogConfig missions = AssetDatabase.LoadAssetAtPath<MissionDefinitionCatalogConfig>(
            "Assets/Game/Configs/Campaign/CampaignMissionCatalog.asset");
        OperationMapCatalogConfig maps = AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(
            "Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset");
        Assert.That(CampaignMissionCatalogProjection.TryProject(
            world.EntityManager, missions, maps, 23, out root, out string error), Is.True, error);
        return world;
    }

    private static ref CampaignMissionDefinitionBlob FindDefinition(
        EntityManager manager, Entity root, string missionId)
    {
        CampaignMissionCatalogComponent catalog = manager.GetComponentData<CampaignMissionCatalogComponent>(root);
        for (int index = 0; index < catalog.Blob.Value.Missions.Length; index++)
            if (catalog.Blob.Value.Missions[index].MissionId.Equals(new FixedString64Bytes(missionId)))
                return ref catalog.Blob.Value.Missions[index];
        throw new InvalidOperationException($"Missing mission definition '{missionId}'.");
    }

    private static void UpdateProjection(World world)
    {
        SystemHandle handle = world.CreateSystem<UiCampaignMissionProjectionSystem>();
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        ref UiCampaignMissionProjectionSystem system = ref
            world.Unmanaged.GetUnsafeSystemRef<UiCampaignMissionProjectionSystem>(handle);
        system.OnUpdate(ref state);
        state.Dependency.Complete();
        world.EntityManager.CompleteAllTrackedJobs();
        world.DestroySystem(handle);
    }

    private static UiMissionBriefingComponent ReadBriefing(EntityManager manager)
    {
        using EntityQuery query = manager.CreateEntityQuery(
            ComponentType.ReadOnly<UiMissionBriefingComponent>());
        Assert.That(query.CalculateEntityCount(), Is.EqualTo(1));
        return query.GetSingleton<UiMissionBriefingComponent>();
    }

    private static string AllText(GameObject root) => string.Join(
        "\n", root.GetComponentsInChildren<TMP_Text>(true).Select(text => text.text));

    private static void DisposeCatalog(EntityManager manager, Entity root)
    {
        if (!manager.Exists(root) || !manager.HasComponent<CampaignMissionCatalogComponent>(root))
            return;
        CampaignMissionCatalogComponent catalog = manager.GetComponentData<CampaignMissionCatalogComponent>(root);
        if (catalog.Blob.IsCreated)
            catalog.Blob.Dispose();
        catalog.Blob = default;
        catalog.OwnsBlob = 0;
        manager.SetComponentData(root, catalog);
    }

    private static void RunValidation(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            validation();
        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
    }

    private sealed class DictionaryResolver : IGameTextResolver
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public DictionaryResolver(IReadOnlyDictionary<string, string> values) => _values = values;

        public string Get(string key, string fallback = "") =>
            _values.TryGetValue(key ?? string.Empty, out string value) ? value : fallback ?? string.Empty;

        public bool TryGet(string key, out string value) =>
            _values.TryGetValue(key ?? string.Empty, out value);

        public string Format(string key, string fallback, params object[] args) =>
            string.Format(Get(key, fallback), args ?? Array.Empty<object>());
    }

    private sealed class ProjectionFixture : IDisposable
    {
        public ProjectionFixture(World world, Entity root, Entity uiRoot, string saveRoot)
        {
            World = world;
            Root = root;
            UiRoot = uiRoot;
            SaveRoot = saveRoot;
        }

        public World World { get; }
        public Entity Root { get; }
        public Entity UiRoot { get; }
        private string SaveRoot { get; }

        public void Dispose()
        {
            DisposeCatalog(World.EntityManager, Root);
            World.Dispose();
            if (Directory.Exists(SaveRoot))
                Directory.Delete(SaveRoot, true);
        }
    }
}
#endif
