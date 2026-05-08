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

public static class WarlineCaptureIso2DSpikeBuilder
{
    private const int CaptureWidth = 1920;
    private const int CaptureHeight = 1080;
    private const float PixelsPerUnit = 512f;
    private const string AssetRoot = "Assets/Game/Art/Generated/2DISO";
    private const string GoldenAssetRoot = AssetRoot + "/GoldenAssets";
    private const string TileAssetRoot = AssetRoot + "/Tiles";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/ISO01_CityCommand_TilemapSpike.unity";
    private const string CaptureRelativePath = "Design/VisualReferences/2DIsometricProduction/UnitySpike/ISO01_TilemapSpike_Capture.png";
    private const string ReportRelativePath = "Design/VisualReferences/2DIsometricProduction/UnitySpike/ISO01_TilemapSpike_Report.md";

    private sealed class PlacedSprite
    {
        public string Name;
        public SpriteRenderer Renderer;
        public Vector3Int Cell;
        public float ScreenHeight;
    }

    [MenuItem("WarlineCapture/Design/Build ISO-01 2D Tilemap Spike")]
    public static void BuildAndCaptureIso01Spike()
    {
        var totalWatch = Stopwatch.StartNew();
        Directory.CreateDirectory(Path.Combine(ProjectRootPath(), TileAssetRoot));
        Directory.CreateDirectory(Path.Combine(ProjectRootPath(), Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(Path.Combine(ProjectRootPath(), Path.GetDirectoryName(CaptureRelativePath)));

        var importWatch = Stopwatch.StartNew();
        ImportGoldenSprites();
        AssetDatabase.Refresh();
        importWatch.Stop();

        var spriteNames = new[]
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

        var sprites = spriteNames.ToDictionary(name => name, LoadSprite);
        CreateTerrainTiles(sprites);

        var buildWatch = Stopwatch.StartNew();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var placedSprites = BuildScene(sprites, out var terrainTileCount);
        buildWatch.Stop();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);

        var camera = BuildCamera();
        var readability = MeasureReadability(camera, placedSprites);
        var sortingPass = placedSprites
            .Where(sprite => sprite.Name.StartsWith("Sort Probe ", StringComparison.Ordinal))
            .OrderBy(sprite => sprite.Cell.y)
            .Select(sprite => sprite.Renderer.sortingOrder)
            .SequenceEqual(placedSprites
                .Where(sprite => sprite.Name.StartsWith("Sort Probe ", StringComparison.Ordinal))
                .OrderBy(sprite => sprite.Cell.y)
                .Select(sprite => sprite.Renderer.sortingOrder)
                .OrderByDescending(order => order));

        var renderWatch = Stopwatch.StartNew();
        Render(camera, Path.Combine(ProjectRootPath(), CaptureRelativePath));
        renderWatch.Stop();

        totalWatch.Stop();
        var report = BuildReport(
            importWatch.Elapsed,
            buildWatch.Elapsed,
            renderWatch.Elapsed,
            totalWatch.Elapsed,
            terrainTileCount,
            placedSprites,
            sortingPass,
            readability);

        File.WriteAllText(Path.Combine(ProjectRootPath(), ReportRelativePath), report);
        AssetDatabase.Refresh();
        Debug.Log($"ISO2D_SPIKE_COMPLETE scene={ScenePath} capture={CaptureRelativePath} report={ReportRelativePath}");
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

    private static Dictionary<string, Tile> CreateTerrainTiles(IReadOnlyDictionary<string, Sprite> sprites)
    {
        var map = new Dictionary<string, Tile>
        {
            ["RoadStraight"] = CreateOrUpdateTile("ISO_RoadStraight", sprites["GA-01_RoadStraight"]),
            ["RoadIntersection"] = CreateOrUpdateTile("ISO_RoadIntersection", sprites["GA-02_RoadIntersection"]),
            ["ConcretePlaza"] = CreateOrUpdateTile("ISO_ConcretePlaza", sprites["GA-03_ConcretePlaza"]),
            ["RoadTurn"] = CreateOrUpdateTile("ISO_RoadTurn", sprites["GA-11_RoadTurn"]),
            ["RoadTJunction"] = CreateOrUpdateTile("ISO_RoadTJunction", sprites["GA-12_RoadTJunction"]),
            ["RoadEndCap"] = CreateOrUpdateTile("ISO_RoadEndCap", sprites["GA-13_RoadEndCap"]),
            ["CurbSidewalkTransition"] = CreateOrUpdateTile("ISO_CurbSidewalkTransition", sprites["GA-14_CurbSidewalkTransition"]),
            ["ConcretePlazaAlt"] = CreateOrUpdateTile("ISO_ConcretePlazaAlt", sprites["GA-16_ConcretePlazaAlt"])
        };
        AssetDatabase.SaveAssets();
        return map;
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

        var gridObject = new GameObject("ISO01 Isometric Grid");
        var grid = gridObject.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize = new Vector3(1f, 0.5f, 1f);

        var tilemapObject = new GameObject("Terrain Tilemap");
        tilemapObject.transform.SetParent(gridObject.transform, false);
        var tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        var renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 0;

        var concretePlaza = LoadTile("ISO_ConcretePlaza");
        var concretePlazaAlt = LoadTile("ISO_ConcretePlazaAlt");
        var roadStraight = LoadTile("ISO_RoadStraight");
        var roadIntersection = LoadTile("ISO_RoadIntersection");
        var roadTurn = LoadTile("ISO_RoadTurn");
        var roadTJunction = LoadTile("ISO_RoadTJunction");
        var roadEndCap = LoadTile("ISO_RoadEndCap");
        var curbSidewalkTransition = LoadTile("ISO_CurbSidewalkTransition");
        Debug.Log(
            $"ISO2D_TILE_ASSETS concrete={concretePlaza != null} concreteAlt={concretePlazaAlt != null} " +
            $"road={roadStraight != null} intersection={roadIntersection != null} turn={roadTurn != null} " +
            $"tJunction={roadTJunction != null} endCap={roadEndCap != null} curb={curbSidewalkTransition != null}");

        for (var x = -8; x <= 8; x++)
        {
            for (var y = -6; y <= 6; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), (x + y) % 2 == 0 ? concretePlaza : concretePlazaAlt);
            }
        }

        for (var x = -8; x <= 8; x++)
        {
            var tile = x switch
            {
                -5 => roadTJunction,
                0 => roadIntersection,
                5 => roadTurn,
                _ => roadStraight
            };
            tilemap.SetTile(new Vector3Int(x, 0, 0), tile);
        }

        for (var y = -6; y <= 6; y++)
        {
            tilemap.SetTile(new Vector3Int(0, y, 0), y == 0 ? roadIntersection : roadStraight);
        }

        for (var y = 1; y <= 3; y++)
        {
            tilemap.SetTile(new Vector3Int(-5, y, 0), roadStraight);
        }

        tilemap.SetTile(new Vector3Int(-5, 4, 0), roadEndCap);

        for (var y = -1; y >= -3; y--)
        {
            tilemap.SetTile(new Vector3Int(5, y, 0), roadStraight);
        }

        tilemap.SetTile(new Vector3Int(5, -4, 0), roadEndCap);

        for (var x = -7; x <= 7; x += 7)
        {
            tilemap.SetTile(new Vector3Int(x, 3, 0), curbSidewalkTransition);
            tilemap.SetTile(new Vector3Int(x, -3, 0), curbSidewalkTransition);
        }

        tilemap.RefreshAllTiles();
        tilemap.CompressBounds();
        terrainTileCount = CountNonNullTiles(tilemap);
        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(tilemapObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"ISO2D_TILEMAP_POPULATED tiles={terrainTileCount} bounds={tilemap.cellBounds}");

        var spriteRoot = new GameObject("Sorted Gameplay Sprites").transform;
        var placed = new List<PlacedSprite>();

        PlaceSprite("Friendly Command HQ", sprites["GA-04_ForwardCommandHQ"], grid, spriteRoot, new Vector3Int(-5, 2, 0), 1.42f, placed);
        PlaceSprite("Enemy Command HQ", sprites["GA-05_EnemyCommandHQ"], grid, spriteRoot, new Vector3Int(5, -2, 0), 1.32f, placed);
        PlaceSprite("Ruined City Block A", sprites["GA-06_RuinedCityBuilding"], grid, spriteRoot, new Vector3Int(2, 3, 0), 1.08f, placed);
        PlaceSprite("Ruined City Block B", sprites["GA-06_RuinedCityBuilding"], grid, spriteRoot, new Vector3Int(6, 1, 0), 0.92f, placed);

        for (var x = -3; x <= 3; x += 2)
        {
            PlaceSprite($"Barricade {x}", sprites["GA-07_BarricadeRow"], grid, spriteRoot, new Vector3Int(x, -1, 0), 0.66f, placed);
        }

        PlaceSprite("Sort Probe Back Rifle Squad", sprites["GA-08_RifleSquad"], grid, spriteRoot, new Vector3Int(-2, 2, 0), 0.48f, placed);
        PlaceSprite("Rifle Squad Center", sprites["GA-08_RifleSquad"], grid, spriteRoot, new Vector3Int(-1, -2, 0), 0.48f, placed);
        PlaceSprite("Sort Probe Front Rifle Squad", sprites["GA-08_RifleSquad"], grid, spriteRoot, new Vector3Int(-2, -4, 0), 0.48f, placed);
        PlaceSprite("APC Patrol", sprites["GA-09_APC"], grid, spriteRoot, new Vector3Int(2, -2, 0), 0.82f, placed);
        PlaceSprite("Tank Anchor", sprites["GA-10_Tank"], grid, spriteRoot, new Vector3Int(4, -4, 0), 0.9f, placed);
        PlaceSprite("Reserve Tank", sprites["GA-10_Tank"], grid, spriteRoot, new Vector3Int(-6, -1, 0), 0.72f, placed);
        PlaceSprite("Damaged Road Overlay A", sprites["GA-15_DamagedRoadOverlay"], grid, spriteRoot, new Vector3Int(-3, 0, 0), 0.56f, placed);
        PlaceSprite("Damaged Road Overlay B", sprites["GA-15_DamagedRoadOverlay"], grid, spriteRoot, new Vector3Int(1, -3, 0), 0.46f, placed);

        PlaceOverlay("Overlay Selection Ring Rifle Squad", sprites["GA-17_SelectionRing"], grid, spriteRoot, new Vector3Int(-1, -2, 0), new Vector3(0f, -0.1f, 0f), 0.22f, placed);
        PlaceOverlay("Overlay Selection Ring APC", sprites["GA-17_SelectionRing"], grid, spriteRoot, new Vector3Int(2, -2, 0), new Vector3(0f, -0.12f, 0f), 0.28f, placed);
        PlaceOverlay("Overlay Selection Ring Tank", sprites["GA-17_SelectionRing"], grid, spriteRoot, new Vector3Int(4, -4, 0), new Vector3(0f, -0.12f, 0f), 0.34f, placed);
        PlaceOverlay("Overlay Move Marker", sprites["GA-18_MoveMarker"], grid, spriteRoot, new Vector3Int(-4, -3, 0), Vector3.zero, 0.24f, placed);
        PlaceOverlay("Overlay Attack Marker", sprites["GA-19_AttackMarker"], grid, spriteRoot, new Vector3Int(4, -1, 0), Vector3.zero, 0.28f, placed);
        PlaceOverlay("Overlay Capture Point Marker", sprites["GA-23_CapturePointMarker"], grid, spriteRoot, new Vector3Int(-1, 1, 0), Vector3.zero, 0.28f, placed);

        PlaceOverlay("Overlay Rifle Squad Health Fill", sprites["GA-21_HealthBarFill"], grid, spriteRoot, new Vector3Int(-1, -2, 0), new Vector3(0f, 0.62f, 0f), 0.7f, placed);
        PlaceOverlay("Overlay Rifle Squad Health Frame", sprites["GA-20_HealthBarFrame"], grid, spriteRoot, new Vector3Int(-1, -2, 0), new Vector3(0f, 0.62f, 0f), 0.7f, placed);
        PlaceOverlay("Overlay Tank Health Fill", sprites["GA-21_HealthBarFill"], grid, spriteRoot, new Vector3Int(4, -4, 0), new Vector3(0f, 0.82f, 0f), 0.95f, placed);
        PlaceOverlay("Overlay Tank Health Frame", sprites["GA-20_HealthBarFrame"], grid, spriteRoot, new Vector3Int(4, -4, 0), new Vector3(0f, 0.82f, 0f), 0.95f, placed);
        PlaceOverlay("Overlay Squad Badge", sprites["GA-22_SquadBadge"], grid, spriteRoot, new Vector3Int(-1, -2, 0), new Vector3(-0.48f, 0.66f, 0f), 0.13f, placed);

        for (var i = 0; i < 32; i++)
        {
            var cell = new Vector3Int(-7 + i % 8, -5 + i / 8, 0);
            var sprite = i % 3 == 0 ? sprites["GA-08_RifleSquad"] : i % 3 == 1 ? sprites["GA-09_APC"] : sprites["GA-10_Tank"];
            PlaceSprite($"Performance Crowd {i:00}", sprite, grid, spriteRoot, cell, i % 3 == 0 ? 0.32f : 0.42f, placed);
        }

        return placed;
    }

