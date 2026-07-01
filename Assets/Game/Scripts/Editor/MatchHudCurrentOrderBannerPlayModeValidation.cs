#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MatchHudCurrentOrderBannerPlayModeValidation
{
    private const string MatchHudContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string DefaultArtifactDirectory = "Design/VisualLockLayered/_MatchHudCurrentOrderBanner/playmode";
    private const int CaptureWidth = 1920;
    private const int CaptureHeight = 1080;
    private static readonly Vector2 ReferenceResolution = new(4800f, 2160f);

    private static readonly ValidationCase[] Cases =
    {
        ValidationCase.Hidden("hidden_start"),
        ValidationCase.CommandMode("move_armed", TacticalCommandMode.Move, "MOVE ORDER", "Select a destination."),
        ValidationCase.Accepted("move_accepted", TacticalCommandMode.Move, "Move order accepted.", "MOVE ORDER", "Units moving to target."),
        ValidationCase.CommandMode("attack_armed", TacticalCommandMode.Attack, "ATTACK ORDER", "Select an enemy target."),
        ValidationCase.Accepted("attack_accepted", TacticalCommandMode.Attack, "Attack order accepted.", "ATTACK ORDER", "Engaging target."),
        ValidationCase.Accepted("hold_accepted", TacticalCommandMode.Hold, "Holding current position.", "HOLD POSITION", "Selected units holding ground."),
        ValidationCase.Accepted("stop_accepted", TacticalCommandMode.Stop, "Stopped selected units.", "STOP ORDER", "Selected units clearing orders."),
        ValidationCase.CommandMode("scan_armed", TacticalCommandMode.Scan, "SCAN ORDER", "Select an area to scan."),
        ValidationCase.Accepted("scan_accepted", TacticalCommandMode.Scan, "Scan order accepted.", "SCAN ORDER", "Recon sweep in progress."),
        ValidationCase.Board("board_armed", "Select a transport."),
        ValidationCase.Accepted("board_accepted", TacticalCommandMode.Board, "Boarding transport.", "BOARD ORDER", "Boarding transport."),
        ValidationCase.CommandMode("build_armed", TacticalCommandMode.Build, "BUILD ORDER", "Place structure on valid terrain."),
        ValidationCase.Rejected("no_selection_rejected")
    };

    private static string artifactDirectory;
    private static int pendingExitCode = int.MinValue;

    public static void RunPlayModeProof()
    {
        try
        {
            artifactDirectory = Environment.GetEnvironmentVariable("WARLINE_CURRENT_ORDER_BANNER_PLAYMODE_DIR");
            if (string.IsNullOrWhiteSpace(artifactDirectory))
                artifactDirectory = DefaultArtifactDirectory;
            Directory.CreateDirectory(artifactDirectory);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MatchHudCurrentOrderBannerPlayModeValidation] result=Failed\n{exception}");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        try
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RunCasesInPlayMode();
                pendingExitCode = 0;
                EditorApplication.ExitPlaymode();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode && pendingExitCode != int.MinValue)
            {
                int exitCode = pendingExitCode;
                pendingExitCode = int.MinValue;
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                if (Application.isBatchMode)
                    EditorApplication.Exit(exitCode);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MatchHudCurrentOrderBannerPlayModeValidation] result=Failed\n{exception}");
            pendingExitCode = 1;
            EditorApplication.ExitPlaymode();
        }
    }

    private static void RunCasesInPlayMode()
    {
        Camera camera = CreateCamera();
        Canvas canvas = CreateCanvas(camera);
        BattleHudRuntimeFeedbackView feedbackView = InstantiateHud(canvas.transform);

        bool graphicsCaptureAvailable = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
        string[] proofPaths = new string[Cases.Length];
        for (int i = 0; i < Cases.Length; i++)
            proofPaths[i] = RunCase(feedbackView, camera, Cases[i], graphicsCaptureAvailable);

        if (graphicsCaptureAvailable)
            CreateContactSheet(proofPaths);

        Debug.Log($"[MatchHudCurrentOrderBannerPlayModeValidation] result=Passed cases={Cases.Length} artifacts={artifactDirectory}");
    }

    private static string RunCase(
        BattleHudRuntimeFeedbackView feedbackView,
        Camera camera,
        ValidationCase validationCase,
        bool graphicsCaptureAvailable)
    {
        BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(feedbackView);
        feedbackView.TickFeedbackLifetime(999f);
        ApplyCase(feedbackView, validationCase);
        Canvas.ForceUpdateCanvases();

        AssertCase(feedbackView.CurrentOrderBanner, validationCase);

        string path = Path.Combine(
            artifactDirectory,
            graphicsCaptureAvailable ? validationCase.FileName + ".png" : validationCase.FileName + ".txt");
        if (graphicsCaptureAvailable)
            CaptureCamera(camera, path);
        else
            File.WriteAllText(path, DescribeCase(feedbackView.CurrentOrderBanner, validationCase));

        return path;
    }

    private static void ApplyCase(BattleHudRuntimeFeedbackView feedbackView, ValidationCase validationCase)
    {
        switch (validationCase.Kind)
        {
            case ValidationCaseKind.Hidden:
                return;
            case ValidationCaseKind.CommandMode:
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(feedbackView, validationCase.Mode);
                return;
            case ValidationCaseKind.Accepted:
                feedbackView.CurrentCommandMode = validationCase.Mode;
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
                    feedbackView,
                    TacticalCommandResult.Success(validationCase.ResultMessage));
                return;
            case ValidationCaseKind.Board:
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyBoardCommandMode(
                    feedbackView,
                    UiBoardCommandModeDirection.PassengerToTransport,
                    boardAllInteractable: false);
                return;
            case ValidationCaseKind.Rejected:
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
                    feedbackView,
                    TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void AssertCase(MatchHudCurrentOrderBannerView banner, ValidationCase validationCase)
    {
        Require(banner != null, "CurrentOrderBanner view is missing.");
        bool expectedVisible = validationCase.Kind != ValidationCaseKind.Hidden &&
                               validationCase.Kind != ValidationCaseKind.Rejected;
        Require(banner.BannerRoot != null, "CurrentOrderBanner root reference is missing.");
        Require(banner.BannerRoot.activeSelf == expectedVisible, $"{validationCase.FileName}: unexpected banner visibility.");
        Require(banner.Chevrons != null, "CurrentOrderBanner chevrons reference is missing.");
        Require(banner.Chevrons.activeSelf == expectedVisible, $"{validationCase.FileName}: unexpected chevrons visibility.");

        if (!expectedVisible)
        {
            Require(string.IsNullOrEmpty(banner.OrderText.text), $"{validationCase.FileName}: hidden banner has stale order text.");
            Require(string.IsNullOrEmpty(banner.DescriptionText.text), $"{validationCase.FileName}: hidden banner has stale description text.");
            Require(banner.Icon == null || !banner.Icon.enabled, $"{validationCase.FileName}: hidden banner icon is still enabled.");
            return;
        }

        Require(banner.OrderText != null, "CurrentOrderBanner order text reference is missing.");
        Require(banner.DescriptionText != null, "CurrentOrderBanner description text reference is missing.");
        Require(banner.Icon != null, "CurrentOrderBanner icon reference is missing.");
        Require(banner.Icon.enabled, $"{validationCase.FileName}: banner icon is disabled.");
        Require(banner.Icon.sprite != null, $"{validationCase.FileName}: banner icon sprite is missing.");
        Require(banner.Icon.preserveAspect, $"{validationCase.FileName}: banner icon must preserve aspect.");
        Require(banner.OrderText.text == validationCase.ExpectedOrderText, $"{validationCase.FileName}: order text '{banner.OrderText.text}' did not match '{validationCase.ExpectedOrderText}'.");
        Require(banner.DescriptionText.text == validationCase.ExpectedDescriptionText, $"{validationCase.FileName}: description text '{banner.DescriptionText.text}' did not match '{validationCase.ExpectedDescriptionText}'.");
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("CurrentOrderBannerPlayModeCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.028f, 0.032f, 0.038f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = CaptureHeight * 0.5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;
        return camera;
    }

    private static Canvas CreateCanvas(Camera camera)
    {
        GameObject canvasObject = new("CurrentOrderBannerPlayModeCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static BattleHudRuntimeFeedbackView InstantiateHud(Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchHudContentPrefabPath);
        Require(prefab != null, $"Missing Match HUD content prefab at {MatchHudContentPrefabPath}.");
        GameObject instance = Object.Instantiate(prefab, parent, false);
        Require(instance != null, $"Could not instantiate Match HUD content prefab at {MatchHudContentPrefabPath}.");

        RectTransform rect = instance.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        BattleHudRuntimeFeedbackView feedbackView = instance.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
        Require(feedbackView != null, "Instantiated Match HUD content has no BattleHudRuntimeFeedbackView.");
        return feedbackView;
    }

    private static void CaptureCamera(Camera camera, string path)
    {
        RenderTexture target = new(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4,
            name = "CurrentOrderBannerPlayModeTarget"
        };
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();

            texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release();
            Object.Destroy(target);
            if (texture != null)
                Object.Destroy(texture);
        }
    }

    private static void CreateContactSheet(string[] paths)
    {
        const int Columns = 3;
        int rows = Mathf.CeilToInt(paths.Length / (float)Columns);
        Texture2D contact = new(CaptureWidth * Columns, CaptureHeight * rows, TextureFormat.RGBA32, false);
        Color32 background = new(11, 13, 16, 255);
        Color32[] pixels = new Color32[contact.width * contact.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = background;
        contact.SetPixels32(pixels);

        for (int i = 0; i < paths.Length; i++)
        {
            byte[] bytes = File.ReadAllBytes(paths[i]);
            Texture2D source = new(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(bytes);
            int column = i % Columns;
            int row = i / Columns;
            contact.SetPixels(
                column * CaptureWidth,
                (rows - row - 1) * CaptureHeight,
                CaptureWidth,
                CaptureHeight,
                source.GetPixels());
            Object.Destroy(source);
        }

        contact.Apply(false, false);
        File.WriteAllBytes(Path.Combine(artifactDirectory, "current_order_banner_playmode_contact_sheet.png"), contact.EncodeToPNG());
        Object.Destroy(contact);
    }

    private static string DescribeCase(MatchHudCurrentOrderBannerView banner, ValidationCase validationCase)
    {
        return $"{validationCase.FileName}: visible={banner.BannerRoot.activeSelf}, order='{banner.OrderText.text}', description='{banner.DescriptionText.text}'";
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private enum ValidationCaseKind
    {
        Hidden,
        CommandMode,
        Accepted,
        Board,
        Rejected
    }

    private readonly struct ValidationCase
    {
        private ValidationCase(
            string fileName,
            ValidationCaseKind kind,
            TacticalCommandMode mode,
            string resultMessage,
            string expectedOrderText,
            string expectedDescriptionText)
        {
            FileName = fileName;
            Kind = kind;
            Mode = mode;
            ResultMessage = resultMessage;
            ExpectedOrderText = expectedOrderText;
            ExpectedDescriptionText = expectedDescriptionText;
        }

        public string FileName { get; }
        public ValidationCaseKind Kind { get; }
        public TacticalCommandMode Mode { get; }
        public string ResultMessage { get; }
        public string ExpectedOrderText { get; }
        public string ExpectedDescriptionText { get; }

        public static ValidationCase Hidden(string fileName)
        {
            return new ValidationCase(fileName, ValidationCaseKind.Hidden, TacticalCommandMode.None, null, string.Empty, string.Empty);
        }

        public static ValidationCase CommandMode(
            string fileName,
            TacticalCommandMode mode,
            string expectedOrderText,
            string expectedDescriptionText)
        {
            return new ValidationCase(fileName, ValidationCaseKind.CommandMode, mode, null, expectedOrderText, expectedDescriptionText);
        }

        public static ValidationCase Accepted(
            string fileName,
            TacticalCommandMode mode,
            string resultMessage,
            string expectedOrderText,
            string expectedDescriptionText)
        {
            return new ValidationCase(fileName, ValidationCaseKind.Accepted, mode, resultMessage, expectedOrderText, expectedDescriptionText);
        }

        public static ValidationCase Board(string fileName, string expectedDescriptionText)
        {
            return new ValidationCase(fileName, ValidationCaseKind.Board, TacticalCommandMode.Board, null, "BOARD ORDER", expectedDescriptionText);
        }

        public static ValidationCase Rejected(string fileName)
        {
            return new ValidationCase(fileName, ValidationCaseKind.Rejected, TacticalCommandMode.Move, null, string.Empty, string.Empty);
        }
    }
}
#endif
