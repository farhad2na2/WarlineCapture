#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;

public static class WarlineCaptureIso2DRuntimePrototypeBuilder
{
    private const int CaptureWidth = 1920;
    private const int CaptureHeight = 1080;
    private const float PixelsPerUnit = 512f;
    private const string AssetRoot = "Assets/Game/Art/Generated/2DISO";
    private const string GoldenAssetRoot = AssetRoot + "/GoldenAssets";
    private const string TileAssetRoot = AssetRoot + "/Tiles";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/ISO02_CityCommand_RuntimePrototype.unity";
    private const string OutputRoot = "Design/VisualReferences/2DIsometricProduction/RuntimePrototype";
    private const string StartCaptureRelativePath = OutputRoot + "/ISO02_RuntimePrototype_Start.png";
    private const string MidCaptureRelativePath = OutputRoot + "/ISO02_RuntimePrototype_Mid.png";
    private const string EndCaptureRelativePath = OutputRoot + "/ISO02_RuntimePrototype_End.png";
    private const string ReportRelativePath = OutputRoot + "/ISO02_RuntimePrototype_Report.md";
    private const float GameplayCameraOrthographicSize = 3.45f;

    private sealed class PlacedSprite
    {
        public string Name;
        public SpriteRenderer Renderer;
        public WarlineCaptureIso2DPrototypeAgent Agent;
        public WarlineCaptureIso2DOverlayFollower Follower;
        public Transform FollowTarget;
        public Vector3 FollowOffset;
        public float ScreenHeight;
    }

    private sealed class AgentSnapshot
    {
        public string Name;
        public Vector3 StartPosition;
        public Vector3 MidPosition;
        public Vector3 EndPosition;
        public int StartSortingOrder;
        public int MidSortingOrder;
        public int EndSortingOrder;
    }

    private sealed class FrameValidation
    {
        public string Name;
        public float MaxOverlayDistance;
    }

