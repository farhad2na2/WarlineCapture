#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PremiumWorldMarkerVisualProofCapture
{
    private const string OutputDirectory = "/private/tmp/warline_premium_world_marker_visual_qa";
    private const int CaptureSize = 1280;

    private const string BuildingPrefabPath = "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab";
    private const string InfantryPrefabPath = "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab";
    private const string VehiclePrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab";
    private const string AircraftPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack.prefab";
    private const string GroundMissileLauncherPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Missle_Launcher_Ground.prefab";

    private const string BuildingSelectionMarkerPath = "Assets/Game/Prefabs/Buildings/BuildingSelectionMarker.prefab";
    private const string VehicleSelectionMarkerPath = "Assets/Game/Prefabs/Vehicles/VehicleSelectionMarker.prefab";
    private const string MoveMarkerPath = "Assets/Game/Prefabs/Shapes/Target_Move.prefab";
    private const string AttackMarkerPath = "Assets/Game/Prefabs/Shapes/Target_Attack.prefab";
    private const string AttackTargetMarkerPath = "Assets/Game/Prefabs/Shapes/AttackTargetSelectionMarker.prefab";

    private static readonly Color BackgroundColor = new(0.035f, 0.04f, 0.048f, 1f);
    private static readonly Color GroundColor = new(0.075f, 0.085f, 0.09f, 1f);

    public static void Run()
    {
        try
        {
            List<CaptureResult> results = CaptureAll();
            string reportPath = WriteReport(results);
            Debug.Log($"[PremiumWorldMarkerVisualProof] PASS report={reportPath}");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PremiumWorldMarkerVisualProof] FAIL {ex}");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }

    private static List<CaptureResult> CaptureAll()
    {
        Directory.CreateDirectory(OutputDirectory);
        var results = new List<CaptureResult>
        {
            CaptureSelectionScenario(
                "building_selection",
                BuildingPrefabPath,
                BuildingSelectionMarkerPath,
                targetPadding: 1.25f,
                minimumMarkerSize: 4.6f,
                "Building selection footprint brackets align to the selected building bounds."),
            CaptureSelectionScenario(
                "infantry_selection",
                InfantryPrefabPath,
                VehicleSelectionMarkerPath,
                targetPadding: 0.55f,
                minimumMarkerSize: 1.35f,
                "Infantry selection marker remains readable without overpowering the soldier."),
            CaptureSelectionScenario(
                "vehicle_selection",
                VehiclePrefabPath,
                VehicleSelectionMarkerPath,
                targetPadding: 1.28f,
                minimumMarkerSize: 3.6f,
                "Vehicle selection marker scales to vehicle footprint and stays below the hull."),
            CaptureSelectionScenario(
                "aircraft_selection",
                AircraftPrefabPath,
                VehicleSelectionMarkerPath,
                targetPadding: 1.18f,
                minimumMarkerSize: 5.0f,
                "Aircraft selection marker uses the shared calm selection family at aircraft scale."),
            CaptureCommandMarkerScenario(
                "move_command_marker",
                MoveMarkerPath,
                "Move target command marker renders as the blue/cyan destination ping."),
            CaptureCommandMarkerScenario(
                "attack_command_marker",
                AttackMarkerPath,
                "Attack target command marker renders as the red/orange strike reticle."),
            CaptureMissileTargetLockScenario()
        };

        return results;
    }

    private static CaptureResult CaptureSelectionScenario(
        string id,
        string modelPath,
        string markerPath,
        float targetPadding,
        float minimumMarkerSize,
        string note)
    {
        SetupScene();
        GameObject model = InstantiatePrefab(modelPath, id + "_model");
        CenterOnGround(model);
        Bounds modelBounds = CalculateRenderableBounds(model);
        Require(modelBounds.size.sqrMagnitude > 0.001f, $"{id} model has no renderable bounds.");

        GameObject marker = InstantiatePrefab(markerPath, id + "_marker");
        if (id.Contains("infantry", StringComparison.OrdinalIgnoreCase))
            SetNamedRendererVisibility(marker, "Vehicle", visible: false);
        float targetX = Mathf.Max(minimumMarkerSize, modelBounds.size.x * targetPadding);
        float targetZ = Mathf.Max(minimumMarkerSize, modelBounds.size.z * targetPadding);
        PlaceMarker(marker, modelBounds, targetX, targetZ, 0.12f);
        ConfigureBoundaryViewIfPresent(marker, modelBounds, targetX, targetZ, MarkerHueExpectation.AnyPremium);
        ConfigureObjectOutlineIfPresent(marker, model, modelBounds, MarkerHueExpectation.Cyan);

        Bounds sceneBounds = CalculateCombinedBounds(model, marker);
        AddGround(sceneBounds);
        return RenderScenario(id, sceneBounds, note, MarkerHueExpectation.AnyPremium);
    }

    private static CaptureResult CaptureCommandMarkerScenario(string id, string markerPath, string note)
    {
        SetupScene();
        GameObject marker = InstantiatePrefab(markerPath, id + "_marker");
        Bounds markerBounds = CalculateRenderableBounds(marker);
        Require(markerBounds.size.sqrMagnitude > 0.001f, $"{id} marker has no renderable bounds.");
        marker.transform.position += new Vector3(-markerBounds.center.x, 0.1f - markerBounds.min.y, -markerBounds.center.z);
        markerBounds = CalculateRenderableBounds(marker);
        marker.transform.localScale *= 1.28f;

        Bounds sceneBounds = CalculateRenderableBounds(marker);
        AddGround(sceneBounds);
        MarkerHueExpectation expectation = id.Contains("move", StringComparison.Ordinal)
            ? MarkerHueExpectation.Cyan
            : MarkerHueExpectation.Red;
        return RenderScenario(id, sceneBounds, note, expectation);
    }

    private static CaptureResult CaptureMissileTargetLockScenario()
    {
        SetupScene();
        GameObject launcher = InstantiatePrefab(GroundMissileLauncherPrefabPath, "ground_missile_launcher_model");
        CenterOnGround(launcher);
        launcher.transform.position += new Vector3(-4.8f, 0f, 0f);
        launcher.transform.rotation = Quaternion.Euler(0f, 26f, 0f);

        GameObject target = InstantiatePrefab(VehiclePrefabPath, "target_vehicle_model");
        CenterOnGround(target);
        target.transform.position += new Vector3(4.4f, 0f, 0f);
        target.transform.rotation = Quaternion.Euler(0f, -34f, 0f);
        Bounds targetBounds = CalculateRenderableBounds(target);

        GameObject marker = InstantiatePrefab(AttackTargetMarkerPath, "ground_missile_target_lock_marker");
        float targetX = Mathf.Max(3.4f, targetBounds.size.x * 1.3f);
        float targetZ = Mathf.Max(3.4f, targetBounds.size.z * 1.3f);
        PlaceMarker(marker, targetBounds, targetX, targetZ, 0.12f);
        ConfigureBoundaryViewIfPresent(marker, targetBounds, targetX, targetZ, MarkerHueExpectation.Red);

        Bounds sceneBounds = CalculateCombinedBounds(launcher, target, marker);
        AddGround(sceneBounds);
        return RenderScenario(
            "ground_missile_target_lock",
            sceneBounds,
            "Ground missile launcher target lock uses the dedicated red/orange entity target-lock marker under the target.",
            MarkerHueExpectation.Red);
    }

    private static void SetupScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.64f, 1f);

        GameObject keyObject = new("Marker Proof Key Light");
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.2f;
        key.color = new Color(1f, 0.94f, 0.84f, 1f);
        key.shadows = LightShadows.None;
        keyObject.transform.rotation = Quaternion.Euler(52f, -42f, 0f);

        GameObject fillObject = new("Marker Proof Fill Light");
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.42f;
        fill.color = new Color(0.6f, 0.76f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fillObject.transform.rotation = Quaternion.Euler(26f, 136f, 0f);
    }

    private static GameObject InstantiatePrefab(string prefabPath, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Require(prefab != null, $"Missing prefab at {prefabPath}.");
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Require(instance != null, $"Could not instantiate prefab at {prefabPath}.");
        instance.name = name;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        DisableNonProofRenderers(instance);
        return instance;
    }

    private static void DisableNonProofRenderers(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            string path = HierarchyPath(renderer.transform);
            if (path.Contains("/HealthBar/", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/HealthBar", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/FactionMarker/", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/FactionMarker", StringComparison.OrdinalIgnoreCase))
            {
                renderer.enabled = false;
            }
        }
    }

    private static void SetNamedRendererVisibility(GameObject root, string nameFragment, bool visible)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null &&
                renderer.name.Contains(nameFragment, StringComparison.OrdinalIgnoreCase))
            {
                renderer.enabled = visible;
            }
        }
    }

    private static void CenterOnGround(GameObject instance)
    {
        Bounds bounds = CalculateRenderableBounds(instance);
        Require(bounds.size.sqrMagnitude > 0.001f, $"{instance.name} has no renderable bounds.");
        instance.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
    }

    private static void PlaceMarker(GameObject marker, Bounds targetBounds, float targetX, float targetZ, float verticalOffset)
    {
        Bounds markerBounds = CalculateRenderableBounds(marker);
        Require(markerBounds.size.sqrMagnitude > 0.001f, $"{marker.name} has no renderable bounds.");

        float baseX = Mathf.Max(0.001f, markerBounds.size.x);
        float baseZ = Mathf.Max(0.001f, markerBounds.size.z);
        marker.transform.localScale = new Vector3(targetX / baseX, 1f, targetZ / baseZ);

        markerBounds = CalculateRenderableBounds(marker);
        Vector3 desiredCenter = new(targetBounds.center.x, targetBounds.min.y + verticalOffset, targetBounds.center.z);
        marker.transform.position += desiredCenter - new Vector3(markerBounds.center.x, markerBounds.min.y, markerBounds.center.z);
    }

    private static void ConfigureBoundaryViewIfPresent(
        GameObject marker,
        Bounds targetBounds,
        float targetX,
        float targetZ,
        MarkerHueExpectation expectation)
    {
        PremiumWorldSelectionBoundaryView boundaryView = marker.GetComponent<PremiumWorldSelectionBoundaryView>();
        if (boundaryView == null)
            return;

        Color baseColor = expectation == MarkerHueExpectation.Red
            ? new Color(1f, 0.08f, 0.04f, 0.96f)
            : new Color(0.05f, 0.88f, 1f, 0.94f);
        Color accentColor = expectation == MarkerHueExpectation.Red
            ? new Color(1f, 0.82f, 0.42f, 1f)
            : new Color(0.86f, 1f, 1f, 1f);
        Vector3 center = targetBounds.center;
        center.y = targetBounds.min.y;
        boundaryView.Configure(
            center,
            marker.transform.rotation,
            new Vector2(targetX, targetZ),
            targetBounds.min.y,
            Mathf.Max(0.9f, targetBounds.size.y),
            baseColor,
            accentColor);
    }

    private static void ConfigureObjectOutlineIfPresent(
        GameObject marker,
        GameObject target,
        Bounds targetBounds,
        MarkerHueExpectation expectation)
    {
        PremiumWorldSelectionObjectOutlineView outlineView = marker.GetComponent<PremiumWorldSelectionObjectOutlineView>();
        if (outlineView == null || target == null)
            return;

        Color baseColor = expectation == MarkerHueExpectation.Red
            ? new Color(1f, 0.08f, 0.04f, 0.96f)
            : new Color(0.05f, 0.88f, 1f, 0.94f);
        Color emissionColor = expectation == MarkerHueExpectation.Red
            ? new Color(0.76f, 0.05f, 0.03f, 1f)
            : new Color(0.08f, 0.96f, 1f, 1f);
        float longestAxis = Mathf.Max(targetBounds.size.x, targetBounds.size.y, targetBounds.size.z);
        float outlineWidth = Mathf.Clamp(longestAxis * 0.005f, 0.028f, 0.075f);
        outlineView.Configure(target, baseColor, emissionColor, outlineWidth);
    }

    private static void AddGround(Bounds bounds)
    {
        float size = Mathf.Max(8f, Mathf.Max(bounds.size.x, bounds.size.z) * 1.75f);
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Marker Proof Ground";
        ground.transform.position = new Vector3(bounds.center.x, -0.012f, bounds.center.z);
        ground.transform.localScale = new Vector3(size * 0.1f, 1f, size * 0.1f);
        Collider collider = ground.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
        {
            name = "MarkerProofGroundMaterial",
            color = GroundColor,
            hideFlags = HideFlags.HideAndDontSave
        };
        ground.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static CaptureResult RenderScenario(
        string id,
        Bounds sceneBounds,
        string note,
        MarkerHueExpectation markerHueExpectation)
    {
        GameObject cameraObject = new("Marker Proof Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
        camera.orthographic = true;
        camera.aspect = 1f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 350f;
        camera.orthographicSize = ResolveOrthographicSize(sceneBounds);

        Vector3 focus = sceneBounds.center + Vector3.up * Mathf.Max(0.1f, sceneBounds.size.y * 0.08f);
        Vector3 viewDirection = new Vector3(0.66f, 0.58f, 0.62f).normalized;
        camera.transform.position = focus + viewDirection * Mathf.Max(20f, sceneBounds.extents.magnitude * 3.0f);
        camera.transform.LookAt(focus);

        string outputPath = Path.Combine(OutputDirectory, id + ".png");
        RenderTexture target = new(CaptureSize, CaptureSize, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1,
            name = "PremiumWorldMarkerVisualProof_" + id
        };
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();

            texture = new Texture2D(CaptureSize, CaptureSize, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, CaptureSize, CaptureSize), 0, 0);
            texture.Apply(false, false);

            int visiblePixels = CountVisiblePixels(texture, BackgroundColor);
            int markerPixels = CountMarkerPixels(texture, markerHueExpectation);
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            Require(visiblePixels > 2000, $"{id} capture is blank.");
            Require(markerPixels > 40, $"{id} capture did not contain enough marker-colored pixels.");

            Debug.Log($"[PremiumWorldMarkerVisualProof] scenario={id} output={outputPath} visiblePixels={visiblePixels} markerPixels={markerPixels}");
            return new CaptureResult(id, outputPath, visiblePixels, markerPixels, note);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (texture != null)
                Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
        }
    }

    private static float ResolveOrthographicSize(Bounds bounds)
    {
        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        float height = Mathf.Max(0.01f, bounds.size.y);
        return Mathf.Max(2.6f, footprint * 0.72f, height * 0.82f);
    }

    private static string WriteReport(IReadOnlyList<CaptureResult> results)
    {
        string reportPath = Path.Combine(OutputDirectory, "premium_world_marker_visual_qa_report.md");
        using var writer = new StreamWriter(reportPath);
        writer.WriteLine("# Premium World Marker Visual QA Proof");
        writer.WriteLine();
        writer.WriteLine("Generated from `PremiumWorldMarkerVisualProofCapture.Run`.");
        writer.WriteLine();
        writer.WriteLine("| Scenario | Screenshot | Visible Pixels | Marker Pixels | Note |");
        writer.WriteLine("| --- | --- | ---: | ---: | --- |");
        for (int i = 0; i < results.Count; i++)
        {
            CaptureResult result = results[i];
            writer.WriteLine($"| {result.Id} | `{result.OutputPath}` | {result.VisiblePixels} | {result.MarkerPixels} | {result.Note} |");
        }

        return reportPath;
    }

    private static Bounds CalculateCombinedBounds(params GameObject[] roots)
    {
        bool hasBounds = false;
        Bounds combined = default;
        for (int i = 0; i < roots.Length; i++)
        {
            Bounds bounds = CalculateRenderableBounds(roots[i]);
            if (bounds.size.sqrMagnitude <= 0.001f)
                continue;

            if (hasBounds)
                combined.Encapsulate(bounds);
            else
            {
                combined = bounds;
                hasBounds = true;
            }
        }

        return hasBounds ? combined : new Bounds(Vector3.zero, Vector3.one);
    }

    private static Bounds CalculateRenderableBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (hasBounds)
                bounds.Encapsulate(renderer.bounds);
            else
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    private static int CountVisiblePixels(Texture2D texture, Color background)
    {
        Color32[] pixels = texture.GetPixels32();
        Color32 bg = background;
        int count = 0;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            Color32 pixel = pixels[i];
            int diff = Mathf.Abs(pixel.r - bg.r) + Mathf.Abs(pixel.g - bg.g) + Mathf.Abs(pixel.b - bg.b);
            if (diff > 24)
                count++;
        }

        return count;
    }

    private static int CountMarkerPixels(Texture2D texture, MarkerHueExpectation expectation)
    {
        Color32[] pixels = texture.GetPixels32();
        int count = 0;
        for (int i = 0; i < pixels.Length; i += 2)
        {
            Color32 pixel = pixels[i];
            bool red = pixel.r > 70 && pixel.r > pixel.g + 12 && pixel.r > pixel.b + 10;
            bool cyan = pixel.g > 65 && pixel.b > 56 && pixel.r < 138;
            bool green = pixel.g > 70 && pixel.g > pixel.r + 8;
            bool match = expectation switch
            {
                MarkerHueExpectation.Red => red,
                MarkerHueExpectation.Cyan => cyan || green,
                _ => red || cyan || green
            };
            if (match)
                count++;
        }

        return count;
    }

    private static string HierarchyPath(Transform transform)
    {
        var segments = new Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            segments.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", segments);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private readonly struct CaptureResult
    {
        public readonly string Id;
        public readonly string OutputPath;
        public readonly int VisiblePixels;
        public readonly int MarkerPixels;
        public readonly string Note;

        public CaptureResult(string id, string outputPath, int visiblePixels, int markerPixels, string note)
        {
            Id = id;
            OutputPath = outputPath;
            VisiblePixels = visiblePixels;
            MarkerPixels = markerPixels;
            Note = note;
        }
    }

    private enum MarkerHueExpectation
    {
        AnyPremium,
        Red,
        Cyan
    }
}
#endif
