#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WarlineCaptureGc30Demo2RoadGreenTerrainBuilder
{
    private const string DemoScenePath = "Assets/Game/Scenes/Demo.unity";
    private const string Demo2ScenePath = "Assets/Game/Scenes/Demo2.unity";
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC30_Demo2RoadGreenTerrain_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048";
    private const string ReportPath = "Design/AgentReports/2026-05-21_gameplay_gc30-demo2-road-green-terrain.md";
    private const float MapSize = 2048f;

    private static readonly List<Zone> Roads = new();
    private static readonly List<Zone> Reserved = new();
    private static readonly List<Zone> PlacedObjects = new();
    private static readonly List<string> BuildLog = new();
    private static readonly List<string> ValidationLog = new();
    private static readonly List<string> MissingAssets = new();
    private static int clonedRootCount;
    private static int acceptedRootCount;
    private static int skippedRoadCount;
    private static int demo2GroundPrefabCount;
    private static int demo2RoadPrefabCount;
    private static int demo2DecorationCount;

    private readonly struct Zone
    {
        public readonly string Name;
        public readonly Rect Rect;

        public Zone(string name, Rect rect)
        {
            Name = name;
            Rect = rect;
        }
    }

    private readonly struct ModuleSpec
    {
        public readonly string Name;
        public readonly Bounds SourceBounds;
        public readonly Vector3 TargetCenter;
        public readonly float RotationY;
        public readonly float Scale;
        public readonly float LegalPadding;

        public ModuleSpec(string name, Bounds sourceBounds, Vector3 targetCenter, float rotationY, float scale, float legalPadding)
        {
            Name = name;
            SourceBounds = sourceBounds;
            TargetCenter = targetCenter;
            RotationY = rotationY;
            Scale = scale;
            LegalPadding = legalPadding;
        }
    }

    [MenuItem("WarlineCapture/Design/Build GC30 Demo2 Road Green Terrain 2048")]
    public static void BuildGc30Demo2RoadGreenTerrain2048()
    {
        Roads.Clear();
        Reserved.Clear();
        PlacedObjects.Clear();
        BuildLog.Clear();
        ValidationLog.Clear();
        MissingAssets.Clear();
        clonedRootCount = 0;
        acceptedRootCount = 0;
        skippedRoadCount = 0;
        demo2GroundPrefabCount = 0;
        demo2RoadPrefabCount = 0;
        demo2DecorationCount = 0;

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));

        Scene demoScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        DefineGameplayContract();
        List<ModuleSpec> modules = BuildModuleSpecs();
        Dictionary<string, List<GameObject>> sourceByModule = new(StringComparer.Ordinal);
        foreach (ModuleSpec module in modules)
            sourceByModule[module.Name] = CollectSourceRoots(module.SourceBounds);

        Scene generatedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(generatedScene);

        GameObject root = new("GC30_Demo2RoadGreenTerrain_2048_Root");
        BuildRenderEnvironment(root);
        BuildBasePlane(root);
        BuildVisualGroundSurfaces(root);
        PlaceDemo2GroundPrefabs(root);
        PlaceDemo2RoadNetwork(root);
        PlaceDemo2OpenTerrainDressing(root);
        CloneLegalDemoModules(root, modules, sourceByModule);
        PlaceProofUnits(root);
        BuildHiddenContractMarkers(root);
        BuildCameras(root);
        ValidateLayout(modules, sourceByModule);

        EditorSceneManager.CloseScene(demoScene, true);
        EditorSceneManager.SaveScene(generatedScene, ScenePath);
        CaptureScene();
        WriteReport(modules, sourceByModule);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC30_DEMO2_ROAD_GREEN_TERRAIN_BUILT acceptedRoots={acceptedRootCount} demo2GroundPieces={demo2GroundPrefabCount} demo2RoadPieces={demo2RoadPrefabCount} skippedRoad={skippedRoadCount} scene={ScenePath} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static List<ModuleSpec> BuildModuleSpecs()
    {
        Bounds airfield = new(new Vector3(82f, 18f, 205f), new Vector3(115f, 120f, 180f));
        Bounds tentCamp = new(new Vector3(20f, 12f, 76f), new Vector3(135f, 105f, 135f));
        Bounds commandDepot = new(new Vector3(35f, 12f, 118f), new Vector3(150f, 105f, 135f));
        Bounds vehicleYard = new(new Vector3(42f, 10f, 80f), new Vector3(145f, 105f, 110f));
        Bounds fuelUtility = new(new Vector3(82f, 14f, 420f), new Vector3(230f, 120f, 250f));
        Bounds southGate = new(new Vector3(-15f, 10f, -72f), new Vector3(180f, 95f, 145f));
        Bounds village = new(new Vector3(-38f, 16f, -58f), new Vector3(220f, 90f, 220f));

        return new List<ModuleSpec>
        {
            new("CityCore_NorthWestBlock", village, BlueprintPoint(150f, 150f), 2f, 1.00f, 10f),
            new("CityMarket_WestBlock", village, BlueprintPoint(168f, 378f), -7f, 1.00f, 10f),
            new("SouthTown_WestBlock", village, BlueprintPoint(170f, 654f), 8f, 1.00f, 10f),
            new("SouthGate_PlayerCamp", southGate, BlueprintPoint(390f, 694f), 12f, 1.00f, 10f),
            new("CentralTentBarracks_InnerBlock", tentCamp, BlueprintPoint(404f, 498f), -4f, 1.00f, 10f),
            new("CommandDepot_CentralEast", commandDepot, BlueprintPoint(628f, 420f), -10f, 1.00f, 10f),
            new("Airfield_NorthEastApron", airfield, BlueprintPoint(724f, 170f), -8f, 1.00f, 10f),
            new("VehicleYard_SouthEast", vehicleYard, BlueprintPoint(684f, 678f), 18f, 1.00f, 10f),
            new("FuelUtility_EastService", fuelUtility, BlueprintPoint(790f, 588f), -52f, 1.00f, 10f),
        };
    }

    private static void DefineGameplayContract()
    {
        AddRoad("MainNorthSouth_West", 286f, 0f, 64f, 890f);
        AddRoad("MainNorthSouth_East", 524f, 0f, 64f, 890f);
        AddRoad("NorthCityToAirfield", 64f, 232f, 760f, 58f);
        AddRoad("CentralCityToCommand", 118f, 430f, 682f, 58f);
        AddRoad("SouthPlayerToFuel", 58f, 744f, 770f, 70f);

        Reserved.Add(new Zone("PlayerSpawn", BlueprintRect(72f, 766f, 36f, 36f)));
        Reserved.Add(new Zone("EnemySpawn_Airfield", BlueprintRect(775f, 227f, 34f, 34f)));
        Reserved.Add(new Zone("EnemySpawn_Command", BlueprintRect(606f, 430f, 28f, 28f)));
        Reserved.Add(new Zone("EnemySpawn_VehicleFuel", BlueprintRect(692f, 744f, 28f, 28f)));
        Reserved.Add(new Zone("Objective_MarketMid", BlueprintRect(360f, 280f, 134f, 52f)));
        Reserved.Add(new Zone("Objective_SouthTown", BlueprintRect(372f, 736f, 134f, 56f)));
        Reserved.Add(new Zone("Objective_CommandOutpost", BlueprintRect(608f, 454f, 86f, 62f)));
    }

    private static void AddRoad(string name, float x, float y, float width, float height)
    {
        Roads.Add(new Zone(name, BlueprintRect(x, y, width, height)));
    }

    private static void BuildRenderEnvironment(GameObject root)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.78f, 0.70f, 0.57f, 1f);
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.56f, 0.47f, 0.35f, 1f);
        RenderSettings.fogDensity = 0.00032f;

        Light key = Child(root, "DirectionalLight_Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.75f;
        key.color = new Color(1f, 0.88f, 0.67f, 1f);
        key.shadows = LightShadows.None;
        key.shadowStrength = 0f;
        key.transform.rotation = Quaternion.Euler(50f, -42f, 0f);

        Light fill = Child(root, "DirectionalLight_Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.42f;
        fill.color = new Color(0.62f, 0.75f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(36f, 138f, 0f);

        Volume volume = Child(root, "GC30_RTS_PresentationVolume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "GC30_RTS_PresentationProfile";
        volume.sharedProfile = profile;
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.05f);
        color.contrast.Override(10f);
        color.saturation.Override(2f);
        color.colorFilter.Override(new Color(1f, 0.96f, 0.88f, 1f));
        profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
    }

    private static void BuildBasePlane(GameObject root)
    {
        Surface(root, "GreenDirtGameplayPlane_2048x2048", Vector3.zero, new Vector2(MapSize, MapSize), CreateMaterial("GC30_Demo2GreenBase", new Color(0.36f, 0.42f, 0.22f, 1f)), -0.04f);
    }

    private static void BuildVisualGroundSurfaces(GameObject root)
    {
        GameObject ground = Child(root, "GroundUnderlayMasks_ForDemo2PrefabTiles");
        Material dirt = CreateMaterial("GC30_Demo2DirtTerrain", new Color(0.54f, 0.43f, 0.25f, 1f));
        Material shoulder = CreateMaterial("GC30_Demo2PackedShoulder", new Color(0.50f, 0.39f, 0.22f, 1f));
        Material road = CreateMaterial("GC30_Demo2DirtRoad", new Color(0.36f, 0.26f, 0.15f, 1f));
        Material tireTrack = CreateMaterial("GC30_Demo2TireTrack", new Color(0.23f, 0.17f, 0.10f, 1f));
        Material runway = CreateMaterial("GC30_Demo2RunwayAsphalt", new Color(0.36f, 0.34f, 0.29f, 1f));
        Material plaza = CreateMaterial("GC30_Demo2ConcretePackedYard", new Color(0.58f, 0.53f, 0.42f, 1f));
        Material grass = CreateMaterial("GC30_Demo2GrassDirtPatch", new Color(0.42f, 0.48f, 0.24f, 1f));

        SurfaceBlueprintPolygon(ground, "DirtField_NorthWest", dirt, 0.014f, (24f, 36f), (288f, 30f), (296f, 214f), (30f, 232f));
        SurfaceBlueprintPolygon(ground, "DirtField_Market", dirt, 0.014f, (34f, 300f), (282f, 300f), (300f, 544f), (36f, 548f));
        SurfaceBlueprintPolygon(ground, "DirtField_SouthTown", dirt, 0.014f, (42f, 566f), (294f, 566f), (312f, 724f), (48f, 724f));
        SurfaceBlueprintPolygon(ground, "DirtField_CentralBarracks", dirt, 0.014f, (344f, 458f), (506f, 456f), (524f, 620f), (328f, 624f));
        SurfaceBlueprintPolygon(ground, "DirtField_PlayerBase", dirt, 0.014f, (320f, 628f), (492f, 626f), (508f, 730f), (312f, 730f));
        SurfaceBlueprintPolygon(ground, "DirtField_Command", dirt, 0.014f, (560f, 346f), (734f, 344f), (760f, 536f), (548f, 540f));
        SurfaceBlueprintPolygon(ground, "DirtField_Airfield", dirt, 0.014f, (646f, 54f), (852f, 54f), (874f, 314f), (636f, 318f));
        SurfaceBlueprintPolygon(ground, "DirtField_FuelDepot", dirt, 0.014f, (604f, 574f), (850f, 572f), (860f, 730f), (618f, 732f));

        SurfaceBlueprintPolygon(ground, "MainNorthSouth_West_Shoulder", shoulder, 0.044f, (292f, -4f), (346f, -4f), (346f, 894f), (292f, 894f));
        SurfaceBlueprintPolygon(ground, "MainNorthSouth_West", road, 0.056f, (306f, 0f), (330f, 0f), (330f, 890f), (306f, 890f));
        SurfaceBlueprintPolygon(ground, "MainNorthSouth_East_Shoulder", shoulder, 0.044f, (530f, -4f), (584f, -4f), (584f, 894f), (530f, 894f));
        SurfaceBlueprintPolygon(ground, "MainNorthSouth_East", road, 0.056f, (544f, 0f), (568f, 0f), (568f, 890f), (544f, 890f));

        SurfaceBlueprintPolygon(ground, "NorthCityToAirfield_Shoulder", shoulder, 0.044f, (46f, 236f), (842f, 236f), (842f, 286f), (46f, 286f));
        SurfaceBlueprintPolygon(ground, "NorthCityToAirfield", road, 0.056f, (64f, 252f), (824f, 252f), (824f, 270f), (64f, 270f));
        SurfaceBlueprintPolygon(ground, "CentralCityToCommand_Shoulder", shoulder, 0.044f, (96f, 434f), (818f, 434f), (818f, 484f), (96f, 484f));
        SurfaceBlueprintPolygon(ground, "CentralCityToCommand", road, 0.056f, (118f, 450f), (800f, 450f), (800f, 468f), (118f, 468f));
        SurfaceBlueprintPolygon(ground, "SouthPlayerToFuel_Shoulder", shoulder, 0.044f, (38f, 746f), (846f, 746f), (846f, 814f), (38f, 814f));
        SurfaceBlueprintPolygon(ground, "SouthPlayerToFuel", road, 0.056f, (58f, 768f), (828f, 768f), (828f, 790f), (58f, 790f));

        SurfaceBlueprintPolygon(ground, "Airfield_Runway_Primary", runway, 0.066f, (714f, 78f), (826f, 78f), (826f, 300f), (714f, 300f));
        SurfaceBlueprintPolygon(ground, "Airfield_Runway_Cross", runway, 0.067f, (658f, 166f), (862f, 166f), (862f, 220f), (658f, 220f));
        SurfaceBlueprintPolygon(ground, "CityGreenBuffer_NorthWest", grass, 0.041f, (58f, 62f), (250f, 62f), (250f, 206f), (58f, 206f));
        SurfaceBlueprintPolygon(ground, "MarketGreenBuffer_West", grass, 0.041f, (58f, 326f), (252f, 326f), (252f, 510f), (58f, 510f));
        SurfaceBlueprintPolygon(ground, "SouthTownGreenBuffer", grass, 0.041f, (62f, 592f), (270f, 592f), (270f, 718f), (62f, 718f));
        SurfaceBlueprintPolygon(ground, "Command_Outpost_Yard", plaza, 0.063f, (560f, 350f), (720f, 350f), (742f, 526f), (552f, 536f));
        SurfaceBlueprintPolygon(ground, "VehicleFuel_Yard", plaza, 0.063f, (606f, 592f), (828f, 592f), (842f, 718f), (628f, 720f));

        SurfaceBlueprintPolygon(ground, "Track_NorthRoad_A", tireTrack, 0.082f, (70f, 258f), (820f, 258f), (820f, 264f), (70f, 264f));
        SurfaceBlueprintPolygon(ground, "Track_CommandRoad_A", tireTrack, 0.082f, (130f, 454f), (790f, 454f), (790f, 462f), (130f, 462f));
        SurfaceBlueprintPolygon(ground, "Track_SouthRoad_A", tireTrack, 0.082f, (66f, 774f), (818f, 774f), (818f, 782f), (66f, 782f));
    }

    private static void PlaceDemo2GroundPrefabs(GameObject root)
    {
        GameObject parent = Child(root, "Demo2GrassDirtGroundPrefabs_SourceScale");
        const string grassSquare = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Square_01.prefab";
        const string grassCircleA = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Circle_01.prefab";
        const string grassCircleB = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Grass_Circle_02.prefab";

        PlaceGroundTileGrid(parent, "NorthWestGreenCell", grassSquare, 58f, 62f, 250f, 206f);
        PlaceGroundTileGrid(parent, "MarketGreenCell", grassSquare, 58f, 326f, 252f, 510f);
        PlaceGroundTileGrid(parent, "SouthTownGreenCell", grassSquare, 62f, 592f, 270f, 718f);
        PlaceGroundTileGrid(parent, "CenterNorthGreenCell", grassSquare, 348f, 86f, 512f, 236f);
        PlaceGroundTileGrid(parent, "CenterCommandGreenCell", grassSquare, 348f, 500f, 512f, 690f);
        PlaceGroundTileGrid(parent, "EastOpenGreenCell", grassSquare, 596f, 270f, 842f, 420f);
        PlaceGroundTileGrid(parent, "EastSouthGreenCell", grassSquare, 604f, 736f, 844f, 846f);

        PlaceGroundPatch(parent, "CityOuterGrassCircleA", grassCircleA, 46f, 228f, 0f);
        PlaceGroundPatch(parent, "CityOuterGrassCircleB", grassCircleB, 278f, 218f, 18f);
        PlaceGroundPatch(parent, "MarketOuterGrassCircleA", grassCircleB, 42f, 540f, -12f);
        PlaceGroundPatch(parent, "SouthOuterGrassCircleA", grassCircleA, 300f, 724f, 9f);
        PlaceGroundPatch(parent, "CommandGrassCircleA", grassCircleB, 542f, 342f, -20f);
        PlaceGroundPatch(parent, "AirfieldGrassCircleA", grassCircleA, 630f, 314f, 15f);
        PlaceGroundPatch(parent, "FuelGrassCircleA", grassCircleB, 860f, 732f, -22f);
        PlaceGroundPatch(parent, "OpenFieldGrassCircleA", grassCircleA, 448f, 184f, 34f);
        PlaceGroundPatch(parent, "OpenFieldGrassCircleB", grassCircleB, 488f, 714f, -32f);
    }

    private static void PlaceGroundTileGrid(GameObject parent, string name, string path, float left, float top, float right, float bottom)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            MissingAssets.Add(path);
            return;
        }

        float worldLeft = BlueprintPoint(left, top).x;
        float worldRight = BlueprintPoint(right, top).x;
        float worldTop = BlueprintPoint(left, top).z;
        float worldBottom = BlueprintPoint(left, bottom).z;
        float minX = Mathf.Min(worldLeft, worldRight);
        float maxX = Mathf.Max(worldLeft, worldRight);
        float minZ = Mathf.Min(worldTop, worldBottom);
        float maxZ = Mathf.Max(worldTop, worldBottom);
        float size = Mathf.Max(18f, EstimatePrefabGroundLength(prefab) * 0.92f);
        int columns = Mathf.Clamp(Mathf.CeilToInt((maxX - minX) / (size * 2.6f)), 1, 5);
        int rows = Mathf.Clamp(Mathf.CeilToInt((maxZ - minZ) / (size * 2.6f)), 1, 5);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                float x = Mathf.Lerp(minX, maxX, (column + 0.5f) / columns);
                float z = Mathf.Lerp(minZ, maxZ, (row + 0.5f) / rows);
                Rect footprint = new(x - size * 0.45f, z - size * 0.45f, size * 0.9f, size * 0.9f);
                if (OverlapsAny(Roads, footprint))
                    continue;
                if ((row + column) % 3 == 1)
                    continue;
                PlaceGroundPrefab(parent, name + "_" + row.ToString("00") + "_" + column.ToString("00"), prefab, new Vector3(x, 0f, z), ((row + column) % 4) * 90f, footprint);
            }
        }
    }

    private static void PlaceGroundPatch(GameObject parent, string name, string path, float x, float y, float rotationY)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            MissingAssets.Add(path);
            return;
        }

        Vector3 position = BlueprintPoint(x, y);
        float size = Mathf.Max(18f, EstimatePrefabGroundLength(prefab));
        Rect footprint = new(position.x - size * 0.45f, position.z - size * 0.45f, size * 0.9f, size * 0.9f);
        if (OverlapsAny(Roads, footprint))
            return;
        PlaceGroundPrefab(parent, name, prefab, position, rotationY, footprint);
    }

    private static void PlaceGroundPrefab(GameObject parent, string name, GameObject prefab, Vector3 position, float rotationY, Rect footprint)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "GC30_Demo2Ground_" + name;
        instance.transform.SetParent(parent.transform, true);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        instance.transform.localScale = Vector3.one;
        AlignBottomNearGround(instance);
        instance.transform.position += Vector3.up * 0.012f;
        ForceStaticFlatShadows(instance);
        PlacedObjects.Add(new Zone(instance.name, footprint));
        demo2GroundPrefabCount++;
    }

    private static void PlaceDemo2RoadNetwork(GameObject root)
    {
        GameObject parent = Child(root, "MeasuredDemo2RoadPrefabNetwork_SourceScale");
        string[] straights =
        {
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_01.prefab",
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_02.prefab",
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_03.prefab",
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_01.prefab",
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_02.prefab",
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_Damaged_03.prefab",
        };
        const string end = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Straight_End_01.prefab";
        const string cross = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Cross_01.prefab";
        const string t = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_T_01.prefab";
        const string cornerA = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Corner_01.prefab";
        const string cornerB = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Road_Corner_02.prefab";

        PlaceMeasuredRoadStrip(parent, "NorthCityToAirfield", straights, new Vector2(72f, 262f), new Vector2(816f, 262f), 0f);
        PlaceMeasuredRoadStrip(parent, "CentralCityToCommand", straights, new Vector2(126f, 459f), new Vector2(792f, 459f), 0f);
        PlaceMeasuredRoadStrip(parent, "SouthPlayerToFuel", straights, new Vector2(70f, 779f), new Vector2(820f, 779f), 0f);
        PlaceMeasuredRoadStrip(parent, "WestNorthSouth", straights, new Vector2(318f, 28f), new Vector2(318f, 864f), 90f);
        PlaceMeasuredRoadStrip(parent, "EastNorthSouth", straights, new Vector2(556f, 28f), new Vector2(556f, 864f), 90f);

        PlaceRoadLoop(parent, "NorthWestCityLoop", straights, cornerA, cornerB, 70f, 86f, 260f, 216f);
        PlaceRoadLoop(parent, "WestMarketLoop", straights, cornerA, cornerB, 72f, 326f, 264f, 526f);
        PlaceRoadLoop(parent, "SouthTownLoop", straights, cornerA, cornerB, 76f, 592f, 274f, 726f);
        PlaceRoadLoop(parent, "CommandServiceLoop", straights, cornerA, cornerB, 586f, 362f, 744f, 526f);
        PlaceRoadLoop(parent, "VehicleFuelServiceLoop", straights, cornerA, cornerB, 632f, 610f, 830f, 720f);
        PlaceMeasuredRoadStrip(parent, "NorthWestCityLoop_ToWestMain", straights, new Vector2(260f, 216f), new Vector2(318f, 216f), 0f);
        PlaceMeasuredRoadStrip(parent, "WestMarketLoop_ToWestMain", straights, new Vector2(264f, 526f), new Vector2(318f, 526f), 0f);
        PlaceMeasuredRoadStrip(parent, "SouthTownLoop_ToWestMain", straights, new Vector2(274f, 726f), new Vector2(318f, 726f), 0f);
        PlaceMeasuredRoadStrip(parent, "CommandServiceLoop_ToEastMain", straights, new Vector2(556f, 526f), new Vector2(586f, 526f), 0f);
        PlaceMeasuredRoadStrip(parent, "VehicleFuelServiceLoop_ToEastMain", straights, new Vector2(556f, 610f), new Vector2(632f, 610f), 0f);

        PlaceDemo2RoadNode(parent, "NorthWestRoadEnd", end, 72f, 262f, 270f);
        PlaceDemo2RoadNode(parent, "NorthEastRoadEnd", end, 816f, 262f, 90f);
        PlaceDemo2RoadNode(parent, "SouthWestRoadEnd", end, 70f, 779f, 270f);
        PlaceDemo2RoadNode(parent, "SouthEastRoadEnd", end, 820f, 779f, 90f);
        PlaceDemo2RoadNode(parent, "Intersection_WestNorth", cross, 318f, 262f, 0f);
        PlaceDemo2RoadNode(parent, "Intersection_EastNorth", cross, 556f, 262f, 0f);
        PlaceDemo2RoadNode(parent, "Intersection_WestCommand", cross, 318f, 459f, 0f);
        PlaceDemo2RoadNode(parent, "Intersection_EastCommand", cross, 556f, 459f, 0f);
        PlaceDemo2RoadNode(parent, "Intersection_WestSouth", cross, 318f, 779f, 0f);
        PlaceDemo2RoadNode(parent, "Intersection_EastSouth", cross, 556f, 779f, 0f);
        PlaceDemo2RoadNode(parent, "Tee_NorthCityWestAccess", t, 318f, 216f, 180f);
        PlaceDemo2RoadNode(parent, "Tee_MarketWestAccess", t, 318f, 526f, 180f);
        PlaceDemo2RoadNode(parent, "Tee_SouthTownWestAccess", t, 318f, 726f, 180f);
        PlaceDemo2RoadNode(parent, "Tee_CommandEastAccess", t, 556f, 526f, 0f);
        PlaceDemo2RoadNode(parent, "Tee_FuelEastAccess", t, 556f, 610f, 0f);
        PlaceDemo2RoadNode(parent, "Tee_NorthCityLoopAccess", t, 260f, 216f, 0f);
        PlaceDemo2RoadNode(parent, "Tee_MarketLoopAccess", t, 264f, 526f, 0f);
        PlaceDemo2RoadNode(parent, "Tee_SouthTownLoopAccess", t, 274f, 726f, 0f);
        PlaceDemo2RoadNode(parent, "Tee_CommandLoopAccess", t, 586f, 526f, 180f);
        PlaceDemo2RoadNode(parent, "Tee_FuelLoopAccess", t, 632f, 610f, 180f);
    }

    private static void PlaceRoadLoop(GameObject parent, string name, string[] straightPaths, string cornerA, string cornerB, float left, float top, float right, float bottom)
    {
        PlaceMeasuredRoadStrip(parent, name + "_North", straightPaths, new Vector2(left, top), new Vector2(right, top), 0f);
        PlaceMeasuredRoadStrip(parent, name + "_East", straightPaths, new Vector2(right, top), new Vector2(right, bottom), 90f);
        PlaceMeasuredRoadStrip(parent, name + "_South", straightPaths, new Vector2(left, bottom), new Vector2(right, bottom), 0f);
        PlaceMeasuredRoadStrip(parent, name + "_West", straightPaths, new Vector2(left, top), new Vector2(left, bottom), 90f);
        PlaceDemo2RoadNode(parent, name + "_CornerNW", cornerA, left, top, 0f);
        PlaceDemo2RoadNode(parent, name + "_CornerNE", cornerB, right, top, 90f);
        PlaceDemo2RoadNode(parent, name + "_CornerSE", cornerA, right, bottom, 180f);
        PlaceDemo2RoadNode(parent, name + "_CornerSW", cornerB, left, bottom, 270f);
    }

    private static void PlaceMeasuredRoadStrip(GameObject parent, string name, string[] paths, Vector2 startBlueprint, Vector2 endBlueprint, float rotationOffset)
    {
        List<GameObject> prefabs = new();
        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                MissingAssets.Add(path);
                continue;
            }
            prefabs.Add(prefab);
        }

        if (prefabs.Count == 0)
            return;

        Vector3 start = BlueprintPoint(startBlueprint.x, startBlueprint.y);
        Vector3 end = BlueprintPoint(endBlueprint.x, endBlueprint.y);
        float length = Vector3.Distance(start, end);
        float prefabLength = Mathf.Max(12f, prefabs.Max(EstimatePrefabGroundLength));
        int count = Mathf.Max(2, Mathf.CeilToInt(length / (prefabLength * 0.92f)));
        float rotationY = RoadRotationY(start, end) + rotationOffset;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : i / (float)(count - 1);
            GameObject prefab = prefabs[i % prefabs.Count];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "GC30_Demo2Road_" + name + "_" + i.ToString("00");
            instance.transform.SetParent(parent.transform, true);
            instance.transform.position = Vector3.Lerp(start, end, t);
            instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            instance.transform.localScale = Vector3.one;
            AlignBottomNearGround(instance);
            ConfigureFlatRoadRenderer(instance);
            demo2RoadPrefabCount++;
        }
    }

    private static void PlaceDemo2RoadNode(GameObject parent, string name, string path, float x, float y, float rotationY)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            MissingAssets.Add(path);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "GC30_Demo2Road_" + name;
        instance.transform.SetParent(parent.transform, true);
        instance.transform.position = BlueprintPoint(x, y);
        instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        instance.transform.localScale = Vector3.one;
        AlignBottomNearGround(instance);
        ConfigureFlatRoadRenderer(instance);
        demo2RoadPrefabCount++;
    }

    private static float EstimatePrefabGroundLength(GameObject prefab)
    {
        GameObject probe = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        probe.transform.position = new Vector3(9000f, 0f, 9000f);
        probe.transform.rotation = Quaternion.identity;
        probe.transform.localScale = Vector3.one;
        Renderer[] renderers = probe.GetComponentsInChildren<Renderer>(false);
        float length = renderers.Length == 0 ? 14f : Mathf.Max(CalculateBounds(renderers).size.x, CalculateBounds(renderers).size.z);
        Object.DestroyImmediate(probe);
        return length;
    }

    private static void ConfigureFlatRoadRenderer(GameObject instance)
    {
        Material road = CreateMaterial("GC30_Demo2RoadAsphaltFallback", new Color(0.19f, 0.15f, 0.11f, 1f));
        Material edge = CreateMaterial("GC30_Demo2RoadEdgeFallback", new Color(0.55f, 0.48f, 0.35f, 1f));
        Material dirt = CreateMaterial("GC30_Demo2RoadDirtFallback", new Color(0.36f, 0.25f, 0.14f, 1f));
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(false))
        {
            string rendererName = renderer.name.ToLowerInvariant();
            Material material = rendererName.Contains("edge") ? edge : rendererName.Contains("dirt") ? dirt : road;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static void PlaceDemo2OpenTerrainDressing(GameObject root)
    {
        GameObject parent = Child(root, "OpenTerrainDressing_OffRoadOnly");
        PlaceOpenTerrainProp(parent, "RockNorthCommand_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Rock_01.prefab", 460f, 294f, 12f);
        PlaceOpenTerrainProp(parent, "RockNorthCommand_B", "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Rock_02.prefab", 488f, 338f, -18f);
        PlaceOpenTerrainProp(parent, "RockEastAirfield_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Rock_01.prefab", 858f, 352f, 42f);
        PlaceOpenTerrainProp(parent, "RockSouthFuel_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Rock_02.prefab", 840f, 824f, 8f);
        PlaceOpenTerrainProp(parent, "TreeMarketGap_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Tree_02.prefab", 288f, 348f, 0f);
        PlaceOpenTerrainProp(parent, "TreeSouthGap_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Tree_03.prefab", 284f, 610f, 0f);
        PlaceOpenTerrainProp(parent, "BushCommandGap_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Env_Bush_03.prefab", 498f, 534f, 0f);
        PlaceOpenTerrainProp(parent, "RockNorthOpen_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Rock_01.prefab", 430f, 120f, 16f);
        PlaceOpenTerrainProp(parent, "RockEastOpen_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Rock_02.prefab", 842f, 438f, -28f);
        PlaceOpenTerrainProp(parent, "TreeSouthOpen_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Tree_02.prefab", 460f, 680f, 0f);
        PlaceOpenTerrainProp(parent, "BushNorthOpen_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Env_Bush_03.prefab", 452f, 356f, 0f);
        PlaceOpenTerrainProp(parent, "BushFarEastOpen_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Env_Bush_03.prefab", 846f, 112f, 0f);
        PlaceOpenTerrainProp(parent, "MountainNorthGap_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Mountains_Grass_01.prefab", 436f, 184f, 22f, 78f);
        PlaceOpenTerrainProp(parent, "MountainEastGap_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Mountains_Grass_02.prefab", 846f, 486f, -36f, 88f);
        PlaceOpenTerrainProp(parent, "MountainSouthGap_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Mountains_Grass_01.prefab", 456f, 830f, 8f, 82f);
        PlaceOpenTerrainProp(parent, "GrassPatchNorth_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Grass_Patch_01.prefab", 430f, 278f, 0f, 36f);
        PlaceOpenTerrainProp(parent, "GrassPatchCommand_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Grass_Patch_02.prefab", 500f, 394f, 0f, 36f);
        PlaceOpenTerrainProp(parent, "GrassPatchSouth_A", "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Grass_Patch_01.prefab", 480f, 722f, 0f, 36f);
    }

    private static void PlaceOpenTerrainProp(GameObject parent, string name, string path, float x, float y, float rotationY)
    {
        PlaceOpenTerrainProp(parent, name, path, x, y, rotationY, 28f);
    }

    private static void PlaceOpenTerrainProp(GameObject parent, string name, string path, float x, float y, float rotationY, float footprintSize)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            MissingAssets.Add(path);
            return;
        }

        Vector3 position = BlueprintPoint(x, y);
        float halfFootprint = footprintSize * 0.5f;
        Rect footprint = new(position.x - halfFootprint, position.z - halfFootprint, footprintSize, footprintSize);
        if (OverlapsAny(Roads, footprint) || OverlapsAny(Reserved, footprint))
        {
            BuildLog.Add($"{name}: skipped terrain dressing because it overlaps road/reserved masks.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "GC30_" + name;
        instance.transform.SetParent(parent.transform, true);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        instance.transform.localScale = Vector3.one;
        AlignBottomNearGround(instance);
        ForceStaticFlatShadows(instance);
        PlacedObjects.Add(new Zone(instance.name, footprint));
        demo2DecorationCount++;
    }

    private static void CloneLegalDemoModules(GameObject root, List<ModuleSpec> modules, Dictionary<string, List<GameObject>> sourceByModule)
    {
        GameObject clusterRoot = Child(root, "AuthoredDemoSceneClusters_LegalizedAgainstRoads");
        foreach (ModuleSpec module in modules)
        {
            GameObject moduleRoot = Child(clusterRoot, module.Name);
            Quaternion rotation = Quaternion.Euler(0f, module.RotationY, 0f);
            int moduleAccepted = 0;
            int moduleSkipped = 0;

            foreach (GameObject source in sourceByModule[module.Name])
            {
                clonedRootCount++;
                GameObject clone = Object.Instantiate(source);
                clone.name = "GC30_" + module.Name + "_" + source.name;
                clone.transform.SetParent(moduleRoot.transform, true);
                Vector3 relative = source.transform.position - module.SourceBounds.center;
                relative = rotation * (relative * module.Scale);
                clone.transform.position = module.TargetCenter + new Vector3(relative.x, source.transform.position.y * 0.18f, relative.z);
                clone.transform.rotation = rotation * source.transform.rotation;
                clone.transform.localScale = source.transform.lossyScale * module.Scale;
                AlignBottomNearGround(clone);
                ForceStaticFlatShadows(clone);

                Rect footprint = RendererFootprint(clone, module.LegalPadding);
                if (OverlapsAny(Roads, footprint) || OverlapsAny(Reserved, footprint))
                {
                    skippedRoadCount++;
                    moduleSkipped++;
                    Object.DestroyImmediate(clone);
                    continue;
                }

                PlacedObjects.Add(new Zone(clone.name, footprint));
                acceptedRootCount++;
                moduleAccepted++;
            }

            BuildLog.Add($"{module.Name}: accepted {moduleAccepted}/{sourceByModule[module.Name].Count} roots at blueprint/world {Format(module.TargetCenter)}, skipped {moduleSkipped} by road/reserved/overlap legality.");
        }
    }

    private static List<GameObject> CollectSourceRoots(Bounds sourceBounds)
    {
        Dictionary<int, GameObject> roots = new();
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (transform == null || transform.gameObject.scene.path != DemoScenePath)
                continue;
            if (transform.GetComponent<Camera>() != null || transform.GetComponent<Light>() != null)
                continue;

            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
                continue;

            Bounds bounds = CalculateBounds(renderers);
            if (!sourceBounds.Intersects(bounds) || IsHugeBackground(transform.name, bounds) || IsTerrainOrHillSource(transform.name, bounds))
                continue;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject) ?? TopSceneObject(transform);
            if (root == null)
                continue;

            Renderer[] rootRenderers = root.GetComponentsInChildren<Renderer>(false);
            if (rootRenderers.Length == 0)
                continue;

            Bounds rootBounds = CalculateBounds(rootRenderers);
            if (!sourceBounds.Intersects(rootBounds) || IsHugeBackground(root.name, rootBounds) || IsTerrainOrHillSource(root.name, rootBounds))
                continue;

            roots[root.GetInstanceID()] = root;
        }

        return roots.Values
            .Where(go => !HasSelectedAncestor(go.transform, roots))
            .OrderBy(go => go.name, StringComparer.Ordinal)
            .ToList();
    }

    private static void PlaceProofUnits(GameObject root)
    {
        GameObject units = Child(root, "ProofUnits_OnWalkableRoadContract");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab", BlueprintPoint(84f, 778f), 70f, 1.45f, "PlayerSquad_01");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab", BlueprintPoint(104f, 772f), 70f, 1.45f, "PlayerSquad_02");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Female_01.prefab", BlueprintPoint(104f, 790f), 70f, 1.45f, "PlayerSquad_03");
        PlaceUnit(units, "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Heavy.prefab", BlueprintPoint(318f, 780f), 76f, 1.0f, "PlayerAPC_RoadProof");
        PlaceUnit(units, "Assets/Game/Prefabs/Characters/Unit_Chr_Insurgent_Male_04.prefab", BlueprintPoint(792f, 246f), 246f, 1.45f, "EnemyAirfield_01");
        PlaceUnit(units, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab", BlueprintPoint(704f, 778f), 246f, 1.0f, "EnemyTank_RoadProof");
    }

    private static void PlaceUnit(GameObject parent, string path, Vector3 position, float rotationY, float scale, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            MissingAssets.Add(path);
            return;
        }

        GameObject unit = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        unit.name = name;
        unit.transform.SetParent(parent.transform, true);
        unit.transform.position = position;
        unit.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        unit.transform.localScale = Vector3.one * scale;
        AlignBottomNearGround(unit);
        ForceStaticFlatShadows(unit);

        if (!Roads.Any(road => road.Rect.Contains(new Vector2(position.x, position.z))))
            ValidationLog.Add($"ERROR: proof unit {name} is not on a walkable road at {Format(position)}.");
    }

    private static void BuildHiddenContractMarkers(GameObject root)
    {
        GameObject debug = Child(root, "HiddenGameplayContractMarkers_DebugOnly");
        debug.SetActive(false);
        Material road = CreateMaterial("GC30_DebugRoad", new Color(0.05f, 0.7f, 0.9f, 0.45f));
        Material reserved = CreateMaterial("GC30_DebugReserved", new Color(1f, 0.28f, 0.05f, 0.45f));
        foreach (Zone zone in Roads)
            Surface(debug, zone.Name, new Vector3(zone.Rect.center.x, 0.14f, zone.Rect.center.y), new Vector2(zone.Rect.width, zone.Rect.height), road, 0.14f);
        foreach (Zone zone in Reserved)
            Surface(debug, zone.Name, new Vector3(zone.Rect.center.x, 0.15f, zone.Rect.center.y), new Vector2(zone.Rect.width, zone.Rect.height), reserved, 0.15f);
    }

    private static void BuildCameras(GameObject root)
    {
        Camera top = CameraObject(root, "Camera_GC30_TopDownBlueprintProof");
        top.orthographic = true;
        top.orthographicSize = 1035f;
        top.transform.position = new Vector3(0f, 1600f, 0f);
        top.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        BuildRtsCamera(root, "Camera_GC30_RtsOverview", new Vector3(-620f, 520f, -820f), BlueprintPoint(388f, 430f), 38f);
        BuildRtsCamera(root, "Camera_GC30_RtsCityReadable", new Vector3(-930f, 330f, -430f), BlueprintPoint(172f, 366f), 34f);
        BuildRtsCamera(root, "Camera_GC30_RtsAirfieldCommand", new Vector3(260f, 430f, -620f), BlueprintPoint(660f, 320f), 36f);
        BuildRtsCamera(root, "Camera_GC30_RtsSouthBase", new Vector3(-120f, 390f, -980f), BlueprintPoint(470f, 704f), 34f);
    }

    private static void BuildRtsCamera(GameObject root, string name, Vector3 position, Vector3 target, float fov)
    {
        Camera camera = CameraObject(root, name);
        camera.fieldOfView = fov;
        camera.transform.position = position;
        camera.transform.LookAt(target);
    }

    private static Camera CameraObject(GameObject root, string name)
    {
        GameObject cameraObject = Child(root, name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.49f, 0.40f, 0.28f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 4000f;
        UniversalAdditionalCameraData data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        return camera;
    }

    private static void ValidateLayout(List<ModuleSpec> modules, Dictionary<string, List<GameObject>> sourceByModule)
    {
        foreach (Zone placed in PlacedObjects)
        {
            foreach (Zone road in Roads)
                if (placed.Rect.Overlaps(road.Rect))
                    ValidationLog.Add($"ERROR: placed visual {placed.Name} overlaps walkable road {road.Name}.");
            foreach (Zone reserved in Reserved)
                if (placed.Rect.Overlaps(reserved.Rect))
                    ValidationLog.Add($"ERROR: placed visual {placed.Name} overlaps reserved gameplay zone {reserved.Name}.");
        }

        foreach (ModuleSpec module in modules)
            if (sourceByModule[module.Name].Count == 0)
                ValidationLog.Add($"ERROR: module {module.Name} found no source roots in Demo scene.");

        if (acceptedRootCount < 120)
            ValidationLog.Add($"ERROR: GC30 accepted only {acceptedRootCount} authored roots; scene is likely too sparse.");
        if (demo2RoadPrefabCount < 40)
            ValidationLog.Add($"ERROR: GC30 placed only {demo2RoadPrefabCount} Demo2 road prefab pieces; target is at least 40 measured source-scale road pieces.");
        if (demo2GroundPrefabCount < 20)
            ValidationLog.Add($"ERROR: GC30 placed only {demo2GroundPrefabCount} Demo2 grass/dirt ground prefab pieces; target is at least 20 source-scale terrain pieces.");
        if (demo2DecorationCount < 5)
            ValidationLog.Add($"ERROR: GC30 placed only {demo2DecorationCount} off-road Demo2 decorations; target is at least 5.");

        if (ValidationLog.Count == 0)
            ValidationLog.Add($"PASS: GC30 placed {acceptedRootCount} source-scale Demo-authored roots around a green/dirt terrain contract, added {demo2GroundPrefabCount} Demo2 grass/circle ground prefab pieces, {demo2RoadPrefabCount} measured Demo2 road prefab pieces and {demo2DecorationCount} off-road decorations, with {skippedRoadCount} illegal road/reserved roots omitted.");
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera.name == "Camera_GC30_TopDownBlueprintProof")
                Render(camera, ProjectPath(CaptureRoot + "/gc30_topdown_blueprint_proof_2048x2048.png"), 2048, 2048);
            if (camera.name == "Camera_GC30_RtsOverview")
                Render(camera, ProjectPath(CaptureRoot + "/gc30_rts_overview_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC30_RtsCityReadable")
                Render(camera, ProjectPath(CaptureRoot + "/gc30_rts_city_readable_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC30_RtsAirfieldCommand")
                Render(camera, ProjectPath(CaptureRoot + "/gc30_rts_airfield_command_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC30_RtsSouthBase")
                Render(camera, ProjectPath(CaptureRoot + "/gc30_rts_south_base_1920x1080.png"), 1920, 1080);
        }
    }

    private static void WriteReport(List<ModuleSpec> modules, Dictionary<string, List<GameObject>> sourceByModule)
    {
        StringBuilder report = new();
        report.AppendLine("# GC30 Demo2 Road Green Terrain Scene");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Build a green/dirt terrain RTS visual scene using Demo2 asphalt road combination prefabs for the road network, authored Demo military/city modules between roads, and off-road decoration that stays out of road masks.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc30Demo2RoadGreenTerrainBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC30_Demo2RoadGreenTerrain_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048/gc30_topdown_blueprint_proof_2048x2048.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048/gc30_rts_overview_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048/gc30_rts_city_readable_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048/gc30_rts_airfield_command_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC30_Demo2RoadGreenTerrain_2048/gc30_rts_south_base_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: GC30 generated visual contract only. Demo modules and Demo2 asphalt road prefabs are visual dressing only and are legalized against road/spawn/objective masks.");
        report.AppendLine("User-visible behavior: no shipped runtime behavior changed; generated scene and captures are available for visual review.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc30Demo2RoadGreenTerrainBuilder.BuildGc30Demo2RoadGreenTerrain2048`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed; see validation log below." : "passed generation validation."));
        report.AppendLine("Known gaps: This is still a visual proof scene. It does not yet convert the layout to ECS/pathfinding data. Demo2 asphalt road prefabs are placed by measured source-scale bounds and use controlled road/edge fallback materials so missing Demo2 road-marking materials do not render magenta in captures.");
        report.AppendLine("Cross-lane impacts: Art/Design can review the authored-module composition before Gameplay locks any movement grid or ECS conversion.");
        report.AppendLine("Next recommended task: visual review of GC30 captures; if accepted, promote this authored-module plus measured-road workflow into the reusable scene-generation contract.");
        report.AppendLine();
        report.AppendLine($"Source roots scanned: {sourceByModule.Values.Sum(list => list.Count)}");
        report.AppendLine($"Source roots cloned: {clonedRootCount}");
        report.AppendLine($"Accepted authored roots: {acceptedRootCount}");
        report.AppendLine($"Skipped road/reserved roots: {skippedRoadCount}");
        report.AppendLine($"Demo2 ground prefab pieces: {demo2GroundPrefabCount}");
        report.AppendLine($"Demo2 road prefab pieces: {demo2RoadPrefabCount}");
        report.AppendLine($"Demo2 off-road decorations: {demo2DecorationCount}");
        report.AppendLine();
        report.AppendLine("Module placement:");
        foreach (ModuleSpec module in modules)
            report.AppendLine($"- {module.Name}: sourceRoots={sourceByModule[module.Name].Count}, target={Format(module.TargetCenter)}, rotationY={module.RotationY:0.#}, scale={module.Scale:0.##}");
        report.AppendLine();
        report.AppendLine("Validation log:");
        foreach (string line in ValidationLog)
            report.AppendLine("- " + line);
        if (MissingAssets.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Missing assets:");
            foreach (string path in MissingAssets.Distinct(StringComparer.Ordinal))
                report.AppendLine("- " + path);
        }
        report.AppendLine();
        report.AppendLine("Build log:");
        foreach (string line in BuildLog)
            report.AppendLine("- " + line);
        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
    }

    private static bool HasSelectedAncestor(Transform transform, Dictionary<int, GameObject> selected)
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (selected.ContainsKey(parent.gameObject.GetInstanceID()))
                return true;
            parent = parent.parent;
        }
        return false;
    }

    private static GameObject TopSceneObject(Transform transform)
    {
        Transform current = transform;
        while (current.parent != null)
            current = current.parent;
        return current.gameObject;
    }

    private static bool IsHugeBackground(string name, Bounds bounds)
    {
        string lower = name.ToLowerInvariant();
        return lower.Contains("sky") || lower.Contains("cloud") || lower.Contains("skydome") ||
            bounds.center.y > 500f || bounds.size.x > 900f || bounds.size.z > 900f;
    }

    private static bool IsTerrainOrHillSource(string name, Bounds bounds)
    {
        string lower = name.ToLowerInvariant();
        if (lower.Contains("terrain") || lower.Contains("mountain") || lower.Contains("sanddune") || lower.Contains("dune"))
            return true;
        return bounds.size.x > 130f && bounds.size.z > 130f && bounds.size.y < 35f;
    }

    private static void ForceStaticFlatShadows(GameObject go)
    {
        foreach (Light light in go.GetComponentsInChildren<Light>(false))
            light.enabled = false;

        foreach (LODGroup lodGroup in go.GetComponentsInChildren<LODGroup>(false))
        {
            lodGroup.ForceLOD(0);
            lodGroup.enabled = false;
        }

        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(false))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static void AlignBottomNearGround(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return;
        Bounds bounds = CalculateBounds(renderers);
        go.transform.position -= new Vector3(0f, bounds.min.y, 0f);
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Rect RendererFootprint(GameObject go, float padding)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return new Rect(go.transform.position.x - padding, go.transform.position.z - padding, padding * 2f, padding * 2f);
        Bounds bounds = CalculateBounds(renderers);
        return new Rect(bounds.min.x - padding, bounds.min.z - padding, bounds.size.x + padding * 2f, bounds.size.z + padding * 2f);
    }

    private static bool OverlapsAny(List<Zone> zones, Rect rect)
    {
        return zones.Any(zone => zone.Rect.Overlaps(rect));
    }

    private static Vector3 BlueprintPoint(float x, float y)
    {
        return new Vector3((x / 890f - 0.5f) * MapSize, 0f, (0.5f - y / 890f) * MapSize);
    }

    private static Rect BlueprintRect(float x, float y, float width, float height)
    {
        Vector3 center = BlueprintPoint(x + width * 0.5f, y + height * 0.5f);
        return new Rect(center.x - width / 890f * MapSize * 0.5f, center.z - height / 890f * MapSize * 0.5f, width / 890f * MapSize, height / 890f * MapSize);
    }

    private static float RoadRotationY(Vector3 start, Vector3 end)
    {
        Vector3 delta = end - start;
        return Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
    }

    private static void Surface(GameObject parent, string name, Vector3 position, Vector2 size, Material material, float y)
    {
        GameObject surface = new(name);
        surface.transform.SetParent(parent.transform, false);
        surface.transform.position = new Vector3(position.x, y, position.z);
        Mesh mesh = new();
        float halfX = size.x * 0.5f;
        float halfZ = size.y * 0.5f;
        mesh.vertices = new[]
        {
            new Vector3(-halfX, 0f, -halfZ),
            new Vector3(-halfX, 0f, halfZ),
            new Vector3(halfX, 0f, halfZ),
            new Vector3(halfX, 0f, -halfZ)
        };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        surface.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void SurfaceBlueprintPolygon(GameObject parent, string name, Material material, float y, params (float x, float y)[] points)
    {
        if (points.Length < 3)
            return;

        GameObject surface = new(name);
        surface.transform.SetParent(parent.transform, false);
        surface.transform.position = new Vector3(0f, y, 0f);
        Mesh mesh = new();
        Vector3[] vertices = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            vertices[i] = BlueprintPoint(points[i].x, points[i].y);

        int[] triangles = new int[(points.Length - 2) * 3];
        int triangle = 0;
        for (int i = 1; i < points.Length - 1; i++)
        {
            triangles[triangle++] = 0;
            triangles[triangle++] = i;
            triangles[triangle++] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.uv = vertices.Select(vertex => new Vector2(vertex.x / MapSize + 0.5f, vertex.z / MapSize + 0.5f)).ToArray();
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        surface.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material material = new(shader) { name = name, color = color };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void Render(Camera camera, string path, int width, int height)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture texture = new(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
        camera.targetTexture = texture;
        RenderTexture.active = texture;
        GL.Clear(true, true, camera.backgroundColor);
        camera.Render();
        Texture2D image = new(width, height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        texture.Release();
        Object.DestroyImmediate(texture);
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);
    }

    private static string Format(Vector3 value)
    {
        return $"({value.x:0.##}, {value.y:0.##}, {value.z:0.##})";
    }
}
#endif
