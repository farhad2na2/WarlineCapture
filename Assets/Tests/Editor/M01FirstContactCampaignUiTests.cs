using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Game.Components;
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

public sealed class M01FirstContactCampaignUiTests
{
    private const string Marker = "[M01FirstContactCampaignUiValidation] result=Passed tests=10 captures=3";
    private const string M01 = UiCampaignMissionProjectionSystem.M01MissionId;
    private const string M02 = UiCampaignMissionProjectionSystem.M02MissionId;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(NewProfileProjectsAvailableM01, ref passed);
            Run(ExplicitLockFailsClosed, ref passed);
            Run(PendingResumeProjectsContinue, ref passed);
            Run(FirstClearProjectsReplayAndRevealsM02, ref passed);
            Run(ReplayProjectsBestMetrics, ref passed);
            Run(UnchangedSourcesDoNotChurnVersion, ref passed);
            Run(GatewayReadsAuthoritativeProjection, ref passed);
            Run(GatewayRejectsLockedAndEnqueuesAvailableAction, ref passed);
            Run(BinderHasNoFramePollingLoop, ref passed);
            Run(ViewAppliesProfilesAndCapturesSupportedAspects, ref passed);
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[M01FirstContactCampaignUiValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    [Test] public static void NewProfileProjectsAvailableM01()
    {
        UiCampaignOperationsComponent model = Project(Array.Empty<CampaignMissionProgressSaveData>());
        Assert.That(model.Available, Is.EqualTo(1));
        Assert.That(model.PrimaryAction, Is.EqualTo(UiCampaignMissionPrimaryActionKind.Start));
        Assert.That(model.BestStars, Is.Zero);
        Assert.That(model.NextMissionRevealed, Is.Zero);
    }

    [Test] public static void ExplicitLockFailsClosed()
    {
        UiCampaignOperationsComponent model = Project(new[] { Entry(M01, false) });
        Assert.That(model.Available, Is.Zero);
        Assert.That(model.PrimaryAction, Is.EqualTo(UiCampaignMissionPrimaryActionKind.Locked));
    }

    [Test] public static void PendingResumeProjectsContinue()
    {
        CampaignMissionProgressSaveData entry = Entry(M01, true);
        entry.pendingResume = true;
        UiCampaignOperationsComponent model = Project(new[] { entry });
        Assert.That(model.PendingResume, Is.EqualTo(1));
        Assert.That(model.PrimaryAction, Is.EqualTo(UiCampaignMissionPrimaryActionKind.Continue));
    }

    [Test] public static void FirstClearProjectsReplayAndRevealsM02()
    {
        CampaignMissionProgressSaveData entry = Entry(M01, true);
        entry.firstClearCompleted = true;
        entry.bestStars = 2;
        UiCampaignOperationsComponent model = Project(new[] { entry, Entry(M02, true) });
        Assert.That(model.FirstClearCompleted, Is.EqualTo(1));
        Assert.That(model.PrimaryAction, Is.EqualTo(UiCampaignMissionPrimaryActionKind.Replay));
        Assert.That(model.NextMissionRevealed, Is.EqualTo(1));
    }

    [Test] public static void ReplayProjectsBestMetrics()
    {
        CampaignMissionProgressSaveData entry = Entry(M01, true);
        entry.firstClearCompleted = true;
        entry.bestStars = 3;
        entry.bestCompletionMilliseconds = 75432;
        entry.successfulReplayCount = 4;
        UiCampaignOperationsComponent model = Project(new[] { entry });
        Assert.That(model.BestStars, Is.EqualTo(3));
        Assert.That(model.BestCompletionMilliseconds, Is.EqualTo(75432));
        Assert.That(model.SuccessfulReplayCount, Is.EqualTo(4));
    }

    [Test] public static void UnchangedSourcesDoNotChurnVersion()
    {
        CampaignMissionProgressSaveData[] progress = { Entry(M01, true) };
        UiCampaignOperationsComponent first = Project(progress);
        UiCampaignOperationsComponent second = Project(progress, first);
        Assert.That(second.Version, Is.EqualTo(first.Version));
        progress[0].bestStars = 1;
        Assert.That(Project(progress, second).Version, Is.EqualTo(second.Version + 1));
    }

    [Test] public static void GatewayReadsAuthoritativeProjection()
    {
        WithGateway(Project(Array.Empty<CampaignMissionProgressSaveData>()), (world, root) =>
        {
            Assert.That(UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel model), Is.True);
            Assert.That(model.SelectedMission.MissionId, Is.EqualTo(M01));
            Assert.That(model.SelectedMission.PrimaryAction, Is.EqualTo(UiCampaignMissionPrimaryActionKind.Start));
        });
    }