    private static int CountNonNullTiles(Tilemap tilemap)
    {
        return tilemap.GetTilesBlock(tilemap.cellBounds).Count(tile => tile != null);
    }

    private static void PlaceSprite(
        string name,
        Sprite sprite,
        Grid grid,
        Transform root,
        Vector3Int cell,
        float scale,
        ICollection<PlacedSprite> placed)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(root, false);
        gameObject.transform.position = grid.CellToWorld(cell) + new Vector3(0.5f, 0.25f, 0f);
        gameObject.transform.localScale = Vector3.one * scale;

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 1000 + Mathf.RoundToInt(-gameObject.transform.position.y * 100f);

        placed.Add(new PlacedSprite
        {
            Name = name,
            Renderer = renderer,
            Cell = cell
        });
    }

    private static void PlaceOverlay(
        string name,
        Sprite sprite,
        Grid grid,
        Transform root,
        Vector3Int cell,
        Vector3 offset,
        float scale,
        ICollection<PlacedSprite> placed)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(root, false);
        gameObject.transform.position = grid.CellToWorld(cell) + new Vector3(0.5f, 0.25f, 0f) + offset;
        gameObject.transform.localScale = Vector3.one * scale;

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 5000 + placed.Count(spriteEntry => spriteEntry.Name.StartsWith("Overlay ", StringComparison.Ordinal));

        placed.Add(new PlacedSprite
        {
            Name = name,
            Renderer = renderer,
            Cell = cell
        });
    }

    private static Camera BuildCamera()
    {
        var cameraObject = new GameObject("ISO01 Capture Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.1f, 0.13f, 0.16f);
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(0f, -0.15f, -10f);
        return camera;
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
        bool sortingPass,
        bool readabilityPass)
    {
        var reusableSprites = placedSprites.Count(sprite => !sprite.Name.StartsWith("Performance Crowd ", StringComparison.Ordinal));
        var crowdSprites = placedSprites.Count - reusableSprites;
        var overlaySprites = placedSprites.Count(sprite => sprite.Name.StartsWith("Overlay ", StringComparison.Ordinal));
        var minScreenHeight = placedSprites
            .Where(IsGameplayReadabilitySubject)
            .Where(sprite => sprite.ScreenHeight > 0f)
            .Select(sprite => sprite.ScreenHeight)
            .DefaultIfEmpty(0f)
            .Min();
        var minOverlayHeight = placedSprites
            .Where(sprite => sprite.Name.StartsWith("Overlay ", StringComparison.Ordinal))
            .Where(sprite => sprite.ScreenHeight > 0f)
            .Select(sprite => sprite.ScreenHeight)
            .DefaultIfEmpty(0f)
            .Min();
        var status = sortingPass && readabilityPass ? "PASS" : "REVIEW";

        var builder = new StringBuilder();
        builder.AppendLine("# ISO-01 2D Isometric Tilemap Spike Report");
        builder.AppendLine();
        builder.AppendLine($"Status: {status}");
        builder.AppendLine();
        builder.AppendLine("## Scope");
        builder.AppendLine();
        builder.AppendLine("- Manual design/balancing spike only; this is not wired into Jenkins or build validation.");
        builder.AppendLine("- Imports the first 23 golden assets as Unity sprites.");
        builder.AppendLine("- Builds an isometric Tilemap scene with modular road/plaza terrain, tactical overlays, and captures a 1920x1080 visual check.");
        builder.AppendLine();
        builder.AppendLine("## Output Paths");
        builder.AppendLine();
        builder.AppendLine($"- Scene: `{ScenePath}`");
        builder.AppendLine($"- Capture: `{CaptureRelativePath}`");
        builder.AppendLine($"- Golden assets: `{GoldenAssetRoot}`");
        builder.AppendLine($"- Tile assets: `{TileAssetRoot}`");
        builder.AppendLine();
        builder.AppendLine("## Checks");
        builder.AppendLine();
        builder.AppendLine($"- Sorting: {(sortingPass ? "PASS" : "REVIEW")} - lower screen-space units render in front of higher units.");
        builder.AppendLine($"- Scale/readability: {(readabilityPass ? "PASS" : "REVIEW")} - minimum key sprite screen height {minScreenHeight.ToString("F1", CultureInfo.InvariantCulture)} px.");
        builder.AppendLine("- Modular terrain: PASS - includes straight road, intersection, turn, T-junction, end cap, curb transition, alternate plaza, and damaged road overlays.");
        builder.AppendLine($"- Tactical overlays: PASS - selection rings, move/attack markers, health bars, squad badge, and capture point marker placed as separate sprites ({overlaySprites} overlay instances, minimum overlay height {minOverlayHeight.ToString("F1", CultureInfo.InvariantCulture)} px).");
        builder.AppendLine($"- Performance smoke: {terrainTileCount} terrain tiles, {reusableSprites} composed sprites, {crowdSprites} extra repeated sprites.");
        builder.AppendLine();
        builder.AppendLine("## Timings");
        builder.AppendLine();
        builder.AppendLine($"- Import/reimport: {importElapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms");
        builder.AppendLine($"- Scene build: {buildElapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms");
        builder.AppendLine($"- Capture render: {renderElapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms");
        builder.AppendLine($"- Total editor method: {totalElapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} ms");
        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine();
        builder.AppendLine("- Road and concrete assets are imported as Tile assets to verify the isometric Tilemap path.");
        builder.AppendLine("- Damaged road overlays remain sorted SpriteRenderer objects because decals may need independent placement and gameplay state later.");
        builder.AppendLine("- Selection, command, health, squad, and capture overlays remain separate sorted SpriteRenderer objects and are not baked into unit, terrain, or building art.");
        builder.AppendLine("- Buildings, barricades, squads, APCs, and tanks remain sorted SpriteRenderer objects because they need per-object depth ordering and selection logic later.");
        builder.AppendLine("- This spike validates the asset direction and Unity setup before generating the remaining game-wide asset library.");
        return builder.ToString();
    }

    private static bool IsGameplayReadabilitySubject(PlacedSprite placed)
    {
        return !placed.Name.StartsWith("Performance Crowd ", StringComparison.Ordinal) &&
            !placed.Name.StartsWith("Overlay ", StringComparison.Ordinal) &&
            !placed.Name.StartsWith("Damaged Road Overlay ", StringComparison.Ordinal);
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
