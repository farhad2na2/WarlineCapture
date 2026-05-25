#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureGameTerrain3Island2048Builder
{
    private const string SourceScenePath = "Assets/Game/Scenes/Game_Terrain3.unity";
    private const string ScenePath = "Assets/Game/Scenes/Generated/Game_Terrain3_Island2048.unity";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GameTerrain3_Island2048";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GameTerrain3_Island2048";
    private const string LayoutJsonPath = DataRoot + "/game_terrain3_island2048_layout.json";
    private const string ReportPath = "Design/AgentReports/2026-05-25_gameplay_game-terrain3-island2048-builder.md";
    private const float MapSize = 2048f;
    private const float HalfMapSize = MapSize * 0.5f;
    private const float IslandRadiusX = 880f;
    private const float IslandRadiusZ = 820f;
    private const float GroundFillSpacing = 24f;
    private const float ShoreGroundSpacing = 30f;
    private const float DetailGrassSpacing = 78f;
    private const float GroundSurfaceScaleXZ = 1.55f;
    private const float ShoreGroundScaleXZ = 1.38f;
    private const float DetailGrassScaleXZ = 0.95f;
    private const float BeachSurfaceScaleXZ = 1.55f;

    private static readonly List<SourcePiece> BeachPieces = new();
    private static readonly List<SourcePiece> GroundPieces = new();
    private static readonly List<SourcePiece> DetailGrassPieces = new();
    private static readonly List<PlacedPiece> PlacedPieces = new();
    private static readonly HashSet<string> UniquePrefabPaths = new(StringComparer.Ordinal);

    private enum SourcePieceRole
    {
        Beach,
        Ground,
        DetailGrass
    }

    private readonly struct SourcePiece
    {
        public readonly string PrefabPath;
        public readonly string SourceName;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;
        public readonly SourcePieceRole Role;

        public SourcePiece(string prefabPath, string sourceName, Vector3 position, Quaternion rotation, Vector3 scale, SourcePieceRole role)
        {
            PrefabPath = prefabPath;
            SourceName = sourceName;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Role = role;
        }
    }

    private readonly struct PlacedPiece
    {
        public readonly string PrefabPath;
        public readonly string Kind;
        public readonly Vector3 Position;
        public readonly float Yaw;

        public PlacedPiece(string prefabPath, string kind, Vector3 position, float yaw)
        {
            PrefabPath = prefabPath;
            Kind = kind;
            Position = position;
            Yaw = yaw;
        }
    }

    [MenuItem("WarlineCapture/Design/Build Game Terrain3 Island 2048")]
    public static void BuildScene()
    {
        BeachPieces.Clear();
        GroundPieces.Clear();
        DetailGrassPieces.Clear();
        PlacedPieces.Clear();
        UniquePrefabPaths.Clear();

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(DataRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ReportPath)));

        CollectSourcePieces();
        if (BeachPieces.Count == 0 || GroundPieces.Count == 0)
            throw new InvalidOperationException("Game_Terrain3 island source pieces were not found. Expected beach and ground prefab instances in " + SourceScenePath);

        Scene generatedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(generatedScene);

        GameObject root = new("Game_Terrain3_Island2048_Root");
        BuildLightingAndCamera(root);
        BuildPrefabOnlyIsland(root);

        EditorSceneManager.SaveScene(generatedScene, ScenePath);
        WriteLayoutJson();
        WriteReport();
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_GAME_TERRAIN3_ISLAND2048_PREFAB_ONLY_BUILT beachSource={BeachPieces.Count} groundSource={GroundPieces.Count} detailGrassSource={DetailGrassPieces.Count} placed={PlacedPieces.Count} scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/Design/Capture Game Terrain3 Island 2048")]
    public static void CaptureCurrentScene()
    {
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            throw new InvalidOperationException("Unable to open generated island scene: " + ScenePath);

        CaptureCamera("Camera_TopDown_2048Proof", 2048, 2048, CaptureRoot + "/game_terrain3_island2048_topdown_2048.png");
        CaptureCamera("Camera_Playable_Angled", 1920, 1080, CaptureRoot + "/game_terrain3_island2048_playable_angle_1920x1080.png");
        Debug.Log($"WARLINECAPTURE_GAME_TERRAIN3_ISLAND2048_CAPTURED root={CaptureRoot}");
    }

    [MenuItem("WarlineCapture/Design/Audit Game Terrain3 Island 2048")]
    public static void AuditCurrentScene()
    {
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            throw new InvalidOperationException("Unable to open generated island scene: " + ScenePath);

        List<Bounds> grassBounds = new();
        List<Bounds> beachBounds = new();
        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            string rootName = FindIslandInstanceName(renderer.transform);
            if (rootName.StartsWith("Ground", StringComparison.Ordinal))
                grassBounds.Add(renderer.bounds);
            else if (rootName.StartsWith("Beach", StringComparison.Ordinal))
                beachBounds.Add(renderer.bounds);
        }

        int interiorSamples = 0;
        int interiorCovered = 0;
        int shoreSamples = 0;
        int shoreBeachCovered = 0;
        int shoreGroundIntrusions = 0;
        const float sampleStep = 32f;
        for (float z = -HalfMapSize; z <= HalfMapSize; z += sampleStep)
        {
            for (float x = -HalfMapSize; x <= HalfMapSize; x += sampleStep)
            {
                Vector2 p = new(x, z);
                if (!EvaluateIsland(p, out float depth, out _))
                    continue;

                bool grass = ContainsXZ(grassBounds, x, z);
                bool beach = ContainsXZ(beachBounds, x, z);
                if (depth >= 0.22f)
                {
                    interiorSamples++;
                    if (grass)
                        interiorCovered++;
                }
                else if (depth <= 0.12f)
                {
                    shoreSamples++;
                    if (beach)
                        shoreBeachCovered++;
                    if (grass)
                        shoreGroundIntrusions++;
                }
            }
        }

        string reportPath = CaptureRoot + "/game_terrain3_island2048_audit.txt";
        StringBuilder report = new();
        report.AppendLine("Game_Terrain3_Island2048 audit");
        report.AppendLine("Renderer counts:");
        report.AppendLine("- Ground renderers: " + grassBounds.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Beach renderers: " + beachBounds.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("Coverage samples:");
        report.AppendLine("- Interior samples: " + interiorSamples.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Interior covered by grass bounds: " + interiorCovered.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Interior uncovered samples: " + (interiorSamples - interiorCovered).ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Shore samples: " + shoreSamples.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Shore covered by beach bounds: " + shoreBeachCovered.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Shore missing beach samples: " + (shoreSamples - shoreBeachCovered).ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Shore samples touched by ground bounds: " + shoreGroundIntrusions.ToString(CultureInfo.InvariantCulture));
        File.WriteAllText(ProjectPath(reportPath), report.ToString());
        Debug.Log(report.ToString());
    }

    private static void CollectSourcePieces()
    {
        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        foreach (GameObject root in sourceScene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(transform.gameObject);
                if (instanceRoot == null || instanceRoot != transform.gameObject)
                    continue;

                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instanceRoot);
                if (prefab == null)
                    prefab = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);
                if (prefab == null)
                    continue;

                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                if (!IsIslandSurfacePrefab(prefabPath, instanceRoot.name, out SourcePieceRole role))
                    continue;

                var piece = new SourcePiece(
                    prefabPath,
                    instanceRoot.name,
                    instanceRoot.transform.position,
                    instanceRoot.transform.rotation,
                    instanceRoot.transform.localScale,
                    role);

                if (role == SourcePieceRole.Beach)
                    BeachPieces.Add(piece);
                else if (role == SourcePieceRole.Ground)
                    GroundPieces.Add(piece);
                else
                    DetailGrassPieces.Add(piece);

                UniquePrefabPaths.Add(prefabPath);
            }
        }

        if (BeachPieces.Count == 0 || GroundPieces.Count == 0)
            AddFallbackSourcePiecesFromKnownGameTerrain3PrefabAssets();
    }

    private static void AddFallbackSourcePiecesFromKnownGameTerrain3PrefabAssets()
    {
        AddFallbackSourcePiece("Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Beach_01.prefab", SourcePieceRole.Beach);
        AddFallbackSourcePiece("Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Beach_Staight_01.prefab", SourcePieceRole.Beach);
        AddFallbackSourcePiece("Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Beach_Staight_02.prefab", SourcePieceRole.Beach);
        AddFallbackSourcePiece("Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Circle_01.prefab", SourcePieceRole.Ground);
        AddFallbackSourcePiece("Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Circle_02.prefab", SourcePieceRole.Ground);
        AddFallbackSourcePiece("Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Square_01.prefab", SourcePieceRole.Ground);
        AddFallbackSourcePiece("Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Grass_Patch_01.prefab", SourcePieceRole.DetailGrass);
        AddFallbackSourcePiece("Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Grass_Patch_02.prefab", SourcePieceRole.DetailGrass);
    }

    private static void AddFallbackSourcePiece(string prefabPath, SourcePieceRole role)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return;

        var piece = new SourcePiece(
            prefabPath,
            Path.GetFileNameWithoutExtension(prefabPath),
            Vector3.zero,
            Quaternion.identity,
            Vector3.one,
            role);

        if (role == SourcePieceRole.Beach)
            BeachPieces.Add(piece);
        else if (role == SourcePieceRole.Ground)
            GroundPieces.Add(piece);
        else
            DetailGrassPieces.Add(piece);

        UniquePrefabPaths.Add(prefabPath);
    }

    private static bool IsIslandSurfacePrefab(string prefabPath, string objectName, out SourcePieceRole role)
    {
        role = SourcePieceRole.DetailGrass;
        if (string.IsNullOrEmpty(prefabPath))
            return false;

        string haystack = (prefabPath + " " + objectName).ToLowerInvariant();
        if (!haystack.Contains("assets/synty/polygonbattleroyale/prefabs/"))
            return false;

        if (haystack.Contains("beach"))
        {
            role = SourcePieceRole.Beach;
            return true;
        }

        if (haystack.Contains("sm_env_grass"))
        {
            role = SourcePieceRole.Ground;
            return true;
        }

        if (haystack.Contains("sm_generic_grass_patch") || haystack.Contains("sm_gerneric_grass_patch"))
        {
            role = SourcePieceRole.DetailGrass;
            return true;
        }

        return false;
    }

    private static void BuildLightingAndCamera(GameObject root)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.76f, 0.78f, 0.68f, 1f);
        RenderSettings.skybox = null;

        Light key = Child(root, "DirectionalLight_Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.45f;
        key.color = new Color(1f, 0.92f, 0.74f, 1f);
        key.shadows = LightShadows.None;
        key.transform.rotation = Quaternion.Euler(52f, -38f, 0f);

        Camera top = Child(root, "Camera_TopDown_2048Proof").AddComponent<Camera>();
        top.transform.position = new Vector3(0f, 1850f, 0f);
        top.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        top.orthographic = true;
        top.orthographicSize = 1100f;
        top.nearClipPlane = 0.1f;
        top.farClipPlane = 3000f;
        top.clearFlags = CameraClearFlags.SolidColor;
        top.backgroundColor = new Color(0.14f, 0.34f, 0.45f, 1f);

        Camera play = Child(root, "Camera_Playable_Angled").AddComponent<Camera>();
        play.transform.position = new Vector3(-620f, 540f, -760f);
        play.transform.rotation = Quaternion.Euler(54f, 38f, 0f);
        play.fieldOfView = 38f;
        play.nearClipPlane = 0.1f;
        play.farClipPlane = 2600f;
        play.clearFlags = CameraClearFlags.SolidColor;
        play.backgroundColor = top.backgroundColor;
    }

    private static void BuildPrefabOnlyIsland(GameObject root)
    {
        GameObject island = Child(root, "ExpandedIsland_SourceGameTerrain3PrefabsOnly");
        GameObject ground = Child(island, "GroundFill_FromGameTerrain3Prefabs");
        GameObject beaches = Child(island, "Beaches_FromGameTerrain3Prefabs");
        GameObject details = Child(island, "DetailGrass_FromGameTerrain3Prefabs");

        PlaceGroundFill(ground.transform);
        PlaceShoreGroundOverlap(ground.transform);
        PlaceCoastalBeaches(beaches.transform);
        PlaceDetailGrassDecor(details.transform);
    }

    private static void PlaceGroundFill(Transform parent)
    {
        int index = 0;
        for (float z = -900f; z <= 900f; z += GroundFillSpacing)
        {
            int row = Mathf.RoundToInt((z + 900f) / GroundFillSpacing);
            float rowOffset = row % 2 == 0 ? 0f : GroundFillSpacing * 0.5f;
            for (float x = -930f + rowOffset; x <= 930f; x += GroundFillSpacing)
            {
                int cellIndex = index++;
                float jx = (Hash01(cellIndex, row, 17) - 0.5f) * 5f;
                float jz = (Hash01(cellIndex, row, 29) - 0.5f) * 5f;
                Vector2 p = new(x + jx, z + jz);
                if (!EvaluateIsland(p, out float depth, out _))
                    continue;
                if (depth < 0.14f)
                    continue;

                SourcePiece source = Pick(GroundPieces, cellIndex, row, 53);
                float yaw = SourceYaw(source) + Mathf.Round(Hash01(cellIndex, row, 67) * 3f) * 90f;
                InstantiateSource(parent, source, new Vector3(p.x, source.Position.y, p.y), yaw, "GroundFill");
            }
        }
    }

    private static void PlaceShoreGroundOverlap(Transform parent)
    {
        const int ringCount = 360;
        for (int i = 0; i < ringCount; i++)
        {
            float angle = (i / (float)ringCount) * Mathf.PI * 2f + (Hash01(i, 0, 211) - 0.5f) * 0.025f;
            Vector2 boundary = BoundaryPoint(angle);
            Vector2 inward = -new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
            Vector2 tangent = new(-inward.y, inward.x);
            Vector2 position = boundary + inward * (44f + Hash01(i, 0, 223) * 46f) + tangent * ((Hash01(i, 0, 227) - 0.5f) * ShoreGroundSpacing);

            if (!EvaluateIsland(position, out float depth, out _) || depth < 0.08f)
                continue;

            SourcePiece source = Pick(GroundPieces, i, 0, 239);
            float yaw = SourceYaw(source) + Mathf.Round(Hash01(i, 0, 241) * 3f) * 90f;
            InstantiateSource(parent, source, new Vector3(position.x, source.Position.y, position.y), yaw, "GroundShore");
        }
    }

    private static void PlaceCoastalBeaches(Transform parent)
    {
        const int ringCount = 540;
        for (int i = 0; i < ringCount; i++)
        {
            float t = i / (float)ringCount;
            float angle = t * Mathf.PI * 2f;
            Vector2 boundary = BoundaryPoint(angle);
            Vector2 inward = -new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
            float lateralNoise = (Hash01(i, 0, 101) - 0.5f) * 8f;
            Vector2 tangent = new(-inward.y, inward.x);
            Vector2 position = boundary + inward * (2f + Hash01(i, 0, 113) * 18f) + tangent * lateralNoise;

            SourcePiece source = Pick(BeachPieces, i, 0, 127);
            float yaw = angle * Mathf.Rad2Deg + 90f + Mathf.Round((Hash01(i, 0, 139) - 0.5f) * 2f) * 12f;
            InstantiateSource(parent, source, new Vector3(position.x, source.Position.y, position.y), yaw, "BeachCoast");

            if (i % 2 == 0)
            {
                SourcePiece secondary = Pick(BeachPieces, i, 1, 151);
                Vector2 p2 = boundary + inward * (22f + Hash01(i, 1, 163) * 28f) - tangent * lateralNoise * 0.45f;
                InstantiateSource(parent, secondary, new Vector3(p2.x, secondary.Position.y, p2.y), yaw + 180f, "BeachBlend");
            }
        }
    }

    private static void PlaceDetailGrassDecor(Transform parent)
    {
        if (DetailGrassPieces.Count == 0)
            return;

        int index = 0;
        for (float z = -830f; z <= 830f; z += DetailGrassSpacing)
        {
            int row = Mathf.RoundToInt((z + 830f) / DetailGrassSpacing);
            float rowOffset = row % 2 == 0 ? 0f : DetailGrassSpacing * 0.5f;
            for (float x = -860f + rowOffset; x <= 860f; x += DetailGrassSpacing)
            {
                if (Hash01(index, row, 307) < 0.46f)
                {
                    index++;
                    continue;
                }

                float jx = (Hash01(index, row, 311) - 0.5f) * 42f;
                float jz = (Hash01(index, row, 313) - 0.5f) * 42f;
                Vector2 p = new(x + jx, z + jz);
                if (!EvaluateIsland(p, out float depth, out _) || depth < 0.24f)
                {
                    index++;
                    continue;
                }

                SourcePiece source = Pick(DetailGrassPieces, index, row, 317);
                float yaw = SourceYaw(source) + Hash01(index, row, 331) * 360f;
                InstantiateSource(parent, source, new Vector3(p.x, source.Position.y, p.y), yaw, "GrassDetail");
                index++;
            }
        }
    }

    private static void InstantiateSource(Transform parent, SourcePiece source, Vector3 position, float yaw, string prefix)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source.PrefabPath);
        if (prefab == null)
            throw new FileNotFoundException("Missing source prefab from Game_Terrain3", source.PrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = $"{prefix}_{Path.GetFileNameWithoutExtension(source.PrefabPath)}_{PlacedPieces.Count.ToString("0000", CultureInfo.InvariantCulture)}";
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localScale = ScaledSourceScale(source, prefix);
        PlacedPieces.Add(new PlacedPiece(source.PrefabPath, prefix, position, yaw));
    }

    private static Vector3 ScaledSourceScale(SourcePiece source, string prefix)
    {
        float scaleXZ = source.Role switch
        {
            SourcePieceRole.Beach => BeachSurfaceScaleXZ,
            SourcePieceRole.DetailGrass => DetailGrassScaleXZ,
            _ => GroundSurfaceScaleXZ
        };
        if (prefix.StartsWith("GroundShore", StringComparison.Ordinal))
            scaleXZ = ShoreGroundScaleXZ;

        Vector3 scaled = source.Scale;
        scaled.x *= scaleXZ;
        scaled.z *= scaleXZ;
        return scaled;
    }

    private static SourcePiece Pick(List<SourcePiece> pieces, int a, int b, int salt)
    {
        int index = Mathf.FloorToInt(Hash01(a, b, salt) * pieces.Count);
        return pieces[Mathf.Clamp(index, 0, pieces.Count - 1)];
    }

    private static bool EvaluateIsland(Vector2 p, out float depth, out float edgeNoise)
    {
        float angle = Mathf.Atan2(p.y, p.x);
        edgeNoise = 0.075f * Mathf.Sin(angle * 3.0f + 0.45f)
            + 0.055f * Mathf.Sin(angle * 5.0f - 1.2f)
            + 0.030f * Mathf.Sin(angle * 8.0f + 2.1f)
            + 0.018f * Mathf.Cos(angle * 13.0f - 0.7f);
        float normalized = Mathf.Sqrt((p.x * p.x) / (IslandRadiusX * IslandRadiusX) + (p.y * p.y) / (IslandRadiusZ * IslandRadiusZ));
        depth = 1f + edgeNoise - normalized;
        return depth >= 0f;
    }

    private static Vector2 BoundaryPoint(float angle)
    {
        Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
        float low = 0f;
        float high = 1100f;
        for (int i = 0; i < 18; i++)
        {
            float mid = (low + high) * 0.5f;
            if (EvaluateIsland(direction * mid, out _, out _))
                low = mid;
            else
                high = mid;
        }
        return direction * low;
    }

    private static float SourceYaw(SourcePiece source)
    {
        return source.Rotation.eulerAngles.y;
    }

    private static float Hash01(int x, int z, int seed)
    {
        unchecked
        {
            uint h = (uint)seed;
            h ^= (uint)(x + 0x9e3779b9) + (h << 6) + (h >> 2);
            h ^= (uint)(z + 0x85ebca6b) + (h << 6) + (h >> 2);
            h ^= h >> 16;
            h *= 0x7feb352d;
            h ^= h >> 15;
            h *= 0x846ca68b;
            h ^= h >> 16;
            return (h & 0x00ffffff) / 16777215f;
        }
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void CaptureCamera(string cameraName, int width, int height, string outputPath)
    {
        Camera camera = GameObject.Find(cameraName)?.GetComponent<Camera>();
        if (camera == null)
            throw new InvalidOperationException("Capture camera not found: " + cameraName);

        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture texture = new(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
        Texture2D image = new(width, height, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = texture;
            RenderTexture.active = texture;
            camera.Render();
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(ProjectPath(outputPath), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    private static string FindIslandInstanceName(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            string name = current.gameObject.name;
            if (name.StartsWith("GroundFill_", StringComparison.Ordinal)
                || name.StartsWith("GroundShore_", StringComparison.Ordinal)
                || name.StartsWith("GrassDetail_", StringComparison.Ordinal)
                || name.StartsWith("BeachCoast_", StringComparison.Ordinal)
                || name.StartsWith("BeachBlend_", StringComparison.Ordinal))
                return name;
            current = current.parent;
        }

        return string.Empty;
    }

    private static bool ContainsXZ(List<Bounds> boundsList, float x, float z)
    {
        foreach (Bounds bounds in boundsList)
        {
            if (x >= bounds.min.x && x <= bounds.max.x && z >= bounds.min.z && z <= bounds.max.z)
                return true;
        }

        return false;
    }

    private static void WriteLayoutJson()
    {
        int beachPlaced = 0;
        int groundPlaced = 0;
        int detailGrassPlaced = 0;
        foreach (PlacedPiece piece in PlacedPieces)
        {
            if (piece.Kind.StartsWith("Beach", StringComparison.Ordinal))
                beachPlaced++;
            else if (piece.Kind.StartsWith("GrassDetail", StringComparison.Ordinal))
                detailGrassPlaced++;
            else
                groundPlaced++;
        }

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"mapId\": \"Game_Terrain3_Island2048\",");
        json.AppendLine("  \"sourceSceneReference\": \"" + SourceScenePath + "\",");
        json.AppendLine("  \"generationRule\": \"Source-prefab-only expansion. No generated terrain meshes and no substitute prefabs.\",");
        json.AppendLine("  \"mapSize\": 2048,");
        json.AppendLine("  \"sourceBeachPrefabInstances\": " + BeachPieces.Count.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"sourceGroundPrefabInstances\": " + GroundPieces.Count.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"sourceDetailGrassPrefabInstances\": " + DetailGrassPieces.Count.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedPrefabInstances\": " + PlacedPieces.Count.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedBeachPrefabInstances\": " + beachPlaced.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedGroundPrefabInstances\": " + groundPlaced.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedDetailGrassPrefabInstances\": " + detailGrassPlaced.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"groundFillSpacing\": " + GroundFillSpacing.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"shoreGroundSpacing\": " + ShoreGroundSpacing.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"detailGrassSpacing\": " + DetailGrassSpacing.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"groundSurfaceScaleXZ\": " + GroundSurfaceScaleXZ.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"shoreGroundScaleXZ\": " + ShoreGroundScaleXZ.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"detailGrassScaleXZ\": " + DetailGrassScaleXZ.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"beachSurfaceScaleXZ\": " + BeachSurfaceScaleXZ.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"uniqueSourcePrefabAssets\": [");
        int i = 0;
        foreach (string path in UniquePrefabPaths)
        {
            string comma = i == UniquePrefabPaths.Count - 1 ? string.Empty : ",";
            json.AppendLine("    \"" + path + "\"" + comma);
            i++;
        }
        json.AppendLine("  ],");
        json.AppendLine("  \"pathfindingRule\": \"Use the same island boundary function from the editor builder for water/edge blocking until a runtime island builder extracts this placement algorithm.\"");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(LayoutJsonPath), json.ToString());
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# Game_Terrain3 Island 2048 Source-Prefab Expansion");
        report.AppendLine();
        report.AppendLine("Date: 2026-05-25");
        report.AppendLine();
        report.AppendLine("Task: Expand the small `Game_Terrain3` island into a 2048x2048 island using the same prefab assets and more placements, without scaling the original island up.");
        report.AppendLine();
        report.AppendLine("Outputs:");
        report.AppendLine("- `" + ScenePath + "`");
        report.AppendLine("- `" + LayoutJsonPath + "`");
        report.AppendLine();
        report.AppendLine("Rules enforced:");
        report.AppendLine("- No generated island underlay mesh.");
        report.AppendLine("- No substitute terrain prefab set.");
        report.AppendLine("- Uses only beach/ground/detail grass prefab assets discovered in `Game_Terrain3`.");
        report.AppendLine("- `SM_Env_Grass_*` prefabs are classified as terrain ground/fill.");
        report.AppendLine("- `SM_Generic_Grass_Patch_*` prefabs are classified as decoration/detail grass, not terrain fill.");
        report.AppendLine("- Ground fill places every valid interior cell with jittered rows; it no longer randomly skips coverage cells.");
        report.AppendLine("- Beach placement uses a denser two-band rim to reduce shoreline gaps.");
        report.AppendLine("- Detail grass is a separate sparse decoration pass on top of the ground, never the primary floor.");
        report.AppendLine("- Prefab Y scale is copied from source instances; X/Z scale is expanded per role so neighboring pieces touch instead of leaving holes.");
        report.AppendLine();
        report.AppendLine("Counts:");
        report.AppendLine("- Source beach prefab instances: " + BeachPieces.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Source ground prefab instances: " + GroundPieces.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Source detail grass prefab instances: " + DetailGrassPieces.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Unique source prefab assets: " + UniquePrefabPaths.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Placed prefab instances: " + PlacedPieces.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Ground fill spacing: " + GroundFillSpacing.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Shore ground spacing: " + ShoreGroundSpacing.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Detail grass spacing: " + DetailGrassSpacing.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Ground X/Z scale multiplier: " + GroundSurfaceScaleXZ.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Shore ground X/Z scale multiplier: " + ShoreGroundScaleXZ.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Detail grass X/Z scale multiplier: " + DetailGrassScaleXZ.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Beach X/Z scale multiplier: " + BeachSurfaceScaleXZ.ToString(CultureInfo.InvariantCulture));
        report.AppendLine();
        report.AppendLine("Runtime transfer note: move the source-prefab collection into a prefab catalog, port `EvaluateIsland`, `BoundaryPoint`, and the placement loops to runtime, and instantiate pooled versions of the same source prefab ids.");
        File.WriteAllText(ProjectPath(ReportPath), report.ToString());
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), relativePath ?? string.Empty);
    }
}
#endif