    [MenuItem("WarlineCapture/Design/Build ISO-02 Runtime Prototype")]
    public static void BuildCaptureAndValidateRuntimePrototype()
    {
        var totalWatch = Stopwatch.StartNew();
        Directory.CreateDirectory(Path.Combine(ProjectRootPath(), TileAssetRoot));
        Directory.CreateDirectory(Path.Combine(ProjectRootPath(), Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(Path.Combine(ProjectRootPath(), OutputRoot));

        var importWatch = Stopwatch.StartNew();
        ImportGoldenSprites();
        AssetDatabase.Refresh();
        importWatch.Stop();

        var sprites = RequiredSpriteNames().ToDictionary(name => name, LoadSprite);
        CreateTerrainTiles(sprites);

        var buildWatch = Stopwatch.StartNew();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var placedSprites = BuildScene(sprites, out var terrainTileCount);
        var camera = BuildCamera();
        ApplyRuntimeState(placedSprites, 0f);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        buildWatch.Stop();

        var agents = placedSprites.Where(sprite => sprite.Agent != null).ToArray();
        var snapshots = agents.Select(sprite => new AgentSnapshot { Name = sprite.Name }).ToArray();
        var frameValidations = new List<FrameValidation>();

        var renderWatch = Stopwatch.StartNew();
        CaptureFrame("Start", 0f, StartCaptureRelativePath, camera, placedSprites, snapshots, frameValidations);
        CaptureFrame("Mid", 0.55f, MidCaptureRelativePath, camera, placedSprites, snapshots, frameValidations);
        CaptureFrame("End", 1f, EndCaptureRelativePath, camera, placedSprites, snapshots, frameValidations);
        renderWatch.Stop();

        totalWatch.Stop();

        var readabilityPass = MeasureReadability(camera, placedSprites);
        var movementPass = snapshots.All(snapshot => Vector3.Distance(snapshot.StartPosition, snapshot.EndPosition) > 0.75f) &&
            snapshots.All(snapshot => Vector3.Distance(snapshot.StartPosition, snapshot.MidPosition) > 0.25f);
        var sortingPass = snapshots.All(snapshot => snapshot.StartSortingOrder != snapshot.EndSortingOrder);
        var overlayFollowPass = frameValidations.All(frame => frame.MaxOverlayDistance < 0.01f);
        var cameraPass = camera.CompareTag("MainCamera") &&
            Mathf.Approximately(camera.orthographicSize, GameplayCameraOrthographicSize) &&
            camera.GetComponent<WarlineCaptureIso2DCameraController>() != null;
        var capturePass = CaptureHasExpectedDimensions(StartCaptureRelativePath) &&
            CaptureHasExpectedDimensions(MidCaptureRelativePath) &&
            CaptureHasExpectedDimensions(EndCaptureRelativePath);

        var report = BuildReport(
            importWatch.Elapsed,
            buildWatch.Elapsed,
            renderWatch.Elapsed,
            totalWatch.Elapsed,
            terrainTileCount,
            placedSprites,
            snapshots,
            frameValidations,
            movementPass,
            sortingPass,
            overlayFollowPass,
            cameraPass,
            readabilityPass,
            capturePass);

        File.WriteAllText(Path.Combine(ProjectRootPath(), ReportRelativePath), report);
        AssetDatabase.Refresh();
        Debug.Log($"ISO2D_RUNTIME_COMPLETE scene={ScenePath} captures={OutputRoot} report={ReportRelativePath}");
    }

    private static string[] RequiredSpriteNames()
    {
        return new[]
        {
            "GA-01_RoadStraight",
            "GA-02_RoadIntersection",
            "GA-03_ConcretePlaza",
            "GA-04_ForwardCommandHQ",
            "GA-05_EnemyCommandHQ",
            "GA-06_RuinedCityBuilding",
            "GA-07_BarricadeRow",
            "GA-08_RifleSquad",
            "GA-09_APC",
            "GA-10_Tank",
            "GA-11_RoadTurn",
            "GA-12_RoadTJunction",
            "GA-13_RoadEndCap",
            "GA-14_CurbSidewalkTransition",
            "GA-15_DamagedRoadOverlay",
            "GA-16_ConcretePlazaAlt",
            "GA-17_SelectionRing",
            "GA-18_MoveMarker",
            "GA-19_AttackMarker",
            "GA-20_HealthBarFrame",
            "GA-21_HealthBarFill",
            "GA-22_SquadBadge",
            "GA-23_CapturePointMarker"
        };
    }

    private static void ImportGoldenSprites()
    {
        foreach (var path in Directory.GetFiles(Path.Combine(ProjectRootPath(), GoldenAssetRoot), "*.png"))
        {
            var assetPath = ToAssetPath(path);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static void CreateTerrainTiles(IReadOnlyDictionary<string, Sprite> sprites)
    {
        CreateOrUpdateTile("ISO_RoadStraight", sprites["GA-01_RoadStraight"]);
        CreateOrUpdateTile("ISO_RoadIntersection", sprites["GA-02_RoadIntersection"]);
        CreateOrUpdateTile("ISO_ConcretePlaza", sprites["GA-03_ConcretePlaza"]);
        CreateOrUpdateTile("ISO_RoadTurn", sprites["GA-11_RoadTurn"]);
        CreateOrUpdateTile("ISO_RoadTJunction", sprites["GA-12_RoadTJunction"]);
        CreateOrUpdateTile("ISO_RoadEndCap", sprites["GA-13_RoadEndCap"]);
        CreateOrUpdateTile("ISO_CurbSidewalkTransition", sprites["GA-14_CurbSidewalkTransition"]);
        CreateOrUpdateTile("ISO_ConcretePlazaAlt", sprites["GA-16_ConcretePlazaAlt"]);
        AssetDatabase.SaveAssets();
    }

    private static Tile CreateOrUpdateTile(string name, Sprite sprite)
    {
        var path = $"{TileAssetRoot}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static List<PlacedSprite> BuildScene(
        IReadOnlyDictionary<string, Sprite> sprites,
        out int terrainTileCount)
    {
        Camera.main?.gameObject.SetActive(false);

        var gridObject = new GameObject("ISO02 Runtime Isometric Grid");
        var grid = gridObject.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize = new Vector3(1f, 0.5f, 1f);

        var tilemapObject = new GameObject("Runtime Terrain Tilemap");
        tilemapObject.transform.SetParent(gridObject.transform, false);
        var tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        var renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 0;

        PopulateTerrain(tilemap);
        terrainTileCount = CountNonNullTiles(tilemap);

        var spriteRoot = new GameObject("Runtime Sorted Sprites").transform;
        var overlayRoot = new GameObject("Runtime Overlay Followers").transform;
        var placed = new List<PlacedSprite>();

        PlaceSortedSprite("Friendly Command HQ - Western Base", sprites["GA-04_ForwardCommandHQ"], grid, spriteRoot, new Vector3Int(-6, -2, 0), 1.34f, placed);
        PlaceSortedSprite("Enemy Command HQ - Eastern Base", sprites["GA-05_EnemyCommandHQ"], grid, spriteRoot, new Vector3Int(6, 2, 0), 1.24f, placed);
        PlaceSortedSprite("Ruined Block - North Checkpoint", sprites["GA-06_RuinedCityBuilding"], grid, spriteRoot, new Vector3Int(0, 2, 0), 0.96f, placed);
        PlaceSortedSprite("Ruined Block - Enemy Approach", sprites["GA-06_RuinedCityBuilding"], grid, spriteRoot, new Vector3Int(4, 1, 0), 0.88f, placed);
        PlaceSortedSprite("Barricade - Frontline Left", sprites["GA-07_BarricadeRow"], grid, spriteRoot, new Vector3Int(-1, 0, 0), 0.62f, placed);
        PlaceSortedSprite("Barricade - Frontline Center", sprites["GA-07_BarricadeRow"], grid, spriteRoot, new Vector3Int(0, 0, 0), 0.62f, placed);
        PlaceSortedSprite("Barricade - Frontline Right", sprites["GA-07_BarricadeRow"], grid, spriteRoot, new Vector3Int(1, 0, 0), 0.62f, placed);
        PlaceSortedSprite("Damaged Road - Kill Zone", sprites["GA-15_DamagedRoadOverlay"], grid, spriteRoot, new Vector3Int(2, 0, 0), 0.48f, placed);

        var rifle = PlaceRuntimeAgent("Runtime Rifle Squad - Road Advance", sprites["GA-08_RifleSquad"], grid, spriteRoot, new[]
        {
            new Vector3Int(-5, -1, 0),
            new Vector3Int(-3, -1, 0),
            new Vector3Int(-2, 0, 0),
            new Vector3Int(-1, 0, 0)
        }, 0.5f, 5.2f, placed);
        var apc = PlaceRuntimeAgent("Runtime APC Patrol - Convoy Lane", sprites["GA-09_APC"], grid, spriteRoot, new[]
        {
            new Vector3Int(-5, 0, 0),
            new Vector3Int(-3, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0)
        }, 0.82f, 5.6f, placed);
        var tank = PlaceRuntimeAgent("Runtime Tank Push - Breach Lane", sprites["GA-10_Tank"], grid, spriteRoot, new[]
        {
            new Vector3Int(-4, 1, 0),
            new Vector3Int(-2, 1, 0),
            new Vector3Int(0, 0, 0),
            new Vector3Int(2, 0, 0)
        }, 0.9f, 6f, placed);

        PlaceFollower("Rifle Selection Ring", sprites["GA-17_SelectionRing"], overlayRoot, rifle.Renderer.transform, new Vector3(0f, -0.1f, 0f), 0.22f, 5000, placed);
        PlaceFollower("Rifle Health Fill", sprites["GA-21_HealthBarFill"], overlayRoot, rifle.Renderer.transform, new Vector3(0f, 0.62f, 0f), 0.7f, 5001, placed);
        PlaceFollower("Rifle Health Frame", sprites["GA-20_HealthBarFrame"], overlayRoot, rifle.Renderer.transform, new Vector3(0f, 0.62f, 0f), 0.7f, 5002, placed);
        PlaceFollower("Rifle Squad Badge", sprites["GA-22_SquadBadge"], overlayRoot, rifle.Renderer.transform, new Vector3(-0.48f, 0.66f, 0f), 0.13f, 5003, placed);

        PlaceFollower("APC Selection Ring", sprites["GA-17_SelectionRing"], overlayRoot, apc.Renderer.transform, new Vector3(0f, -0.12f, 0f), 0.28f, 5010, placed);
        PlaceFollower("APC Health Fill", sprites["GA-21_HealthBarFill"], overlayRoot, apc.Renderer.transform, new Vector3(0f, 0.75f, 0f), 0.85f, 5011, placed);
        PlaceFollower("APC Health Frame", sprites["GA-20_HealthBarFrame"], overlayRoot, apc.Renderer.transform, new Vector3(0f, 0.75f, 0f), 0.85f, 5012, placed);

        PlaceFollower("Tank Selection Ring", sprites["GA-17_SelectionRing"], overlayRoot, tank.Renderer.transform, new Vector3(0f, -0.12f, 0f), 0.34f, 5020, placed);
        PlaceFollower("Tank Health Fill", sprites["GA-21_HealthBarFill"], overlayRoot, tank.Renderer.transform, new Vector3(0f, 0.84f, 0f), 0.95f, 5021, placed);
        PlaceFollower("Tank Health Frame", sprites["GA-20_HealthBarFrame"], overlayRoot, tank.Renderer.transform, new Vector3(0f, 0.84f, 0f), 0.95f, 5022, placed);

        PlaceOverlayAtCell("Move Command Marker - Frontline Rally", sprites["GA-18_MoveMarker"], grid, overlayRoot, new Vector3Int(-1, 0, 0), Vector3.zero, 0.26f, 5030, placed);
        PlaceOverlayAtCell("Attack Command Marker - Eastern Push", sprites["GA-19_AttackMarker"], grid, overlayRoot, new Vector3Int(3, 0, 0), Vector3.zero, 0.3f, 5031, placed);
        PlaceOverlayAtCell("Capture Point Marker - Central Road Control", sprites["GA-23_CapturePointMarker"], grid, overlayRoot, new Vector3Int(0, 1, 0), Vector3.zero, 0.3f, 5032, placed);

        return placed;
    }

    private static void PopulateTerrain(Tilemap tilemap)
    {
        var concretePlaza = LoadTile("ISO_ConcretePlaza");
        var concretePlazaAlt = LoadTile("ISO_ConcretePlazaAlt");
        var roadStraight = LoadTile("ISO_RoadStraight");
        var roadIntersection = LoadTile("ISO_RoadIntersection");
        var roadTurn = LoadTile("ISO_RoadTurn");
        var roadTJunction = LoadTile("ISO_RoadTJunction");
        var roadEndCap = LoadTile("ISO_RoadEndCap");
        var curbSidewalkTransition = LoadTile("ISO_CurbSidewalkTransition");

        for (var x = -8; x <= 8; x++)
        {
            for (var y = -5; y <= 5; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), (x + y) % 2 == 0 ? concretePlaza : concretePlazaAlt);
            }
        }

        SetRoadSegment(tilemap, roadStraight, roadIntersection, roadTJunction, roadTurn, roadEndCap, -7, 7, 0);
        SetRoadSegment(tilemap, roadStraight, roadIntersection, roadTJunction, roadTurn, roadEndCap, -6, -2, -2);
        SetRoadSegment(tilemap, roadStraight, roadIntersection, roadTJunction, roadTurn, roadEndCap, 2, 6, 2);

        tilemap.SetTile(new Vector3Int(-6, -1, 0), curbSidewalkTransition);
        tilemap.SetTile(new Vector3Int(-5, -1, 0), curbSidewalkTransition);
        tilemap.SetTile(new Vector3Int(-4, -1, 0), curbSidewalkTransition);
        tilemap.SetTile(new Vector3Int(4, 1, 0), curbSidewalkTransition);
        tilemap.SetTile(new Vector3Int(5, 1, 0), curbSidewalkTransition);
        tilemap.SetTile(new Vector3Int(6, 1, 0), curbSidewalkTransition);

        tilemap.RefreshAllTiles();
        tilemap.CompressBounds();
        EditorUtility.SetDirty(tilemap);
    }

    private static void SetRoadSegment(
        Tilemap tilemap,
        Tile roadStraight,
        Tile roadIntersection,
        Tile roadTJunction,
        Tile roadTurn,
        Tile roadEndCap,
        int startX,
        int endX,
        int y)
    {
        for (var x = startX; x <= endX; x++)
        {
            var tile = x == startX || x == endX
                ? roadEndCap
                : x == 0
                    ? roadIntersection
                    : x == -2 || x == 2
                        ? roadTJunction
                        : x == -4 || x == 4
                            ? roadTurn
                            : roadStraight;
            tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }
    }

    private static PlacedSprite PlaceSortedSprite(
        string name,
        Sprite sprite,
        Grid grid,
        Transform root,
        Vector3Int cell,
        float scale,
        ICollection<PlacedSprite> placed)
    {
        var gameObject = CreateSpriteObject(name, sprite, root, CellCenter(grid, cell), scale);
        var sorting = gameObject.AddComponent<WarlineCaptureIso2DSorting>();
        sorting.Configure(gameObject.GetComponent<SpriteRenderer>(), 1000, 100, 0, true);

        var entry = new PlacedSprite
        {
            Name = name,
            Renderer = gameObject.GetComponent<SpriteRenderer>()
        };
        placed.Add(entry);
        return entry;
    }

    private static PlacedSprite PlaceRuntimeAgent(
        string name,
        Sprite sprite,
        Grid grid,
        Transform root,
        Vector3Int[] pathCells,
        float scale,
        float duration,
        ICollection<PlacedSprite> placed)
    {
        var path = pathCells.Select(cell => CellCenter(grid, cell)).ToArray();
        var start = path[0];
        var gameObject = CreateSpriteObject(name, sprite, root, start, scale);
        var renderer = gameObject.GetComponent<SpriteRenderer>();
        var sorting = gameObject.AddComponent<WarlineCaptureIso2DSorting>();
        sorting.Configure(renderer, 1000, 100, 0, true);
        var agent = gameObject.AddComponent<WarlineCaptureIso2DPrototypeAgent>();
        agent.ConfigureWaypoints(path, duration, false);

        var entry = new PlacedSprite
        {
            Name = name,
            Renderer = renderer,
            Agent = agent
        };
        placed.Add(entry);
        return entry;
    }

    private static void PlaceFollower(
        string name,
        Sprite sprite,
        Transform root,
        Transform target,
        Vector3 offset,
        float scale,
        int sortingOrder,
        ICollection<PlacedSprite> placed)
    {
        var gameObject = CreateSpriteObject(name, sprite, root, target.position + offset, scale);
        var renderer = gameObject.GetComponent<SpriteRenderer>();
        var follower = gameObject.AddComponent<WarlineCaptureIso2DOverlayFollower>();
        follower.Configure(target, offset, renderer, sortingOrder);

        placed.Add(new PlacedSprite
        {
            Name = name,
            Renderer = renderer,
            Follower = follower,
            FollowTarget = target,
            FollowOffset = offset
        });
    }

    private static void PlaceOverlayAtCell(
        string name,
        Sprite sprite,
        Grid grid,
        Transform root,
        Vector3Int cell,
        Vector3 offset,
        float scale,
        int sortingOrder,
        ICollection<PlacedSprite> placed)
    {
        var gameObject = CreateSpriteObject(name, sprite, root, CellCenter(grid, cell) + offset, scale);
        var renderer = gameObject.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;

        placed.Add(new PlacedSprite
        {
            Name = name,
            Renderer = renderer
        });
    }

    private static GameObject CreateSpriteObject(string name, Sprite sprite, Transform root, Vector3 position, float scale)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(root, false);
        gameObject.transform.position = position;
        gameObject.transform.localScale = Vector3.one * scale;

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 1000 + Mathf.RoundToInt(-position.y * 100f);
        return gameObject;
    }

    private static Vector3 CellCenter(Grid grid, Vector3Int cell)
    {
        return grid.CellToWorld(cell) + new Vector3(0.5f, 0.25f, 0f);
    }

    private static Camera BuildCamera()
    {
        var cameraObject = new GameObject("ISO02 Gameplay Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.1f, 0.13f, 0.16f);
        camera.orthographic = true;
        camera.orthographicSize = GameplayCameraOrthographicSize;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(0.65f, -0.35f, -10f);

        var listener = cameraObject.AddComponent<AudioListener>();
        listener.enabled = true;

        var controller = cameraObject.AddComponent<WarlineCaptureIso2DCameraController>();
        controller.Configure(camera, 2.4f, 5.2f, 4f, 3f);
        return camera;
    }

    private static void CaptureFrame(
        string frameName,
        float normalizedTime,
        string relativePath,
        Camera camera,
        IReadOnlyCollection<PlacedSprite> placedSprites,
        AgentSnapshot[] snapshots,
        ICollection<FrameValidation> frameValidations)
    {
        ApplyRuntimeState(placedSprites, normalizedTime);
        RecordAgentSnapshot(frameName, placedSprites, snapshots);
        frameValidations.Add(new FrameValidation
        {
            Name = frameName,
            MaxOverlayDistance = MaxOverlayDistance(placedSprites)
        });
        Render(camera, Path.Combine(ProjectRootPath(), relativePath));
    }

    private static void ApplyRuntimeState(IEnumerable<PlacedSprite> placedSprites, float normalizedTime)
    {
        foreach (var placed in placedSprites)
        {
            placed.Agent?.SetNormalizedTime(normalizedTime);
        }

        foreach (var placed in placedSprites)
        {
            placed.Follower?.ApplyFollow();
        }
    }

    private static void RecordAgentSnapshot(string frameName, IEnumerable<PlacedSprite> placedSprites, AgentSnapshot[] snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            var placed = placedSprites.First(sprite => sprite.Name == snapshot.Name);
            if (frameName == "Start")
            {
                snapshot.StartPosition = placed.Renderer.transform.position;
                snapshot.StartSortingOrder = placed.Renderer.sortingOrder;
            }
            else if (frameName == "Mid")
            {
                snapshot.MidPosition = placed.Renderer.transform.position;
                snapshot.MidSortingOrder = placed.Renderer.sortingOrder;
            }
            else if (frameName == "End")
            {
                snapshot.EndPosition = placed.Renderer.transform.position;
                snapshot.EndSortingOrder = placed.Renderer.sortingOrder;
            }
        }
    }

    private static float MaxOverlayDistance(IEnumerable<PlacedSprite> placedSprites)
    {
        return placedSprites
            .Where(sprite => sprite.Follower != null && sprite.FollowTarget != null)
            .Select(sprite => Vector3.Distance(sprite.Renderer.transform.position, sprite.FollowTarget.position + sprite.FollowOffset))
            .DefaultIfEmpty(0f)
            .Max();
    }

    private static bool MeasureReadability(Camera camera, IEnumerable<PlacedSprite> placedSprites)
    {
        var pass = true;
        foreach (var placed in placedSprites)
        {
            var bounds = placed.Renderer.bounds;
            var min = camera.WorldToScreenPoint(bounds.min);
            var max = camera.WorldToScreenPoint(bounds.max);
            placed.ScreenHeight = Mathf.Abs(max.y - min.y);
            if (IsGameplayReadabilitySubject(placed) && placed.ScreenHeight < 26f)
            {
                pass = false;
            }
        }

        return pass;
    }

    private static bool IsGameplayReadabilitySubject(PlacedSprite placed)
    {
        return placed.Agent != null ||
            placed.Name.Contains("Command HQ", StringComparison.Ordinal) ||
            placed.Name.Contains("Ruined Block", StringComparison.Ordinal) ||
            placed.Name.Contains("Barricade", StringComparison.Ordinal);
    }

    private static bool CaptureHasExpectedDimensions(string relativePath)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            var path = Path.Combine(ProjectRootPath(), relativePath);
            if (!File.Exists(path))
            {
                return false;
            }

            texture.LoadImage(File.ReadAllBytes(path));
            return texture.width == CaptureWidth && texture.height == CaptureHeight;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void Render(Camera camera, string outputPath)
    {
        var previousActive = RenderTexture.active;
        var previousTarget = camera.targetTexture;
        var texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
        var renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4
        };

        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(texture);
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static string BuildReport(
        TimeSpan importElapsed,
        TimeSpan buildElapsed,
        TimeSpan renderElapsed,
        TimeSpan totalElapsed,
        int terrainTileCount,
        IReadOnlyCollection<PlacedSprite> placedSprites,
        IReadOnlyCollection<AgentSnapshot> snapshots,
        IReadOnlyCollection<FrameValidation> frameValidations,
        bool movementPass,
        bool sortingPass,
        bool overlayFollowPass,
        bool cameraPass,
        bool readabilityPass,
        bool capturePass)
    {
        var overlaySprites = placedSprites.Count(sprite => sprite.Follower != null || sprite.Name.Contains("Marker", StringComparison.Ordinal));
        var runtimeAgents = snapshots.Count;
        var minScreenHeight = placedSprites
            .Where(IsGameplayReadabilitySubject)
            .Where(sprite => sprite.ScreenHeight > 0f)
            .Select(sprite => sprite.ScreenHeight)
            .DefaultIfEmpty(0f)
            .Min();
        var maxOverlayDistance = frameValidations.Select(frame => frame.MaxOverlayDistance).DefaultIfEmpty(0f).Max();
        var status = movementPass && sortingPass && overlayFollowPass && cameraPass && readabilityPass && capturePass ? "PASS" : "REVIEW";

        var builder = new StringBuilder();
        builder.AppendLine("# ISO-02 2D Isometric Runtime Prototype Report");
        builder.AppendLine();
        builder.AppendLine($"Status: {status}");
        builder.AppendLine();
        builder.AppendLine("## Scope");
        builder.AppendLine();
        builder.AppendLine("- Manual design/balancing prototype only; this is not wired into Jenkins or build validation.");
        builder.AppendLine("- Builds a separate runtime scene using the generated 2D isometric golden assets and Tilemap terrain.");
        builder.AppendLine("- Validates runtime-style depth sorting, basic unit movement, and overlay followers before integration into the real gameplay scene.");
        builder.AppendLine();
        builder.AppendLine("## Output Paths");
        builder.AppendLine();
        builder.AppendLine($"- Scene: `{ScenePath}`");
        builder.AppendLine($"- Start capture: `{StartCaptureRelativePath}`");
        builder.AppendLine($"- Mid capture: `{MidCaptureRelativePath}`");
        builder.AppendLine($"- End capture: `{EndCaptureRelativePath}`");
        builder.AppendLine($"- Report: `{ReportRelativePath}`");
        builder.AppendLine();
        builder.AppendLine("## Checks");
        builder.AppendLine();
        builder.AppendLine($"- Runtime movement: {(movementPass ? "PASS" : "REVIEW")} - {runtimeAgents} prototype agents move between isometric waypoints.");
        builder.AppendLine($"- Runtime sorting: {(sortingPass ? "PASS" : "REVIEW")} - moving agents recalculate SpriteRenderer sorting order during movement.");
        builder.AppendLine($"- Overlay followers: {(overlayFollowPass ? "PASS" : "REVIEW")} - maximum target/follower offset error {maxOverlayDistance.ToString("F4", CultureInfo.InvariantCulture)} world units.");
        builder.AppendLine($"- Gameplay camera: {(cameraPass ? "PASS" : "REVIEW")} - scene includes `ISO02 Gameplay Camera` tagged `MainCamera`, orthographic size {GameplayCameraOrthographicSize.ToString("F2", CultureInfo.InvariantCulture)}, with Play Mode pan/zoom controls.");
        builder.AppendLine($"- Scale/readability: {(readabilityPass ? "PASS" : "REVIEW")} - minimum key sprite screen height {minScreenHeight.ToString("F1", CultureInfo.InvariantCulture)} px.");
        builder.AppendLine($"- Capture output: {(capturePass ? "PASS" : "REVIEW")} - start/mid/end captures are {CaptureWidth}x{CaptureHeight}.");
        builder.AppendLine($"- Performance smoke: {terrainTileCount} terrain tiles, {placedSprites.Count} SpriteRenderer objects, {overlaySprites} overlay or command marker sprites.");
        builder.AppendLine();
        builder.AppendLine("## Agent Snapshots");
        builder.AppendLine();
        foreach (var snapshot in snapshots)
        {
            builder.AppendLine($"- {snapshot.Name}: start {FormatVector(snapshot.StartPosition)} order {snapshot.StartSortingOrder}, mid {FormatVector(snapshot.MidPosition)} order {snapshot.MidSortingOrder}, end {FormatVector(snapshot.EndPosition)} order {snapshot.EndSortingOrder}.");
        }

        builder.AppendLine();
        builder.AppendLine("## Timings");
        builder.AppendLine();
        builder.AppendLine($"- Import/reimport: {importElapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms");
        builder.AppendLine($"- Scene build: {buildElapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms");
        builder.AppendLine($"- Capture renders: {renderElapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms");
        builder.AppendLine($"- Total editor method: {totalElapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms");
        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine();
        builder.AppendLine("- The scene is intentionally isolated from `Assets/Game/Scenes/Game.unity`.");
        builder.AppendLine("- Runtime components are lightweight MonoBehaviours under `Assets/Game/Scripts/Iso2D` and can be reused later by the real tactical gameplay implementation.");
        builder.AppendLine("- In Play Mode, use arrow keys/WASD to pan and mouse wheel to inspect zoom levels on the prototype camera.");
        builder.AppendLine("- The prototype captures validate movement states in editor automation; final visual approval should still be done by opening the scene and running Play Mode.");
        return builder.ToString();
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x.ToString("F2", CultureInfo.InvariantCulture)}, {value.y.ToString("F2", CultureInfo.InvariantCulture)}, {value.z.ToString("F2", CultureInfo.InvariantCulture)})";
    }

    private static int CountNonNullTiles(Tilemap tilemap)
    {
        return tilemap.GetTilesBlock(tilemap.cellBounds).Count(tile => tile != null);
    }

    private static Sprite LoadSprite(string name)
    {
        var path = $"{GoldenAssetRoot}/{name}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new FileNotFoundException($"Missing generated 2D ISO sprite: {path}");
        }

        return sprite;
    }

    private static Tile LoadTile(string name)
    {
        var path = $"{TileAssetRoot}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            throw new FileNotFoundException($"Missing generated 2D ISO tile: {path}");
        }

        return tile;
    }

    private static string ProjectRootPath()
    {
        return Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
    }

    private static string ToAssetPath(string path)
    {
        var fullPath = Path.GetFullPath(path).Replace('\\', '/');
        var projectRoot = ProjectRootPath().Replace('\\', '/');
        return fullPath.StartsWith(projectRoot, StringComparison.Ordinal)
            ? fullPath.Substring(projectRoot.Length + 1)
            : fullPath;
    }
}
#endif
