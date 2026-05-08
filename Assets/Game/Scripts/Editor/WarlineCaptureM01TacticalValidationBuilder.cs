#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureM01TacticalValidationBuilder
{
    private const string MapId = "iso.ch01.district_edge_01";
    private const string LevelId = "level.ch01.district_edge_01";
    private const string MissionId = "saga.ch01.m01.first_contact";
    private const string ScenarioSetupId = "scenario.ch01.m01.first_contact";
    private const string MapPreviewArtId = "preview.ch01.first_contact";
    private const string MinimapArtId = "minimap.ch01.first_contact";
    private const string GroundPath = "Assets/Game/Art/Generated/IsometricMaps/TacticalGroundQualityTest_A/tactical_ground_quality_test_close_pot_a.png";
    private const string EntityRoot = "Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites";
    private const string DataRoot = "Assets/Game/Data/TacticalMaps/Chapter01";
    private const string DefinitionPath = DataRoot + "/" + MapId + ".asset";
    private const string GridConfigPath = DataRoot + "/" + MapId + ".grid.asset";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/Chapter01/Chapter01_M01_TacticalValidation.unity";

    private static readonly Vector2 VisibleWorldSize = new(3.4f, 1.92f);
    private static readonly Vector2 WorldOrigin = -VisibleWorldSize * 0.5f;
    private static readonly Rect CameraBounds = new(-1.35f, -0.68f, 2.7f, 1.36f);

    private readonly struct EntitySpec
    {
        public readonly string Name;
        public readonly string Path;
        public readonly string AnchorId;
        public readonly TacticalVisualScaleRole ScaleRole;
        public readonly int SortingOrder;
        public readonly Color Tint;

        public EntitySpec(string name, string path, string anchorId, TacticalVisualScaleRole scaleRole, int sortingOrder, Color tint)
        {
            Name = name;
            Path = path;
            AnchorId = anchorId;
            ScaleRole = scaleRole;
            SortingOrder = sortingOrder;
            Tint = tint;
        }
    }

    private static readonly EntitySpec[] Entities =
    {
        new("M01_Player_RifleSquad_01", EntityRoot + "/infantry_squad.png", "player_spawn.command_squad", TacticalVisualScaleRole.InfantrySquad, 30, Color.white),
        new("M01_Enemy_Patrol_01", EntityRoot + "/infantry_squad.png", "enemy_spawn.patrol_start", TacticalVisualScaleRole.InfantrySquad, 31, new Color(1f, 0.58f, 0.48f, 1f)),
        new("M01_Decor_CommandPoint", EntityRoot + "/command_building.png", "decor.command_point", TacticalVisualScaleRole.CommandBuilding, 22, Color.white),
        new("M01_Decor_TentCluster", EntityRoot + "/tent_cluster.png", "decor.tent_cluster_01", TacticalVisualScaleRole.TentCluster, 23, Color.white),
    };

    [MenuItem("WarlineCapture/Design/Build Chapter01 M01 Tactical Validation")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Chapter01TacticalScaleContract scaleContract = WarlineCaptureChapter01TacticalScaleContractUtility.LoadOrCreate();
        WarlineCaptureChapter01TacticalAssetManifestUtility.LoadOrCreate();
        WarlineCaptureChapter01TacticalAtlasContractUtility.LoadOrCreate();
        EnsureSpriteImport(GroundPath, false, scaleContract.GroundPixelsPerUnit, scaleContract.GroundMaxTextureSize, TextureImporterCompression.CompressedHQ);
        foreach (EntitySpec entity in Entities)
        {
            EnsureSpriteImport(entity.Path, true, scaleContract.EntityPixelsPerUnit, scaleContract.EntityMaxTextureSize, TextureImporterCompression.Uncompressed);
        }

        AssetDatabase.Refresh();

        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundPath);
        if (groundSprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_M01_TACTICAL_VALIDATION_MISSING_GROUND path={GroundPath}");
            return;
        }

        TacticalMapDefinition definition = CreateOrUpdateDefinition(groundSprite, scaleContract);
        GridAuthoringSceneConfigAsset gridConfig = CreateOrUpdateGridConfig(scaleContract);

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene(definition, gridConfig, scaleContract);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_M01_TACTICAL_VALIDATION_BUILT scene={ScenePath} definition={DefinitionPath} grid={GridConfigPath}");
    }

    private static TacticalMapDefinition CreateOrUpdateDefinition(Sprite groundSprite, Chapter01TacticalScaleContract scaleContract)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));

        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<TacticalMapDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }

        definition.Configure(
            MapId,
            LevelId,
            MissionId,
            ScenarioSetupId,
            groundSprite,
            MapPreviewArtId,
            MinimapArtId,
            scaleContract.DefaultGridWidth,
            scaleContract.DefaultGridHeight,
            scaleContract.TacticalWorldSize.x / scaleContract.DefaultGridWidth,
            WorldOrigin,
            scaleContract.TacticalWorldSize,
            NormalizedToWorld(new Vector2(0.28f, 0.52f)),
            scaleContract.CloseCameraOrthographicSize,
            CameraBounds,
            CreateAnchors(),
            CreateSurfaces(),
            CreateRoutes(),
            CreateEntityFootprints(),
            CreateReasonCodes());

        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        return definition;
    }

    private static GridAuthoringSceneConfigAsset CreateOrUpdateGridConfig(Chapter01TacticalScaleContract scaleContract)
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));

        GridAuthoringSceneConfigAsset gridConfig = AssetDatabase.LoadAssetAtPath<GridAuthoringSceneConfigAsset>(GridConfigPath);
        if (gridConfig == null)
        {
            gridConfig = ScriptableObject.CreateInstance<GridAuthoringSceneConfigAsset>();
            AssetDatabase.CreateAsset(gridConfig, GridConfigPath);
        }

        SerializedObject serialized = new(gridConfig);
        serialized.FindProperty("width").intValue = scaleContract.DefaultGridWidth;
        serialized.FindProperty("height").intValue = scaleContract.DefaultGridHeight;
        serialized.FindProperty("cellSize").floatValue = scaleContract.TacticalWorldSize.x / scaleContract.DefaultGridWidth;
        serialized.FindProperty("blockedCells").arraySize = 0;
        Vector2Int[] blockedCells = CreateBlockedEdgeCells(scaleContract.DefaultGridWidth, scaleContract.DefaultGridHeight);
        SerializedProperty blocked = serialized.FindProperty("blockedCells");
        blocked.arraySize = blockedCells.Length;
        for (int i = 0; i < blockedCells.Length; i++)
        {
            blocked.GetArrayElementAtIndex(i).vector2IntValue = blockedCells[i];
        }

        serialized.FindProperty("drawGrid").boolValue = true;
        serialized.FindProperty("drawWhenNotSelected").boolValue = true;
        serialized.FindProperty("drawRuntimeDebugInPlayMode").boolValue = true;
        serialized.FindProperty("fillWalkableCells").boolValue = false;
        serialized.FindProperty("fillRoadCells").boolValue = true;
        serialized.FindProperty("fillSidewalkCells").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gridConfig);
        AssetDatabase.SaveAssets();
        return gridConfig;
    }

    private static void BuildScene(TacticalMapDefinition definition, GridAuthoringSceneConfigAsset gridConfig, Chapter01TacticalScaleContract scaleContract)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("Chapter01_M01_TacticalValidation");
        root.AddComponent<TacticalMapDefinitionReference>().Configure(definition);

        GameObject groundRoot = new("Ground_Proxy_ApprovedCloseupScale");
        groundRoot.transform.SetParent(root.transform, false);

        GameObject ground = CreateSpriteObject("M01_GroundProxy_ReplaceWithFinalNativeTacticalArt", definition.GroundSprite, 0);
        ground.transform.SetParent(groundRoot.transform, false);

        GameObject grid = new("GridAuthoring_64x36_M01Contract");
        grid.transform.SetParent(root.transform, false);
        grid.transform.localPosition = new Vector3(WorldOrigin.x, WorldOrigin.y, 0f);
        GridAuthoring authoring = grid.AddComponent<GridAuthoring>();
        SerializedObject serializedAuthoring = new(authoring);
        serializedAuthoring.FindProperty("config").objectReferenceValue = gridConfig;
        serializedAuthoring.ApplyModifiedPropertiesWithoutUndo();

        GameObject metadataRoot = new("ContractMarkers_DoNotShip");
        metadataRoot.transform.SetParent(root.transform, false);
        CreateSurfaceMarkers(definition, metadataRoot.transform);
        CreateAnchorMarkers(definition, metadataRoot.transform);
        CreateRouteMarkers(definition, metadataRoot.transform);
        CreateReviewLegend(definition, scaleContract, metadataRoot.transform);

        GameObject entityRoot = new("SeparateRuntimeScaleEntities");
        entityRoot.transform.SetParent(root.transform, false);
        CreateEntities(definition, scaleContract, entityRoot.transform);

        CreateCamera("M01_FirstContact_CloseGameplayCamera", new Vector3(definition.CameraDefaultCenter.x, definition.CameraDefaultCenter.y, -10f), definition.DefaultOrthographicSize, true);
        CreateCamera("M01_VISUAL_REVIEW_MetadataAndScaleCamera", new Vector3(definition.CameraDefaultCenter.x, definition.CameraDefaultCenter.y, -10f), definition.DefaultOrthographicSize, false);
        CreateCamera("M01_FirstContact_FullMetadataReviewCamera", new Vector3(0f, 0f, -10f), 1.15f, false);
        Selection.activeObject = root;
    }

    private static void CreateEntities(TacticalMapDefinition definition, Chapter01TacticalScaleContract scaleContract, Transform parent)
    {
        foreach (EntitySpec entity in Entities)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entity.Path);
            if (sprite == null)
            {
                Debug.LogError($"WARLINECAPTURE_M01_TACTICAL_VALIDATION_MISSING_ENTITY name={entity.Name} path={entity.Path}");
                continue;
            }

            if (!definition.TryGetAnchor(entity.AnchorId, out TacticalMapAnchor anchor))
            {
                Debug.LogError($"WARLINECAPTURE_M01_TACTICAL_VALIDATION_MISSING_ENTITY_ANCHOR name={entity.Name} anchor={entity.AnchorId}");
                continue;
            }

            Vector2 world = definition.NormalizedToWorld(anchor.NormalizedPosition);
            GameObject entityObject = CreateSpriteObject(entity.Name, sprite, entity.SortingOrder);
            entityObject.transform.SetParent(parent, false);
            entityObject.transform.localPosition = new Vector3(world.x, world.y, -0.05f);
            float visualScale = scaleContract.GetScale(entity.ScaleRole);
            entityObject.transform.localScale = new Vector3(visualScale, visualScale, 1f);
            entityObject.GetComponent<SpriteRenderer>().color = entity.Tint;
            entityObject.AddComponent<BoxCollider2D>();
        }
    }

    private static void CreateSurfaceMarkers(TacticalMapDefinition definition, Transform parent)
    {
        foreach (TacticalMapSurface surface in definition.Surfaces)
        {
            if (surface.Id == "block.map_edge")
            {
                CreateMapEdgeSurfaceMarkers(parent);
                continue;
            }

            Color color = surface.Type switch
            {
                TacticalMapSurfaceType.Blocked => new Color(1f, 0.2f, 0.2f, 0.12f),
                TacticalMapSurfaceType.CivilianZone => new Color(1f, 0.72f, 0.2f, 0.12f),
                TacticalMapSurfaceType.MainRoad => new Color(0.2f, 0.65f, 1f, 0.12f),
                TacticalMapSurfaceType.RoadShoulder => new Color(0.2f, 1f, 0.45f, 0.10f),
                _ => new Color(0.4f, 1f, 0.9f, 0.08f)
            };

            Rect normalized = surface.NormalizedBounds;
            Vector2 min = definition.NormalizedToWorld(normalized.min);
            Vector2 max = definition.NormalizedToWorld(normalized.max);
            Vector2 center = (min + max) * 0.5f;
            Vector2 size = new(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
            GameObject marker = CreateQuad(surface.Id, center, size, color, -0.025f);
            marker.transform.SetParent(parent, false);
        }
    }

    private static void CreateMapEdgeSurfaceMarkers(Transform parent)
    {
        Color color = new(1f, 0.2f, 0.2f, 0.16f);
        const float borderThickness = 0.045f;
        CreateQuad("block.map_edge.bottom", new Vector2(0f, WorldOrigin.y + borderThickness * 0.5f), new Vector2(VisibleWorldSize.x, borderThickness), color, -0.025f).transform.SetParent(parent, false);
        CreateQuad("block.map_edge.top", new Vector2(0f, -WorldOrigin.y - borderThickness * 0.5f), new Vector2(VisibleWorldSize.x, borderThickness), color, -0.025f).transform.SetParent(parent, false);
        CreateQuad("block.map_edge.left", new Vector2(WorldOrigin.x + borderThickness * 0.5f, 0f), new Vector2(borderThickness, VisibleWorldSize.y), color, -0.025f).transform.SetParent(parent, false);
        CreateQuad("block.map_edge.right", new Vector2(-WorldOrigin.x - borderThickness * 0.5f, 0f), new Vector2(borderThickness, VisibleWorldSize.y), color, -0.025f).transform.SetParent(parent, false);
    }

    private static void CreateAnchorMarkers(TacticalMapDefinition definition, Transform parent)
    {
        foreach (TacticalMapAnchor anchor in definition.Anchors)
        {
            Vector2 world = definition.NormalizedToWorld(anchor.NormalizedPosition);
            Color color = anchor.Type switch
            {
                TacticalMapAnchorType.Spawn => new Color(0.2f, 0.9f, 1f, 0.75f),
                TacticalMapAnchorType.Objective => new Color(1f, 0.2f, 0.16f, 0.75f),
                TacticalMapAnchorType.MoveTarget => new Color(0.2f, 1f, 0.35f, 0.75f),
                TacticalMapAnchorType.Threat => new Color(1f, 0.68f, 0.1f, 0.75f),
                _ => new Color(1f, 1f, 1f, 0.55f)
            };

            GameObject marker = CreateQuad(anchor.Id, world, new Vector2(0.045f, 0.045f), color, -0.03f);
            marker.transform.SetParent(parent, false);
            TextMesh label = CreateLabel(anchor.Id, world + new Vector2(0.02f, 0.045f), color);
            label.transform.SetParent(parent, false);
        }
    }

    private static void CreateRouteMarkers(TacticalMapDefinition definition, Transform parent)
    {
        foreach (TacticalMapRoute route in definition.Routes)
        {
            Vector2[] points = route.NormalizedWaypoints;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 world = definition.NormalizedToWorld(points[i]);
                GameObject marker = CreateQuad($"{route.Id}.waypoint_{i + 1}", world, new Vector2(0.032f, 0.032f), new Color(1f, 0.92f, 0.2f, 0.75f), -0.031f);
                marker.transform.SetParent(parent, false);
            }
        }
    }

    private static void CreateReviewLegend(TacticalMapDefinition definition, Chapter01TacticalScaleContract scaleContract, Transform parent)
    {
        Vector2 topLeft = definition.CameraDefaultCenter + new Vector2(-0.48f, 0.48f);
        TextMesh title = CreateLabel("M01 VISUAL REVIEW: scale + metadata", topLeft, new Color(1f, 1f, 1f, 0.9f));
        title.fontSize = 22;
        title.characterSize = 0.02f;
        title.transform.SetParent(parent, false);

        TextMesh scale = CreateLabel(
            $"camera {scaleContract.CloseCameraOrthographicSize:0.###} | infantry {scaleContract.GetScale(TacticalVisualScaleRole.InfantrySquad):0.###} | grid {definition.GridWidth}x{definition.GridHeight}",
            topLeft + new Vector2(0f, -0.055f),
            new Color(0.70f, 0.95f, 1f, 0.88f));
        scale.fontSize = 18;
        scale.characterSize = 0.017f;
        scale.transform.SetParent(parent, false);

        CreateLegendChip("road", topLeft + new Vector2(0f, -0.12f), new Color(0.2f, 0.65f, 1f, 0.55f), parent);
        CreateLegendChip("walk", topLeft + new Vector2(0.16f, -0.12f), new Color(0.2f, 1f, 0.45f, 0.55f), parent);
        CreateLegendChip("blocked", topLeft + new Vector2(0.32f, -0.12f), new Color(1f, 0.2f, 0.2f, 0.55f), parent);
        CreateLegendChip("anchors", topLeft + new Vector2(0.52f, -0.12f), new Color(1f, 0.92f, 0.2f, 0.75f), parent);
    }

    private static void CreateLegendChip(string labelText, Vector2 position, Color color, Transform parent)
    {
        GameObject chip = CreateQuad("legend." + labelText, position + new Vector2(0.018f, 0f), new Vector2(0.032f, 0.024f), color, -0.04f);
        chip.transform.SetParent(parent, false);
        TextMesh label = CreateLabel(labelText, position + new Vector2(0.043f, 0f), color);
        label.fontSize = 16;
        label.characterSize = 0.015f;
        label.transform.SetParent(parent, false);
    }

    private static TacticalMapAnchor[] CreateAnchors()
    {
        return new[]
        {
            new TacticalMapAnchor("player_spawn.command_squad", TacticalMapAnchorType.Spawn, new Vector2(0.22f, 0.52f), "Initial player rifle squad."),
            new TacticalMapAnchor("decor.command_point", TacticalMapAnchorType.Objective, new Vector2(0.18f, 0.72f), "Visual-only command point proxy, kept off the road corridor."),
            new TacticalMapAnchor("decor.tent_cluster_01", TacticalMapAnchorType.Objective, new Vector2(0.36f, 0.76f), "Visual-only tent proxy, kept off the road corridor."),
            new TacticalMapAnchor("camera.default_start", TacticalMapAnchorType.Camera, new Vector2(0.28f, 0.52f), "Close tactical start camera."),
            new TacticalMapAnchor("tutorial.move_target.cover_01", TacticalMapAnchorType.MoveTarget, new Vector2(0.42f, 0.54f), "First move/cover destination."),
            new TacticalMapAnchor("enemy_spawn.patrol_start", TacticalMapAnchorType.Spawn, new Vector2(0.78f, 0.54f), "Initial enemy patrol."),
            new TacticalMapAnchor("route.enemy_patrol_01.a", TacticalMapAnchorType.RouteWaypoint, new Vector2(0.78f, 0.54f)),
            new TacticalMapAnchor("route.enemy_patrol_01.b", TacticalMapAnchorType.RouteWaypoint, new Vector2(0.68f, 0.53f)),
            new TacticalMapAnchor("route.enemy_patrol_01.c", TacticalMapAnchorType.RouteWaypoint, new Vector2(0.58f, 0.52f)),
            new TacticalMapAnchor("objective.destroy_patrol_group", TacticalMapAnchorType.Objective, new Vector2(0.64f, 0.53f)),
            new TacticalMapAnchor("threat.patrol_warning_01", TacticalMapAnchorType.Threat, new Vector2(0.70f, 0.53f)),
            new TacticalMapAnchor("minimap.viewport_start", TacticalMapAnchorType.Minimap, new Vector2(0.28f, 0.52f)),
        };
    }

    private static TacticalMapSurface[] CreateSurfaces()
    {
        return new[]
        {
            new TacticalMapSurface("walk.main_road", TacticalMapSurfaceType.MainRoad, new Rect(0.14f, 0.46f, 0.74f, 0.16f), "Primary movement/vehicle corridor."),
            new TacticalMapSurface("walk.road_shoulders", TacticalMapSurfaceType.RoadShoulder, new Rect(0.12f, 0.40f, 0.78f, 0.28f), "Infantry valid shoulder movement."),
            new TacticalMapSurface("walk.command_point_pad", TacticalMapSurfaceType.CommandPointPad, new Rect(0.16f, 0.47f, 0.12f, 0.12f), "Player-side command pad."),
            new TacticalMapSurface("walk.cover_pullout_01", TacticalMapSurfaceType.Cover, new Rect(0.37f, 0.49f, 0.12f, 0.11f), "First tutorial cover pullout."),
            new TacticalMapSurface("block.map_edge", TacticalMapSurfaceType.Blocked, new Rect(0f, 0f, 1f, 1f), "Outer grid rows blocked by generated edge art."),
            new TacticalMapSurface("block.civilian_structures", TacticalMapSurfaceType.Blocked, new Rect(0.48f, 0.18f, 0.18f, 0.18f), "Sample civilian blocker footprint for runtime hookup."),
            new TacticalMapSurface("zone.civilian_edge", TacticalMapSurfaceType.CivilianZone, new Rect(0.02f, 0.05f, 0.18f, 0.28f), "No-fire/no-build civilian edge zone."),
        };
    }

    private static TacticalMapRoute[] CreateRoutes()
    {
        return new[]
        {
            new TacticalMapRoute("route.enemy_patrol_01", new[]
            {
                new Vector2(0.78f, 0.54f),
                new Vector2(0.68f, 0.53f),
                new Vector2(0.58f, 0.52f),
            }, "First visible enemy patrol route.")
        };
    }

    private static TacticalMapEntityFootprint[] CreateEntityFootprints()
    {
        return new[]
        {
            new TacticalMapEntityFootprint("unit.player.rifle_squad_01", new Vector2Int(1, 1), "Infantry squad test footprint."),
            new TacticalMapEntityFootprint("unit.enemy.patrol_01", new Vector2Int(1, 1), "Enemy patrol test footprint."),
            new TacticalMapEntityFootprint("decor.command_point", new Vector2Int(4, 3), "Command/decor footprint for blocker validation."),
        };
    }

    private static string[] CreateReasonCodes()
    {
        return new[]
        {
            "NoSelection",
            "TargetOutOfBounds",
            "TargetBlocked",
            "TargetUnreachable",
            "TargetNotEnemy",
            "TargetNotAttackable",
            "CommandUnavailable",
            "MissionDoesNotAllowBuild",
            "CameraJumpUnavailable",
        };
    }

    private static Vector2Int[] CreateBlockedEdgeCells(int gridWidth, int gridHeight)
    {
        HashSet<Vector2Int> cells = new();
        for (int x = 0; x < gridWidth; x++)
        {
            cells.Add(new Vector2Int(x, 0));
            cells.Add(new Vector2Int(x, gridHeight - 1));
        }

        for (int y = 0; y < gridHeight; y++)
        {
            cells.Add(new Vector2Int(0, y));
            cells.Add(new Vector2Int(gridWidth - 1, y));
        }

        return new List<Vector2Int>(cells).ToArray();
    }

    private static GameObject CreateSpriteObject(string name, Sprite sprite, int sortingOrder)
    {
        GameObject spriteObject = new(name);
        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Simple;
        return spriteObject;
    }

    private static GameObject CreateQuad(string name, Vector2 center, Vector2 size, Color color, float z)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.position = new Vector3(center.x, center.y, z);
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateOverlayMaterial(color);
        renderer.sortingOrder = 80;
        return quad;
    }

    private static TextMesh CreateLabel(string text, Vector2 position, Color color)
    {
        GameObject labelObject = new(text + "_label");
        labelObject.transform.position = new Vector3(position.x, position.y, -0.04f);
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 18;
        label.characterSize = 0.018f;
        label.anchor = TextAnchor.MiddleLeft;
        label.alignment = TextAlignment.Left;
        label.color = color;
        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 90;
        return label;
    }

    private static Material CreateOverlayMaterial(Color color)
    {
        Material material = new(Shader.Find("Sprites/Default"));
        material.color = color;
        return material;
    }

    private static Camera CreateCamera(string name, Vector3 position, float orthographicSize, bool enabled)
    {
        GameObject cameraObject = new(name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = enabled;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.041f, 0.036f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = position;
        camera.transform.rotation = Quaternion.identity;
        return camera;
    }

    private static Vector2 NormalizedToWorld(Vector2 normalizedPosition)
    {
        return WorldOrigin + new Vector2(normalizedPosition.x * VisibleWorldSize.x, normalizedPosition.y * VisibleWorldSize.y);
    }

    private static void EnsureSpriteImport(string assetPath, bool alpha, float pixelsPerUnit, int maxTextureSize, TextureImporterCompression compression)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_M01_TACTICAL_VALIDATION_IMPORTER_MISSING path={assetPath}");
            return;
        }

        bool changed = false;
        changed |= SetTextureType(importer, TextureImporterType.Sprite);
        changed |= SetSpriteImportMode(importer, SpriteImportMode.Single);
        changed |= SetAlphaSource(importer, alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None);
        changed |= SetAlphaIsTransparency(importer, alpha);
        changed |= SetMipmapEnabled(importer, false);
        changed |= SetSrgbTexture(importer, true);
        changed |= SetFilterMode(importer, FilterMode.Bilinear);
        changed |= SetTextureCompression(importer, compression);

        if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
        {
            importer.spritePixelsPerUnit = pixelsPerUnit;
            changed = true;
        }

        changed |= EnsurePlatformSettings(importer, "DefaultTexturePlatform", false, maxTextureSize, TextureImporterFormat.Automatic);
        changed |= EnsurePlatformSettings(importer, "Android", true, maxTextureSize, TextureImporterFormat.ASTC_6x6);

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static bool EnsurePlatformSettings(TextureImporter importer, string platformName, bool overridden, int maxTextureSize, TextureImporterFormat format)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
        bool changed = settings.overridden != overridden || settings.maxTextureSize != maxTextureSize || settings.format != format;
        if (!changed)
            return false;

        settings.overridden = overridden;
        settings.maxTextureSize = maxTextureSize;
        settings.format = format;
        importer.SetPlatformTextureSettings(settings);
        return true;
    }

    private static bool SetTextureType(TextureImporter importer, TextureImporterType textureType)
    {
        if (importer.textureType == textureType)
            return false;

        importer.textureType = textureType;
        return true;
    }

    private static bool SetSpriteImportMode(TextureImporter importer, SpriteImportMode importMode)
    {
        if (importer.spriteImportMode == importMode)
            return false;

        importer.spriteImportMode = importMode;
        return true;
    }

    private static bool SetAlphaSource(TextureImporter importer, TextureImporterAlphaSource alphaSource)
    {
        if (importer.alphaSource == alphaSource)
            return false;

        importer.alphaSource = alphaSource;
        return true;
    }

    private static bool SetAlphaIsTransparency(TextureImporter importer, bool value)
    {
        if (importer.alphaIsTransparency == value)
            return false;

        importer.alphaIsTransparency = value;
        return true;
    }

    private static bool SetMipmapEnabled(TextureImporter importer, bool value)
    {
        if (importer.mipmapEnabled == value)
            return false;

        importer.mipmapEnabled = value;
        return true;
    }

    private static bool SetSrgbTexture(TextureImporter importer, bool value)
    {
        if (importer.sRGBTexture == value)
            return false;

        importer.sRGBTexture = value;
        return true;
    }

    private static bool SetFilterMode(TextureImporter importer, FilterMode value)
    {
        if (importer.filterMode == value)
            return false;

        importer.filterMode = value;
        return true;
    }

    private static bool SetTextureCompression(TextureImporter importer, TextureImporterCompression value)
    {
        if (importer.textureCompression == value)
            return false;

        importer.textureCompression = value;
        return true;
    }

    private static string ProjectPath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
    }
}
#endif
