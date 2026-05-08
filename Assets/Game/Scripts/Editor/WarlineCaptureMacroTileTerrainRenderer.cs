#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureMacroTileTerrainRenderer
{
    private const int TilePixels = 4096;
    private const string ArtRoot = "Assets/Game/Art/Generated/IsometricMaps/MacroTiles";
    private const string SharedRoot = ArtRoot + "/Shared";
    private const string Fg01Root = ArtRoot + "/iso_fg_l01_coastal_command";
    private const string SceneRoot = "Assets/Game/Scenes/DesignTargets/MacroTileRenders";

    private static readonly Color TransparentClear = new(0f, 0f, 0f, 0f);
    private static readonly Color Sand = new(0.60f, 0.50f, 0.36f, 1f);
    private static readonly Color SandDark = new(0.45f, 0.36f, 0.25f, 1f);
    private static readonly Color Limestone = new(0.58f, 0.54f, 0.45f, 1f);
    private static readonly Color LimestoneLight = new(0.68f, 0.62f, 0.50f, 1f);
    private static readonly Color LimestoneSide = new(0.40f, 0.34f, 0.26f, 1f);
    private static readonly Color Asphalt = new(0.09f, 0.10f, 0.10f, 1f);
    private static readonly Color AsphaltWorn = new(0.18f, 0.17f, 0.15f, 1f);
    private static readonly Color RoadDust = new(0.33f, 0.28f, 0.21f, 1f);
    private static readonly Color RoadMarking = new(0.72f, 0.66f, 0.48f, 1f);
    private static readonly Color TealTrim = new(0.02f, 0.56f, 0.62f, 1f);
    private static readonly Color Water = new(0.03f, 0.39f, 0.48f, 1f);
    private static readonly Color WaterHighlight = new(0.32f, 0.76f, 0.80f, 1f);
    private static readonly Color PlantGreen = new(0.24f, 0.39f, 0.16f, 1f);
    private static readonly Color PalmTrunk = new(0.43f, 0.29f, 0.15f, 1f);

    [MenuItem("WarlineCapture/Design/Render FG-01 Initial Macro Terrain Tiles")]
    public static void RenderFg01InitialMacroTiles()
    {
        Directory.CreateDirectory(ProjectPath(SharedRoot));
        Directory.CreateDirectory(ProjectPath(Fg01Root));
        Directory.CreateDirectory(ProjectPath(SceneRoot));

        RenderTile(
            "FG01_MT_UrbanStraightRoad_A",
            Path.Combine(SharedRoot, "fg_mt_urban_straight_road_a.png"),
            BuildUrbanStraightRoad);

        RenderTile(
            "FG01_MT_UrbanStraightRoad_A_Rot90",
            Path.Combine(SharedRoot, "fg_mt_urban_straight_road_a_rot90.png"),
            BuildUrbanStraightRoadRot90);

        RenderTile(
            "FG01_MT_UrbanIntersection_A",
            Path.Combine(SharedRoot, "fg_mt_urban_intersection_a.png"),
            BuildUrbanIntersection);

        RenderTile(
            "FG01_MT_CommandPlaza_A",
            Path.Combine(Fg01Root, "fg_mt_command_plaza_a.png"),
            BuildCommandPlaza);

        RenderTile(
            "FG01_MT_PortEdge_A",
            Path.Combine(SharedRoot, "fg_mt_port_edge_a.png"),
            BuildPortEdge);

        RenderTile(
            "FG01_MT_SeawallBatteryPad_A",
            Path.Combine(Fg01Root, "fg_mt_seawall_battery_pad_a.png"),
            BuildSeawallBatteryPad);

        AssetDatabase.Refresh();
        Debug.Log("WARLINECAPTURE_MACRO_TILE_RENDER_COMPLETE set=FG01_INITIAL count=6 pixels=4096");
    }

    private static void RenderTile(string tileName, string assetPath, Action buildTile)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupRenderWorld();
        buildTile();

        var scenePath = ProjectPath(Path.Combine(SceneRoot, tileName + ".unity"));
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);

        var camera = BuildIsoCamera(tileName + " Camera");
        Render(camera, ProjectPath(assetPath));
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
        Debug.Log($"WARLINECAPTURE_MACRO_TILE_RENDERED tile={tileName} path={assetPath}");
    }

    private static void SetupRenderWorld()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.56f, 0.54f, 0.49f);
        RenderSettings.skybox = null;
        RenderSettings.fog = false;

        var sunObject = new GameObject("FG01 Macro Tile Warm Sun");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.42f;
        sun.color = new Color(1f, 0.90f, 0.72f);
        sun.shadows = LightShadows.Soft;
        sunObject.transform.rotation = Quaternion.Euler(40f, -32f, 0f);

        var coolObject = new GameObject("FG01 Macro Tile Cool Fill");
        var cool = coolObject.AddComponent<Light>();
        cool.type = LightType.Directional;
        cool.intensity = 0.52f;
        cool.color = new Color(0.62f, 0.74f, 0.92f);
        coolObject.transform.rotation = Quaternion.Euler(26f, 132f, 0f);
    }

    private static Camera BuildIsoCamera(string name)
    {
        var cameraObject = new GameObject(name);
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = TransparentClear;
        camera.orthographic = true;
        camera.orthographicSize = 58f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 240f;
        camera.transform.position = new Vector3(46f, 38f, -46f);
        camera.transform.LookAt(new Vector3(0f, 0f, 0f));
        camera.allowHDR = false;
        camera.allowMSAA = true;
        return camera;
    }

    private static void BuildUrbanStraightRoad()
    {
        AddGround();
        AddRoad(new Vector3(0f, 0.06f, 0f), new Vector3(13f, 0.08f, 74f), "Straight Road Asphalt");
        AddSidewalk(new Vector3(-11.6f, 0.24f, 0f), new Vector3(5.8f, 0.38f, 74f), "Left Raised Sidewalk");
        AddSidewalk(new Vector3(11.6f, 0.24f, 0f), new Vector3(5.8f, 0.38f, 74f), "Right Raised Sidewalk");
        AddCurb(new Vector3(-7.3f, 0.5f, 0f), new Vector3(0.55f, 0.8f, 74f), "Left Road Curb");
        AddCurb(new Vector3(7.3f, 0.5f, 0f), new Vector3(0.55f, 0.8f, 74f), "Right Road Curb");
        AddRoadEdgeLines(false);
        AddPavementGrid(new Vector3(-11.6f, 0.49f, 0f), 5.4f, 72f, false, "Left Sidewalk Slabs");
        AddPavementGrid(new Vector3(11.6f, 0.49f, 0f), 5.4f, 72f, false, "Right Sidewalk Slabs");

        for (int i = -3; i <= 3; i++)
        {
            if (i != 0)
                AddRoadDash(new Vector3(0f, 0.13f, i * 9.5f), Quaternion.identity);
        }

        AddPlanterStrip(-18f, -27f, 0f);
        AddPlanterStrip(18f, -17f, 1f);
        AddPlanterStrip(-18f, 8f, 2f);
        AddPlanterStrip(18f, 22f, 3f);
        AddShadeCanopy(new Vector3(-24f, 1.7f, -16f), 10f, 7f, 0f);
        AddLowWall(new Vector3(25f, 0.85f, 18f), new Vector3(1.1f, 1.5f, 18f), "Right Service Wall");
        AddLowWall(new Vector3(-25f, 0.85f, 22f), new Vector3(1.1f, 1.5f, 14f), "Left Service Wall");
        AddRockDressing();
    }

    private static void BuildUrbanStraightRoadRot90()
    {
        AddGround();
        AddRoad(new Vector3(0f, 0.06f, 0f), new Vector3(74f, 0.08f, 13f), "Straight Road Asphalt Rot90");
        AddSidewalk(new Vector3(0f, 0.24f, -11.6f), new Vector3(74f, 0.38f, 5.8f), "Bottom Raised Sidewalk");
        AddSidewalk(new Vector3(0f, 0.24f, 11.6f), new Vector3(74f, 0.38f, 5.8f), "Top Raised Sidewalk");
        AddCurb(new Vector3(0f, 0.5f, -7.3f), new Vector3(74f, 0.8f, 0.55f), "Bottom Road Curb");
        AddCurb(new Vector3(0f, 0.5f, 7.3f), new Vector3(74f, 0.8f, 0.55f), "Top Road Curb");
        AddRoadEdgeLines(true);
        AddPavementGrid(new Vector3(0f, 0.49f, -11.6f), 72f, 5.4f, true, "Bottom Sidewalk Slabs");
        AddPavementGrid(new Vector3(0f, 0.49f, 11.6f), 72f, 5.4f, true, "Top Sidewalk Slabs");

        for (int i = -3; i <= 3; i++)
        {
            if (i != 0)
                AddRoadDash(new Vector3(i * 9.5f, 0.13f, 0f), Quaternion.Euler(0f, 90f, 0f));
        }

        AddPlanterStrip(-25f, -18f, 12f);
        AddPlanterStrip(-11f, 18f, 13f);
        AddPlanterStrip(14f, -18f, 14f);
        AddPlanterStrip(27f, 18f, 15f);
        AddShadeCanopy(new Vector3(19f, 1.7f, 24f), 10f, 7f, 90f);
        AddLowWall(new Vector3(-22f, 0.85f, -25f), new Vector3(18f, 1.5f, 1.1f), "Bottom Service Wall");
        AddLowWall(new Vector3(22f, 0.85f, 25f), new Vector3(14f, 1.5f, 1.1f), "Top Service Wall");
        AddRockDressing();
    }

    private static void BuildUrbanIntersection()
    {
        AddGround();
        AddRoad(new Vector3(0f, 0.06f, 0f), new Vector3(13f, 0.08f, 74f), "North South Asphalt");
        AddRoad(new Vector3(0f, 0.065f, 0f), new Vector3(74f, 0.08f, 13f), "East West Asphalt");
        AddSidewalk(new Vector3(-22f, 0.24f, 22f), new Vector3(18f, 0.38f, 18f), "NW Sidewalk");
        AddSidewalk(new Vector3(22f, 0.24f, 22f), new Vector3(18f, 0.38f, 18f), "NE Sidewalk");
        AddSidewalk(new Vector3(-22f, 0.24f, -22f), new Vector3(18f, 0.38f, 18f), "SW Sidewalk");
        AddSidewalk(new Vector3(22f, 0.24f, -22f), new Vector3(18f, 0.38f, 18f), "SE Sidewalk");
        AddPavementGrid(new Vector3(-22f, 0.49f, 22f), 17f, 17f, false, "NW Paving Slabs");
        AddPavementGrid(new Vector3(22f, 0.49f, 22f), 17f, 17f, false, "NE Paving Slabs");
        AddPavementGrid(new Vector3(-22f, 0.49f, -22f), 17f, 17f, false, "SW Paving Slabs");
        AddPavementGrid(new Vector3(22f, 0.49f, -22f), 17f, 17f, false, "SE Paving Slabs");
        AddRoadEdgeLines(false);
        AddRoadEdgeLines(true);
        AddMedian(new Vector3(0f, 0.42f, 24f), new Vector3(2.8f, 0.55f, 12f), "North Median");
        AddMedian(new Vector3(0f, 0.42f, -24f), new Vector3(2.8f, 0.55f, 12f), "South Median");
        AddMedian(new Vector3(24f, 0.42f, 0f), new Vector3(12f, 0.55f, 2.8f), "East Median");
        AddMedian(new Vector3(-24f, 0.42f, 0f), new Vector3(12f, 0.55f, 2.8f), "West Median");
        AddRoadDustPatch(new Vector3(0f, 0.14f, 0f), new Vector3(18f, 0.02f, 18f), "Center Road Wear");
        AddPlanterStrip(-28f, 24f, 4f);
        AddPlanterStrip(28f, 22f, 5f);
        AddPlanterStrip(-28f, -22f, 6f);
        AddPlanterStrip(28f, -24f, 7f);
        AddRockDressing();
    }

    private static void BuildCommandPlaza()
    {
        AddGround();
        AddRoad(new Vector3(0f, 0.06f, -25f), new Vector3(14f, 0.08f, 26f), "South Service Road");
        AddRoad(new Vector3(24f, 0.06f, 0f), new Vector3(24f, 0.08f, 12f), "East Service Road");
        AddRoad(new Vector3(0f, 0.06f, 28f), new Vector3(12f, 0.08f, 20f), "North Connector");
        AddPavedPlaza(new Vector3(0f, 0.25f, 2f), new Vector3(44f, 0.38f, 42f), "Command Paved Plaza");
        AddPavementGrid(new Vector3(0f, 0.50f, 2f), 42f, 40f, false, "Command Plaza Slab Grid");
        AddFoundationPad(new Vector3(-6f, 0.65f, 2f), new Vector3(19f, 1.0f, 16f), "Empty Command Foundation");
        AddFoundationPad(new Vector3(15f, 0.62f, 12f), new Vector3(9f, 0.85f, 9f), "Empty Radar Foundation");
        AddFoundationPad(new Vector3(13f, 0.58f, -10f), new Vector3(12f, 0.75f, 7f), "Empty Service Pad");
        AddLowWall(new Vector3(-26f, 0.95f, 0f), new Vector3(1.2f, 1.6f, 38f), "West Plaza Wall");
        AddLowWall(new Vector3(0f, 0.95f, 24f), new Vector3(34f, 1.6f, 1.2f), "North Plaza Wall");
        AddPlanterStrip(-20f, -18f, 8f);
        AddPlanterStrip(22f, 20f, 9f);
        AddRockDressing();
    }

    private static void BuildPortEdge()
    {
        AddGround();
        AddWater(new Vector3(25f, -0.05f, 0f), new Vector3(28f, 0.08f, 78f), "Port Water Lane");
        AddSeawall(new Vector3(10f, 0.8f, 0f), new Vector3(3.8f, 1.5f, 78f), "Raised Seawall");
        AddRoad(new Vector3(-7f, 0.06f, 0f), new Vector3(16f, 0.08f, 74f), "Port Edge Road");
        AddSidewalk(new Vector3(-19f, 0.24f, 0f), new Vector3(6f, 0.38f, 74f), "Port Sidewalk");
        AddRoadEdgeLines(false, -7f);
        AddPavementGrid(new Vector3(-19f, 0.49f, 0f), 5.4f, 72f, false, "Port Sidewalk Slabs");
        AddFoundationPad(new Vector3(-28f, 0.58f, -16f), new Vector3(11f, 0.72f, 10f), "Empty Loading Pad A");
        AddFoundationPad(new Vector3(-28f, 0.58f, 17f), new Vector3(11f, 0.72f, 10f), "Empty Loading Pad B");
        AddLowWall(new Vector3(-34f, 0.75f, 0f), new Vector3(1.1f, 1.3f, 44f), "Warehouse Edge Wall");
        AddPlanterStrip(-23f, 26f, 10f);
        AddRockDressing();
    }

    private static void BuildSeawallBatteryPad()
    {
        AddGround();
        AddWater(new Vector3(25f, -0.05f, 0f), new Vector3(28f, 0.08f, 78f), "Battery Water Edge");
        AddSeawall(new Vector3(11f, 0.8f, 0f), new Vector3(4f, 1.5f, 78f), "Battery Seawall");
        AddRoad(new Vector3(-12f, 0.06f, -18f), new Vector3(18f, 0.08f, 38f), "Battery Access Road");
        AddFoundationPad(new Vector3(-9f, 0.7f, 10f), new Vector3(20f, 1.1f, 20f), "Empty Coastal Battery Pad");
        AddOctagonalPad(new Vector3(-9f, 1.34f, 10f), 7f, 0.35f, "Battery Circular Socket Top");
        AddLowWall(new Vector3(-27f, 1.0f, 10f), new Vector3(1.3f, 1.8f, 24f), "Battery Blast Wall West");
        AddLowWall(new Vector3(-9f, 1.0f, 27f), new Vector3(26f, 1.8f, 1.3f), "Battery Blast Wall North");
        AddPlanterStrip(-26f, -22f, 11f);
        AddRockDressing();
    }

    private static void AddGround()
    {
        AddBox("Sand Terrain Base", new Vector3(0f, -0.08f, 0f), new Vector3(78f, 0.16f, 78f), Sand);
        AddRoadDustPatch(new Vector3(-28f, 0.03f, 28f), new Vector3(16f, 0.03f, 12f), "Sand Variation NW");
        AddRoadDustPatch(new Vector3(29f, 0.035f, -26f), new Vector3(13f, 0.03f, 15f), "Sand Variation SE");
        AddBox("Subtle Sand Plane Seam A", new Vector3(-18f, 0.045f, -5f), new Vector3(0.18f, 0.025f, 54f), SandDark);
        AddBox("Subtle Sand Plane Seam B", new Vector3(18f, 0.045f, 8f), new Vector3(0.16f, 0.025f, 48f), SandDark);
    }

    private static void AddRoad(Vector3 center, Vector3 scale, string name)
    {
        AddBox(name, center, scale, Asphalt);
        AddBox(name + " Worn Center", center + new Vector3(0f, 0.05f, 0f), new Vector3(scale.x * 0.82f, 0.02f, scale.z * 0.82f), AsphaltWorn);
        AddBox(name + " Dust", center + new Vector3(0f, 0.075f, 0f), new Vector3(scale.x * 0.54f, 0.02f, scale.z * 0.28f), RoadDust);
    }

    private static void AddSidewalk(Vector3 center, Vector3 scale, string name)
    {
        AddBox(name, center, scale, Limestone);
        AddBox(name + " Teal Trim A", center + new Vector3(scale.x * 0.42f, 0.22f, 0f), new Vector3(0.45f, 0.08f, scale.z * 0.88f), TealTrim);
    }

    private static void AddCurb(Vector3 center, Vector3 scale, string name)
    {
        AddBox(name, center, scale, LimestoneSide);
    }

    private static void AddMedian(Vector3 center, Vector3 scale, string name)
    {
        AddBox(name, center, scale, Limestone);
        AddBox(name + " Teal Cap", center + new Vector3(0f, scale.y * 0.55f, 0f), new Vector3(scale.x * 0.82f, 0.08f, scale.z * 0.36f), TealTrim);
        AddPalm(center + new Vector3(0f, scale.y + 0.55f, 0f), 0.78f);
    }

    private static void AddPavedPlaza(Vector3 center, Vector3 scale, string name)
    {
        AddBox(name, center, scale, Limestone);
        AddBox(name + " Inner Teal Line", center + new Vector3(0f, scale.y * 0.55f, -scale.z * 0.35f), new Vector3(scale.x * 0.78f, 0.08f, 0.42f), TealTrim);
    }

    private static void AddFoundationPad(Vector3 center, Vector3 scale, string name)
    {
        AddBox(name + " Side", center, scale, LimestoneSide);
        AddBox(name + " Top", center + new Vector3(0f, scale.y * 0.54f, 0f), new Vector3(scale.x * 0.92f, 0.12f, scale.z * 0.92f), Limestone);
        AddBox(name + " Socket Inset", center + new Vector3(0f, scale.y * 0.62f, 0f), new Vector3(scale.x * 0.62f, 0.08f, scale.z * 0.58f), new Color(0.43f, 0.39f, 0.32f));
    }

    private static void AddSeawall(Vector3 center, Vector3 scale, string name)
    {
        AddBox(name, center, scale, LimestoneSide);
        AddBox(name + " Top Cap", center + new Vector3(0f, scale.y * 0.55f, 0f), new Vector3(scale.x * 1.1f, 0.22f, scale.z), Limestone);
        AddBox(name + " Teal Edge", center + new Vector3(-scale.x * 0.35f, scale.y * 0.74f, 0f), new Vector3(0.32f, 0.10f, scale.z * 0.9f), TealTrim);
    }

    private static void AddWater(Vector3 center, Vector3 scale, string name)
    {
        AddBox(name, center, scale, Water);
        for (int i = -3; i <= 3; i++)
            AddBox(name + " Highlight " + i, center + new Vector3(-2f + i * 1.4f, 0.08f, i * 8f), new Vector3(0.08f, 0.02f, 8f), WaterHighlight);
    }

    private static void AddRoadDash(Vector3 center, Quaternion rotation)
    {
        AddBox("Road Dash", center, new Vector3(0.22f, 0.03f, 3.3f), RoadMarking, rotation);
    }

    private static void AddRoadDustPatch(Vector3 center, Vector3 scale, string name)
    {
        AddBox(name, center, scale, RoadDust);
    }

    private static void AddPlanterStrip(float x, float z, float seed)
    {
        AddBox("Raised Planter " + seed, new Vector3(x, 0.42f, z), new Vector3(6f, 0.82f, 7f), LimestoneSide);
        AddBox("Planter Soil " + seed, new Vector3(x, 0.88f, z), new Vector3(4.5f, 0.18f, 5.5f), new Color(0.20f, 0.16f, 0.10f));
        AddPalm(new Vector3(x, 1.3f, z), 1f + seed * 0.03f);
        AddShrub(new Vector3(x + 1.4f, 1.12f, z - 1.7f), 1.15f);
        AddShrub(new Vector3(x - 1.7f, 1.12f, z + 1.2f), 0.9f);
    }

    private static void AddRoadEdgeLines(bool horizontal, float centerOffset = 0f)
    {
        if (horizontal)
        {
            AddBox("Road Edge Marking Bottom", new Vector3(centerOffset, 0.15f, -5.25f), new Vector3(68f, 0.025f, 0.16f), RoadMarking);
            AddBox("Road Edge Marking Top", new Vector3(centerOffset, 0.15f, 5.25f), new Vector3(68f, 0.025f, 0.16f), RoadMarking);
            return;
        }

        AddBox("Road Edge Marking Left", new Vector3(-5.25f + centerOffset, 0.15f, 0f), new Vector3(0.16f, 0.025f, 68f), RoadMarking);
        AddBox("Road Edge Marking Right", new Vector3(5.25f + centerOffset, 0.15f, 0f), new Vector3(0.16f, 0.025f, 68f), RoadMarking);
    }

    private static void AddPavementGrid(Vector3 center, float width, float depth, bool horizontal, string name)
    {
        const float lineHeight = 0.035f;
        var lineColor = new Color(0.43f, 0.39f, 0.32f, 1f);
        int widthSteps = Mathf.Max(1, Mathf.FloorToInt(width / 7f));
        int depthSteps = Mathf.Max(1, Mathf.FloorToInt(depth / 7f));
        for (int i = -widthSteps; i <= widthSteps; i++)
        {
            float x = i * 7f;
            if (Mathf.Abs(x) < width * 0.5f)
                AddBox(name + " Width Seam " + i, center + new Vector3(x, lineHeight, 0f), new Vector3(0.12f, 0.025f, depth * 0.94f), lineColor);
        }

        for (int i = -depthSteps; i <= depthSteps; i++)
        {
            float z = i * 7f;
            if (Mathf.Abs(z) < depth * 0.5f)
                AddBox(name + " Depth Seam " + i, center + new Vector3(0f, lineHeight, z), new Vector3(width * 0.94f, 0.025f, 0.12f), lineColor);
        }

        if (horizontal)
            AddBox(name + " Accent", center + new Vector3(0f, lineHeight * 1.6f, depth * 0.37f), new Vector3(width * 0.82f, 0.03f, 0.20f), TealTrim);
        else
            AddBox(name + " Accent", center + new Vector3(width * 0.37f, lineHeight * 1.6f, 0f), new Vector3(0.20f, 0.03f, depth * 0.82f), TealTrim);
    }

    private static void AddPalm(Vector3 basePosition, float scale)
    {
        AddCylinder("Palm Trunk", basePosition + new Vector3(0f, 2.1f * scale, 0f), 0.28f * scale, 4.4f * scale, PalmTrunk);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f;
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Palm Frond";
            leaf.transform.position = basePosition + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 1.1f * scale, 4.65f * scale, Mathf.Sin(angle * Mathf.Deg2Rad) * 1.1f * scale);
            leaf.transform.rotation = Quaternion.Euler(18f, angle, 0f);
            leaf.transform.localScale = new Vector3(0.45f * scale, 0.12f * scale, 3.0f * scale);
            leaf.GetComponent<Renderer>().sharedMaterial = Material("Palm Frond", PlantGreen);
        }
    }

    private static void AddShrub(Vector3 center, float scale)
    {
        AddSphere("Shrub", center, new Vector3(1.8f * scale, 0.9f * scale, 1.8f * scale), PlantGreen);
    }

    private static void AddShadeCanopy(Vector3 center, float width, float depth, float yaw)
    {
        AddBox("Canopy Posts A", center + new Vector3(-width * 0.4f, -0.65f, -depth * 0.4f), new Vector3(0.25f, 2.5f, 0.25f), LimestoneSide);
        AddBox("Canopy Posts B", center + new Vector3(width * 0.4f, -0.65f, depth * 0.4f), new Vector3(0.25f, 2.5f, 0.25f), LimestoneSide);
        AddBox("Canopy Cloth", center + new Vector3(0f, 0.65f, 0f), new Vector3(width, 0.18f, depth), new Color(0.78f, 0.73f, 0.62f), Quaternion.Euler(0f, yaw, 0f));
    }

    private static void AddLowWall(Vector3 center, Vector3 scale, string name)
    {
        AddBox(name, center, scale, LimestoneSide);
        AddBox(name + " Cap", center + new Vector3(0f, scale.y * 0.56f, 0f), new Vector3(scale.x * 1.15f, 0.18f, scale.z * 1.08f), Limestone);
    }

    private static void AddRockDressing()
    {
        for (int i = 0; i < 18; i++)
        {
            float x = Mathf.Sin(i * 12.9898f) * 33f;
            float z = Mathf.Cos(i * 7.233f) * 33f;
            if (Mathf.Abs(x) < 14f && Mathf.Abs(z) < 30f)
                x += x < 0f ? -18f : 18f;
            AddBox("Rock Dressing " + i, new Vector3(x, 0.14f, z), new Vector3(1.1f + i % 3, 0.3f + (i % 2) * 0.18f, 1.0f + i % 4 * 0.35f), new Color(0.55f, 0.48f, 0.36f), Quaternion.Euler(0f, i * 19f, 0f));
        }
    }

    private static void AddOctagonalPad(Vector3 center, float radius, float height, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.position = center;
        go.transform.localScale = new Vector3(radius, height, radius);
        go.GetComponent<Renderer>().sharedMaterial = Material(name + " Material", Limestone);
    }

    private static void AddBox(string name, Vector3 center, Vector3 scale, Color color)
    {
        AddBox(name, center, scale, color, Quaternion.identity);
    }

    private static void AddBox(string name, Vector3 center, Vector3 scale, Color color, Quaternion rotation)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = center;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = Material(name + " Material", color);
        renderer.receiveShadows = true;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    private static void AddCylinder(string name, Vector3 center, float radius, float height, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.position = center;
        go.transform.localScale = new Vector3(radius, height * 0.5f, radius);
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = Material(name + " Material", color);
        renderer.receiveShadows = true;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    private static void AddSphere(string name, Vector3 center, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.position = center;
        go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = Material(name + " Material", color);
        renderer.receiveShadows = true;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    private static Material Material(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
        var material = new Material(shader)
        {
            name = name,
            color = color
        };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.18f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        return material;
    }

    private static void Render(Camera camera, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        var descriptor = new RenderTextureDescriptor(TilePixels, TilePixels, RenderTextureFormat.ARGB32, 24)
        {
            msaaSamples = 4
        };
        var renderTexture = new RenderTexture(descriptor);
        var previousActive = RenderTexture.active;
        var previousTarget = camera.targetTexture;

        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();

            var texture = new Texture2D(TilePixels, TilePixels, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, TilePixels, TilePixels), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static string ProjectRootPath()
    {
        return Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(ProjectRootPath(), relativePath);
    }
}
#endif
