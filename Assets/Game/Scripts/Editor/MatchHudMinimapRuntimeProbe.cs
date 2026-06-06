#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Scripts.UI;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Transforms;

[InitializeOnLoad]
public static class MatchHudMinimapRuntimeProbe
{
    private const string ActiveKey = "WarlineCapture.MatchHudMinimapRuntimeProbe.Active";
    private const string StageKey = "WarlineCapture.MatchHudMinimapRuntimeProbe.Stage";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string PngPath = "/private/tmp/warline_match_hud_minimap_runtime.png";
    private const string ReportPath = "/private/tmp/warline_match_hud_minimap_runtime.json";
    private const double StartupTimeoutSeconds = 45d;
    private const double MatchTimeoutSeconds = 80d;
    private const double WarmupSeconds = 12d;
    private const double CameraFocusWarmupSeconds = 2d;

    private static double s_stageStartTime;
    private static bool s_clickedDeploy;
    private static bool s_focusedWorldCamera;
    private static bool s_finished;
    private static string s_result = string.Empty;

    static MatchHudMinimapRuntimeProbe()
    {
        if (SessionState.GetInt(ActiveKey, 0) == 1)
            Attach();
    }

    public static void Run()
    {
        s_stageStartTime = EditorApplication.timeSinceStartup;
        s_clickedDeploy = false;
        s_focusedWorldCamera = false;
        s_finished = false;
        s_result = string.Empty;
        SessionState.SetInt(ActiveKey, 1);
        SessionState.SetInt(StageKey, 0);
        Attach();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void Attach()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void Detach()
    {
        EditorApplication.update -= Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(StageKey, 1);
            s_stageStartTime = EditorApplication.timeSinceStartup;
        }
        else if (state == PlayModeStateChange.EnteredEditMode && s_finished)
        {
            SessionState.SetInt(ActiveKey, 0);
            SessionState.SetInt(StageKey, 0);
            Detach();
            EditorApplication.Exit(s_result == "completed" ? 0 : 1);
        }
    }

    private static void Update()
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
            return;

