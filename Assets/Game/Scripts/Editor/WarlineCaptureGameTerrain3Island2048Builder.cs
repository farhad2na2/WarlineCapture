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
    private const string TargetScenePath = "Assets/Game/Scenes/Game_Terrain4.unity";
    private const string OldGeneratedScenePath = "Assets/Game/Scenes/Generated/Game_Terrain3_Island2048.unity";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GameTerrain3_Island2048";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GameTerrain3_Island2048";
    private const string LayoutJsonPath = DataRoot + "/game_terrain3_island2048_layout.json";
    private const string ReportPath = "Design/AgentReports/2026-05-25_gameplay_game-terrain3-island2048-builder.md";
    private const string MapPackRoot = "Design/VisualTargets/Gameplay/MapPacks/SyntyHighlands_01";
    private const string BaseVisualPath = MapPackRoot + "/base_visual.png";
    private const string SurfaceMaterialMaskPath = MapPackRoot + "/surface_material_mask.png";
    private const string TreeDensityMaskPath = MapPackRoot + "/tree_density_mask.png";
    private const string RockDensityMaskPath = MapPackRoot + "/rock_density_mask.png";
    private const string HeightMaskPath = MapPackRoot + "/height_mask.png";
    private const string GrassGreenMaterialPath = "Assets/Synty/PolygonBattleRoyale/Materials/PolygonBattleRoyale_01_A.mat";
    private const string DirtMaterialPath = "Assets/Synty/PolygonBattleRoyale/Materials/PolygonBattleRoyale_02_A.mat";
    private const string GrassDarkMaterialPath = "Assets/Synty/PolygonBattleRoyale/Materials/PolygonBattleRoyale_03_A.mat";
    private const float MapSize = 2048f;
    private const float HalfMapSize = MapSize * 0.5f;
    private const float GameplayMapExtent = 2023f;
    private const float HalfGameplayMapExtent = GameplayMapExtent * 0.5f;
    private const float GreenPlayableHalfExtentX = 1260f;
    private const float GreenPlayableHalfExtentZ = 1240f;
    private const float DetailGrassHalfExtent = 1180f;
    private const float IslandRadiusX = 1320f;
    private const float IslandRadiusZ = 1300f;
    private const float GroundFillSpacing = 18f;
    private const float ShoreGroundSpacing = 30f;
    private const float DetailGrassSpacing = 78f;
    private const float GroundSurfaceScaleXZ = 2.15f;
    private const float ShoreGroundScaleXZ = 1.45f;
    private const float DetailGrassScaleXZ = 0.95f;
    private const float BeachSurfaceScaleXZ = 2.5f;
    private const int MapGridSize = 2024;
    private const float MapGridMaxCoordinate = MapGridSize - 1f;
    private const int DensityMediumThreshold = 96;
    private const int DensityDenseThreshold = 176;
    private const int HeightHighThreshold = 208;

    private static readonly List<SourcePiece> BeachPieces = new();
    private static readonly List<SourcePiece> GroundPieces = new();
    private static readonly List<SourcePiece> DetailGrassPieces = new();
    private static readonly List<PlacedPiece> PlacedPieces = new();
    private static readonly HashSet<string> UniquePrefabPaths = new(StringComparer.Ordinal);
    private static readonly ReserveZoneSpec[] ReserveZoneSpecs =
    {
        new("CityReserve", 520, 720, 720, 560),
        new("NorthwestBaseReserve", 190, 1430, 430, 360),
        new("SoutheastBaseReserve", 1410, 250, 430, 360)
    };
    private static Material GrassGreenMaterial;
    private static Material DirtMaterial;
    private static Material GrassDarkMaterial;
    private static SurfaceReferenceMap SurfaceReference;

    private enum SourcePieceRole
    {
        Beach,
        Ground,
        DetailGrass
    }

    private enum SurfaceMaterialRole
    {
        SourceDefault,
        GrassGreen,
        Dirt,
        GrassDark,
        BeachSand
    }

    private enum SurfaceReferenceClass
    {
        GreenGrass,
        DarkGrass,
        Dirt,
        RockMountain,
        ForestCanopy,
        ReserveClear
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
        public readonly SurfaceMaterialRole MaterialRole;

        public PlacedPiece(string prefabPath, string kind, Vector3 position, float yaw, SurfaceMaterialRole materialRole)
        {
            PrefabPath = prefabPath;
            Kind = kind;
            Position = position;
            Yaw = yaw;
            MaterialRole = materialRole;
        }
    }

    private readonly struct ReserveZoneSpec
    {
        public readonly string Id;
        public readonly int XMin;
        public readonly int ZMin;
        public readonly int Width;
        public readonly int Height;

        public ReserveZoneSpec(string id, int xMin, int zMin, int width, int height)
        {
            Id = id;
            XMin = xMin;
            ZMin = zMin;
            Width = width;
            Height = height;
        }

        public bool IsValid => !string.IsNullOrEmpty(Id);

        public bool Contains(int gridX, int gridZ)
        {
            return gridX >= XMin && gridX < XMin + Width && gridZ >= ZMin && gridZ < ZMin + Height;
        }
    }

    [MenuItem("WarlineCapture/Design/Build Game Terrain4 Island 2048")]
    public static void BuildScene()
    {
        BeachPieces.Clear();
        GroundPieces.Clear();
        DetailGrassPieces.Clear();
        PlacedPieces.Clear();
        UniquePrefabPaths.Clear();
        SurfaceReference = null;

        Directory.CreateDirectory(ProjectPath(DataRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ReportPath)));

        CollectSourcePieces();
        LoadSurfaceMaterials();
        SurfaceReference = SurfaceReferenceMap.Load();
        if (BeachPieces.Count == 0 || GroundPieces.Count == 0)
            throw new InvalidOperationException("Game_Terrain3 island source pieces were not found. Expected beach and ground prefab instances in " + SourceScenePath);

        Scene targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        if (!targetScene.IsValid())
            throw new InvalidOperationException("Unable to open target scene: " + TargetScenePath);

        GameObject islandRoot = FindRootGameObject(targetScene, "Island");
        if (islandRoot == null)
            throw new InvalidOperationException("Target scene does not contain a root GameObject named Island: " + TargetScenePath);

        islandRoot.SetActive(true);
        islandRoot.transform.localPosition = Vector3.zero;
        islandRoot.transform.localRotation = Quaternion.identity;
        islandRoot.transform.localScale = Vector3.one;
        ClearChildren(islandRoot.transform);
        BuildPrefabOnlyIsland(islandRoot);

        EditorSceneManager.SaveScene(targetScene, TargetScenePath);
        WriteLayoutJson();
        WriteReport();
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_GAME_TERRAIN4_ISLAND2048_PREFAB_ONLY_BUILT beachSource={BeachPieces.Count} groundSource={GroundPieces.Count} detailGrassSource={DetailGrassPieces.Count} placed={PlacedPieces.Count} scene={TargetScenePath}");
    }

    [MenuItem("WarlineCapture/Design/Capture Game Terrain3 Island 2048")]
    public static void CaptureCurrentScene()
    {
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            throw new InvalidOperationException("Unable to open island target scene: " + TargetScenePath);

        RenderSettings.fog = false;
        EnsureCaptureCameras();
        CaptureCamera("Camera_TopDown_2048Proof", 2048, 2048, CaptureRoot + "/game_terrain3_island2048_topdown_2048.png");
        CaptureCamera("Camera_Playable_Angled", 1920, 1080, CaptureRoot + "/game_terrain3_island2048_playable_angle_1920x1080.png");
        Debug.Log($"WARLINECAPTURE_GAME_TERRAIN3_ISLAND2048_CAPTURED root={CaptureRoot}");
    }

    [MenuItem("WarlineCapture/Design/Audit Game Terrain3 Island 2048")]
    public static void AuditCurrentScene()
    {
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            throw new InvalidOperationException("Unable to open island target scene: " + TargetScenePath);

        List<Bounds> grassBounds = new();
        List<Bounds> beachBounds = new();
        Dictionary<string, int> materialRendererCounts = new(StringComparer.Ordinal);
        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            string rootName = FindIslandInstanceName(renderer.transform);
            if (rootName.StartsWith("Ground", StringComparison.Ordinal))
                grassBounds.Add(renderer.bounds);
            else if (rootName.StartsWith("Beach", StringComparison.Ordinal))
                beachBounds.Add(renderer.bounds);

            if (!string.IsNullOrEmpty(rootName))
            {
                string materialName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "None";
                if (!materialRendererCounts.ContainsKey(materialName))
                    materialRendererCounts[materialName] = 0;
                materialRendererCounts[materialName]++;
            }
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
        report.AppendLine("Island renderer material counts:");
        foreach (KeyValuePair<string, int> entry in materialRendererCounts)
            report.AppendLine("- " + entry.Key + ": " + entry.Value.ToString(CultureInfo.InvariantCulture));
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

    private static void LoadSurfaceMaterials()
    {
        GrassGreenMaterial = AssetDatabase.LoadAssetAtPath<Material>(GrassGreenMaterialPath);
        DirtMaterial = AssetDatabase.LoadAssetAtPath<Material>(DirtMaterialPath);
        GrassDarkMaterial = AssetDatabase.LoadAssetAtPath<Material>(GrassDarkMaterialPath);

        if (GrassGreenMaterial == null)
            throw new FileNotFoundException("Missing Game_Terrain3 green grass material", GrassGreenMaterialPath);
        if (DirtMaterial == null)
            throw new FileNotFoundException("Missing Game_Terrain3 dirt material", DirtMaterialPath);
        if (GrassDarkMaterial == null)
            throw new FileNotFoundException("Missing Game_Terrain3 dark grass/beach material", GrassDarkMaterialPath);
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
        RenderSettings.fog = false;

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
        top.orthographicSize = 1520f;
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
        PlaceTargetedSeamBeaches(beaches.transform);
        PlaceDetailGrassDecor(details.transform);
    }

    private static void PlaceGroundFill(Transform parent)
    {
        int index = 0;
        for (float z = -GreenPlayableHalfExtentZ; z <= GreenPlayableHalfExtentZ; z += GroundFillSpacing)
        {
            int row = Mathf.RoundToInt((z + GreenPlayableHalfExtentZ) / GroundFillSpacing);
            float rowOffset = row % 2 == 0 ? 0f : GroundFillSpacing * 0.5f;
            for (float x = -GreenPlayableHalfExtentX + rowOffset; x <= GreenPlayableHalfExtentX; x += GroundFillSpacing)
            {
                int cellIndex = index++;
                float jx = (Hash01(cellIndex, row, 17) - 0.5f) * 5f;
                float jz = (Hash01(cellIndex, row, 29) - 0.5f) * 5f;
                Vector2 p = new(x + jx, z + jz);
                if (!EvaluateIsland(p, out float depth, out _))
                    continue;
                if (depth < 0.08f)
                    continue;

                SourcePiece source = Pick(GroundPieces, cellIndex, row, 53);
                float yaw = SourceYaw(source) + Mathf.Round(Hash01(cellIndex, row, 67) * 3f) * 90f;
                InstantiateSource(parent, source, new Vector3(p.x, source.Position.y, p.y), yaw, "GroundFill");
            }
        }
    }

    private static void PlaceShoreGroundOverlap(Transform parent)
    {
        const int ringCount = 1440;
        for (int i = 0; i < ringCount; i++)
        {
            float angle = (i / (float)ringCount) * Mathf.PI * 2f + (Hash01(i, 0, 211) - 0.5f) * 0.025f;
            Vector2 boundary = BoundaryPoint(angle);
            Vector2 inward = -new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
            Vector2 tangent = new(-inward.y, inward.x);
            Vector2 position = boundary + inward * (8f + Hash01(i, 0, 223) * 70f) + tangent * ((Hash01(i, 0, 227) - 0.5f) * ShoreGroundSpacing * 0.45f);

            if (!EvaluateIsland(position, out float depth, out _) || depth < -0.015f)
                continue;

            SourcePiece source = Pick(GroundPieces, i, 0, 239);
            float yaw = SourceYaw(source) + Mathf.Round(Hash01(i, 0, 241) * 3f) * 90f;
            InstantiateSource(parent, source, new Vector3(position.x, source.Position.y, position.y), yaw, "GroundShore");
        }
    }

    private static void PlaceCoastalBeaches(Transform parent)
    {
        const int ringCount = 1500;
        for (int i = 0; i < ringCount; i++)
        {
            float t = i / (float)ringCount;
            float angle = t * Mathf.PI * 2f;
            Vector2 boundary = BoundaryPoint(angle);
            Vector2 inward = -new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
            Vector2 outward = -inward;
            float lateralNoise = (Hash01(i, 0, 101) - 0.5f) * 8f;
            Vector2 tangent = new(-inward.y, inward.x);
            Vector2 position = boundary + outward * (10f + Hash01(i, 0, 113) * 22f) + tangent * lateralNoise;
            position = PushBeachCenterOutsideGameplayMap(position);

            SourcePiece source = Pick(BeachPieces, i, 0, 127);
            float yaw = angle * Mathf.Rad2Deg + 90f + Mathf.Round((Hash01(i, 0, 139) - 0.5f) * 2f) * 12f;
            InstantiateSource(parent, source, new Vector3(position.x, source.Position.y, position.y), yaw, "BeachCoast");

            SourcePiece secondary = Pick(BeachPieces, i, 1, 151);
            Vector2 p2 = boundary + inward * (24f + Hash01(i, 1, 163) * 36f) - tangent * lateralNoise * 0.45f;
            p2 = PushBeachCenterOutsideGameplayMap(p2);
            InstantiateSource(parent, secondary, new Vector3(p2.x, secondary.Position.y, p2.y), yaw + 180f, "BeachBlend");

            if (i % 2 == 0)
            {
                SourcePiece inner = Pick(BeachPieces, i, 2, 167);
                Vector2 p3 = boundary + inward * (58f + Hash01(i, 2, 173) * 46f) + tangent * lateralNoise * 0.25f;
                p3 = PushBeachCenterOutsideGameplayMap(p3);
                InstantiateSource(parent, inner, new Vector3(p3.x, inner.Position.y, p3.y), yaw + 90f, "BeachInner");

                SourcePiece landEdge = Pick(BeachPieces, i, 3, 181);
                Vector2 p4 = boundary + inward * (96f + Hash01(i, 3, 191) * 48f) - tangent * lateralNoise * 0.2f;
                p4 = PushBeachCenterOutsideGameplayMap(p4);
                InstantiateSource(parent, landEdge, new Vector3(p4.x, landEdge.Position.y, p4.y), yaw - 90f, "BeachLandEdge");
            }
        }
    }

    private static Vector2 PushBeachCenterOutsideGameplayMap(Vector2 position)
    {
        const float margin = 10f;
        if (Mathf.Abs(position.x) > HalfGameplayMapExtent || Mathf.Abs(position.y) > HalfGameplayMapExtent)
            return position;

        float limit = HalfGameplayMapExtent + margin;
        if (Mathf.Abs(position.x) > Mathf.Abs(position.y))
            position.x = Mathf.Sign(position.x == 0f ? 1f : position.x) * limit;
        else
            position.y = Mathf.Sign(position.y == 0f ? 1f : position.y) * limit;

        return position;
    }

    private static void PlaceTargetedSeamBeaches(Transform parent)
    {
        Vector2[] seamPatches =
        {
            new(-520f, -1258f),
            new(-480f, -1258f),
            new(-440f, -1256f),
            new(-400f, -1254f),
            new(-360f, -1252f),
            new(-430f, -1190f),
            new(-390f, -1188f),
            new(-350f, -1186f),
            new(-330f, -1248f),
            new(-290f, -1244f),
            new(-250f, -1242f),
            new(-210f, -1238f),
            new(1224f, 650f),
            new(1228f, 610f),
            new(1232f, 570f),
            new(1258f, 650f),
            new(1262f, 610f),
            new(1264f, 570f),
            new(1268f, 530f),
            new(1270f, 490f),
            new(1272f, 450f)
        };

        for (int i = 0; i < seamPatches.Length; i++)
        {
            Vector2 p = PushBeachCenterOutsideGameplayMap(seamPatches[i]);
            SourcePiece source = Pick(BeachPieces, i, 4, 421);
            float yaw = i < 4 ? Hash01(i, 4, 431) * 18f : 90f + Hash01(i, 4, 433) * 18f;
            InstantiateSource(parent, source, new Vector3(p.x, source.Position.y, p.y), yaw, "BeachBlend");
        }
    }

    private static void PlaceDetailGrassDecor(Transform parent)
    {
        if (DetailGrassPieces.Count == 0)
            return;

        int index = 0;
        for (float z = -DetailGrassHalfExtent; z <= DetailGrassHalfExtent; z += DetailGrassSpacing)
        {
            int row = Mathf.RoundToInt((z + DetailGrassHalfExtent) / DetailGrassSpacing);
            float rowOffset = row % 2 == 0 ? 0f : DetailGrassSpacing * 0.5f;
            for (float x = -DetailGrassHalfExtent + rowOffset; x <= DetailGrassHalfExtent; x += DetailGrassSpacing)
            {
                float jx = (Hash01(index, row, 311) - 0.5f) * 42f;
                float jz = (Hash01(index, row, 313) - 0.5f) * 42f;
                Vector2 p = new(x + jx, z + jz);
                SurfaceReferenceClass surfaceClass = SurfaceReferenceClass.GreenGrass;
                if (SurfaceReference != null)
                    surfaceClass = SurfaceReference.ClassifyWorld(p);

                float keepChance = DetailGrassKeepChance(surfaceClass, p, index, row);
                if (Hash01(index, row, 307) > keepChance)
                {
                    index++;
                    continue;
                }

                if (!EvaluateIsland(p, out float depth, out _) || depth < 0.24f)
                {
                    index++;
                    continue;
                }

                SurfaceMaterialRole groundMaterial = ChooseGroundMaterial(new Vector3(p.x, 0f, p.y), "GroundFill");
                if (groundMaterial == SurfaceMaterialRole.Dirt)
                {
                    index++;
                    continue;
                }

                SourcePiece source = PickDetailGrass(index, row, 317);
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
        SurfaceMaterialRole materialRole = ResolveMaterialRole(source, position, prefix);
        ApplySurfaceMaterial(instance, materialRole);
        PlacedPieces.Add(new PlacedPiece(source.PrefabPath, prefix, position, yaw, materialRole));
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

    private static SourcePiece PickDetailGrass(int a, int b, int salt)
    {
        if (Hash01(a, b, salt + 7) < 0.78f)
        {
            List<SourcePiece> patch01 = new();
            foreach (SourcePiece piece in DetailGrassPieces)
            {
                if (piece.PrefabPath.IndexOf("SM_Generic_Grass_Patch_01", StringComparison.OrdinalIgnoreCase) >= 0
                    || piece.PrefabPath.IndexOf("SM_Gerneric_Grass_Patch_01", StringComparison.OrdinalIgnoreCase) >= 0)
                    patch01.Add(piece);
            }

            if (patch01.Count > 0)
                return Pick(patch01, a, b, salt + 11);
        }

        return Pick(DetailGrassPieces, a, b, salt);
    }

    private static SurfaceMaterialRole ResolveMaterialRole(SourcePiece source, Vector3 position, string prefix)
    {
        return source.Role switch
        {
            SourcePieceRole.Beach => SurfaceMaterialRole.BeachSand,
            SourcePieceRole.DetailGrass => SurfaceMaterialRole.GrassGreen,
            SourcePieceRole.Ground => ChooseGroundMaterial(position, prefix),
            _ => SurfaceMaterialRole.SourceDefault
        };
    }

    private static SurfaceMaterialRole ChooseGroundMaterial(Vector3 position, string prefix)
    {
        Vector2 p = new(position.x, position.z);
        if (!EvaluateIsland(p, out float depth, out _))
            return SurfaceMaterialRole.GrassGreen;

        if (prefix.StartsWith("GroundShore", StringComparison.Ordinal) || depth < 0.16f)
            return SurfaceMaterialRole.GrassGreen;

        SurfaceReferenceClass referenceClass = SurfaceReference != null
            ? SurfaceReference.ClassifyWorld(p)
            : SurfaceReferenceClass.GreenGrass;

        float dirtNoise = ValueNoise01(p, 210f, 701);
        float darkGrassNoise = ValueNoise01(p + new Vector2(83f, -57f), 165f, 709);
        float smallBreakup = ValueNoise01(p + new Vector2(-41f, 119f), 92f, 719);

        if (referenceClass == SurfaceReferenceClass.ReserveClear)
            return dirtNoise > 0.74f ? SurfaceMaterialRole.Dirt : SurfaceMaterialRole.GrassGreen;
        if (referenceClass == SurfaceReferenceClass.Dirt)
            return smallBreakup > 0.28f ? SurfaceMaterialRole.Dirt : SurfaceMaterialRole.GrassGreen;
        if (referenceClass == SurfaceReferenceClass.RockMountain)
            return smallBreakup > 0.32f ? SurfaceMaterialRole.Dirt : SurfaceMaterialRole.GrassDark;
        if (referenceClass == SurfaceReferenceClass.ForestCanopy)
            return darkGrassNoise > 0.34f ? SurfaceMaterialRole.GrassDark : SurfaceMaterialRole.GrassGreen;
        if (referenceClass == SurfaceReferenceClass.DarkGrass)
            return dirtNoise > 0.78f ? SurfaceMaterialRole.Dirt : SurfaceMaterialRole.GrassDark;

        if (depth > 0.24f && dirtNoise > 0.78f && smallBreakup > 0.62f)
            return SurfaceMaterialRole.Dirt;
        if (depth > 0.18f && darkGrassNoise > 0.78f)
            return SurfaceMaterialRole.GrassDark;

        return SurfaceMaterialRole.GrassGreen;
    }

    private static float DetailGrassKeepChance(SurfaceReferenceClass surfaceClass, Vector2 position, int index, int row)
    {
        if (ReserveAtWorld(position).IsValid)
            return 0.05f;

        float breakup = ValueNoise01(position + new Vector2(37f, -91f), 135f, 733);
        return surfaceClass switch
        {
            SurfaceReferenceClass.GreenGrass => Mathf.Lerp(0.54f, 0.78f, breakup),
            SurfaceReferenceClass.DarkGrass => Mathf.Lerp(0.62f, 0.86f, breakup),
            SurfaceReferenceClass.ForestCanopy => Mathf.Lerp(0.34f, 0.58f, breakup),
            SurfaceReferenceClass.RockMountain => Mathf.Lerp(0.08f, 0.22f, breakup),
            SurfaceReferenceClass.Dirt => Mathf.Lerp(0.04f, 0.16f, breakup),
            SurfaceReferenceClass.ReserveClear => 0.04f,
            _ => 0.46f
        };
    }

    private static void ApplySurfaceMaterial(GameObject instance, SurfaceMaterialRole materialRole)
    {
        Material material = materialRole switch
        {
            SurfaceMaterialRole.GrassGreen => GrassGreenMaterial,
            SurfaceMaterialRole.Dirt => DirtMaterial,
            SurfaceMaterialRole.GrassDark => GrassDarkMaterial,
            SurfaceMaterialRole.BeachSand => GrassDarkMaterial,
            _ => null
        };

        if (material == null)
            return;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
                continue;

            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static ReserveZoneSpec ReserveAtWorld(Vector2 position)
    {
        int gridX = WorldToGrid(position.x);
        int gridZ = WorldToGrid(position.y);
        foreach (ReserveZoneSpec zone in ReserveZoneSpecs)
        {
            if (zone.Contains(gridX, gridZ))
                return zone;
        }

        return default;
    }

    private static int WorldToGrid(float value)
    {
        return Mathf.Clamp(Mathf.RoundToInt(value + MapGridMaxCoordinate * 0.5f), 0, MapGridSize - 1);
    }

    private static bool EvaluateIsland(Vector2 p, out float depth, out float edgeNoise)
    {
        float angle = Mathf.Atan2(p.y, p.x);
        edgeNoise = 0.075f * Mathf.Sin(angle * 3.0f + 0.45f)
            + 0.055f * Mathf.Sin(angle * 5.0f - 1.2f)
            + 0.030f * Mathf.Sin(angle * 8.0f + 2.1f)
            + 0.018f * Mathf.Cos(angle * 13.0f - 0.7f);
        float normalizedX = Mathf.Abs(p.x) / IslandRadiusX;
        float normalizedZ = Mathf.Abs(p.y) / IslandRadiusZ;
        float normalized = Mathf.Pow(Mathf.Pow(normalizedX, 4f) + Mathf.Pow(normalizedZ, 4f), 0.25f);
        depth = 1f + edgeNoise - normalized;
        return depth >= 0f;
    }

    private static Vector2 BoundaryPoint(float angle)
    {
        Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
        float low = 0f;
        float high = Mathf.Max(IslandRadiusX, IslandRadiusZ) * 1.25f;
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

    private static float ValueNoise01(Vector2 p, float cellSize, int seed)
    {
        float gx = (p.x + HalfMapSize) / cellSize;
        float gz = (p.y + HalfMapSize) / cellSize;
        int x0 = Mathf.FloorToInt(gx);
        int z0 = Mathf.FloorToInt(gz);
        float tx = Smooth01(gx - x0);
        float tz = Smooth01(gz - z0);

        float a = Hash01(x0, z0, seed);
        float b = Hash01(x0 + 1, z0, seed);
        float c = Hash01(x0, z0 + 1, seed);
        float d = Hash01(x0 + 1, z0 + 1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
    }

    private static float Smooth01(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static GameObject FindRootGameObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        return null;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static void EnsureCaptureCameras()
    {
        if (GameObject.Find("Camera_TopDown_2048Proof") != null && GameObject.Find("Camera_Playable_Angled") != null)
            return;

        GameObject root = new("Game_Terrain4_Island2048_CaptureOnly");
        BuildLightingAndCamera(root);
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
                || name.StartsWith("BeachBlend_", StringComparison.Ordinal)
                || name.StartsWith("BeachInner_", StringComparison.Ordinal)
                || name.StartsWith("BeachLandEdge_", StringComparison.Ordinal)
                || name == "GroundFill_FromGameTerrain3Prefabs"
                || name == "Beaches_FromGameTerrain3Prefabs"
                || name == "DetailGrass_FromGameTerrain3Prefabs")
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
        int grassGreenPlaced = 0;
        int dirtPlaced = 0;
        int grassDarkPlaced = 0;
        int beachSandPlaced = 0;
        foreach (PlacedPiece piece in PlacedPieces)
        {
            if (piece.Kind.StartsWith("Beach", StringComparison.Ordinal))
                beachPlaced++;
            else if (piece.Kind.StartsWith("GrassDetail", StringComparison.Ordinal))
                detailGrassPlaced++;
            else
                groundPlaced++;

            if (piece.MaterialRole == SurfaceMaterialRole.GrassGreen)
                grassGreenPlaced++;
            else if (piece.MaterialRole == SurfaceMaterialRole.Dirt)
                dirtPlaced++;
            else if (piece.MaterialRole == SurfaceMaterialRole.GrassDark)
                grassDarkPlaced++;
            else if (piece.MaterialRole == SurfaceMaterialRole.BeachSand)
                beachSandPlaced++;
        }

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"mapId\": \"Game_Terrain3_Island2048\",");
        json.AppendLine("  \"sourceSceneReference\": \"" + SourceScenePath + "\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"generationRule\": \"Source-prefab-only expansion. No generated terrain meshes and no substitute prefabs.\",");
        json.AppendLine("  \"mapSize\": 2048,");
        json.AppendLine("  \"gameplayMapExtent\": " + GameplayMapExtent.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"playableGreenLandRule\": \"Green/dirt source-prefab terrain must fully cover the 2024 gameplay map footprint before beach/coast prefabs are placed outside it.\",");
        json.AppendLine("  \"greenPlayableHalfExtentX\": " + GreenPlayableHalfExtentX.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"greenPlayableHalfExtentZ\": " + GreenPlayableHalfExtentZ.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"islandRadiusX\": " + IslandRadiusX.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"islandRadiusZ\": " + IslandRadiusZ.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"sourceBeachPrefabInstances\": " + BeachPieces.Count.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"sourceGroundPrefabInstances\": " + GroundPieces.Count.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"sourceDetailGrassPrefabInstances\": " + DetailGrassPieces.Count.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedPrefabInstances\": " + PlacedPieces.Count.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedBeachPrefabInstances\": " + beachPlaced.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedGroundPrefabInstances\": " + groundPlaced.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedDetailGrassPrefabInstances\": " + detailGrassPlaced.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"materialRule\": \"Source-like material override pass: PolygonBattleRoyale_01_A green grass, PolygonBattleRoyale_02_A dirt patches, PolygonBattleRoyale_03_A darker grass and beaches.\",");
        json.AppendLine("  \"placedGrassGreenMaterialInstances\": " + grassGreenPlaced.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedDirtMaterialInstances\": " + dirtPlaced.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedGrassDarkMaterialInstances\": " + grassDarkPlaced.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"placedBeachSandMaterialInstances\": " + beachSandPlaced.ToString(CultureInfo.InvariantCulture) + ",");
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
        report.AppendLine("Step 2 update: the island foundation now targets a compact green/dirt playable interior. The 2024 gameplay map footprint is `2023x2023` world units, and the builder fills green/dirt terrain across `2520x2480` before placing a slightly overlapping beach ring outside the playable area.");
        report.AppendLine();
        report.AppendLine("Outputs:");
        report.AppendLine("- `" + TargetScenePath + "` under root GameObject `Island`");
        report.AppendLine("- `" + LayoutJsonPath + "`");
        report.AppendLine("- Removed standalone generated-scene target: `" + OldGeneratedScenePath + "`");
        report.AppendLine();
        report.AppendLine("Rules enforced:");
        report.AppendLine("- No generated island underlay mesh.");
        report.AppendLine("- No substitute terrain prefab set.");
        report.AppendLine("- Uses only beach/ground/detail grass prefab assets discovered in `Game_Terrain3`.");
        report.AppendLine("- Applies the same material-override pattern seen in `Game_Terrain3`: green grass uses `PolygonBattleRoyale_01_A`, dirt patches use `PolygonBattleRoyale_02_A`, and darker grass/beach areas use `PolygonBattleRoyale_03_A`.");
        report.AppendLine("- `SM_Env_Grass_*` prefabs are classified as terrain ground/fill.");
        report.AppendLine("- `SM_Generic_Grass_Patch_*` prefabs are classified as decoration/detail grass, not terrain fill; `SM_Generic_Grass_Patch_01` is preferred on green and darker grass areas.");
        report.AppendLine("- Ground fill places every valid interior cell with jittered rows; it no longer randomly skips coverage cells.");
        report.AppendLine("- Beach placement uses a denser two-band rim to reduce shoreline gaps.");
        report.AppendLine("- Green/dirt terrain is intentionally larger than the 2024 gameplay map target; beach/coast content is pushed to the outer island border.");
        report.AppendLine("- Detail grass is a separate sparse decoration pass on top of the ground, never the primary floor.");
        report.AppendLine("- Prefab Y scale is copied from source instances; X/Z scale is expanded per role so neighboring pieces touch instead of leaving holes.");
        report.AppendLine();
        report.AppendLine("Counts:");
        report.AppendLine("- Source beach prefab instances: " + BeachPieces.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Source ground prefab instances: " + GroundPieces.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Source detail grass prefab instances: " + DetailGrassPieces.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Unique source prefab assets: " + UniquePrefabPaths.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Placed prefab instances: " + PlacedPieces.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Green material placements: " + CountMaterialRole(SurfaceMaterialRole.GrassGreen).ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Dirt material placements: " + CountMaterialRole(SurfaceMaterialRole.Dirt).ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Dark grass material placements: " + CountMaterialRole(SurfaceMaterialRole.GrassDark).ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Beach material placements: " + CountMaterialRole(SurfaceMaterialRole.BeachSand).ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Ground fill spacing: " + GroundFillSpacing.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Gameplay map target extent: " + GameplayMapExtent.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Green playable half extent X/Z: " + GreenPlayableHalfExtentX.ToString(CultureInfo.InvariantCulture) + " / " + GreenPlayableHalfExtentZ.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("- Island radius X/Z: " + IslandRadiusX.ToString(CultureInfo.InvariantCulture) + " / " + IslandRadiusZ.ToString(CultureInfo.InvariantCulture));
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

    private sealed class SurfaceReferenceMap
    {
        private readonly Color32[] basePixels;
        private readonly int baseWidth;
        private readonly int baseHeight;
        private readonly int[] surfaceMask;
        private readonly int[] treeMask;
        private readonly int[] rockMask;
        private readonly int[] heightMask;

        private SurfaceReferenceMap(Color32[] basePixels, int baseWidth, int baseHeight, int[] surfaceMask, int[] treeMask, int[] rockMask, int[] heightMask)
        {
            this.basePixels = basePixels;
            this.baseWidth = baseWidth;
            this.baseHeight = baseHeight;
            this.surfaceMask = surfaceMask;
            this.treeMask = treeMask;
            this.rockMask = rockMask;
            this.heightMask = heightMask;
        }

        public static SurfaceReferenceMap Load()
        {
            Color32[] basePixels = LoadPixels(BaseVisualPath, out int baseWidth, out int baseHeight);
            int[] surface = File.Exists(ProjectPath(SurfaceMaterialMaskPath)) ? LoadLuminanceGrid(SurfaceMaterialMaskPath) : null;
            int[] tree = LoadLuminanceGrid(TreeDensityMaskPath);
            int[] rock = LoadLuminanceGrid(RockDensityMaskPath);
            int[] height = LoadLuminanceGrid(HeightMaskPath);
            return new SurfaceReferenceMap(basePixels, baseWidth, baseHeight, surface, tree, rock, height);
        }

        public SurfaceReferenceClass ClassifyWorld(Vector2 position)
        {
            int gridX = WorldToGrid(position.x);
            int gridZ = WorldToGrid(position.y);
            foreach (ReserveZoneSpec reserve in ReserveZoneSpecs)
            {
                if (reserve.Contains(gridX, gridZ))
                    return SurfaceReferenceClass.ReserveClear;
            }

            int index = gridZ * MapGridSize + gridX;
            if (surfaceMask != null)
                return ClassifySurfaceMask(surfaceMask[index]);

            int treeValue = treeMask[index];
            int rockValue = rockMask[index];
            int heightValue = heightMask[index];
            Color32 color = SampleBaseColor(gridX, gridZ);

            if (heightValue >= HeightHighThreshold || rockValue >= DensityDenseThreshold)
                return SurfaceReferenceClass.RockMountain;
            if (treeValue >= DensityDenseThreshold)
                return SurfaceReferenceClass.ForestCanopy;
            if (treeValue >= DensityMediumThreshold)
                return SurfaceReferenceClass.DarkGrass;

            float luminance = Luminance01(color);
            bool warm = color.r >= color.g && color.g >= color.b * 0.72f;
            bool mutedGreen = color.g >= color.r * 0.82f && color.g >= color.b * 0.82f;
            if (warm && luminance >= 0.36f)
                return SurfaceReferenceClass.Dirt;
            if (mutedGreen && luminance < 0.32f)
                return SurfaceReferenceClass.DarkGrass;
            if (luminance < 0.25f)
                return SurfaceReferenceClass.ForestCanopy;

            return SurfaceReferenceClass.GreenGrass;
        }

        private Color32 SampleBaseColor(int gridX, int gridZ)
        {
            int pixelX = Mathf.Clamp(Mathf.RoundToInt(gridX / MapGridMaxCoordinate * (baseWidth - 1)), 0, baseWidth - 1);
            int pixelY = Mathf.Clamp(Mathf.RoundToInt((1f - gridZ / MapGridMaxCoordinate) * (baseHeight - 1)), 0, baseHeight - 1);
            return basePixels[pixelY * baseWidth + pixelX];
        }

        private static SurfaceReferenceClass ClassifySurfaceMask(int value)
        {
            if (value < 43)
                return SurfaceReferenceClass.GreenGrass;
            if (value < 86)
                return SurfaceReferenceClass.DarkGrass;
            if (value < 129)
                return SurfaceReferenceClass.Dirt;
            if (value < 172)
                return SurfaceReferenceClass.RockMountain;
            if (value < 215)
                return SurfaceReferenceClass.ForestCanopy;
            return SurfaceReferenceClass.ReserveClear;
        }

        private static float Luminance01(Color32 color)
        {
            return (color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f) / 255f;
        }

        private static Color32[] LoadPixels(string path, out int width, out int height)
        {
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(ProjectPath(path))))
                    throw new InvalidOperationException("Unable to decode terrain reference image: " + path);

                width = texture.width;
                height = texture.height;
                return texture.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static int[] LoadLuminanceGrid(string path)
        {
            Color32[] pixels = LoadPixels(path, out int width, out int height);
            int[] values = new int[MapGridSize * MapGridSize];
            for (int gridZ = 0; gridZ < MapGridSize; gridZ++)
            {
                int pixelY = Mathf.Clamp(Mathf.RoundToInt((1f - gridZ / MapGridMaxCoordinate) * (height - 1)), 0, height - 1);
                int rowOffset = gridZ * MapGridSize;
                int pixelRow = pixelY * width;
                for (int gridX = 0; gridX < MapGridSize; gridX++)
                {
                    int pixelX = Mathf.Clamp(Mathf.RoundToInt(gridX / MapGridMaxCoordinate * (width - 1)), 0, width - 1);
                    Color32 color = pixels[pixelRow + pixelX];
                    values[rowOffset + gridX] = Mathf.Clamp(Mathf.RoundToInt(color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f), 0, 255);
                }
            }

            return values;
        }
    }

    private static int CountMaterialRole(SurfaceMaterialRole role)
    {
        int count = 0;
        foreach (PlacedPiece piece in PlacedPieces)
        {
            if (piece.MaterialRole == role)
                count++;
        }
        return count;
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), relativePath ?? string.Empty);
    }
}
#endif
