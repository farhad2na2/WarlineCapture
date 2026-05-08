#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public static class WarlineCaptureFictionalGulfIsoMapBuilder
{
    private const int CaptureWidth = 1920;
    private const int CaptureHeight = 1080;
    private const string DefaultMapPath = "Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Maps/FG-L01_CoastalCommand.map.json";
    private const string OutputRoot = "Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Reports";
    private const string MacroTileArtRoot = "Assets/Game/Art/Generated/IsometricMaps/MacroTiles";
    private const float IsoHalfWidth = 0.08f;
    private const float IsoHalfHeight = 0.04f;

    private readonly struct MapBuildSpec
    {
        public readonly string MapPath;
        public readonly string ScenePath;
        public readonly string CleanTargetCapturePath;
        public readonly string PlaceholderTerrainCapturePath;
        public readonly string CapturePath;
        public readonly string ReportPath;
        public readonly string PreviewAssetPath;

        public MapBuildSpec(
            string mapPath,
            string scenePath,
            string cleanTargetCapturePath,
            string placeholderTerrainCapturePath,
            string capturePath,
            string reportPath,
            string previewAssetPath)
        {
            MapPath = mapPath;
            ScenePath = scenePath;
            CleanTargetCapturePath = cleanTargetCapturePath;
            PlaceholderTerrainCapturePath = placeholderTerrainCapturePath;
            CapturePath = capturePath;
            ReportPath = reportPath;
            PreviewAssetPath = previewAssetPath;
        }
    }

    private static readonly MapBuildSpec Fg01 = new(
        DefaultMapPath,
        "Assets/Game/Scenes/DesignTargets/FG01_CoastalCommand_UsableIsoMap.unity",
        OutputRoot + "/FG-L01_CoastalCommand_CleanVisualTarget.png",
        OutputRoot + "/FG-L01_CoastalCommand_PlaceholderTerrain.png",
        OutputRoot + "/FG-L01_CoastalCommand_MetadataOverlay.png",
        OutputRoot + "/FG-L01_CoastalCommand_UnityImport_Report.md",
        "Assets/Game/Art/Generated/IsometricMaps/Previews/FG-L01_CoastalCommand_Preview.png");

    private static readonly MapBuildSpec Fg02 = new(
        "Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Maps/FG-L02_PortBreach.map.json",
        "Assets/Game/Scenes/DesignTargets/FG02_PortBreach_UsableIsoMap.unity",
        OutputRoot + "/FG-L02_PortBreach_CleanVisualTarget.png",
        OutputRoot + "/FG-L02_PortBreach_PlaceholderTerrain.png",
        OutputRoot + "/FG-L02_PortBreach_MetadataOverlay.png",
        OutputRoot + "/FG-L02_PortBreach_UnityImport_Report.md",
        "Assets/Game/Art/Generated/IsometricMaps/Previews/FG-L02_PortBreach_Preview.png");

    private static readonly MapBuildSpec Fg03 = new(
        "Design/VisualReferences/2DIsometricProduction/MacroTilePrototype/FictionalGulfStyle/UsableIsoMaps/Maps/FG-L03_AirNavalDefense.map.json",
        "Assets/Game/Scenes/DesignTargets/FG03_AirNavalDefense_UsableIsoMap.unity",
        OutputRoot + "/FG-L03_AirNavalDefense_CleanVisualTarget.png",
        OutputRoot + "/FG-L03_AirNavalDefense_PlaceholderTerrain.png",
        OutputRoot + "/FG-L03_AirNavalDefense_MetadataOverlay.png",
        OutputRoot + "/FG-L03_AirNavalDefense_UnityImport_Report.md",
        "Assets/Game/Art/Generated/IsometricMaps/Previews/FG-L03_AirNavalDefense_Preview.png");

    private sealed class IsoMap
    {
        public string MapId;
        public string DisplayName;
        public GridData Grid;
        public MacroTileGridData MacroTileGrid;
        public RoadGraphData RoadGraph;
        public RegionData Regions;
        public readonly List<SocketData> Sockets = new();
        public readonly List<SpawnGroupData> SpawnGroups = new();
        public PerformanceTargets PerformanceTargets;
    }

    private sealed class GridData
    {
        public int WidthCells;
        public int HeightCells;
        public float CellSizeWorld;
        public int SectorSizeCells;
    }

    private sealed class MacroTileGridData
    {
        public int MacroTileSizeCells;
        public int SourcePixels;
        public readonly List<MacroTilePlacement> Placements = new();
    }

    private sealed class MacroTilePlacement
    {
        public int X;
        public int Y;
        public string TileId;
        public string Variant;
        public int Rotation;
    }

    private readonly struct MacroTileArtCandidate
    {
        public readonly string AssetPath;
        public readonly bool AlreadyRotated;

        public MacroTileArtCandidate(string assetPath, bool alreadyRotated)
        {
            AssetPath = assetPath;
            AlreadyRotated = alreadyRotated;
        }
    }

    private sealed class RoadGraphData
    {
        public readonly List<RoadNode> Nodes = new();
        public readonly List<RoadEdge> Edges = new();
    }

    private sealed class RoadNode
    {
        public string Id;
        public Vector2Int Cell;
        public string Kind;
    }

    private sealed class RoadEdge
    {
        public string From;
        public string To;
        public int WidthCells;
        public string Surface;
        public bool VehiclePreferred;
        public bool InfantryPreferred;
    }

    private sealed class RegionData
    {
        public readonly List<RectRegion> Blocked = new();
        public readonly List<RectRegion> Water = new();
        public readonly List<RectRegion> RoadCorridors = new();
        public readonly List<RectRegion> SidewalkCorridors = new();
        public readonly List<RectRegion> CombatZones = new();
        public readonly List<RectRegion> CameraZones = new();
    }

    private sealed class RectRegion
    {
        public string Id;
        public RectInt Rect;
    }

    private sealed class SocketData
    {
        public string Id;
        public string Kind;
        public Vector2Int Cell;
        public Vector2Int SizeCells;
        public string Faction;
    }

    private sealed class SpawnGroupData
    {
        public string Id;
        public string Faction;
        public string Kind;
        public readonly List<Vector2Int> Cells = new();
        public int MaxUnits;
    }

    private sealed class PerformanceTargets
    {
        public int TargetLiveUnits;
        public int StressLiveUnits;
        public int MaxPathRequestsPerFrame;
        public int MaxResidentTextureMb;
    }

    [MenuItem("WarlineCapture/Design/Build FG-01 Usable Iso Map")]
    public static void BuildFg01CoastalCommandScene()
    {
        BuildMapScene(Fg01);
    }

    [MenuItem("WarlineCapture/Design/Build FG-02 Usable Iso Map")]
    public static void BuildFg02PortBreachScene()
    {
        BuildMapScene(Fg02);
    }

    [MenuItem("WarlineCapture/Design/Build FG-03 Usable Iso Map")]
    public static void BuildFg03AirNavalDefenseScene()
    {
        BuildMapScene(Fg03);
    }

    [MenuItem("WarlineCapture/Design/Build All Fictional Gulf Usable Iso Maps")]
    public static void BuildAllFictionalGulfScenes()
    {
        BuildMapScene(Fg01);
        BuildMapScene(Fg02);
        BuildMapScene(Fg03);
    }

    private static void BuildMapScene(MapBuildSpec spec)
    {
        Directory.CreateDirectory(Path.Combine(ProjectRootPath(), Path.GetDirectoryName(spec.ScenePath)));
        Directory.CreateDirectory(WorkspacePath(OutputRoot));

        var map = LoadMap(spec.MapPath);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene(map, spec);
        BuildCamera(map);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), spec.ScenePath);

        CopyCleanVisualTarget(spec);
        WriteSoftwareTerrainCapture(map, WorkspacePath(spec.PlaceholderTerrainCapturePath));
        WriteSoftwareCapture(map, WorkspacePath(spec.CapturePath));
        File.WriteAllText(WorkspacePath(spec.ReportPath), BuildReport(map, spec), Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log($"FICTIONAL_GULF_USABLE_ISO_MAP_COMPLETE map={map.MapId} scene={spec.ScenePath} capture={spec.CapturePath} report={spec.ReportPath}");
    }

    private static IsoMap LoadMap(string relativePath)
    {
        var fullPath = WorkspacePath(relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Missing usable iso map JSON: {fullPath}");
        }

        return MapJsonParser.Parse(File.ReadAllText(fullPath));
    }

    private static void BuildScene(IsoMap map, MapBuildSpec spec)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.82f, 0.79f, 0.72f);
        RenderSettings.skybox = null;

        var sunObject = new GameObject("Fictional Gulf Warm Sun");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.color = new Color(1f, 0.88f, 0.68f);
        sunObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        var mapRoot = new GameObject($"{map.DisplayName} - Usable Iso Map");
        CreateVisualReferencePlane(mapRoot.transform, map, spec);
        var chunkRoot = NewChild(mapRoot.transform, "01 Macro Visual Chunks - Authored Or Placeholder");
        var regionRoot = NewChild(mapRoot.transform, "02 Gameplay Metadata Regions");
        var graphRoot = NewChild(mapRoot.transform, "03 Road Graph");
        var socketRoot = NewChild(mapRoot.transform, "04 Sockets");
        var spawnRoot = NewChild(mapRoot.transform, "05 Spawn Groups");

        foreach (var placement in map.MacroTileGrid.Placements)
        {
            var rect = new RectInt(
                placement.X * map.MacroTileGrid.MacroTileSizeCells,
                placement.Y * map.MacroTileGrid.MacroTileSizeCells,
                map.MacroTileGrid.MacroTileSizeCells,
                map.MacroTileGrid.MacroTileSizeCells);
            CreateMacroTileVisualChunk(chunkRoot, map, placement, rect);
        }

        foreach (var region in map.Regions.RoadCorridors)
            CreateCellRectMesh(regionRoot, "Road " + region.Id, region.Rect, new Color(0.12f, 0.16f, 0.18f, 0.78f), 20, -0.02f);
        foreach (var region in map.Regions.SidewalkCorridors)
            CreateCellRectMesh(regionRoot, "Sidewalk " + region.Id, region.Rect, new Color(0.95f, 0.82f, 0.48f, 0.55f), 25, -0.03f);
        foreach (var region in map.Regions.Water)
            CreateCellRectMesh(regionRoot, "Water " + region.Id, region.Rect, new Color(0.04f, 0.48f, 0.62f, 0.82f), 30, -0.04f);
        foreach (var region in map.Regions.Blocked)
            CreateCellRectMesh(regionRoot, "Blocked " + region.Id, region.Rect, new Color(0.36f, 0.16f, 0.10f, 0.58f), 35, -0.05f);
        foreach (var region in map.Regions.CombatZones)
            CreateCellRectMesh(regionRoot, "Combat Zone " + region.Id, region.Rect, new Color(1f, 0.44f, 0.08f, 0.20f), 40, -0.06f);

        var nodesById = new Dictionary<string, RoadNode>(StringComparer.Ordinal);
        foreach (var node in map.RoadGraph.Nodes)
        {
            nodesById[node.Id] = node;
            CreateMarker(graphRoot, "Node " + node.Id, node.Cell, Color.white, 0.52f, 80);
        }

        foreach (var edge in map.RoadGraph.Edges)
        {
            if (!nodesById.TryGetValue(edge.From, out var from) || !nodesById.TryGetValue(edge.To, out var to))
            {
                continue;
            }

            CreateLine(
                graphRoot,
                $"Edge {edge.From} -> {edge.To} {edge.Surface}",
                CellToIso(from.Cell),
                CellToIso(to.Cell),
                edge.Surface == "sidewalk" ? new Color(1f, 0.86f, 0.34f, 0.92f) : new Color(0.18f, 0.82f, 1f, 0.88f),
                Mathf.Clamp(edge.WidthCells * 0.015f, 0.08f, 0.42f),
                90);
        }

        foreach (var socket in map.Sockets)
        {
            var rect = new RectInt(
                socket.Cell.x - socket.SizeCells.x / 2,
                socket.Cell.y - socket.SizeCells.y / 2,
                socket.SizeCells.x,
                socket.SizeCells.y);
            CreateCellRectMesh(
                socketRoot,
                $"Socket {socket.Id} {socket.Kind} {socket.Faction}",
                rect,
                ColorForFaction(socket.Faction, 0.78f),
                70,
                -0.08f);
        }

        foreach (var spawn in map.SpawnGroups)
        {
            foreach (var cell in spawn.Cells)
            {
                CreateMarker(
                    spawnRoot,
                    $"Spawn {spawn.Id} {spawn.Kind} max{spawn.MaxUnits}",
                    cell,
                    ColorForFaction(spawn.Faction, 0.95f),
                    0.75f,
                    100);
            }
        }

        CreateLabel(mapRoot.transform, map.DisplayName + " Metadata Overlay", CellToIso(new Vector2Int(14, map.Grid.HeightCells - 24)), 4.2f, Color.white, 130);
        CreateLabel(mapRoot.transform, "JSON source of truth: " + spec.MapPath, CellToIso(new Vector2Int(14, map.Grid.HeightCells - 42)), 2.2f, new Color(0.86f, 0.92f, 1f), 130);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static Camera BuildCamera(IsoMap map)
    {
        var min = CellToIso(new Vector2Int(0, 0));
        var right = CellToIso(new Vector2Int(map.Grid.WidthCells, 0));
        var top = CellToIso(new Vector2Int(0, map.Grid.HeightCells));
        var max = CellToIso(new Vector2Int(map.Grid.WidthCells, map.Grid.HeightCells));
        var boundsMin = Vector3.Min(Vector3.Min(min, right), Vector3.Min(top, max));
        var boundsMax = Vector3.Max(Vector3.Max(min, right), Vector3.Max(top, max));
        var center = (boundsMin + boundsMax) * 0.5f;

        var cameraObject = new GameObject(map.MapId + " Metadata Overlay Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.075f, 0.085f);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(boundsMax.y - boundsMin.y, 1f) * 0.58f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(center.x, center.y, -10f);
        camera.transform.rotation = Quaternion.identity;
        return camera;
    }

    private static void CreateVisualReferencePlane(Transform parent, IsoMap map, MapBuildSpec spec)
    {
        Texture2D preview = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.PreviewAssetPath);
        if (preview == null)
        {
            Debug.LogWarning($"Missing visual validation preview asset: {spec.PreviewAssetPath}");
            return;
        }

        var min = CellToIso(new Vector2Int(0, 0));
        var right = CellToIso(new Vector2Int(map.Grid.WidthCells, 0));
        var top = CellToIso(new Vector2Int(0, map.Grid.HeightCells));
        var max = CellToIso(new Vector2Int(map.Grid.WidthCells, map.Grid.HeightCells));
        var boundsMin = Vector3.Min(Vector3.Min(min, right), Vector3.Min(top, max));
        var boundsMax = Vector3.Max(Vector3.Max(min, right), Vector3.Max(top, max));
        var center = (boundsMin + boundsMax) * 0.5f;

        float width = Mathf.Max(boundsMax.x - boundsMin.x, 1f);
        float height = Mathf.Max(boundsMax.y - boundsMin.y, 1f);
        float imageAspect = preview.width / Mathf.Max((float)preview.height, 1f);
        float boundsAspect = width / Mathf.Max(height, 1f);
        if (imageAspect > boundsAspect)
            height = width / imageAspect;
        else
            width = height * imageAspect;

        var go = new GameObject("00 Visual Validation Target - " + Path.GetFileNameWithoutExtension(spec.PreviewAssetPath));
        go.transform.SetParent(parent, false);
        var meshFilter = go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateTexturedMaterial(go.name + " Material", preview, new Color(1f, 1f, 1f, 0.86f));
        meshRenderer.sortingOrder = -100;

        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float z = 0.08f;
        var mesh = new Mesh { name = go.name + " Mesh" };
        mesh.vertices = new[]
        {
            new Vector3(center.x - halfWidth, center.y - halfHeight, z),
            new Vector3(center.x + halfWidth, center.y - halfHeight, z),
            new Vector3(center.x + halfWidth, center.y + halfHeight, z),
            new Vector3(center.x - halfWidth, center.y + halfHeight, z)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
    }

    private static Transform NewChild(Transform parent, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static void CreateMacroTileVisualChunk(Transform parent, IsoMap map, MacroTilePlacement placement, RectInt rect)
    {
        string artPath = ResolveMacroTileTexturePath(map, placement, out bool alreadyRotated);
        string baseName = $"Macro {placement.X},{placement.Y} {placement.TileId} {placement.Variant} rot{placement.Rotation}";
        if (!string.IsNullOrEmpty(artPath))
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(artPath);
            if (texture != null)
            {
                CreateTexturedCellRectMesh(
                    parent,
                    baseName + " ART " + Path.GetFileNameWithoutExtension(artPath),
                    rect,
                    texture,
                    alreadyRotated ? 0 : placement.Rotation,
                    0,
                    0f);
                return;
            }
        }

        CreateCellRectMesh(
            parent,
            baseName + " PLACEHOLDER",
            rect,
            ColorForMacroTile(placement.TileId),
            0,
            0f);
    }

    private static void CreateCellRectMesh(Transform parent, string name, RectInt rect, Color color, int sortingOrder, float z)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var meshFilter = go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateMaterial(name + " Material", color);
        meshRenderer.sortingOrder = sortingOrder;

        var a = CellToIso(new Vector2Int(rect.xMin, rect.yMin)); a.z = z;
        var b = CellToIso(new Vector2Int(rect.xMax, rect.yMin)); b.z = z;
        var c = CellToIso(new Vector2Int(rect.xMax, rect.yMax)); c.z = z;
        var d = CellToIso(new Vector2Int(rect.xMin, rect.yMax)); d.z = z;

        var mesh = new Mesh { name = name + " Mesh" };
        mesh.vertices = new[] { a, b, c, d };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
    }

    private static void CreateTexturedCellRectMesh(Transform parent, string name, RectInt rect, Texture2D texture, int rotation, int sortingOrder, float z)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var meshFilter = go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateTexturedMaterial(name + " Material", texture, Color.white);
        meshRenderer.sortingOrder = sortingOrder;

        var a = CellToIso(new Vector2Int(rect.xMin, rect.yMin)); a.z = z;
        var b = CellToIso(new Vector2Int(rect.xMax, rect.yMin)); b.z = z;
        var c = CellToIso(new Vector2Int(rect.xMax, rect.yMax)); c.z = z;
        var d = CellToIso(new Vector2Int(rect.xMin, rect.yMax)); d.z = z;

        var mesh = new Mesh { name = name + " Mesh" };
        mesh.vertices = new[] { a, b, c, d };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = UvsForRotation(rotation);
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
    }

    private static Vector2[] UvsForRotation(int rotation)
    {
        int turns = ((rotation % 360) + 360) % 360 / 90;
        var uvs = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        for (int i = 0; i < turns; i++)
        {
            var first = uvs[0];
            uvs[0] = uvs[3];
            uvs[3] = uvs[2];
            uvs[2] = uvs[1];
            uvs[1] = first;
        }

        return uvs;
    }

    private static void CreateMarker(Transform parent, string name, Vector2Int cell, Color color, float radius, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var meshFilter = go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateMaterial(name + " Material", color);
        meshRenderer.sortingOrder = sortingOrder;

        const int segmentCount = 16;
        var vertices = new Vector3[segmentCount + 1];
        var triangles = new int[segmentCount * 3];
        var center = CellToIso(cell);
        center.z = -0.1f;
        vertices[0] = center;
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segmentCount;
            vertices[i + 1] = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i == segmentCount - 1 ? 1 : i + 2;
        }

        var mesh = new Mesh { name = name + " Mesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
    }

    private static void CreateLine(Transform parent, string name, Vector3 start, Vector3 end, Color color, float width, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = CreateMaterial(name + " Material", color);
        line.positionCount = 2;
        start.z = -0.12f;
        end.z = -0.12f;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 4;
        line.sortingOrder = sortingOrder;
        line.useWorldSpace = true;
    }

    private static void CreateLabel(Transform parent, string text, Vector3 position, float size, Color color, int sortingOrder)
    {
        var go = new GameObject("Label " + text);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(position.x, position.y, -0.2f);
        var label = go.AddComponent<TextMesh>();
        label.text = text;
        label.characterSize = size * 0.08f;
        label.anchor = TextAnchor.MiddleLeft;
        label.alignment = TextAlignment.Left;
        label.color = color;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sortingOrder = sortingOrder;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        var material = new Material(shader)
        {
            name = name,
            color = color
        };
        material.renderQueue = 3000;
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    private static Material CreateTexturedMaterial(string name, Texture2D texture, Color color)
    {
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Unlit/Color");
        var material = new Material(shader)
        {
            name = name,
            mainTexture = texture,
            color = color
        };
        material.renderQueue = 2500;
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        return material;
    }

    private static Color ColorForMacroTile(string tileId)
    {
        if (tileId.Contains("port", StringComparison.Ordinal) || tileId.Contains("seawall", StringComparison.Ordinal))
            return new Color(0.10f, 0.40f, 0.48f, 0.62f);
        if (tileId.Contains("command", StringComparison.Ordinal) || tileId.Contains("lz", StringComparison.Ordinal))
            return new Color(0.16f, 0.45f, 0.82f, 0.50f);
        if (tileId.Contains("enemy", StringComparison.Ordinal))
            return new Color(0.62f, 0.12f, 0.09f, 0.52f);
        if (tileId.Contains("road", StringComparison.Ordinal) || tileId.Contains("intersection", StringComparison.Ordinal))
            return new Color(0.42f, 0.39f, 0.32f, 0.52f);
        if (tileId.Contains("damaged", StringComparison.Ordinal))
            return new Color(0.45f, 0.32f, 0.24f, 0.52f);
        return new Color(0.48f, 0.42f, 0.31f, 0.48f);
    }

    private static Color ColorForFaction(string faction, float alpha)
    {
        return faction switch
        {
            "friendly" => new Color(0.14f, 0.62f, 1f, alpha),
            "enemy" => new Color(1f, 0.16f, 0.10f, alpha),
            "neutral" => new Color(0.86f, 0.86f, 0.72f, alpha),
            _ => new Color(1f, 0.85f, 0.22f, alpha)
        };
    }

    private static Vector3 CellToIso(Vector2Int cell)
    {
        return new Vector3((cell.x - cell.y) * IsoHalfWidth, (cell.x + cell.y) * IsoHalfHeight, 0f);
    }

    private static string BuildReport(IsoMap map, MapBuildSpec spec)
    {
        bool graphConnected = IsRoadGraphConnected(map.RoadGraph);
        bool socketsInBounds = AllSocketsInBounds(map);
        bool spawnsInBounds = AllSpawnsInBounds(map);
        bool macroCoverageComplete = MacroCoverageComplete(map);
        bool roadEdgesValid = AllRoadEdgesResolve(map.RoadGraph);
        int authoredMacroTiles = CountAuthoredMacroTileArt(map);
        int fallbackMacroTiles = Mathf.Max(0, map.MacroTileGrid.Placements.Count - authoredMacroTiles);
        int blockedArea = TotalArea(map.Regions.Blocked);
        int roadArea = TotalArea(map.Regions.RoadCorridors);
        int waterArea = TotalArea(map.Regions.Water);

        var sb = new StringBuilder();
        sb.AppendLine("# " + map.DisplayName + " Unity Import Report");
        sb.AppendLine();
        sb.AppendLine("Date: 2026-05-05");
        sb.AppendLine();
        sb.AppendLine("## Source");
        sb.AppendLine();
        sb.AppendLine($"- Map: `{spec.MapPath}`");
        sb.AppendLine($"- Scene: `{spec.ScenePath}`");
        sb.AppendLine($"- Clean visual target capture: `{spec.CleanTargetCapturePath}`");
        sb.AppendLine($"- Placeholder terrain capture: `{spec.PlaceholderTerrainCapturePath}`");
        sb.AppendLine($"- Metadata overlay capture: `{spec.CapturePath}`");
        sb.AppendLine($"- Visual validation target: `{spec.PreviewAssetPath}`");
        sb.AppendLine($"- Macro tile art root: `{MacroTileArtRoot}`");
        sb.AppendLine();
        sb.AppendLine("## Parsed Data");
        sb.AppendLine();
        sb.AppendLine($"- Map id: `{map.MapId}`");
        sb.AppendLine($"- Grid: `{map.Grid.WidthCells} x {map.Grid.HeightCells}` cells");
        sb.AppendLine($"- Macro chunks: `{map.MacroTileGrid.Placements.Count}`");
        sb.AppendLine($"- Road graph nodes: `{map.RoadGraph.Nodes.Count}`");
        sb.AppendLine($"- Road graph edges: `{map.RoadGraph.Edges.Count}`");
        sb.AppendLine($"- Blocked regions: `{map.Regions.Blocked.Count}` area `{blockedArea}` cells");
        sb.AppendLine($"- Road regions: `{map.Regions.RoadCorridors.Count}` area `{roadArea}` cells");
        sb.AppendLine($"- Water regions: `{map.Regions.Water.Count}` area `{waterArea}` cells");
        sb.AppendLine($"- Sockets: `{map.Sockets.Count}`");
        sb.AppendLine($"- Spawn groups: `{map.SpawnGroups.Count}`");
        sb.AppendLine($"- Authored macro visual chunks: `{authoredMacroTiles} / {map.MacroTileGrid.Placements.Count}`");
        sb.AppendLine($"- Placeholder macro visual chunks: `{fallbackMacroTiles}`");
        sb.AppendLine();
        sb.AppendLine("## Validation");
        sb.AppendLine();
        sb.AppendLine($"- Road graph connected: `{PassFail(graphConnected)}`");
        sb.AppendLine($"- Road graph edges resolve: `{PassFail(roadEdgesValid)}`");
        sb.AppendLine($"- Macro tile coverage complete: `{PassFail(macroCoverageComplete)}`");
        sb.AppendLine($"- Sockets in bounds: `{PassFail(socketsInBounds)}`");
        sb.AppendLine($"- Spawn cells in bounds: `{PassFail(spawnsInBounds)}`");
        sb.AppendLine($"- Target live units: `{map.PerformanceTargets.TargetLiveUnits}`");
        sb.AppendLine($"- Stress live units: `{map.PerformanceTargets.StressLiveUnits}`");
        sb.AppendLine($"- Max path requests per frame: `{map.PerformanceTargets.MaxPathRequestsPerFrame}`");
        sb.AppendLine();
        sb.AppendLine("## Visual Validation Status");
        sb.AppendLine();
        sb.AppendLine("- Current Unity scene includes the full-map visual preview as a reference plane.");
        sb.AppendLine("- Authored macro tile PNGs are used when available; otherwise placeholder macro chunks remain on top for alignment checks.");
        sb.AppendLine("- Clean target, placeholder terrain, and metadata overlay captures are separate artifacts for manual review.");
        sb.AppendLine("- Gate status: `AWAITING_FG_L01_VISUAL_APPROVAL`.");
        sb.AppendLine("- This is ready for first eye-test validation of the map direction, but not yet a production macro-tile assembly.");
        if (fallbackMacroTiles > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Missing Macro Tile Art");
            sb.AppendLine();
            foreach (var placement in map.MacroTileGrid.Placements)
            {
                if (string.IsNullOrEmpty(ResolveMacroTileTexturePath(map, placement, out _)))
                    sb.AppendLine($"- `{placement.TileId}` variant `{placement.Variant}` rotation `{placement.Rotation}` at macro `{placement.X},{placement.Y}`");
            }
        }
        sb.AppendLine();
        sb.AppendLine("## Next Step");
        sb.AppendLine();
        sb.AppendLine("Replace placeholder macro chunk colors with authored visual chunk sprites, then feed the same regions into ECS grid buffers for movement validation.");
        return sb.ToString();
    }

    private static int CountAuthoredMacroTileArt(IsoMap map)
    {
        int count = 0;
        foreach (var placement in map.MacroTileGrid.Placements)
        {
            if (!string.IsNullOrEmpty(ResolveMacroTileTexturePath(map, placement, out _)))
                count++;
        }

        return count;
    }

    private static string ResolveMacroTileTexturePath(IsoMap map, MacroTilePlacement placement, out bool alreadyRotated)
    {
        foreach (var candidate in MacroTileArtCandidates(map, placement))
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(candidate.AssetPath) != null)
            {
                alreadyRotated = candidate.AlreadyRotated;
                return candidate.AssetPath;
            }
        }

        alreadyRotated = false;
        return string.Empty;
    }

    private static IEnumerable<MacroTileArtCandidate> MacroTileArtCandidates(IsoMap map, MacroTilePlacement placement)
    {
        string mapKey = SanitizeAssetKey(map.MapId);
        string tileKey = SanitizeAssetKey(placement.TileId);
        string variantKey = SanitizeAssetKey(placement.Variant);
        string exactName = $"{tileKey}_{variantKey}_rot{placement.Rotation}.png";
        string variantName = $"{tileKey}_{variantKey}.png";
        string baseName = tileKey + ".png";

        yield return new MacroTileArtCandidate($"{MacroTileArtRoot}/{mapKey}/{exactName}", true);
        yield return new MacroTileArtCandidate($"{MacroTileArtRoot}/{mapKey}/{variantName}", false);
        yield return new MacroTileArtCandidate($"{MacroTileArtRoot}/Shared/{exactName}", true);
        yield return new MacroTileArtCandidate($"{MacroTileArtRoot}/Shared/{variantName}", false);
        yield return new MacroTileArtCandidate($"{MacroTileArtRoot}/Shared/{baseName}", false);
        yield return new MacroTileArtCandidate($"{MacroTileArtRoot}/{exactName}", true);
        yield return new MacroTileArtCandidate($"{MacroTileArtRoot}/{variantName}", false);
        yield return new MacroTileArtCandidate($"{MacroTileArtRoot}/{baseName}", false);
    }

    private static string SanitizeAssetKey(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "default";

        var builder = new StringBuilder(value.Length);
        bool previousUnderscore = false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = char.ToLowerInvariant(value[i]);
            bool keep = c >= 'a' && c <= 'z' || c >= '0' && c <= '9';
            if (keep)
            {
                builder.Append(c);
                previousUnderscore = false;
            }
            else if (!previousUnderscore)
            {
                builder.Append('_');
                previousUnderscore = true;
            }
        }

        return builder.ToString().Trim('_');
    }

    private static bool MacroCoverageComplete(IsoMap map)
    {
        if (map.MacroTileGrid.MacroTileSizeCells <= 0)
            return false;

        int expectedX = Mathf.CeilToInt(map.Grid.WidthCells / (float)map.MacroTileGrid.MacroTileSizeCells);
        int expectedY = Mathf.CeilToInt(map.Grid.HeightCells / (float)map.MacroTileGrid.MacroTileSizeCells);
        var occupied = new HashSet<Vector2Int>();
        foreach (var placement in map.MacroTileGrid.Placements)
            occupied.Add(new Vector2Int(placement.X, placement.Y));

        for (int y = 0; y < expectedY; y++)
        {
            for (int x = 0; x < expectedX; x++)
            {
                if (!occupied.Contains(new Vector2Int(x, y)))
                    return false;
            }
        }

        return true;
    }

    private static bool AllRoadEdgesResolve(RoadGraphData graph)
    {
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes)
            nodeIds.Add(node.Id);

        foreach (var edge in graph.Edges)
        {
            if (!nodeIds.Contains(edge.From) || !nodeIds.Contains(edge.To))
                return false;
        }

        return true;
    }

    private static bool IsRoadGraphConnected(RoadGraphData graph)
    {
        if (graph.Nodes.Count == 0)
            return false;

        var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes)
            adjacency[node.Id] = new List<string>();
        foreach (var edge in graph.Edges)
        {
            if (!adjacency.ContainsKey(edge.From) || !adjacency.ContainsKey(edge.To))
                continue;
            adjacency[edge.From].Add(edge.To);
            adjacency[edge.To].Add(edge.From);
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(graph.Nodes[0].Id);
        visited.Add(graph.Nodes[0].Id);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            foreach (var next in adjacency[id])
            {
                if (visited.Add(next))
                    queue.Enqueue(next);
            }
        }

        return visited.Count == graph.Nodes.Count;
    }

    private static bool AllSocketsInBounds(IsoMap map)
    {
        foreach (var socket in map.Sockets)
        {
            if (!InBounds(socket.Cell, map.Grid))
                return false;
        }

        return true;
    }

    private static bool AllSpawnsInBounds(IsoMap map)
    {
        foreach (var spawn in map.SpawnGroups)
        {
            foreach (var cell in spawn.Cells)
            {
                if (!InBounds(cell, map.Grid))
                    return false;
            }
        }

        return true;
    }

    private static bool InBounds(Vector2Int cell, GridData grid)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < grid.WidthCells && cell.y < grid.HeightCells;
    }

    private static int TotalArea(List<RectRegion> regions)
    {
        int area = 0;
        foreach (var region in regions)
            area += Mathf.Max(0, region.Rect.width) * Mathf.Max(0, region.Rect.height);
        return area;
    }

    private static string PassFail(bool passed)
    {
        return passed ? "PASS" : "FAIL";
    }

    private static void WriteSoftwareCapture(IsoMap map, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var pixels = new Color32[CaptureWidth * CaptureHeight];
        Fill(pixels, new Color32(14, 19, 22, 255));

        var transform = SoftwareIsoTransform.Create(map, CaptureWidth, CaptureHeight, 96f);

        foreach (var placement in map.MacroTileGrid.Placements)
        {
            var rect = new RectInt(
                placement.X * map.MacroTileGrid.MacroTileSizeCells,
                placement.Y * map.MacroTileGrid.MacroTileSizeCells,
                map.MacroTileGrid.MacroTileSizeCells,
                map.MacroTileGrid.MacroTileSizeCells);
            DrawCellRect(pixels, transform, rect, ColorForMacroTile(placement.TileId));
        }

        foreach (var region in map.Regions.SidewalkCorridors)
            DrawCellRect(pixels, transform, region.Rect, new Color(0.95f, 0.82f, 0.48f, 0.45f));
        foreach (var region in map.Regions.RoadCorridors)
            DrawCellRect(pixels, transform, region.Rect, new Color(0.10f, 0.13f, 0.15f, 0.88f));
        foreach (var region in map.Regions.Water)
            DrawCellRect(pixels, transform, region.Rect, new Color(0.02f, 0.45f, 0.60f, 0.86f));
        foreach (var region in map.Regions.Blocked)
            DrawCellRect(pixels, transform, region.Rect, new Color(0.52f, 0.18f, 0.10f, 0.56f));
        foreach (var region in map.Regions.CombatZones)
            DrawCellRect(pixels, transform, region.Rect, new Color(1f, 0.44f, 0.08f, 0.18f));

        var nodesById = new Dictionary<string, RoadNode>(StringComparer.Ordinal);
        foreach (var node in map.RoadGraph.Nodes)
            nodesById[node.Id] = node;

        foreach (var edge in map.RoadGraph.Edges)
        {
            if (!nodesById.TryGetValue(edge.From, out var from) || !nodesById.TryGetValue(edge.To, out var to))
                continue;

            var color = edge.Surface == "sidewalk"
                ? new Color(1f, 0.86f, 0.34f, 0.88f)
                : new Color(0.18f, 0.82f, 1f, 0.84f);
            DrawLine(pixels, transform.ToPixel(from.Cell), transform.ToPixel(to.Cell), Mathf.Clamp(edge.WidthCells * 2.2f, 7f, 26f), color);
        }

        foreach (var node in map.RoadGraph.Nodes)
            DrawCircle(pixels, transform.ToPixel(node.Cell), 7f, new Color(1f, 1f, 1f, 0.95f));

        foreach (var socket in map.Sockets)
        {
            var rect = new RectInt(
                socket.Cell.x - socket.SizeCells.x / 2,
                socket.Cell.y - socket.SizeCells.y / 2,
                socket.SizeCells.x,
                socket.SizeCells.y);
            DrawCellRect(pixels, transform, rect, ColorForFaction(socket.Faction, 0.74f));
        }

        foreach (var spawn in map.SpawnGroups)
        {
            foreach (var cell in spawn.Cells)
                DrawCircle(pixels, transform.ToPixel(cell), 10f, ColorForFaction(spawn.Faction, 0.96f));
        }

        DrawFrame(pixels, new Color32(215, 202, 162, 255));
        WritePng(path, CaptureWidth, CaptureHeight, pixels);
    }

    private static void CopyCleanVisualTarget(MapBuildSpec spec)
    {
        var source = WorkspacePath(spec.PreviewAssetPath);
        var destination = WorkspacePath(spec.CleanTargetCapturePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        if (!File.Exists(source))
        {
            Debug.LogWarning($"Missing clean visual target source: {source}");
            return;
        }

        File.Copy(source, destination, true);
    }

    private static void WriteSoftwareTerrainCapture(IsoMap map, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var pixels = new Color32[CaptureWidth * CaptureHeight];
        Fill(pixels, new Color32(26, 31, 33, 255));

        var transform = SoftwareIsoTransform.Create(map, CaptureWidth, CaptureHeight, 96f);
        foreach (var placement in map.MacroTileGrid.Placements)
        {
            var rect = new RectInt(
                placement.X * map.MacroTileGrid.MacroTileSizeCells,
                placement.Y * map.MacroTileGrid.MacroTileSizeCells,
                map.MacroTileGrid.MacroTileSizeCells,
                map.MacroTileGrid.MacroTileSizeCells);
            DrawCellRect(pixels, transform, rect, ColorForMacroTile(placement.TileId));
        }

        foreach (var region in map.Regions.Water)
            DrawCellRect(pixels, transform, region.Rect, new Color(0.02f, 0.36f, 0.46f, 0.68f));
        foreach (var region in map.Regions.RoadCorridors)
            DrawCellRect(pixels, transform, region.Rect, new Color(0.12f, 0.14f, 0.15f, 0.62f));
        foreach (var region in map.Regions.SidewalkCorridors)
            DrawCellRect(pixels, transform, region.Rect, new Color(0.74f, 0.66f, 0.44f, 0.42f));

        DrawFrame(pixels, new Color32(164, 146, 104, 255));
        WritePng(path, CaptureWidth, CaptureHeight, pixels);
    }

    private readonly struct SoftwareIsoTransform
    {
        private readonly float scale;
        private readonly float offsetX;
        private readonly float offsetY;
        private readonly int imageHeight;

        private SoftwareIsoTransform(float scale, float offsetX, float offsetY, int imageHeight)
        {
            this.scale = scale;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this.imageHeight = imageHeight;
        }

        public static SoftwareIsoTransform Create(IsoMap map, int imageWidth, int imageHeight, float margin)
        {
            var a = CellToIso(new Vector2Int(0, 0));
            var b = CellToIso(new Vector2Int(map.Grid.WidthCells, 0));
            var c = CellToIso(new Vector2Int(0, map.Grid.HeightCells));
            var d = CellToIso(new Vector2Int(map.Grid.WidthCells, map.Grid.HeightCells));
            float minX = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x));
            float maxX = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x));
            float minY = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
            float maxY = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));
            float scale = Mathf.Min((imageWidth - margin * 2f) / Mathf.Max(maxX - minX, 1f), (imageHeight - margin * 2f) / Mathf.Max(maxY - minY, 1f));
            float offsetX = margin - minX * scale;
            float offsetY = margin - minY * scale;
            return new SoftwareIsoTransform(scale, offsetX, offsetY, imageHeight);
        }

        public Vector2 ToPixel(Vector2Int cell)
        {
            var iso = CellToIso(cell);
            return new Vector2(iso.x * scale + offsetX, imageHeight - (iso.y * scale + offsetY));
        }
    }

    private static void DrawCellRect(Color32[] pixels, SoftwareIsoTransform transform, RectInt rect, Color color)
    {
        var points = new[]
        {
            transform.ToPixel(new Vector2Int(rect.xMin, rect.yMin)),
            transform.ToPixel(new Vector2Int(rect.xMax, rect.yMin)),
            transform.ToPixel(new Vector2Int(rect.xMax, rect.yMax)),
            transform.ToPixel(new Vector2Int(rect.xMin, rect.yMax))
        };
        DrawPolygon(pixels, points, color);
    }

    private static void DrawPolygon(Color32[] pixels, Vector2[] points, Color color)
    {
        float minX = points[0].x;
        float maxX = points[0].x;
        float minY = points[0].y;
        float maxY = points[0].y;
        for (int i = 1; i < points.Length; i++)
        {
            minX = Mathf.Min(minX, points[i].x);
            maxX = Mathf.Max(maxX, points[i].x);
            minY = Mathf.Min(minY, points[i].y);
            maxY = Mathf.Max(maxY, points[i].y);
        }

        int x0 = Mathf.Clamp(Mathf.FloorToInt(minX), 0, CaptureWidth - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(maxX), 0, CaptureWidth - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(minY), 0, CaptureHeight - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(maxY), 0, CaptureHeight - 1);
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (ContainsPoint(points, new Vector2(x + 0.5f, y + 0.5f)))
                    BlendPixel(pixels, x, y, color);
            }
        }
    }

    private static bool ContainsPoint(Vector2[] polygon, Vector2 point)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            bool crosses = polygon[i].y > point.y != polygon[j].y > point.y;
            if (crosses)
            {
                float x = (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x;
                if (point.x < x)
                    inside = !inside;
            }
        }

        return inside;
    }

    private static void DrawLine(Color32[] pixels, Vector2 start, Vector2 end, float width, Color color)
    {
        float radius = width * 0.5f;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(start.x, end.x) - radius), 0, CaptureWidth - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(start.x, end.x) + radius), 0, CaptureWidth - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(start.y, end.y) - radius), 0, CaptureHeight - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(start.y, end.y) + radius), 0, CaptureHeight - 1);
        var delta = end - start;
        float lengthSquared = Mathf.Max(delta.sqrMagnitude, 0.001f);
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(point - start, delta) / lengthSquared);
                var closest = start + delta * t;
                if ((point - closest).sqrMagnitude <= radius * radius)
                    BlendPixel(pixels, x, y, color);
            }
        }
    }

    private static void DrawCircle(Color32[] pixels, Vector2 center, float radius, Color color)
    {
        int x0 = Mathf.Clamp(Mathf.FloorToInt(center.x - radius), 0, CaptureWidth - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(center.x + radius), 0, CaptureWidth - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(center.y - radius), 0, CaptureHeight - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(center.y + radius), 0, CaptureHeight - 1);
        float radiusSquared = radius * radius;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                if ((point - center).sqrMagnitude <= radiusSquared)
                    BlendPixel(pixels, x, y, color);
            }
        }
    }

    private static void DrawFrame(Color32[] pixels, Color32 color)
    {
        for (int x = 0; x < CaptureWidth; x++)
        {
            SetPixel(pixels, x, 0, color);
            SetPixel(pixels, x, CaptureHeight - 1, color);
        }

        for (int y = 0; y < CaptureHeight; y++)
        {
            SetPixel(pixels, 0, y, color);
            SetPixel(pixels, CaptureWidth - 1, y, color);
        }
    }

    private static void Fill(Color32[] pixels, Color32 color)
    {
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
    }

    private static void BlendPixel(Color32[] pixels, int x, int y, Color color)
    {
        int index = y * CaptureWidth + x;
        var source = (Color32)color;
        var destination = pixels[index];
        float alpha = source.a / 255f;
        float inverse = 1f - alpha;
        pixels[index] = new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(source.r * alpha + destination.r * inverse), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(source.g * alpha + destination.g * inverse), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(source.b * alpha + destination.b * inverse), 0, 255),
            255);
    }

    private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
    {
        pixels[y * CaptureWidth + x] = color;
    }

    private static void WritePng(string path, int width, int height, Color32[] pixels)
    {
        byte[] raw = new byte[height * (1 + width * 4)];
        int rawIndex = 0;
        for (int y = 0; y < height; y++)
        {
            raw[rawIndex++] = 0;
            for (int x = 0; x < width; x++)
            {
                var pixel = pixels[y * width + x];
                raw[rawIndex++] = pixel.r;
                raw[rawIndex++] = pixel.g;
                raw[rawIndex++] = pixel.b;
                raw[rawIndex++] = pixel.a;
            }
        }

        using var stream = new MemoryStream();
        stream.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);

        using (var ihdr = new MemoryStream())
        {
            WriteBigEndian(ihdr, width);
            WriteBigEndian(ihdr, height);
            ihdr.WriteByte(8);
            ihdr.WriteByte(6);
            ihdr.WriteByte(0);
            ihdr.WriteByte(0);
            ihdr.WriteByte(0);
            WriteChunk(stream, "IHDR", ihdr.ToArray());
        }

        WriteChunk(stream, "IDAT", BuildZlibStoreStream(raw));
        WriteChunk(stream, "IEND", Array.Empty<byte>());
        File.WriteAllBytes(path, stream.ToArray());
    }

    private static byte[] BuildZlibStoreStream(byte[] raw)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(0x78);
        stream.WriteByte(0x01);

        int offset = 0;
        while (offset < raw.Length)
        {
            int length = Mathf.Min(65535, raw.Length - offset);
            bool final = offset + length >= raw.Length;
            stream.WriteByte(final ? (byte)1 : (byte)0);
            stream.WriteByte((byte)(length & 0xff));
            stream.WriteByte((byte)((length >> 8) & 0xff));
            int nlen = ~length;
            stream.WriteByte((byte)(nlen & 0xff));
            stream.WriteByte((byte)((nlen >> 8) & 0xff));
            stream.Write(raw, offset, length);
            offset += length;
        }

        WriteBigEndian(stream, Adler32(raw));
        return stream.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        WriteBigEndian(stream, data.Length);
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes, 0, typeBytes.Length);
        stream.Write(data, 0, data.Length);
        uint crc = Crc32(typeBytes, data);
        WriteBigEndian(stream, unchecked((int)crc));
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        uint crc = 0xffffffffu;
        for (int i = 0; i < type.Length; i++)
            crc = Crc32Byte(crc, type[i]);
        for (int i = 0; i < data.Length; i++)
            crc = Crc32Byte(crc, data[i]);
        return crc ^ 0xffffffffu;
    }

    private static uint Crc32Byte(uint crc, byte value)
    {
        crc ^= value;
        for (int i = 0; i < 8; i++)
            crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
        return crc;
    }

    private static int Adler32(byte[] data)
    {
        const uint mod = 65521;
        uint a = 1;
        uint b = 0;
        for (int i = 0; i < data.Length; i++)
        {
            a = (a + data[i]) % mod;
            b = (b + a) % mod;
        }

        return unchecked((int)((b << 16) | a));
    }

    private static void WriteBigEndian(Stream stream, int value)
    {
        stream.WriteByte((byte)((value >> 24) & 0xff));
        stream.WriteByte((byte)((value >> 16) & 0xff));
        stream.WriteByte((byte)((value >> 8) & 0xff));
        stream.WriteByte((byte)(value & 0xff));
    }

    private static string ProjectRootPath()
    {
        return Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
    }

    private static string WorkspaceRootPath()
    {
        var projectRoot = ProjectRootPath();
        if (File.Exists(Path.Combine(projectRoot, DefaultMapPath)))
            return projectRoot;

        var parent = Directory.GetParent(projectRoot);
        if (parent != null)
        {
            var mainProjectCandidate = Path.Combine(parent.FullName, "WarlineCapture");
            if (File.Exists(Path.Combine(mainProjectCandidate, DefaultMapPath)))
                return mainProjectCandidate;
        }

        return projectRoot;
    }

    private static string WorkspacePath(string relativePath)
    {
        return Path.Combine(WorkspaceRootPath(), relativePath);
    }

    private static class MapJsonParser
    {
        public static IsoMap Parse(string json)
        {
            var map = new IsoMap
            {
                MapId = GetString(json, "mapId"),
                DisplayName = GetString(json, "displayName"),
                Grid = ParseGrid(ExtractDelimited(json, "grid", '{', '}')),
                MacroTileGrid = ParseMacroTileGrid(ExtractDelimited(json, "macroTileGrid", '{', '}')),
                RoadGraph = ParseRoadGraph(ExtractDelimited(json, "roadGraph", '{', '}')),
                Regions = ParseRegions(ExtractDelimited(json, "regions", '{', '}')),
                PerformanceTargets = ParsePerformanceTargets(ExtractDelimited(json, "performanceTargets", '{', '}'))
            };

            foreach (var socket in ParseSockets(ExtractDelimited(json, "sockets", '[', ']')))
                map.Sockets.Add(socket);
            foreach (var spawn in ParseSpawnGroups(ExtractDelimited(json, "spawnGroups", '[', ']')))
                map.SpawnGroups.Add(spawn);

            return map;
        }

        private static GridData ParseGrid(string json)
        {
            return new GridData
            {
                WidthCells = GetInt(json, "widthCells"),
                HeightCells = GetInt(json, "heightCells"),
                CellSizeWorld = GetFloat(json, "cellSizeWorld"),
                SectorSizeCells = GetInt(json, "sectorSizeCells")
            };
        }

        private static MacroTileGridData ParseMacroTileGrid(string json)
        {
            var data = new MacroTileGridData
            {
                MacroTileSizeCells = GetInt(json, "macroTileSizeCells"),
                SourcePixels = GetInt(json, "sourcePixels")
            };
            foreach (var item in ExtractObjects(ExtractDelimited(json, "placements", '[', ']')))
            {
                data.Placements.Add(new MacroTilePlacement
                {
                    X = GetInt(item, "x"),
                    Y = GetInt(item, "y"),
                    TileId = GetString(item, "tileId"),
                    Variant = GetString(item, "variant"),
                    Rotation = GetInt(item, "rotation")
                });
            }

            return data;
        }

        private static RoadGraphData ParseRoadGraph(string json)
        {
            var data = new RoadGraphData();
            foreach (var item in ExtractObjects(ExtractDelimited(json, "nodes", '[', ']')))
            {
                var cell = GetIntArray(item, "cell");
                data.Nodes.Add(new RoadNode
                {
                    Id = GetString(item, "id"),
                    Cell = ToVector2Int(cell),
                    Kind = GetString(item, "kind")
                });
            }

            foreach (var item in ExtractObjects(ExtractDelimited(json, "edges", '[', ']')))
            {
                data.Edges.Add(new RoadEdge
                {
                    From = GetString(item, "from"),
                    To = GetString(item, "to"),
                    WidthCells = GetInt(item, "widthCells"),
                    Surface = GetString(item, "surface"),
                    VehiclePreferred = GetBool(item, "vehiclePreferred"),
                    InfantryPreferred = GetBool(item, "infantryPreferred")
                });
            }

            return data;
        }

        private static RegionData ParseRegions(string json)
        {
            var data = new RegionData();
            ParseRegionArray(json, "blocked", data.Blocked);
            ParseRegionArray(json, "water", data.Water);
            ParseRegionArray(json, "roadCorridors", data.RoadCorridors);
            ParseRegionArray(json, "sidewalkCorridors", data.SidewalkCorridors);
            ParseRegionArray(json, "combatZones", data.CombatZones);
            ParseRegionArray(json, "cameraZones", data.CameraZones);
            return data;
        }

        private static void ParseRegionArray(string json, string name, List<RectRegion> output)
        {
            var array = TryExtractDelimited(json, name, '[', ']');
            if (string.IsNullOrEmpty(array))
                return;
            foreach (var item in ExtractObjects(array))
            {
                var rect = GetIntArray(item, "rect");
                if (rect.Length < 4)
                    continue;
                output.Add(new RectRegion
                {
                    Id = GetString(item, "id"),
                    Rect = new RectInt(rect[0], rect[1], rect[2], rect[3])
                });
            }
        }

        private static List<SocketData> ParseSockets(string json)
        {
            var sockets = new List<SocketData>();
            foreach (var item in ExtractObjects(json))
            {
                sockets.Add(new SocketData
                {
                    Id = GetString(item, "id"),
                    Kind = GetString(item, "kind"),
                    Cell = ToVector2Int(GetIntArray(item, "cell")),
                    SizeCells = ToVector2Int(GetIntArray(item, "sizeCells")),
                    Faction = GetString(item, "faction")
                });
            }

            return sockets;
        }

        private static List<SpawnGroupData> ParseSpawnGroups(string json)
        {
            var spawns = new List<SpawnGroupData>();
            foreach (var item in ExtractObjects(json))
            {
                var spawn = new SpawnGroupData
                {
                    Id = GetString(item, "id"),
                    Faction = GetString(item, "faction"),
                    Kind = GetString(item, "kind"),
                    MaxUnits = GetInt(item, "maxUnits")
                };
                foreach (var cell in GetIntPairArray(item, "cells"))
                    spawn.Cells.Add(cell);
                spawns.Add(spawn);
            }

            return spawns;
        }

        private static PerformanceTargets ParsePerformanceTargets(string json)
        {
            return new PerformanceTargets
            {
                TargetLiveUnits = GetInt(json, "targetLiveUnits"),
                StressLiveUnits = GetInt(json, "stressLiveUnits"),
                MaxPathRequestsPerFrame = GetInt(json, "maxPathRequestsPerFrame"),
                MaxResidentTextureMb = GetInt(json, "maxResidentTextureMb")
            };
        }

        private static Vector2Int ToVector2Int(int[] values)
        {
            return values.Length >= 2 ? new Vector2Int(values[0], values[1]) : Vector2Int.zero;
        }

        private static string GetString(string json, string name)
        {
            var match = Regex.Match(json, "\"" + Regex.Escape(name) + "\"\\s*:\\s*\"([^\"]*)\"");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static int GetInt(string json, string name)
        {
            var match = Regex.Match(json, "\"" + Regex.Escape(name) + "\"\\s*:\\s*(-?\\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        }

        private static float GetFloat(string json, string name)
        {
            var match = Regex.Match(json, "\"" + Regex.Escape(name) + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)");
            return match.Success ? float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0f;
        }

        private static bool GetBool(string json, string name)
        {
            var match = Regex.Match(json, "\"" + Regex.Escape(name) + "\"\\s*:\\s*(true|false)");
            return match.Success && match.Groups[1].Value == "true";
        }

        private static int[] GetIntArray(string json, string name)
        {
            var array = ExtractDelimited(json, name, '[', ']');
            var matches = Regex.Matches(array, "-?\\d+");
            var values = new int[matches.Count];
            for (int i = 0; i < matches.Count; i++)
                values[i] = int.Parse(matches[i].Value, CultureInfo.InvariantCulture);
            return values;
        }

        private static List<Vector2Int> GetIntPairArray(string json, string name)
        {
            var array = ExtractDelimited(json, name, '[', ']');
            var matches = Regex.Matches(array, "\\[\\s*(-?\\d+)\\s*,\\s*(-?\\d+)\\s*\\]");
            var values = new List<Vector2Int>();
            foreach (Match match in matches)
            {
                values.Add(new Vector2Int(
                    int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static List<string> ExtractObjects(string arrayJson)
        {
            var objects = new List<string>();
            int depth = 0;
            int start = -1;
            bool inString = false;
            for (int i = 0; i < arrayJson.Length; i++)
            {
                char c = arrayJson[i];
                if (c == '"' && (i == 0 || arrayJson[i - 1] != '\\'))
                    inString = !inString;
                if (inString)
                    continue;
                if (c == '{')
                {
                    if (depth == 0)
                        start = i;
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        objects.Add(arrayJson.Substring(start, i - start + 1));
                        start = -1;
                    }
                }
            }

            return objects;
        }

        private static string ExtractDelimited(string json, string name, char open, char close)
        {
            var value = TryExtractDelimited(json, name, open, close);
            if (string.IsNullOrEmpty(value))
                throw new FormatException($"Missing JSON field `{name}`.");
            return value;
        }

        private static string TryExtractDelimited(string json, string name, char open, char close)
        {
            int nameIndex = json.IndexOf("\"" + name + "\"", StringComparison.Ordinal);
            if (nameIndex < 0)
                return string.Empty;
            int colonIndex = json.IndexOf(':', nameIndex);
            if (colonIndex < 0)
                return string.Empty;
            int start = json.IndexOf(open, colonIndex);
            if (start < 0)
                return string.Empty;

            int depth = 0;
            bool inString = false;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                    inString = !inString;
                if (inString)
                    continue;
                if (c == open)
                    depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                        return json.Substring(start, i - start + 1);
                }
            }

            return string.Empty;
        }
    }
}
#endif