        int stage = SessionState.GetInt(StageKey, 0);
        double now = EditorApplication.timeSinceStartup;
        if (stage == 1)
        {
            if (TryClickDeploy())
            {
                SessionState.SetInt(StageKey, 2);
                s_stageStartTime = now;
                return;
            }

            if (now - s_stageStartTime > StartupTimeoutSeconds)
                Finish("timeout_waiting_for_menu", "DeployCommandButton/MenuView game-start path was not available.");
        }
        else if (stage == 2)
        {
            if (now - s_stageStartTime > MatchTimeoutSeconds)
            {
                Finish("timeout_waiting_for_minimap", "Match HUD minimap did not expose a generated runtime texture.");
                return;
            }

            if (now - s_stageStartTime < WarmupSeconds)
                return;

            MatchHudMinimapView view = FindMinimapView();
            if (!s_focusedWorldCamera)
            {
                TryFocusWorldCameraOnPlayerUnits();
                s_focusedWorldCamera = true;
                s_stageStartTime = now;
                return;
            }

            if (now - s_stageStartTime < CameraFocusWarmupSeconds)
                return;

            Texture mapTexture = ResolveRuntimeMapTexture(view);
            if (mapTexture == null)
                return;

            try
            {
                CaptureTexture(view, mapTexture);
            }
            catch (Exception ex)
            {
                Finish("exception", ex.ToString());
            }
        }
    }

    private static Texture ResolveRuntimeMapTexture(MatchHudMinimapView view)
    {
        if (view == null)
            return null;
        if (view.MapImage == null || view.MapImage.sprite == null)
            return null;
        return view.MapImage.sprite.name == "Runtime_MatchHudMinimapSprite"
            ? view.MapImage.sprite.texture
            : null;
    }

    private static bool TryFocusWorldCameraOnPlayerUnits()
    {
        if (!TryGetPlayerUnitCenter(out Vector3 target))
            return false;

        Camera camera = FindWorldCamera();
        if (camera == null)
            return false;

        Plane groundPlane = new(Vector3.up, new Vector3(0f, target.y, 0f));
        Ray centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!groundPlane.Raycast(centerRay, out float distance))
            return false;

        Vector3 currentCenter = centerRay.GetPoint(distance);
        Vector3 delta = target - currentCenter;
        camera.transform.position += delta;
        return true;
    }

    private static Camera FindWorldCamera()
    {
        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
            return main;

        Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        int uiLayer = LayerMask.NameToLayer("UI");
        Camera best = null;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || !camera.isActiveAndEnabled)
                continue;
            if (uiLayer >= 0 && camera.cullingMask == (1 << uiLayer))
                continue;
            if (best == null || camera.depth > best.depth)
                best = camera;
        }

        return best;
    }

    private static bool TryGetPlayerUnitCenter(out Vector3 center)
    {
        center = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitHealth>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Vector3 sum = Vector3.zero;
        int count = 0;
        using Unity.Collections.NativeArray<Entity> entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            if (health.Current <= 0)
                continue;

            Faction faction = em.GetComponentData<Faction>(entity);
            if (!FactionIdentitySystem.IsPlayerControlled(faction.Id))
                continue;

            Unity.Mathematics.float3 position = em.GetComponentData<LocalTransform>(entity).Position;
            sum += new Vector3(position.x, position.y, position.z);
            count++;
        }

        if (count == 0)
            return false;

        center = sum / count;
        return true;
    }

    private static void CaptureTexture(MatchHudMinimapView view, Texture texture)
    {
        Texture2D readableTexture = null;
        MinimapPixelStats stats;
        try
        {
            readableTexture = ReadableCopy(texture);
            stats = MinimapPixelStats.From(readableTexture.GetPixels32());
            Directory.CreateDirectory(Path.GetDirectoryName(PngPath));
            File.WriteAllBytes(PngPath, readableTexture.EncodeToPNG());
        }
        finally
        {
            if (readableTexture != null)
                UnityEngine.Object.DestroyImmediate(readableTexture);
        }

        int activeMarkers = CountActiveMarkers(view);
        bool flatCapture = stats.LuminanceRange < 14 || stats.LuminanceStdDev < 3.5f || stats.QuantizedColorCount < 10;
        WriteReport(flatCapture ? "flat_capture" : "completed", view, texture, stats, activeMarkers);
        Finish(flatCapture ? "flat_capture" : "completed", flatCapture
            ? "Runtime minimap texture is still near-flat."
            : "Runtime minimap texture captured.");
    }

    private static Texture2D ReadableCopy(Texture texture)
    {
        if (texture is Texture2D texture2D)
        {
            Texture2D copy = new(texture2D.width, texture2D.height, TextureFormat.RGBA32, false);
            try
            {
                copy.SetPixels32(texture2D.GetPixels32());
                copy.Apply(false);
                return copy;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        if (texture is RenderTexture renderTexture)
        {
            return ReadableCopyFromRenderTexture(renderTexture);
        }

        RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        try
        {
            Graphics.Blit(texture, temporary);
            return ReadableCopyFromRenderTexture(temporary);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    private static Texture2D ReadableCopyFromRenderTexture(RenderTexture renderTexture)
    {
        RenderTexture previousActive = RenderTexture.active;
        Texture2D copy = new(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = renderTexture;
            copy.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
            copy.Apply(false);
            return copy;
        }
        finally
        {
            RenderTexture.active = previousActive;
        }
    }

    private static bool TryClickDeploy()
    {
        Scene menuScene = SceneManager.GetSceneByName("Menu");
        if (!menuScene.IsValid() || !menuScene.isLoaded)
            return false;

        foreach (GameObject root in menuScene.GetRootGameObjects())
        {
            WarlineCaptureShellRouteButtonView routeButton = FindComponentInTree<WarlineCaptureShellRouteButtonView>(root.transform, IsDeployCommandButton);
            if (routeButton == null)
                continue;

            routeButton.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            s_clickedDeploy = true;
            return true;
        }

        MenuView menu = FindMenuView(menuScene);
        if (menu == null)
            return false;

        if (menu.buttonGame != null)
            menu.buttonGame.onClick.Invoke();
        else
            menu.RequestGameStart();

        s_clickedDeploy = true;
        return true;
    }

    private static bool IsDeployCommandButton(WarlineCaptureShellRouteButtonView routeButton)
    {
        return routeButton != null &&
               routeButton.name == "DeployCommandButton" &&
               routeButton.Intent == UiShellRouteIntent.EnterMatch &&
               routeButton.Route == WarlineCaptureRoute.Match;
    }

    private static MatchHudMinimapView FindMinimapView()
    {
        MatchHudMinimapView[] views = Resources.FindObjectsOfTypeAll<MatchHudMinimapView>();
        for (int i = 0; i < views.Length; i++)
        {
            MatchHudMinimapView view = views[i];
            if (view == null || view.MapRect == null || view.gameObject.scene.name == null)
                continue;
            if (view.gameObject.scene.IsValid() && view.gameObject.scene.isLoaded)
                return view;
        }

        return null;
    }

    private static MenuView FindMenuView(Scene menuScene)
    {
        foreach (GameObject root in menuScene.GetRootGameObjects())
        {
            MenuView menu = FindComponentInTree<MenuView>(root.transform, static candidate => candidate != null);
            if (menu != null)
                return menu;
        }

        return null;
    }

    private static T FindComponentInTree<T>(Transform root, Func<T, bool> predicate)
        where T : Component
    {
        if (root == null)
            return null;

        T component = root.GetComponent<T>();
        if (component != null && predicate(component))
            return component;

        for (int i = 0; i < root.childCount; i++)
        {
            T child = FindComponentInTree(root.GetChild(i), predicate);
            if (child != null)
                return child;
        }

        return null;
    }

    private static int CountActiveMarkers(MatchHudMinimapView view)
    {
        RectTransform markerRoot = view != null ? view.MarkerRoot : null;
        if (markerRoot == null)
            return 0;

        int count = 0;
        for (int i = 0; i < markerRoot.childCount; i++)
        {
            Transform child = markerRoot.GetChild(i);
            if (child != null && child.name == "MinimapMarker" && child.gameObject.activeInHierarchy)
                count++;
        }

        return count;
    }

    private static void WriteReport(
        string result,
        MatchHudMinimapView view,
        Texture texture,
        MinimapPixelStats stats,
        int activeMarkers)
    {
        StringBuilder json = new();
        json.AppendLine("{");
        AppendJson(json, "result", result, comma: true);
        AppendJson(json, "clickedDeploy", s_clickedDeploy, comma: true);
        AppendJson(json, "png", PngPath, comma: true);
        AppendJson(json, "textureName", texture.name, comma: true);
        AppendJson(json, "textureWidth", texture.width, comma: true);
        AppendJson(json, "textureHeight", texture.height, comma: true);
        AppendJson(json, "mapPath", GetHierarchyPath(view.transform), comma: true);
        AppendJson(json, "activeMarkers", activeMarkers, comma: true);
        ResolveViewportCenter(view, out Vector2 viewportCenter, out float viewportCenterOffset);
        AppendJson(json, "viewportCenterX", viewportCenter.x, comma: true);
        AppendJson(json, "viewportCenterY", viewportCenter.y, comma: true);
        AppendJson(json, "viewportCenterOffset", viewportCenterOffset, comma: true);
        AppendJson(json, "minLuminance", stats.MinLuminance, comma: true);
        AppendJson(json, "maxLuminance", stats.MaxLuminance, comma: true);
        AppendJson(json, "luminanceRange", stats.LuminanceRange, comma: true);
        AppendJson(json, "averageLuminance", stats.AverageLuminance, comma: true);
        AppendJson(json, "luminanceStdDev", stats.LuminanceStdDev, comma: true);
        AppendJson(json, "quantizedColorCount", stats.QuantizedColorCount, comma: false);
        json.AppendLine("}");
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, json.ToString());
    }

    private static void ResolveViewportCenter(
        MatchHudMinimapView view,
        out Vector2 normalizedCenter,
        out float centerOffset)
    {
        normalizedCenter = new Vector2(-1f, -1f);
        centerOffset = -1f;
        RectTransform mapRect = view != null ? view.MapRect : null;
        RectTransform viewportRect = view != null ? view.ViewportRect : null;
        if (mapRect == null || viewportRect == null)
            return;

        Vector3[] corners = new Vector3[4];
        viewportRect.GetWorldCorners(corners);
        Vector3 viewportCenterWorld = (corners[0] + corners[2]) * 0.5f;
        Vector3 mapLocalCenter = mapRect.InverseTransformPoint(viewportCenterWorld);
        Rect map = mapRect.rect;
        normalizedCenter = new Vector2(
            (mapLocalCenter.x - map.xMin) / Mathf.Max(0.001f, map.width),
            (mapLocalCenter.y - map.yMin) / Mathf.Max(0.001f, map.height));
        centerOffset = Vector2.Distance(normalizedCenter, new Vector2(0.5f, 0.5f));
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static void Finish(string result, string detail)
    {
        if (s_finished)
            return;

        s_result = result;
        s_finished = true;
        Debug.Log($"[MatchHudMinimapRuntimeProbe] result={result} detail={detail} png={PngPath} report={ReportPath}");
        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
        else
            EditorApplication.Exit(result == "completed" ? 0 : 1);
    }

    private static void AppendJson(StringBuilder json, string name, string value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": \"").Append(EscapeJson(value)).Append('"');
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder json, string name, bool value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value ? "true" : "false");
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder json, string name, int value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture));
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder json, string name, float value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value.ToString("0.###", CultureInfo.InvariantCulture));
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static string EscapeJson(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private readonly struct MinimapPixelStats
    {
        public readonly int MinLuminance;
        public readonly int MaxLuminance;
        public readonly int LuminanceRange;
        public readonly float AverageLuminance;
        public readonly float LuminanceStdDev;
        public readonly int QuantizedColorCount;

        private MinimapPixelStats(
            int minLuminance,
            int maxLuminance,
            float averageLuminance,
            float luminanceStdDev,
            int quantizedColorCount)
        {
            MinLuminance = minLuminance;
            MaxLuminance = maxLuminance;
            LuminanceRange = maxLuminance - minLuminance;
            AverageLuminance = averageLuminance;
            LuminanceStdDev = luminanceStdDev;
            QuantizedColorCount = quantizedColorCount;
        }

        public static MinimapPixelStats From(Color32[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                return new MinimapPixelStats(0, 0, 0f, 0f, 0);

            int min = 255;
            int max = 0;
            double sum = 0d;
            double sumSquares = 0d;
            HashSet<int> colors = new();
            int step = Mathf.Max(1, pixels.Length / 16384);
            int count = 0;
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 pixel = pixels[i];
                int lum = (pixel.r + pixel.g + pixel.b) / 3;
                min = Mathf.Min(min, lum);
                max = Mathf.Max(max, lum);
                sum += lum;
                sumSquares += lum * lum;
                int bucket = (pixel.r >> 4) << 8 | (pixel.g >> 4) << 4 | (pixel.b >> 4);
                colors.Add(bucket);
                count++;
            }

            float average = count > 0 ? (float)(sum / count) : 0f;
            float variance = count > 0 ? Mathf.Max(0f, (float)(sumSquares / count) - average * average) : 0f;
            return new MinimapPixelStats(min, max, average, Mathf.Sqrt(variance), colors.Count);
        }
    }
}
#endif
