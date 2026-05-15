#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class WarlineCaptureM01RuntimeVisualMatchProofCapture
{
    private const string MatchOverlayPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab";
    private const string CapturePath = "Design/AgentReports/Captures/M01-01_RuntimeCapture_1920x1080.png";
    private const string GameSceneCapturePath = "Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_1920x1080.png";
    private const string GameSceneCaptureV2Path = "Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v2_1920x1080.png";
    private const string GameSceneCaptureV3Path = "Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v3_1920x1080.png";
    private const string GameScenePath = "Assets/Game/Scenes/Game.unity";
    private const string DefinitionPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset";
    private const int CaptureWidth = 1920;
    private const int CaptureHeight = 1080;
    private static double gameSceneCaptureStartTime;
    private static int gameSceneCaptureFrameCount;
    private static bool gameSceneCaptureRequested;
    private static bool gameFlowCaptureRequested;
    private static bool gameFlowLaunchTriggered;

    public static void Capture()
    {
        WarlineCaptureMissionSession.BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera camera = BuildRuntimeWorld();
        GameObject hud = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MatchOverlayPrefabPath));
        hud.name = "Screen_MatchOverlay_RuntimeProof";
        M01InfantryOnlyHudScopeController scope = hud.GetComponent<M01InfantryOnlyHudScopeController>();
        if (scope != null)
            scope.Refresh();

        Canvas[] canvases = hud.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = camera;
            canvases[i].planeDistance = 1f;
        }

        CaptureFrame(camera, CapturePath);
        WarlineCaptureMissionSession.Clear();
        Debug.Log($"WARLINECAPTURE_M01_RUNTIME_VISUAL_MATCH_CAPTURED path={CapturePath}");
    }

    public static void CaptureGameScenePlayMode()
    {
        gameSceneCaptureRequested = true;
        gameSceneCaptureStartTime = EditorApplication.timeSinceStartup;
        gameSceneCaptureFrameCount = 0;
        WarlineCaptureMissionSession.BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);
        EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        EditorApplication.playModeStateChanged += OnPlayModeStateChangedForGameSceneCapture;
        EditorApplication.update += PollGameSceneCaptureTimeout;
        EditorApplication.EnterPlaymode();
    }

    public static void CaptureGameSceneViaExistingFlow()
    {
        gameSceneCaptureRequested = true;
        gameFlowCaptureRequested = true;
        gameFlowLaunchTriggered = false;
        gameSceneCaptureStartTime = EditorApplication.timeSinceStartup;
        gameSceneCaptureFrameCount = 0;
        WarlineCaptureMissionSession.Clear();
        EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        EditorApplication.playModeStateChanged += OnPlayModeStateChangedForGameSceneCapture;
        EditorApplication.update += PollGameSceneCaptureTimeout;
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChangedForGameSceneCapture(PlayModeStateChange state)
    {
        if (!gameSceneCaptureRequested || state != PlayModeStateChange.EnteredPlayMode)
            return;

        Screen.SetResolution(CaptureWidth, CaptureHeight, false);
        EditorApplication.update += gameFlowCaptureRequested
            ? CaptureGameSceneViaFlowWhenReady
            : CaptureGameSceneWhenReady;
    }

    private static void CaptureGameSceneViaFlowWhenReady()
    {
        if (!gameSceneCaptureRequested || !EditorApplication.isPlaying)
            return;

        gameSceneCaptureFrameCount++;
        if (!gameFlowLaunchTriggered)
        {
            if (gameSceneCaptureFrameCount < 45)
                return;

            if (!TryLaunchM01ThroughExistingFlow())
                return;

            gameFlowLaunchTriggered = true;
            gameSceneCaptureFrameCount = 0;
            return;
        }

        bool runtimeReady = TryEnsureM01RuntimeInitialized(out Chapter01M01PlayableRuntime.RuntimeState runtimeState);
        if (!runtimeReady || gameSceneCaptureFrameCount < 180)
            return;

        EditorApplication.update -= CaptureGameSceneViaFlowWhenReady;
        bool captured = CaptureLoadedGameSceneCamera(GameSceneCaptureV3Path);
        Debug.Log(captured
            ? $"WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path={GameSceneCaptureV3Path} player={runtimeState.PlayerSquad} enemy={runtimeState.EnemyPatrol}"
            : $"WARLINECAPTURE_M01_GAME_FLOW_CAPTURE_FAILED path={GameSceneCaptureV3Path}");
        CleanupGameSceneCaptureRequest();
        EditorApplication.ExitPlaymode();
        EditorApplication.Exit(captured ? 0 : 1);
    }

    private static void CaptureGameSceneWhenReady()
    {
        if (!gameSceneCaptureRequested || !EditorApplication.isPlaying)
            return;

        gameSceneCaptureFrameCount++;
        if (gameSceneCaptureFrameCount < 180)
            return;

        EditorApplication.update -= CaptureGameSceneWhenReady;
        bool captured = CaptureLoadedGameSceneCamera(GameSceneCaptureV2Path);
        Debug.Log(captured
            ? $"WARLINECAPTURE_M01_GAME_SCENE_CAPTURED path={GameSceneCaptureV2Path}"
            : $"WARLINECAPTURE_M01_GAME_SCENE_CAPTURE_FAILED path={GameSceneCaptureV2Path}");
        EditorApplication.update -= PollGameSceneCaptureTimeout;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChangedForGameSceneCapture;
        gameSceneCaptureRequested = false;
        gameFlowCaptureRequested = false;
        EditorApplication.ExitPlaymode();
        EditorApplication.Exit(captured ? 0 : 1);
    }

    private static void FinishGameSceneCaptureWhenFileExists()
    {
        string outputPath = ProjectPath(GameSceneCapturePath);
        if (!File.Exists(outputPath) && EditorApplication.timeSinceStartup - gameSceneCaptureStartTime < 45d)
            return;

        EditorApplication.update -= FinishGameSceneCaptureWhenFileExists;
        EditorApplication.update -= PollGameSceneCaptureTimeout;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChangedForGameSceneCapture;
        gameSceneCaptureRequested = false;
        gameFlowCaptureRequested = false;
        Debug.Log(File.Exists(outputPath)
            ? $"WARLINECAPTURE_M01_GAME_SCENE_CAPTURED path={GameSceneCapturePath}"
            : $"WARLINECAPTURE_M01_GAME_SCENE_CAPTURE_MISSING path={GameSceneCapturePath}");
        EditorApplication.ExitPlaymode();
        EditorApplication.Exit(File.Exists(outputPath) ? 0 : 1);
    }

    private static void PollGameSceneCaptureTimeout()
    {
        if (!gameSceneCaptureRequested || EditorApplication.timeSinceStartup - gameSceneCaptureStartTime < 60d)
            return;

        EditorApplication.update -= CaptureGameSceneWhenReady;
        EditorApplication.update -= CaptureGameSceneViaFlowWhenReady;
        EditorApplication.update -= FinishGameSceneCaptureWhenFileExists;
        EditorApplication.update -= PollGameSceneCaptureTimeout;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChangedForGameSceneCapture;
        gameSceneCaptureRequested = false;
        gameFlowCaptureRequested = false;
        Debug.LogError($"WARLINECAPTURE_M01_GAME_SCENE_CAPTURE_TIMEOUT path={GameSceneCapturePath}");
        EditorApplication.Exit(1);
    }

    private static void CleanupGameSceneCaptureRequest()
    {
        EditorApplication.update -= CaptureGameSceneWhenReady;
        EditorApplication.update -= CaptureGameSceneViaFlowWhenReady;
        EditorApplication.update -= FinishGameSceneCaptureWhenFileExists;
        EditorApplication.update -= PollGameSceneCaptureTimeout;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChangedForGameSceneCapture;
        gameSceneCaptureRequested = false;
        gameFlowCaptureRequested = false;
        gameFlowLaunchTriggered = false;
    }

    private static bool TryLaunchM01ThroughExistingFlow()
    {
        WarlineCaptureRouter router = Object.FindAnyObjectByType<WarlineCaptureRouter>(FindObjectsInactive.Include);
        QuickCustomScreenController quickCustom = Object.FindAnyObjectByType<QuickCustomScreenController>(FindObjectsInactive.Include);
        if (router == null || quickCustom == null)
            return false;

        router.gameObject.SetActive(true);
        router.Initialize();
        bool hasSplash = router.TryGetRegisteredScreen(WarlineCaptureRoute.Splash, out _);
        bool hasMainMenu = router.TryGetRegisteredScreen(WarlineCaptureRoute.MainMenu, out _);
        bool hasQuickCustom = router.TryGetRegisteredScreen(WarlineCaptureRoute.QuickCustomSetup, out _);
        bool hasMatch = router.TryGetRegisteredScreen(WarlineCaptureRoute.Match, out _);
        if (!hasSplash || !hasMainMenu || !hasQuickCustom || !hasMatch)
        {
            Debug.LogError($"WARLINECAPTURE_M01_GAME_FLOW_ROUTE_MISSING splash={(hasSplash ? 1 : 0)} main={(hasMainMenu ? 1 : 0)} quick={(hasQuickCustom ? 1 : 0)} match={(hasMatch ? 1 : 0)}");
            EditorApplication.Exit(1);
            return false;
        }

        router.GoTo(WarlineCaptureRoute.Splash, false);
        router.GoTo(WarlineCaptureRoute.MainMenu);
        router.GoTo(WarlineCaptureRoute.QuickCustomSetup);
        quickCustom.LaunchMission();
        Debug.Log($"WARLINECAPTURE_M01_GAME_FLOW_LAUNCHED splash=1 main=1 quickCustom=1 match=1 activeMission={WarlineCaptureMissionSession.ActiveMissionId}");
        return WarlineCaptureMissionSession.HasActiveMission &&
            WarlineCaptureMissionSession.ActiveMissionId == ChapterOneMissionCatalog.FirstContactMissionId;
    }

    private static bool TryEnsureM01RuntimeInitialized(out Chapter01M01PlayableRuntime.RuntimeState runtimeState)
    {
        runtimeState = default;
        GameBootstrap bootstrap = Object.FindAnyObjectByType<GameBootstrap>(FindObjectsInactive.Exclude);
        if (bootstrap == null || bootstrap.Chapter01TacticalBinder == null)
            return false;

        TacticalMapRuntimeLoader loader = bootstrap.Chapter01TacticalBinder.TacticalMapLoader;
        bool initialized = Chapter01M01PlayableRuntime.TryInitializeActiveMission(
            Unity.Entities.World.DefaultGameObjectInjectionWorld,
            loader,
            out runtimeState);
        if (initialized && gameSceneCaptureFrameCount % 30 == 0)
            Debug.Log($"WARLINECAPTURE_M01_GAME_FLOW_RUNTIME_READY player={runtimeState.PlayerSquad} enemy={runtimeState.EnemyPatrol}");
        return initialized;
    }

    private static Camera BuildRuntimeWorld()
    {
        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        if (definition == null || definition.GroundSprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_M01_RUNTIME_VISUAL_MATCH_MISSING_DEFINITION path={DefinitionPath}");
            return null;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("M01_01_RuntimeWorldProof");
        GameObject ground = new("Ground_M01_RuntimeProof");
        ground.transform.SetParent(root.transform, false);
        SpriteRenderer groundRenderer = ground.AddComponent<SpriteRenderer>();
        groundRenderer.sprite = definition.GroundSprite;
        groundRenderer.sortingOrder = 0;

        CreatePresenterSprite(definition, Chapter01M01PlayableRuntime.PlayerSquadEntityId, Chapter01M01PlayableRuntime.PlayerSpawnAnchorId, Color.white, 24, root.transform);
        CreatePresenterSprite(definition, Chapter01M01PlayableRuntime.EnemyPatrolEntityId, Chapter01M01PlayableRuntime.EnemySpawnAnchorId, new Color(1f, 0.58f, 0.48f, 1f), 25, root.transform);

        GameObject cameraObject = new("M01_01_RuntimeProofCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.039f, 0.040f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = definition.DefaultOrthographicSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(definition.CameraDefaultCenter.x, definition.CameraDefaultCenter.y, -10f);
        return camera;
    }

    private static void CreatePresenterSprite(TacticalMapDefinition definition, string runtimeEntityId, string anchorId, Color tint, int sortingOrder, Transform parent)
    {
        if (!Chapter01M01SpritePresenterCatalog.TryCreatePresenter(runtimeEntityId, out MissionRuntimeSpritePresenter presenter) ||
            !definition.TryGetAnchor(anchorId, out TacticalMapAnchor anchor) ||
            !MissionRuntimeAtlasQuadPresentationSystem.TryResolveSprite(presenter, out Sprite sprite))
        {
            Debug.LogWarning($"WARLINECAPTURE_M01_RUNTIME_VISUAL_MATCH_ENTITY_SKIPPED id={runtimeEntityId} anchor={anchorId}");
            return;
        }

        Vector2 world = definition.NormalizedToWorld(anchor.NormalizedPosition);
        GameObject entity = new($"RuntimeProof_{runtimeEntityId}");
        entity.transform.SetParent(parent, false);
        entity.transform.position = new Vector3(world.x, world.y, -0.04f);
        float scale = Chapter01M01SpriteAssetResolver.TryGetScale(presenter.ManifestAssetId.ToString(), out float resolvedScale) ? resolvedScale : 1f;
        entity.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer renderer = entity.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CaptureFrame(Camera camera, string assetPath)
    {
        if (camera == null)
            return;

        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(assetPath)));
        RenderTexture renderTexture = new(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
        Texture2D png = new(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
                MissionRuntimeAtlasQuadPresentationSystem.DrawAllRuntimeQuadsForCamera(world.EntityManager, camera);
            camera.Render();
            Canvas.ForceUpdateCanvases();
            png.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            png.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), assetPath), png.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(png);
        }
    }

    private static bool CaptureLoadedGameSceneCamera(string assetPath)
    {
        Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (camera == null)
            return false;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        RenderMode[] previousModes = new RenderMode[canvases.Length];
        Camera[] previousCameras = new Camera[canvases.Length];
        float[] previousPlaneDistances = new float[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            previousModes[i] = canvases[i].renderMode;
            previousCameras[i] = canvases[i].worldCamera;
            previousPlaneDistances[i] = canvases[i].planeDistance;
            canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = camera;
            canvases[i].planeDistance = 1f;
            canvases[i].enabled = canvases[i].gameObject.activeInHierarchy;
        }

        try
        {
            CaptureFrame(camera, assetPath);
            return File.Exists(ProjectPath(assetPath));
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null)
                    continue;

                canvases[i].renderMode = previousModes[i];
                canvases[i].worldCamera = previousCameras[i];
                canvases[i].planeDistance = previousPlaneDistances[i];
            }
        }
    }

    private static string ProjectPath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
    }
}
#endif
