using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class M01FirstContactHudResultTests
{
    private const string Marker = "[M01FirstContactHudResultValidation] result=Passed tests=11 captures=3";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(ResultModelCarriesAuthoritativeFacts, ref passed);
            Run(ContinueRequiresAcceptedSettlement, ref passed);
            Run(ContinueAdvancesToOwnedDestination, ref passed);
            Run(ContinueAdvancesReplayToCampaignReturn, ref passed);
            Run(RetryQueuesOneCorrelatedAttempt, ref passed);
            Run(GatewayRejectsDuplicateResultInput, ref passed);
            Run(HudViewDoesNotOwnElapsedTime, ref passed);
            Run(ContinueQueuesReturnToMainMenu, ref passed);
            Run(ResultGatewayFormatsOnlyAuthoritativeOutcomeRewards, ref passed);
            Run(PrefabsCarryProductionBindings, ref passed);
            Run(ResultPopupCapturesSupportedAspects, ref passed);
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[M01FirstContactHudResultValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    [Test] public static void ResultModelCarriesAuthoritativeFacts()
    {
        UiMissionResultPopupModel model = new(9, "saga.ch01.m01.first_contact",
            UiMissionResultOutcome.Victory, "VICTORY", "FIRST CONTACT", "Secure.", 3,
            "03:14", "0", "3/3", "300 Credits", "CONTINUE", true, false);
        Assert.That(model.Version, Is.EqualTo(9));
        Assert.That(model.Stars, Is.EqualTo(3));
        Assert.That(model.ElapsedText, Is.EqualTo("03:14"));
        Assert.That(model.PrimaryActionEnabled, Is.True);
    }

    [Test] public static void ContinueRequiresAcceptedSettlement()
    {
        using World world = CreateResultWorld(MissionOutcomeKind.Victory, out Entity root);
        AddAction(world.EntityManager, root, MissionActionKind.Continue);
        Assert.That(CampaignMissionRuntimeSystem.TryConsumeAction(world.EntityManager, root), Is.True);
        Assert.That(world.EntityManager.GetBuffer<CampaignMissionActionResultElement>(root)[0].Accepted, Is.Zero);
        Assert.That(world.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase,
            Is.EqualTo(MissionPhaseKind.Result));
    }

    [Test] public static void ContinueAdvancesToOwnedDestination()
    {
        using World world = CreateResultWorld(MissionOutcomeKind.Victory, out Entity root);
        world.EntityManager.GetBuffer<CampaignMissionSettlementResultElement>(root).Add(new()
        {
            SourceVersion = 8, SessionToken = new FixedString64Bytes("session.m01"), Accepted = 1,
            FirstClear = 1
        });
        AddAction(world.EntityManager, root, MissionActionKind.Continue);
        Assert.That(CampaignMissionRuntimeSystem.TryConsumeAction(world.EntityManager, root), Is.True);
        Assert.That(world.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase,
            Is.EqualTo(MissionPhaseKind.DebriefFirstClear));
    }

    [Test] public static void RetryQueuesOneCorrelatedAttempt()
    {
        using World world = CreateResultWorld(MissionOutcomeKind.Defeat, out Entity root);
        AddAction(world.EntityManager, root, MissionActionKind.Retry);
        Assert.That(CampaignMissionRuntimeSystem.TryConsumeAction(world.EntityManager, root), Is.True);
        DynamicBuffer<CampaignMissionLaunchRequestElement> launches =
            world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(root);
        Assert.That(launches.Length, Is.EqualTo(1));
        Assert.That(launches[0].RunKind, Is.EqualTo(MissionRunKind.Retry));
        Assert.That(launches[0].AttemptOrdinal, Is.EqualTo(2));
        Assert.That(launches[0].TransitionToken, Is.EqualTo(78));
    }

    [Test] public static void ContinueAdvancesReplayToCampaignReturn()
    {
        using World world = CreateResultWorld(MissionOutcomeKind.Victory, out Entity root);
        CampaignMissionRuntimeComponent runtime =
            world.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
        runtime.RunKind = MissionRunKind.Replay;
        runtime.ReturnDestination = MissionReturnDestinationKind.CampaignOperations;
        world.EntityManager.SetComponentData(root, runtime);
        world.EntityManager.GetBuffer<CampaignMissionSettlementResultElement>(root).Add(new()
        {
            SourceVersion = 8, SessionToken = new FixedString64Bytes("session.m01"), Accepted = 1
        });
        AddAction(world.EntityManager, root, MissionActionKind.Continue);
        Assert.That(CampaignMissionRuntimeSystem.TryConsumeAction(world.EntityManager, root), Is.True);
        Assert.That(world.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase,
            Is.EqualTo(MissionPhaseKind.ReturnReplay));
    }

    [Test] public static void GatewayRejectsDuplicateResultInput()
    {
        World previous = World.DefaultGameObjectInjectionWorld;
        using World world = CreateResultWorld(MissionOutcomeKind.Defeat, out Entity root);
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            Assert.That(UiShellEcsGateway.TryEnqueueMissionResultAction(UiMissionResultActionKind.Retry), Is.True);
            Assert.That(UiShellEcsGateway.TryEnqueueMissionResultAction(UiMissionResultActionKind.Retry), Is.False);
            Assert.That(world.EntityManager.GetBuffer<CampaignMissionActionRequestElement>(root).Length,
                Is.EqualTo(1));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previous;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test] public static void HudViewDoesNotOwnElapsedTime()
    {
        string source = File.ReadAllText("Assets/Game/Scripts/UI/Components/MatchHudObjectivesElapsedView.cs");
        StringAssert.DoesNotContain("Time.deltaTime", source);
        StringAssert.Contains("TryReadMatchHudStatusSurfaces", source);
    }

    [Test] public static void ContinueQueuesReturnToMainMenu()
    {
        string source = File.ReadAllText(
            "Assets/Game/Scripts/UI/Screens/CampaignMissionHudResultBinder.cs");
        StringAssert.Contains("UiShellRouteIntent.ReturnToMainMenu", source);
        StringAssert.Contains("UIRoute.MainMenu", source);
        StringAssert.Contains("action == UiMissionResultActionKind.Continue", source);
    }

    [Test] public static void PrefabsCarryProductionBindings()
    {
        GameObject result = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab");
        GameObject canvas = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab");
        Assert.NotNull(result.GetComponent<MissionResultPopupView>());
        RectTransform resultRect = result.GetComponent<RectTransform>();
        Assert.That(resultRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(resultRect.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(resultRect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(resultRect.sizeDelta, Is.EqualTo(Vector2.zero),
            "A stretch-anchored popup must not add its authored reference resolution to the live screen.");
        Assert.NotNull(canvas.GetComponent<CampaignMissionHudResultBinder>());
        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(canvas), Is.Zero);
    }

    [Test] public static void ResultGatewayFormatsOnlyAuthoritativeOutcomeRewards()
    {
        string source = File.ReadAllText(
            "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.Core.cs");
        StringAssert.Contains("victory ? BuildMissionRewardText(ref rewards) : \"NO REWARD\"", source);
        StringAssert.Contains("? \"COMMANDER XP\"", source);
        StringAssert.Contains("settlementAccepted != 0 && settlementFirstClear != 0", source);
    }

    [Test] public static void ResultPopupCapturesSupportedAspects()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab");
        Assert.NotNull(prefab);
        CaptureResult(prefab, 1280, 720, VictoryModel(), "victory_16x9");
        CaptureResult(prefab, 1000, 450, DefeatModel(), "defeat_20x9");
        CaptureResult(prefab, 1024, 768, VictoryModel(), "victory_tablet4x3");
    }

    private static World CreateResultWorld(MissionOutcomeKind outcome, out Entity root)
    {
        World world = new("M01 HUD result tests");
        EntityManager entityManager = world.EntityManager;
        root = entityManager.CreateEntity(typeof(CampaignMissionRootComponent));
        entityManager.AddComponentData(root, new CampaignMissionRuntimeComponent
        {
            MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
            ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
            OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
            SessionToken = new FixedString64Bytes("session.m01"),
            Phase = MissionPhaseKind.Result,
            Outcome = outcome,
            LaunchOrigin = MissionLaunchOriginKind.FirstLaunch,
            RunKind = MissionRunKind.FirstClear,
            ReturnDestination = outcome == MissionOutcomeKind.Victory
                ? MissionReturnDestinationKind.CommandBase : MissionReturnDestinationKind.CampaignOperations,
            TransitionToken = 77,
            Version = 8,
            SourceVersion = 3,
            AttemptOrdinal = 1,
            DeterministicSeed = 120031
        });
        entityManager.AddBuffer<CampaignMissionActionRequestElement>(root);
        entityManager.AddBuffer<CampaignMissionActionResultElement>(root);
        entityManager.AddBuffer<CampaignMissionLaunchRequestElement>(root);
        entityManager.AddBuffer<CampaignMissionSettlementResultElement>(root);
        return world;
    }

    private static void AddAction(EntityManager entityManager, Entity root, MissionActionKind action)
    {
        entityManager.GetBuffer<CampaignMissionActionRequestElement>(root).Add(new()
        {
            Action = action, TransitionToken = 77, SessionToken = new FixedString64Bytes("session.m01"),
            AttemptOrdinal = 1
        });
    }

    private static UiMissionResultPopupModel VictoryModel() => new(
        9, "saga.ch01.m01.first_contact", UiMissionResultOutcome.Victory,
        "VICTORY", "FIRST CONTACT • OLD MARKET", "The hostile patrol is neutralized. The corridor is secure.",
        3, "03:14", "0", "3 / 3", "1,200 CREDITS  •  260 COMMANDER XP", "CONTINUE", true, false);

    private static UiMissionResultPopupModel DefeatModel() => new(
        10, "saga.ch01.m01.first_contact", UiMissionResultOutcome.Loss,
        "MISSION FAILED", "FIRST CONTACT • OLD MARKET", "The command squad was lost. Regroup and redeploy.",
        0, "01:05", "1", "1 / 3", "NO REWARD", "RETRY", true, true);

    private static void CaptureResult(
        GameObject prefab, int width, int height, UiMissionResultPopupModel model, string name)
    {
        GameObject cameraObject = new("M01DC031CaptureCamera", typeof(Camera));
        GameObject canvasObject = new(
            "M01DC031CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        GameObject instance = null;
        RenderTexture target = null;
        Texture2D image = null;
        try
        {
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.aspect = width / (float)height;
            camera.orthographicSize = 540f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1080f * camera.aspect, 1080f);
            canvasRect.position = Vector3.zero;

            instance = UnityEngine.Object.Instantiate(prefab, canvasObject.transform, false);
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            MissionResultPopupView view = instance.GetComponent<MissionResultPopupView>();
            Assert.NotNull(view);
            view.Apply(in model);
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

            string directory = Path.Combine("Build", "EditorEvidence", "M01FirstContact", "M01DC031");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"result_{name}.png");
            byte[] bytes = image.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[M01DC031Capture] aspect={name} size={width}x{height} sha256={Sha256(bytes)} path={path}");
        }
        finally
        {
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            if (target != null) RenderTexture.ReleaseTemporary(target);
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static string Sha256(byte[] bytes)
    {
        using SHA256 algorithm = SHA256.Create();
        return string.Concat(algorithm.ComputeHash(bytes).Select(value => value.ToString("x2")));
    }

    private static void Run(Action test, ref int passed) { test(); passed++; }
}