    [Test] public static void GatewayRejectsLockedAndEnqueuesAvailableAction()
    {
        WithGateway(Project(new[] { Entry(M01, false) }), (world, root) =>
            Assert.That(UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                UiCampaignMissionActionKind.OpenBriefing, M01), Is.False));
        WithGateway(Project(Array.Empty<CampaignMissionProgressSaveData>()), (world, root) =>
        {
            Assert.That(UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(
                UiCampaignMissionActionKind.OpenBriefing, M01), Is.True);
            DynamicBuffer<UiCampaignMissionActionRequestElement> requests =
                world.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(root);
            Assert.That(requests.Length, Is.EqualTo(1));
            Assert.That(requests[0].Action, Is.EqualTo(UiCampaignMissionActionKind.OpenBriefing));
        });
    }

    [Test] public static void BinderHasNoFramePollingLoop()
    {
        Assert.That(typeof(CampaignMissionScreenBinder).GetMethod(
            "Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null);
        Assert.That(typeof(CampaignOperationsScreenView).GetFields(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
            Has.None.Matches<FieldInfo>(field => field.FieldType == typeof(UiCampaignOperationsModel)));
    }

    [Test] public static void ViewAppliesProfilesAndCapturesSupportedAspects()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab");
        Assert.NotNull(prefab);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            CampaignOperationsScreenView view =
                instance.GetComponentInChildren<CampaignOperationsScreenView>(true);
            Assert.NotNull(view);
            Assert.NotNull(instance.GetComponentInChildren<CampaignMissionScreenBinder>(true));
            UiCampaignOperationsModel model = ToContract(Project(new[]
            {
                new CampaignMissionProgressSaveData
                {
                    missionId = M01, available = true, firstClearCompleted = true,
                    bestStars = 3, bestCompletionMilliseconds = 75432, successfulReplayCount = 2
                },
                Entry(M02, true)
            }));
            view.Apply(model);
            Assert.That(view.LaunchMissionButton.interactable, Is.True);
            Assert.That(view.MissionName.text, Is.EqualTo("FIRST CONTACT"));
            Assert.That(view.LaunchMissionLabel.text, Is.EqualTo("START BRIEFING"));
            Capture(instance, 1280, 720, "16x9");
            Capture(instance, 1000, 450, "20x9");
            Capture(instance, 1024, 768, "tablet4x3");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static UiCampaignOperationsComponent Project(
        CampaignMissionProgressSaveData[] progress, UiCampaignOperationsComponent current = default)
    {
        return UiCampaignMissionProjectionSystem.Project(
            9, 0, new FixedString64Bytes(M01),
            new FixedString64Bytes("scenario.ch01.m01.first_contact"),
            new FixedString64Bytes("opmap.ch01.district_edge_01"), progress, in current);
    }

    private static CampaignMissionProgressSaveData Entry(string missionId, bool available) => new()
    {
        missionId = missionId,
        available = available
    };

    private static UiCampaignOperationsModel ToContract(UiCampaignOperationsComponent component)
    {
        UiCampaignMissionModel mission = new(
            component.SelectedMissionId.ToString(), component.ScenarioId.ToString(),
            component.OperationMapId.ToString(), component.DisplayName.ToString(), component.Available != 0,
            component.FirstClearCompleted != 0, component.PendingResume != 0, component.BestStars,
            component.BestCompletionMilliseconds, component.SuccessfulReplayCount,
            component.PrimaryAction, component.PrimaryActionLabel.ToString());
        return new UiCampaignOperationsModel(
            component.Version, component.CatalogSourceVersion, component.ProgressSourceVersion,
            mission, component.NextMissionId.ToString(), component.NextMissionRevealed != 0);
    }

    private static void WithGateway(
        UiCampaignOperationsComponent component, Action<World, Entity> test)
    {
        World prior = World.DefaultGameObjectInjectionWorld;
        using World world = new("M01CampaignUiGatewayTests");
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            Entity root = world.EntityManager.CreateEntity(
                typeof(UiShellRootComponent), typeof(UiCampaignOperationsComponent));
            world.EntityManager.SetComponentData(root, component);
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            test(world, root);
        }
        finally
        {
            UiShellRuntimeGateway.Register(null);
            World.DefaultGameObjectInjectionWorld = prior;
        }
    }

    private static void Capture(GameObject instance, int width, int height, string name)
    {
        GameObject cameraObject = new("M01DC029CaptureCamera", typeof(Camera));
        GameObject canvasObject = new("M01DC029CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
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
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(4800f, 2160f);
            canvasRect.position = Vector3.zero;
            instance.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
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
            string directory = Path.Combine("Build", "EditorEvidence", "M01FirstContact", "M01DC029");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"campaign_{name}.png");
            byte[] bytes = image.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[M01DC029Capture] aspect={name} size={width}x{height} sha256={Sha256(bytes)} path={path}");
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
        char[] text = new char[digest.Length * 2];
        const string hex = "0123456789abcdef";
        for (int index = 0; index < digest.Length; index++)
        {
            text[index * 2] = hex[digest[index] >> 4];
            text[index * 2 + 1] = hex[digest[index] & 15];
        }
        return new string(text);
    }

    private static void Run(Action test, ref int passed)
    {
        test();
        passed++;
        Debug.Log($"[M01FirstContactCampaignUiValidation] passed={test.Method.Name}");
    }
}
