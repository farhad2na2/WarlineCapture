#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Components;
using Game.Composition;
using Game.Configs;
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

public static class M01FirstContactMissionBriefingTests
{
    private const string Marker = "[M01FirstContactMissionBriefingValidation] result=Passed tests=11 captures=3";
    private const string MissionPath = "Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M01_FirstContact.asset";
    private const string ScenarioPath = "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M01_FirstContact.asset";
    private const string MapCatalogPath = "Assets/Game/Configs/OperationMaps/Chapter01/OperationMapCatalog_Chapter01.asset";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(CatalogProjectsCanonicalBriefingAuthority, ref passed);
            Run(FirstClearBriefingUsesCanonicalRewardsAndNoReplayToggle, ref passed);
            Run(ReplayBriefingUsesReplayRewardAndDefaultOffToggle, ref passed);
            Run(CurrentGuidanceMapsIntoCampaignPayload, ref passed);
            Run(DuplicateDeployClicksPublishExactlyOneLaunch, ref passed);
            Run(TerminalLaunchClearsQueuedStateForRetry, ref passed);
            Run(ReplayToggleActionIsTypedAndValueBearing, ref passed);
            Run(GatewayReadsProjectedBriefing, ref passed);
            Run(PrefabIsBoundAndContainsNoLegacyPlaceholderAuthority, ref passed);
            Run(ViewAppliesCanonicalFirstClearAndReplayCaptures, ref passed);
            Run(BinderHasNoFramePollingOrDefinitionLoading, ref passed);
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[M01FirstContactMissionBriefingValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    [Test] public static void CatalogProjectsCanonicalBriefingAuthority()
    {
        using World world = Project(out Entity root);
        CampaignMissionCatalogComponent catalog = world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        ref CampaignMissionDefinitionBlob mission = ref catalog.Blob.Value.Missions[0];
        Assert.That(mission.DisplayNameKey.ToString(), Is.EqualTo("mission.m01.name"));
        Assert.That(mission.DisplaySummaryKey.ToString(), Is.EqualTo("mission.m01.summary"));
        Assert.That(mission.LocationNameKey.ToString(), Is.EqualTo("mission.m01.location"));
        Assert.That(mission.Objectives.Length, Is.EqualTo(2));
        Assert.That(mission.Objectives[0].Rule, Is.EqualTo(MissionObjectiveRuleKind.DestroyMissionRole));
        Assert.That(mission.Objectives[0].RequiredCount, Is.EqualTo(3));
        Assert.That(mission.Objectives[1].FailureOnRuleBreak, Is.EqualTo(1));
        Assert.That(mission.FirstClearRewards.Length, Is.EqualTo(2));
        Assert.That(mission.FirstClearRewards[0].Amount, Is.EqualTo(260));
        Assert.That(mission.FirstClearRewards[1].Amount, Is.EqualTo(1200));
        Assert.That(mission.ReplayRewards.Length, Is.EqualTo(1));
        Assert.That(mission.ReplayRewards[0].Amount, Is.EqualTo(250));
        DisposeCatalog(world.EntityManager, root);
    }

    [Test] public static void FirstClearBriefingUsesCanonicalRewardsAndNoReplayToggle()
    {
        using World world = Project(out Entity root);
        ref CampaignMissionDefinitionBlob mission = ref Definition(world.EntityManager, root);
        UiCampaignOperationsComponent operations = Operations(completed: false);
        UiMissionBriefingComponent briefing = UiCampaignMissionProjectionSystem.ProjectBriefing(
            ref mission, in operations, true, default);
        Assert.That(briefing.Replay, Is.Zero);
        Assert.That(briefing.ReplayTutorialEnabled, Is.Zero);
        Assert.That(briefing.ReplayTutorialToggleVisible, Is.Zero);
        Assert.That(briefing.Rewards.Length, Is.EqualTo(2));
        Assert.That(briefing.Rewards[0].Amount, Is.EqualTo(260));
        Assert.That(briefing.Rewards[1].Amount, Is.EqualTo(1200));
        Assert.That(briefing.HostileUnitCount, Is.EqualTo(3));
        DisposeCatalog(world.EntityManager, root);
    }

    [Test] public static void ReplayBriefingUsesReplayRewardAndDefaultOffToggle()
    {
        using World world = Project(out Entity root);
        ref CampaignMissionDefinitionBlob mission = ref Definition(world.EntityManager, root);
        UiCampaignOperationsComponent operations = Operations(completed: true);
        UiMissionBriefingComponent briefing = UiCampaignMissionProjectionSystem.ProjectBriefing(
            ref mission, in operations, false, default);
        Assert.That(briefing.Replay, Is.EqualTo(1));
        Assert.That(briefing.ReplayTutorialToggleVisible, Is.EqualTo(1));
        Assert.That(briefing.ReplayTutorialEnabled, Is.Zero);
        Assert.That(briefing.Rewards.Length, Is.EqualTo(1));
        Assert.That(briefing.Rewards[0].Amount, Is.EqualTo(250));
        DisposeCatalog(world.EntityManager, root);
    }

    [Test] public static void CurrentGuidanceMapsIntoCampaignPayload()
    {
        using World world = CreateProjectionWorld(out Entity missionRoot, out Entity uiRoot);
        Entity settings = world.EntityManager.CreateEntity(typeof(AssistantSettingsComponent));
        world.EntityManager.SetComponentData(settings, new AssistantSettingsComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.HintsOnly
        });
        UpdateProjection(world);
        world.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(uiRoot).Add(new UiCampaignMissionActionRequestElement
        {
            Action = UiCampaignMissionActionKind.Deploy,
            MissionId = new FixedString64Bytes(UiCampaignMissionProjectionSystem.M01MissionId)
        });
        UpdateProjection(world);
        DynamicBuffer<CampaignMissionLaunchRequestElement> launches =
            world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot);
        Assert.That(launches.Length, Is.EqualTo(1));
        Assert.That(launches[0].LaunchOrigin, Is.EqualTo(MissionLaunchOriginKind.CampaignOperations));
        Assert.That(launches[0].RunKind, Is.EqualTo(MissionRunKind.FirstClear));
        Assert.That(launches[0].Guidance, Is.EqualTo(NarrativeGuidanceMode.Contextual));
        Assert.That(launches[0].ReplayTutorialEnabled, Is.Zero);
        Assert.That(launches[0].AttemptOrdinal, Is.Zero);
        DisposeCatalog(world.EntityManager, missionRoot);
    }

    [Test] public static void DuplicateDeployClicksPublishExactlyOneLaunch()
    {
        using World world = CreateProjectionWorld(out Entity missionRoot, out Entity uiRoot);
        UpdateProjection(world);
        DynamicBuffer<UiCampaignMissionActionRequestElement> actions =
            world.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(uiRoot);
        UiCampaignMissionActionRequestElement deploy = new()
        {
            Action = UiCampaignMissionActionKind.Deploy,
            MissionId = new FixedString64Bytes(UiCampaignMissionProjectionSystem.M01MissionId)
        };
        actions.Add(deploy);
        actions.Add(deploy);
        UpdateProjection(world);
        Assert.That(world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot).Length, Is.EqualTo(1));
        Assert.That(world.EntityManager.GetComponentData<UiMissionBriefingComponent>(uiRoot).DeployQueued, Is.EqualTo(1));
        DynamicBuffer<UiShellRouteRequestComponent> routes =
            world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(uiRoot);
        Assert.That(routes.Length, Is.EqualTo(1));
        Assert.That(routes[0].Intent, Is.EqualTo(UiShellRouteIntent.EnterMatch));
        Assert.That(routes[0].Route, Is.EqualTo(UIRoute.Match));
        Assert.That(routes[0].PushHistory, Is.Zero);
        DisposeCatalog(world.EntityManager, missionRoot);
    }

    [Test] public static void TerminalLaunchClearsQueuedStateForRetry()
    {
        using World world = CreateProjectionWorld(out Entity missionRoot, out Entity uiRoot);
        UpdateProjection(world);
        DynamicBuffer<UiCampaignMissionActionRequestElement> actions =
            world.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(uiRoot);
        actions.Add(new UiCampaignMissionActionRequestElement
        {
            Action = UiCampaignMissionActionKind.Deploy,
            MissionId = new FixedString64Bytes(UiCampaignMissionProjectionSystem.M01MissionId)
        });
        UpdateProjection(world);
        UiMissionBriefingComponent briefing =
            world.EntityManager.GetComponentData<UiMissionBriefingComponent>(uiRoot);
        Assert.That(briefing.DeployQueued, Is.EqualTo(1));
        Assert.That(briefing.DeployTransitionToken, Is.EqualTo(1UL));

        world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot).Clear();
        CampaignMissionLaunchQueueComponent queue =
            world.EntityManager.GetComponentData<CampaignMissionLaunchQueueComponent>(missionRoot);
        queue.LastTransitionToken = briefing.DeployTransitionToken;
        world.EntityManager.SetComponentData(missionRoot, queue);
        UpdateProjection(world);
        briefing = world.EntityManager.GetComponentData<UiMissionBriefingComponent>(uiRoot);
        Assert.That(briefing.DeployQueued, Is.Zero);

        actions = world.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(uiRoot);
        actions.Add(new UiCampaignMissionActionRequestElement
        {
            Action = UiCampaignMissionActionKind.Deploy,
            MissionId = new FixedString64Bytes(UiCampaignMissionProjectionSystem.M01MissionId)
        });
        UpdateProjection(world);
        briefing = world.EntityManager.GetComponentData<UiMissionBriefingComponent>(uiRoot);
        Assert.That(briefing.DeployTransitionToken, Is.EqualTo(2UL));
        world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot).Clear();
        world.EntityManager.GetBuffer<CampaignMissionLaunchResultElement>(missionRoot).Add(
            new CampaignMissionLaunchResultElement
            {
                TransitionToken = briefing.DeployTransitionToken,
                Accepted = 0,
                ReasonCode = new FixedString64Bytes("readiness-failed")
            });
        UpdateProjection(world);
        Assert.That(world.EntityManager.GetComponentData<UiMissionBriefingComponent>(uiRoot).DeployQueued, Is.Zero);
        DisposeCatalog(world.EntityManager, missionRoot);
    }

    [Test] public static void ReplayToggleActionIsTypedAndValueBearing()
    {
        World prior = World.DefaultGameObjectInjectionWorld;
        using World world = new("m01-briefing-toggle-gateway");
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            Entity uiRoot = world.EntityManager.CreateEntity(
                typeof(UiShellRootComponent), typeof(UiCampaignOperationsComponent));
            world.EntityManager.SetComponentData(uiRoot, Operations(completed: true));
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            Assert.That(UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                UiCampaignMissionActionKind.SetReplayTutorial,
                UiCampaignMissionProjectionSystem.M01MissionId, true), Is.True);
            UiCampaignMissionActionRequestElement request =
                world.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(uiRoot)[0];
            Assert.That(request.Action, Is.EqualTo(UiCampaignMissionActionKind.SetReplayTutorial));
            Assert.That(request.Value, Is.EqualTo(1));
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            World.DefaultGameObjectInjectionWorld = prior;
        }
    }

    [Test] public static void GatewayReadsProjectedBriefing()
    {
        World prior = World.DefaultGameObjectInjectionWorld;
        using World world = CreateProjectionWorld(out Entity missionRoot, out _);
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UpdateProjection(world);
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            Assert.That(UiShellRuntimeGateway.TryReadMissionBriefing(out UiMissionBriefingModel briefing), Is.True);
            Assert.That(briefing.MissionId, Is.EqualTo(UiCampaignMissionProjectionSystem.M01MissionId));
            Assert.That(briefing.Objectives.Length, Is.EqualTo(2));
            Assert.That(briefing.Rewards.Select(reward => reward.Amount), Is.EquivalentTo(new[] { 260, 1200 }));
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            World.DefaultGameObjectInjectionWorld = prior;
            DisposeCatalog(world.EntityManager, missionRoot);
        }
    }

    [Test] public static void PrefabIsBoundAndContainsNoLegacyPlaceholderAuthority()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);
        Assert.NotNull(prefab.GetComponent<MissionBriefingScreenView>());
        Assert.NotNull(prefab.GetComponent<CampaignMissionScreenBinder>());
        string allText = string.Join("\n", prefab.GetComponentsInChildren<TMP_Text>(true).Select(text => text.text));
        Assert.That(allText, Does.Not.Contain("BLACKOUT AT SAHRIN"));
        Assert.That(allText, Does.Not.Contain("RESTORE THE RELAY"));
        Assert.That(allText, Does.Not.Contain("2,500"));
        Assert.That(allText, Does.Not.Contain("+1,200"));
        Assert.That(prefab.GetComponent<MissionBriefingScreenView>().DeployOperationButton.interactable, Is.True);
    }

    [Test] public static void BinderHasNoFramePollingOrDefinitionLoading()
    {
        Assert.That(typeof(CampaignMissionScreenBinder).GetMethod(
            "Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null);
        string binder = System.IO.File.ReadAllText("Assets/Game/Scripts/UI/Screens/CampaignMissionScreenBinder.cs");
        string view = System.IO.File.ReadAllText("Assets/Game/Scripts/UI/Screens/MissionBriefingScreenView.cs");
        Assert.That(binder + view, Does.Not.Contain("AssetDatabase"));
        Assert.That(binder + view, Does.Not.Contain("Resources.Load"));
        Assert.That(binder + view, Does.Not.Contain("GameObject.Find"));
    }

    [Test] public static void ViewAppliesCanonicalFirstClearAndReplayCaptures()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            MissionBriefingScreenView view = instance.GetComponent<MissionBriefingScreenView>();
            Assert.NotNull(view);
            UiMissionObjectiveModel[] objectives =
            {
                new("obj.ch01.m01.destroy_patrol", "mission.m01.objective.destroy_patrol",
                    "hostile_patrol", UiMissionObjectiveRuleKind.DestroyMissionRole, 3, false),
                new("obj.ch01.m01.protect_command", "mission.m01.objective.protect_command",
                    "command_squad", UiMissionObjectiveRuleKind.ProtectMissionRole, 1, true)
            };
            UiMissionBriefingModel firstClear = Briefing(
                objectives,
                new[]
                {
                    new UiMissionRewardModel(UiMissionRewardKind.None, "reward.commander_xp",
                        "mission.m01.reward.commander_xp", 260),
                    new UiMissionRewardModel(UiMissionRewardKind.Credits, string.Empty,
                        "mission.m01.reward.credits", 1200)
                },
                replay: false);
            view.Apply(firstClear);
            Assert.That(view.ReplayTutorialToggle.gameObject.activeSelf, Is.False);
            string firstClearText = AllText(instance);
            Assert.That(firstClearText, Does.Contain("DESTROY THE HOSTILE PATROL (3)"));
            Assert.That(firstClearText, Does.Contain("KEEP THE COMMAND SQUAD ALIVE"));
            Assert.That(firstClearText, Does.Contain("Old Market, Sahrin"));
            Assert.That(firstClearText, Does.Contain("COMMANDER XP"));
            Assert.That(firstClearText, Does.Contain("+1,200"));
            Assert.That(firstClearText, Does.Not.Contain("mission.m01"));
            Assert.That(firstClearText, Does.Not.Contain("role.hostile"));
            Assert.That(firstClearText, Does.Not.Contain("role.friendly"));
            Assert.That(firstClearText, Does.Not.Contain("reward.commander"));
            Capture(instance, 1280, 720, "first_clear_16x9");

            UiMissionBriefingModel replay = Briefing(
                objectives,
                new[]
                {
                    new UiMissionRewardModel(UiMissionRewardKind.Credits, string.Empty,
                        "mission.m01.reward.replay_credits", 250)
                },
                replay: true);
            view.Apply(replay);
            Assert.That(view.ReplayTutorialToggle.gameObject.activeSelf, Is.True);
            Assert.That(view.ReplayTutorialToggle.isOn, Is.False);
            Assert.That(AllText(instance), Does.Contain("+250"));
            Assert.That(AllText(instance), Does.Not.Contain("+1,200"));
            Capture(instance, 1000, 450, "replay_20x9");
            Capture(instance, 1024, 768, "replay_tablet4x3");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static UiMissionBriefingModel Briefing(
        UiMissionObjectiveModel[] objectives, UiMissionRewardModel[] rewards, bool replay) => new(
        1,
        UiCampaignMissionProjectionSystem.M01MissionId,
        "scenario.ch01.m01.first_contact",
        "opmap.ch01.district_edge_01",
        "mission.m01.name",
        "mission.m01.summary",
        "mission.m01.location",
        objectives,
        rewards,
        3,
        buildingDisabled: true,
        productionDisabled: true,
        economyDisabled: true,
        transportDisabled: true,
        airDisabled: true,
        replay,
        replayAllowed: true,
        replayTutorialEnabled: false,
        replayTutorialToggleVisible: replay,
        deployQueued: false);

    private static string AllText(GameObject root) => string.Join(
        "\n", root.GetComponentsInChildren<TMP_Text>(true).Select(text => text.text));

    private static void Capture(GameObject instance, int width, int height, string name)
    {
        GameObject cameraObject = new("M01DC030CaptureCamera", typeof(Camera));
        GameObject canvasObject = new(
            "M01DC030CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        RenderTexture target = null;
        Texture2D image = null;
        try
        {
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.aspect = width / (float)height;
            camera.orthographicSize = Mathf.Max(1080f, 2400f / camera.aspect);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(4800f, 2160f);
            canvasRect.position = Vector3.zero;
            instance.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture prior = RenderTexture.active;
            RenderTexture.active = target;
            image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = prior;
            string directory = Path.Combine("Build", "EditorEvidence", "M01FirstContact", "M01DC030");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"briefing_{name}.png");
            byte[] bytes = image.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[M01DC030Capture] aspect={name} size={width}x{height} sha256={Sha256(bytes)} path={path}");
        }
        finally
        {
            instance.transform.SetParent(null, false);
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            if (target != null) RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static string Sha256(byte[] bytes)
    {
        using SHA256 algorithm = SHA256.Create();
        byte[] digest = algorithm.ComputeHash(bytes);
        return string.Concat(digest.Select(value => value.ToString("x2")));
    }

    private static World Project(out Entity root)
    {
        World world = new("m01-briefing-catalog");
        MissionDefinitionConfig mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(MissionPath);
        ScenarioSetupConfig scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(ScenarioPath);
        OperationMapCatalogConfig maps = AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(MapCatalogPath);
        Assert.That(CampaignMissionCatalogProjection.TryProject(
            world.EntityManager, mission, scenario, maps, 30, out root, out string error), Is.True, error);
        return world;
    }

    private static World CreateProjectionWorld(out Entity missionRoot, out Entity uiRoot)
    {
        World world = Project(out missionRoot);
        string saveRoot = Path.Combine(
            Path.GetTempPath(), "WarlineCapture", "M01MissionBriefingTests", Guid.NewGuid().ToString("N"));
        CampaignMissionProgressStoreReferenceComponent progressStore = world.EntityManager
            .GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(missionRoot);
        progressStore.Store = new CampaignMissionProgressStore(
            new SaveService(new JsonSaveRepository(saveRoot)));
        world.EntityManager.AddComponentObject(missionRoot, new TestProgressStoreRootComponent
        {
            Root = saveRoot
        });
        uiRoot = world.EntityManager.CreateEntity(typeof(UiShellRootComponent));
        world.EntityManager.AddBuffer<UiShellRouteRequestComponent>(uiRoot);
        return world;
    }

    private static ref CampaignMissionDefinitionBlob Definition(EntityManager manager, Entity root)
    {
        CampaignMissionCatalogComponent catalog = manager.GetComponentData<CampaignMissionCatalogComponent>(root);
        return ref catalog.Blob.Value.Missions[0];
    }

    private static UiCampaignOperationsComponent Operations(bool completed) => new()
    {
        Version = 1,
        SelectedMissionId = new FixedString64Bytes(UiCampaignMissionProjectionSystem.M01MissionId),
        ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
        OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
        Available = 1,
        FirstClearCompleted = completed ? (byte)1 : (byte)0,
        LastAttemptOrdinal = completed ? 0 : -1
    };

    private static void UpdateProjection(World world)
    {
        SystemHandle handle = world.CreateSystem<UiCampaignMissionProjectionSystem>();
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        ref UiCampaignMissionProjectionSystem system = ref world.Unmanaged.GetUnsafeSystemRef<UiCampaignMissionProjectionSystem>(handle);
        system.OnUpdate(ref state);
        state.Dependency.Complete();
        world.EntityManager.CompleteAllTrackedJobs();
        world.DestroySystem(handle);
    }

    private static void DisposeCatalog(EntityManager manager, Entity root)
    {
        CampaignMissionCatalogComponent catalog = manager.GetComponentData<CampaignMissionCatalogComponent>(root);
        if (catalog.Blob.IsCreated) catalog.Blob.Dispose();
        catalog.Blob = default;
        catalog.OwnsBlob = 0;
        manager.SetComponentData(root, catalog);
        if (manager.HasComponent<TestProgressStoreRootComponent>(root))
        {
            string saveRoot = manager.GetComponentObject<TestProgressStoreRootComponent>(root).Root;
            if (!string.IsNullOrWhiteSpace(saveRoot) && Directory.Exists(saveRoot))
                Directory.Delete(saveRoot, true);
        }
    }

    private sealed class TestProgressStoreRootComponent : IComponentData
    {
        public string Root;
    }

    private static void Run(Action test, ref int passed)
    {
        test();
        passed++;
    }
}
#endif
