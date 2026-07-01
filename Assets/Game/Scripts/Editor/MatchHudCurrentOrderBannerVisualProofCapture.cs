#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MatchHudCurrentOrderBannerVisualProofCapture
{
    public const string OutputFolder = "Design/VisualLockLayered/_MatchHudCurrentOrderBanner";
    public const string ContactSheetPath = OutputFolder + "/current_order_banner_contact_sheet.png";
    private const string MatchHudContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const int CaptureWidth = 1920;
    private const int CaptureHeight = 1080;
    private static readonly Vector2 ReferenceResolution = new(4800f, 2160f);

    private static readonly ProofCase[] ProofCases =
    {
        new("hidden_start", "Hidden at match start", TacticalCommandMode.None, null, false),
        new("move_armed", "Move armed", TacticalCommandMode.Move, null, false),
        new("attack_armed", "Attack armed", TacticalCommandMode.Attack, null, false),
        new("hold_accepted", "Hold accepted", TacticalCommandMode.Hold, "Holding current position.", true),
        new("stop_accepted", "Stop accepted", TacticalCommandMode.Stop, "Stopped selected units.", true),
        new("scan_armed", "Scan armed", TacticalCommandMode.Scan, null, false),
        new("board_armed", "Board armed", TacticalCommandMode.Board, null, false),
        new("build_armed", "Build armed", TacticalCommandMode.Build, null, false),
        new("no_selection_rejected", "No selection rejected", TacticalCommandMode.Move, "Select units or a building first.", false)
    };

    [MenuItem("Warline Capture/UI/Capture Current Order Banner Visual Proof")]
    public static void CaptureVisualProof()
    {
        try
        {
            string[] paths = CaptureProofSet();
            string contactSheet = CreateContactSheet(paths);
            Debug.Log($"[MatchHudCurrentOrderBannerVisualProof] result=Passed contactSheet={contactSheet}");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MatchHudCurrentOrderBannerVisualProof] result=Failed\n{exception}");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }

    private static string[] CaptureProofSet()
    {
        Directory.CreateDirectory(OutputFolder);
        string[] paths = new string[ProofCases.Length];
        for (int i = 0; i < ProofCases.Length; i++)
            paths[i] = CaptureCase(ProofCases[i]);
        return paths;
    }

    private static string CaptureCase(ProofCase proofCase)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera camera = CreateCamera();
        Canvas canvas = CreateCanvas(camera);
        BattleHudRuntimeFeedbackView feedbackView = InstantiateHud(canvas.transform);

        ApplyProofState(feedbackView, proofCase);
        Canvas.ForceUpdateCanvases();

        string path = Path.Combine(OutputFolder, proofCase.FileName + ".png");
        CaptureCamera(camera, path);
        return path;
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("CurrentOrderBannerProofCamera");
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
        GameObject canvasObject = new("CurrentOrderBannerProofCanvas");
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
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Require(instance != null, $"Could not instantiate Match HUD content prefab at {MatchHudContentPrefabPath}.");
        instance.transform.SetParent(parent, false);

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

    private static void ApplyProofState(BattleHudRuntimeFeedbackView feedbackView, ProofCase proofCase)
    {
        BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(feedbackView);
        if (proofCase.CommandMode == TacticalCommandMode.None)
            return;

        if (proofCase.IsAcceptedResult)
        {
            feedbackView.CurrentCommandMode = proofCase.CommandMode;
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
                feedbackView,
                TacticalCommandResult.Success(proofCase.ResultMessage));
            return;
        }

        if (proofCase.FileName == "no_selection_rejected")
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(
                feedbackView,
                TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return;
        }

        if (proofCase.CommandMode == TacticalCommandMode.Board)
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyBoardCommandMode(
                feedbackView,
                UiBoardCommandModeDirection.PassengerToTransport,
                boardAllInteractable: false);
            return;
        }

        BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(feedbackView, proofCase.CommandMode);
    }

    private static void CaptureCamera(Camera camera, string path)
    {
        RenderTexture target = new(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4,
            name = "CurrentOrderBannerProofTarget"
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
            Object.DestroyImmediate(target);
            if (texture != null)
                Object.DestroyImmediate(texture);
        }
    }

    private static string CreateContactSheet(string[] paths)
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
            Object.DestroyImmediate(source);
        }

        contact.Apply(false, false);
        File.WriteAllBytes(ContactSheetPath, contact.EncodeToPNG());
        Object.DestroyImmediate(contact);
        return ContactSheetPath;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private readonly struct ProofCase
    {
        public ProofCase(
            string fileName,
            string label,
            TacticalCommandMode commandMode,
            string resultMessage,
            bool isAcceptedResult)
        {
            FileName = fileName;
            Label = label;
            CommandMode = commandMode;
            ResultMessage = resultMessage;
            IsAcceptedResult = isAcceptedResult;
        }

        public string FileName { get; }
        public string Label { get; }
        public TacticalCommandMode CommandMode { get; }
        public string ResultMessage { get; }
        public bool IsAcceptedResult { get; }
    }
}
#endif
