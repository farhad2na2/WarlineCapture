#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureAssetTrueTargetRenderer
{
    private const int Width = 1920;
    private const int Height = 1080;
    private const string OutputRoot = "Design2/VisualReferences/AssetTrueTargets";

    [MenuItem("WarlineCapture/Design2/Capture Asset-True Targets")]
    public static void RenderInitialAssetTrueTargets()
    {
        RenderTarget(
            "VR-01_TownHallSkirmish",
            BuildTownHallSkirmish,
            new Vector3(18f, 22f, -24f),
            new Vector3(0f, 0f, 0f),
            18f);

        RenderTarget(
            "VR-02_ForwardBaseDefense",
            BuildForwardBaseDefense,
            new Vector3(19f, 23f, -25f),
            new Vector3(0f, 0f, 1f),
            18f);

        RenderTarget(
            "VR-03_RoadCheckpointAmbush",
            BuildRoadCheckpointAmbush,
            new Vector3(18f, 22f, -25f),
            new Vector3(0f, 0f, 0f),
            18f);

        AssetDatabase.Refresh();
        Debug.Log("WarlineCapture Design2 asset-true target capture complete.");
    }

    public static void InspectPolygonMilitaryDemoScene()
    {
        EditorSceneManager.OpenScene("Assets/PolygonMilitary/Scenes/Demo.unity", OpenSceneMode.Single);

        var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        foreach (var renderer in renderers)
        {
            var name = renderer.gameObject.name;
            if (name.Contains("Tent", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Barracks", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("GuardTower", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Barrier_Base", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("RazorWire", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Vehicle", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Veh_", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("DirtRoad", StringComparison.OrdinalIgnoreCase))
            {
                var p = renderer.bounds.center;
                var s = renderer.bounds.size;
                Debug.Log($"DEMO_OBJECT name={name} pos=({p.x:F1},{p.y:F1},{p.z:F1}) size=({s.x:F1},{s.y:F1},{s.z:F1}) path={HierarchyPath(renderer.transform)}");
            }
        }
    }

    public static void RenderForwardBaseDefenseFromDemoScene()
    {
        EditorSceneManager.OpenScene("Assets/PolygonMilitary/Scenes/Demo.unity", OpenSceneMode.Single);

        var cameraObject = new GameObject("Design2 Demo ForwardBaseDefense Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.orthographic = true;
        camera.orthographicSize = 34f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 600f;
        camera.transform.position = new Vector3(88f, 62f, 108f);
        camera.transform.LookAt(new Vector3(30f, 2f, 178f));

        Render(camera, TargetPath("VR-02_ForwardBaseDefense"));
        UnityEngine.Object.DestroyImmediate(cameraObject);
        AssetDatabase.Refresh();
        Debug.Log("WarlineCapture Design2 demo-scene ForwardBaseDefense capture complete.");
    }

    public static void BuildAndRenderForwardBaseDefenseArtTarget()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.58f, 0.56f, 0.52f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.55f, 0.53f, 0.49f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 65f;
        RenderSettings.fogEndDistance = 190f;
        RenderSettings.skybox = null;

        BuildForwardBaseDefenseArtTargetScene();

        var sunObject = new GameObject("Design2 Art Target Sun");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.color = new Color(1f, 0.9f, 0.76f);
        sun.shadows = LightShadows.Soft;
        sunObject.transform.rotation = Quaternion.Euler(44f, -36f, 0f);

        var rimObject = new GameObject("Design2 Art Target Cool Rim");
        var rim = rimObject.AddComponent<Light>();
        rim.type = LightType.Directional;
        rim.intensity = 0.72f;
        rim.color = new Color(0.48f, 0.62f, 0.9f);
        rimObject.transform.rotation = Quaternion.Euler(28f, 136f, 0f);

        var scenePath = Path.Combine(ProjectRootPath(), "Assets/Game/Scenes/DesignTargets/ForwardBaseDefense_ArtTarget.unity");
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);

        var cameraObject = new GameObject("Design2 ForwardBaseDefense Art Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.52f, 0.58f, 0.62f);
        camera.orthographic = true;
        camera.orthographicSize = 20f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 600f;
        camera.transform.position = new Vector3(36f, 33f, -38f);
        camera.transform.LookAt(new Vector3(0f, 0f, -4f));

        Render(camera, TargetPath("VR-02_ForwardBaseDefense"));
        UnityEngine.Object.DestroyImmediate(cameraObject);
        AssetDatabase.Refresh();
        Debug.Log("WarlineCapture Design2 authored ForwardBaseDefense art target capture complete.");
    }

    private static void RenderTarget(string targetName, Action buildScene, Vector3 cameraPosition, Vector3 focus, float orthoSize)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.67f, 0.72f);
        RenderSettings.skybox = null;

        buildScene();

        var sunObject = new GameObject("Design2 Capture Sun");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.45f;
        sun.color = new Color(1f, 0.92f, 0.78f);
        sunObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

        var fillObject = new GameObject("Design2 Capture Fill");
        var fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.42f;
        fill.color = new Color(0.55f, 0.68f, 0.95f);
        fillObject.transform.rotation = Quaternion.Euler(35f, 130f, 0f);

        var cameraObject = new GameObject("Design2 Capture Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.54f, 0.67f, 0.78f);
        camera.orthographic = true;
        camera.orthographicSize = orthoSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 500f;
        camera.transform.position = cameraPosition;
        camera.transform.LookAt(focus);

        Render(camera, TargetPath(targetName));
        UnityEngine.Object.DestroyImmediate(cameraObject);
    }

    private static void BuildTownHallSkirmish()
    {
        AddGroundGrid(4, 4, 8f);
        AddRoadCross();

        Place(Building("SM_Bld_Hall_01"), new Vector3(-5f, 0f, 4f), 35f, 1.15f);
        Place(Building("SM_Bld_Shop_01"), new Vector3(7f, 0f, 5f), -35f, 0.95f);
        Place(Building("SM_Bld_Shop_02"), new Vector3(11f, 0f, 0f), 215f, 0.95f);
        Place(Building("SM_Bld_Village_House_01"), new Vector3(-11f, 0f, -5f), 25f, 1f);
        Place(Building("SM_Bld_Village_House_03"), new Vector3(8f, 0f, -8f), -20f, 1f);
        Place(Building("SM_Bld_Village_ClothCover_Large_01"), new Vector3(3f, 0f, 9f), 10f, 1f);

        Place(Prop("Military/SM_Prop_Road_Barrier_01"), new Vector3(-1.5f, 0f, -1f), 90f, 1.1f);
        Place(Prop("Military/SM_Prop_Road_Barrier_01"), new Vector3(2f, 0f, -1f), 90f, 1.1f);
        Place(Prop("SM_Prop_Barrier_Tall_Group_01"), new Vector3(5f, 0f, 2.5f), 15f, 1f);

        Place(Vehicle("SM_Veh_Light_Armored_Car_01"), new Vector3(-8f, 0f, -8f), 35f, 1f);
        Place(Character("SM_Chr_Soldier_Male_01"), new Vector3(-4f, 0f, -4.5f), 35f, 1.25f);
        Place(Character("SM_Chr_Soldier_Female_01_Alt_01"), new Vector3(-2.5f, 0f, -5.5f), 30f, 1.25f);
        Place(Character("SM_Chr_Contractor_Male_01"), new Vector3(-6f, 0f, -3.8f), 45f, 1.2f);
        Place(Character("SM_Chr_Insurgent_Male_01"), new Vector3(7f, 0f, 1.8f), 220f, 1.2f);
        Place(Character("SM_Chr_Insurgent_Female_01"), new Vector3(9f, 0f, -1.2f), 235f, 1.2f);
        Place(Character("SM_Chr_Civilian_Female_01"), new Vector3(-12f, 0f, 1f), 70f, 1.15f);
        Place(Character("SM_Chr_Civilian_Male_01"), new Vector3(-10.5f, 0f, 2.5f), 70f, 1.15f);
    }

    private static void BuildForwardBaseDefense()
    {
        AddGroundGrid(5, 4, 8f);
        AddDirtRoad(new Vector3(0f, 0.02f, -6f), 90f, 4);

        Place(Building("SM_Bld_Barracks_01"), new Vector3(-7f, 0f, 3f), 25f, 1.05f);
        Place(Building("SM_Bld_Tent_01"), new Vector3(1f, 0f, 5.5f), -20f, 1f);
        Place(Building("SM_Bld_Tent_Sandbags_01"), new Vector3(6f, 0f, 3f), 20f, 1f);
        Place(Building("SM_Bld_CamoNet_Tent_01"), new Vector3(3f, 0f, 9f), 0f, 1f);
        Place(Building("SM_Bld_GuardTower_01"), new Vector3(-12f, 0f, -2f), 45f, 1f);
        Place(Building("SM_Bld_GuardTower_02"), new Vector3(11f, 0f, -1.5f), -45f, 1f);

        AddBarrierLine(-13f, 0f, 5, 0f);
        AddBarrierLine(13f, 0f, 5, 180f);
        AddFenceLine(new Vector3(-7f, 0f, -6f), 0f, 5);
        AddFenceLine(new Vector3(5f, 0f, -6f), 0f, 4);

        Place(Prop("Military/SM_Prop_Fuel_Bladder_01"), new Vector3(-2f, 0f, 10f), 20f, 1f);
        Place(Prop("Military/SM_Prop_Crate_Stack_01"), new Vector3(-5f, 0f, 8f), 0f, 1f);
        Place(Vehicle("SM_Veh_APC_01"), new Vector3(-3.5f, 0f, -2f), 20f, 1f);
        Place(Vehicle("SM_Veh_Tank_USA_01"), new Vector3(5.5f, 0f, -2.5f), -25f, 0.95f);

        Place(Character("SM_Chr_Soldier_Male_02"), new Vector3(-1f, 0f, 1f), 190f, 1.2f);
        Place(Character("SM_Chr_Soldier_Female_02"), new Vector3(1f, 0f, 0.5f), 185f, 1.2f);
        Place(Character("SM_Chr_Soldier_Male_01_Alt_02"), new Vector3(3f, 0f, 0f), 170f, 1.2f);
        Place(Character("SM_Chr_Insurgent_Male_04"), new Vector3(-9f, 0f, -11f), 25f, 1.2f);
        Place(Character("SM_Chr_Insurgent_Male_05"), new Vector3(8f, 0f, -11f), -25f, 1.2f);
    }

    private static void BuildRoadCheckpointAmbush()
    {
        AddGroundGrid(5, 4, 8f);
        AddDirtRoad(new Vector3(0f, 0.03f, 0f), 0f, 5);

        Place(Building("SM_Bld_Shop_04"), new Vector3(-9f, 0f, 4.8f), 35f, 1f);
        Place(Building("SM_Bld_Shop_07"), new Vector3(8f, 0f, 5.5f), -25f, 1f);
        Place(Building("SM_Bld_Village_House_02"), new Vector3(-11f, 0f, -6f), 145f, 1f);
        Place(Building("SM_Bld_Village_House_05"), new Vector3(10f, 0f, -6f), -140f, 1f);
        Place(Building("SM_Bld_GuardTower_01"), new Vector3(-5f, 0f, -3.5f), 30f, 1f);

        Place(Prop("Military/SM_Prop_Road_Barrier_01"), new Vector3(-1.6f, 0f, -1f), 90f, 1.15f);
        Place(Prop("Military/SM_Prop_Road_Barrier_01"), new Vector3(1.8f, 0f, -1f), 90f, 1.15f);
        Place(Prop("SM_Prop_Barrier_Tall_Group_02"), new Vector3(-5f, 0f, 2f), 20f, 1f);
        Place(Prop("SM_Prop_Barrier_Tall_Group_03"), new Vector3(5f, 0f, 2f), -25f, 1f);

        Place(Vehicle("SM_Veh_APC_02"), new Vector3(-1f, 0f, -7.5f), 0f, 1f);
        Place(Vehicle("SM_Veh_Truck_01_Canopy"), new Vector3(0f, 0f, 0.5f), 0f, 1f);
        Place(Vehicle("SM_Veh_Truck_01_Tray"), new Vector3(0f, 0f, 7f), 0f, 1f);

        Place(Character("SM_Chr_Soldier_Male_01"), new Vector3(-3.5f, 0f, -4f), 20f, 1.2f);
        Place(Character("SM_Chr_Soldier_Female_01_Alt_02"), new Vector3(3.2f, 0f, -3.8f), -20f, 1.2f);
        Place(Character("SM_Chr_Insurgent_Male_02"), new Vector3(-8f, 0f, 1f), 115f, 1.2f);
        Place(Character("SM_Chr_Insurgent_Male_03"), new Vector3(8f, 0f, 1.5f), -115f, 1.2f);
        Place(Character("SM_Chr_Insurgent_Female_02"), new Vector3(9.5f, 0f, 8f), -145f, 1.2f);
        Place(Character("SM_Chr_Civilian_Male_02"), new Vector3(-12f, 0f, 7f), 55f, 1.15f);
    }

    private static void BuildForwardBaseDefenseArtTargetScene()
    {
        const float yaw = -28f;

        AddArtGround(yaw);
        AddArtRoads(yaw);
        AddArtPerimeter(yaw);
        AddArtFobCore(yaw);
        AddArtVehicles(yaw);
        AddArtInfantry(yaw);
        AddArtDressing(yaw);
    }

    private static void AddArtGround(float yaw)
    {
        for (var x = -6; x <= 6; x++)
        {
            for (var z = -6; z <= 5; z++)
            {
                var prefab = Math.Abs(x + z) % 2 == 0 ? "SM_Env_Ground_Square_01" : "SM_Env_Ground_Square_02";
                Place(Environment(prefab), new Vector3(x * 8f, -0.08f, z * 8f), yaw + ((x + z) % 2) * 90f, 1.08f);
            }
        }

        for (var i = 0; i < 10; i++)
        {
            var x = -30f + i * 6.5f;
            Place(Environment("SM_Env_SandEdge_01"), ArtPoint(new Vector3(x, 0f, -25f), yaw), yaw + 20f, 0.85f);
            Place(Environment("SM_Env_SandEdge_04"), ArtPoint(new Vector3(x - 2f, 0f, 20f), yaw), yaw + 200f, 0.85f);
        }
    }

    private static void AddArtRoads(float yaw)
    {
        for (var i = -5; i <= 5; i++)
        {
            Place(Environment("SM_Env_DirtRoad_Straight_01"), ArtPoint(new Vector3(i * 7f, 0.02f, -16f), yaw), yaw + 90f, 1.06f);
        }

        for (var i = -3; i <= 4; i++)
        {
            Place(Environment("SM_Env_DirtRoad_Straight_01"), ArtPoint(new Vector3(21f, 0.03f, i * 7f), yaw), yaw, 1.02f);
        }

        Place(Environment("SM_Env_DirtRoad_Corner_01"), ArtPoint(new Vector3(21f, 0.04f, -16f), yaw), yaw + 180f, 1.05f);
        Place(Environment("SM_Env_DirtRoad_Exit_01"), ArtPoint(new Vector3(-33f, 0.04f, -16f), yaw), yaw + 90f, 1.05f);
        Place(Environment("SM_Env_DirtRoad_Exit_02"), ArtPoint(new Vector3(38f, 0.04f, -16f), yaw), yaw - 90f, 1.05f);
    }

    private static void AddArtPerimeter(float yaw)
    {
        AddArtBarrierLine(new Vector3(-24f, 0f, -7f), new Vector3(24f, 0f, -7f), yaw, "Military/SM_Prop_Barrier_Base_Row_03", 9);
        AddArtBarrierLine(new Vector3(-24f, 0f, 14f), new Vector3(24f, 0f, 14f), yaw, "Military/SM_Prop_Barrier_Base_Row_03", 9);
        AddArtBarrierLine(new Vector3(-27f, 0f, -5f), new Vector3(-27f, 0f, 13f), yaw + 90f, "Military/SM_Prop_Barrier_Base_Row_02", 4);
        AddArtBarrierLine(new Vector3(27f, 0f, -5f), new Vector3(27f, 0f, 13f), yaw + 90f, "Military/SM_Prop_Barrier_Base_Row_02", 4);

        AddArtBarrierLine(new Vector3(-22f, 0.1f, -9f), new Vector3(22f, 0.1f, -9f), yaw + 90f, "Military/SM_Prop_RazorWire_01", 9);
        AddArtBarrierLine(new Vector3(-22f, 0.1f, 17f), new Vector3(22f, 0.1f, 17f), yaw + 90f, "Military/SM_Prop_RazorWire_01", 9);

        Place(Building("SM_Bld_GuardTower_01"), ArtPoint(new Vector3(-26f, 0f, -8f), yaw), yaw + 40f, 1.08f);
        Place(Building("SM_Bld_GuardTower_02"), ArtPoint(new Vector3(26f, 0f, -8f), yaw), yaw - 40f, 1.08f);
        Place(Building("SM_Bld_GuardTower_01"), ArtPoint(new Vector3(-26f, 0f, 15f), yaw), yaw + 135f, 1.08f);
        Place(Building("SM_Bld_GuardTower_02"), ArtPoint(new Vector3(26f, 0f, 15f), yaw), yaw - 135f, 1.08f);

        Place(Prop("Military/SM_Prop_Barrier_Base_Group_04"), ArtPoint(new Vector3(-4f, 0f, -9.8f), yaw), yaw + 90f, 1f);
        Place(Prop("Military/SM_Prop_Barrier_Base_Group_03"), ArtPoint(new Vector3(6f, 0f, -9.8f), yaw), yaw + 90f, 1f);
    }

    private static void AddArtFobCore(float yaw)
    {
        Place(Building("SM_Bld_Barracks_01"), ArtPoint(new Vector3(-19f, 0f, 2f), yaw), yaw + 90f, 1.04f);
        Place(Building("SM_Bld_Barracks_01"), ArtPoint(new Vector3(18f, 0f, 1f), yaw), yaw - 90f, 1.04f);

        Place(Building("SM_Bld_Tent_01"), ArtPoint(new Vector3(-12f, 0f, 9f), yaw), yaw + 90f, 1f);
        Place(Building("SM_Bld_Tent_Desert_01"), ArtPoint(new Vector3(-4f, 0f, 9f), yaw), yaw + 90f, 1f);
        Place(Building("SM_Bld_Tent_Sandbags_01"), ArtPoint(new Vector3(4f, 0f, 9f), yaw), yaw + 90f, 1f);
        Place(Building("SM_Bld_Tent_Open_01"), ArtPoint(new Vector3(12f, 0f, 9f), yaw), yaw + 90f, 1f);
        Place(Building("SM_Bld_CamoNet_Tent_01"), ArtPoint(new Vector3(-11f, 0f, -2f), yaw), yaw + 20f, 1f);
        Place(Building("SM_Bld_CamoNet_Tent_03"), ArtPoint(new Vector3(11f, 0f, -2f), yaw), yaw - 20f, 1f);

        Place(Prop("Military/SM_Prop_Fuel_Bladder_01"), ArtPoint(new Vector3(20f, 0f, 10f), yaw), yaw + 15f, 1f);
        Place(Prop("Military/SM_Prop_Fuel_Bladder_02"), ArtPoint(new Vector3(24f, 0f, 8f), yaw), yaw + 10f, 1f);
        Place(Building("SM_Bld_WaterTank_01"), ArtPoint(new Vector3(-23f, 0f, 10f), yaw), yaw, 0.95f);
        Place(Prop("SM_Prop_Generator_Large_01"), ArtPoint(new Vector3(-21f, 0f, -3f), yaw), yaw - 30f, 1f);
        Place(Prop("Military/SM_Prop_Crate_Stack_01"), ArtPoint(new Vector3(-17f, 0f, -4f), yaw), yaw + 12f, 1f);
        Place(Prop("Military/SM_Prop_Crate_Stack_Cover_02"), ArtPoint(new Vector3(17f, 0f, -4f), yaw), yaw - 18f, 1f);
        Place(Prop("Military/SM_Prop_AmmoBox_02"), ArtPoint(new Vector3(13.5f, 0f, -5.5f), yaw), yaw + 20f, 1f);
        Place(Prop("Military/SM_Prop_Missle_Crate_01"), ArtPoint(new Vector3(20f, 0f, -5f), yaw), yaw - 15f, 1f);
    }

    private static void AddArtVehicles(float yaw)
    {
        Place(Vehicle("SM_Veh_Tank_USA_01"), ArtPoint(new Vector3(-10f, 0f, -20f), yaw), yaw + 18f, 1f);
        Place(Vehicle("SM_Veh_APC_Heavy_01"), ArtPoint(new Vector3(2f, 0f, -20f), yaw), yaw + 8f, 1f);
        Place(Vehicle("SM_Veh_APC_01"), ArtPoint(new Vector3(14f, 0f, -19f), yaw), yaw - 10f, 1f);
        Place(Vehicle("SM_Veh_Truck_01_Canopy"), ArtPoint(new Vector3(-23f, 0f, -15f), yaw), yaw + 88f, 1f);
        Place(Vehicle("SM_Veh_Radar_Tank_01"), ArtPoint(new Vector3(24f, 0f, -13f), yaw), yaw - 20f, 0.95f);
        Place(Vehicle("SM_Veh_Light_Armored_Car_01"), ArtPoint(new Vector3(-32f, 0f, -20f), yaw), yaw + 70f, 1f);
        Place(Vehicle("Destroyed/SM_Veh_APC_01_Destroyed"), ArtPoint(new Vector3(33f, 0f, 17f), yaw), yaw - 40f, 1f);
    }

    private static void AddArtInfantry(float yaw)
    {
        var soldierPositions = new[]
        {
            new Vector3(-16f, 0f, -12f), new Vector3(-13f, 0f, -10.5f),
            new Vector3(-4f, 0f, -12f), new Vector3(-1f, 0f, -10f),
            new Vector3(8f, 0f, -11.5f), new Vector3(11f, 0f, -10.5f),
            new Vector3(22f, 0f, -7f), new Vector3(-24f, 0f, 6f)
        };

        for (var i = 0; i < soldierPositions.Length; i++)
        {
            var prefab = i % 2 == 0 ? "SM_Chr_Soldier_Male_02_Alt_04" : "SM_Chr_Soldier_Female_02_Alt_02";
            Place(Character(prefab), ArtPoint(soldierPositions[i], yaw), yaw + 160f - i * 11f, 1.75f);
        }

        Place(Character("SM_Chr_Contractor_Male_02"), ArtPoint(new Vector3(-19f, 0f, -6f), yaw), yaw + 130f, 1.6f);
        Place(Character("SM_Chr_Leader_Male_01"), ArtPoint(new Vector3(-2f, 0f, -6f), yaw), yaw + 155f, 1.65f);
        Place(Character("SM_Chr_Insurgent_Male_04"), ArtPoint(new Vector3(35f, 0f, 12f), yaw), yaw - 130f, 1.55f);
        Place(Character("SM_Chr_Insurgent_Male_05"), ArtPoint(new Vector3(36.5f, 0f, 17f), yaw), yaw - 135f, 1.55f);
    }

    private static void AddArtDressing(float yaw)
    {
        var props = new[]
        {
            ("Military/SM_Prop_Crate_Cube_01", -26f, -3f, 0f),
            ("Military/SM_Prop_Crate_Cube_02", -24f, -1f, 20f),
            ("Military/SM_Prop_Crate_Plastic_03", -18f, -11f, -18f),
            ("SM_Prop_Pallet_03", -18f, -8f, 35f),
            ("SM_Prop_WireSpool_01", -15f, -7f, -20f),
            ("SM_Prop_PowerBox_06", 15f, -6f, -20f),
            ("SM_Prop_Generator_Small_01", 22f, -5f, -15f),
            ("Debris/SM_Prop_Rubble_Pile_02", 31f, 6f, 60f),
            ("Debris/SM_Prop_Vehicle_Debris_03", 27f, 18f, 25f),
            ("SM_Prop_Lamp_01", -29f, -9f, 0f),
            ("SM_Prop_Lamp_02", 29f, -9f, 0f),
            ("SM_Prop_Wire_Lights_01", 0f, 15.5f, 0f)
        };

        foreach (var (name, x, z, rotation) in props)
        {
            Place(Prop(name), ArtPoint(new Vector3(x, 0f, z), yaw), yaw + rotation, 1f);
        }

        for (var i = 0; i < 18; i++)
        {
            var x = -34f + (i * 4.1f) % 68f;
            var z = i % 2 == 0 ? -25f - (i % 3) : 22f + (i % 4);
            var prefab = i % 3 == 0 ? "SM_Env_Crater_01" : i % 3 == 1 ? "SM_Env_SandEdge_02" : "SM_Env_SandEdge_06";
            Place(Environment(prefab), ArtPoint(new Vector3(x, 0f, z), yaw), yaw + i * 19f, 0.75f + (i % 4) * 0.08f);
        }

        for (var i = 0; i < 16; i++)
        {
            var x = -36f + i * 4.8f;
            var z = i % 2 == 0 ? 27f : -28f;
            Place(Environment(i % 2 == 0 ? "SM_Env_SandEdge_03" : "SM_Env_SandEdge_05"), ArtPoint(new Vector3(x, 0.01f, z), yaw), yaw + i * 17f, 0.85f);
        }

        Place(Fx("FX_Smoke_Medium_01"), ArtPoint(new Vector3(33f, 0f, 17f), yaw), yaw, 0.8f);
        Place(Fx("FX_Dust_Blowing_Soft_Large_01"), ArtPoint(new Vector3(8f, 0f, -17f), yaw), yaw, 1f);
    }

    private static void AddArtBarrierLine(Vector3 start, Vector3 end, float yaw, string propName, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = count == 1 ? 0.5f : i / (float)(count - 1);
            var local = Vector3.Lerp(start, end, t);
            Place(Prop(propName), ArtPoint(local, yaw), yaw, 1f);
        }
    }

    private static Vector3 ArtPoint(Vector3 local, float yaw)
    {
        return Quaternion.Euler(0f, yaw, 0f) * local;
    }

    private static void AddGroundGrid(int width, int depth, float spacing)
    {
        for (var x = 0; x < width; x++)
        {
            for (var z = 0; z < depth; z++)
            {
                var px = (x - (width - 1) * 0.5f) * spacing;
                var pz = (z - (depth - 1) * 0.5f) * spacing;
                Place(Environment("SM_Env_Ground_Square_01"), new Vector3(px, -0.05f, pz), 0f, 1.02f);
            }
        }
    }

    private static void AddRoadCross()
    {
        AddDirtRoad(new Vector3(0f, 0.03f, 0f), 0f, 4);
        AddDirtRoad(new Vector3(0f, 0.04f, 0f), 90f, 4);
    }

    private static void AddPavedRoad(Vector3 center, float yaw, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var offset = (i - (count - 1) * 0.5f) * 7.5f;
            var local = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, offset);
            Place(Environment("SM_Env_Road_01"), center + local, yaw, 1f);
        }
    }

    private static void AddDirtRoad(Vector3 center, float yaw, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var offset = (i - (count - 1) * 0.5f) * 7.5f;
            var local = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, offset);
            Place(Environment("SM_Env_DirtRoad_Straight_01"), center + local, yaw, 1f);
        }
    }

    private static void AddBarrierLine(float x, float z, int count, float yaw)
    {
        for (var i = 0; i < count; i++)
        {
            Place(Prop("Military/SM_Prop_Barrier_Base_Row_01"), new Vector3(x, 0f, z + (i - 2) * 3f), yaw, 1f);
        }
    }

    private static void AddFenceLine(Vector3 start, float yaw, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var local = Quaternion.Euler(0f, yaw, 0f) * new Vector3(i * 3.2f, 0f, 0f);
            Place(Prop("SM_Prop_Fence_01"), start + local, yaw, 1f);
        }
    }

    private static GameObject Place(string assetPath, Vector3 position, float yaw, float scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Missing prefab for Design2 asset-true capture: {assetPath}");
            return null;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
        if (instance == null)
        {
            return null;
        }

        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localScale = Vector3.one * scale;
        return instance;
    }

    private static string Building(string name)
    {
        return $"Assets/PolygonMilitary/Prefabs/Buildings/{name}.prefab";
    }

    private static string Character(string name)
    {
        return $"Assets/PolygonMilitary/Prefabs/Characters/{name}.prefab";
    }

    private static string Vehicle(string name)
    {
        return $"Assets/PolygonMilitary/Prefabs/Vehicles/{name}.prefab";
    }

    private static string Environment(string name)
    {
        return $"Assets/PolygonMilitary/Prefabs/Environment/{name}.prefab";
    }

    private static string Fx(string name)
    {
        return $"Assets/PolygonMilitary/Prefabs/FX/{name}.prefab";
    }

    private static string Prop(string relativeName)
    {
        return $"Assets/PolygonMilitary/Prefabs/Props/{relativeName}.prefab";
    }

    private static string TargetPath(string targetName)
    {
        var root = Path.GetFullPath(Path.Combine(ProjectRootPath(), OutputRoot, targetName));
        Directory.CreateDirectory(root);
        return Path.Combine(root, $"{targetName}_UnityTarget.png");
    }

    private static string ProjectRootPath()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var projectName = Path.GetFileName(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        if (projectName == "WarlineCapture-CodexUnity" || projectName == "WarlineCapture-CodexUnity2")
        {
            var siblingMainProject = Path.Combine(Directory.GetParent(projectRoot).FullName, "WarlineCapture");
            if (Directory.Exists(Path.Combine(siblingMainProject, "Design2")))
            {
                return siblingMainProject;
            }
        }

        return projectRoot;
    }

    private static string HierarchyPath(Transform transform)
    {
        var path = transform.name;
        var parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static void Render(Camera camera, string outputPath)
    {
        var descriptor = new RenderTextureDescriptor(Width, Height, RenderTextureFormat.ARGB32, 24)
        {
            msaaSamples = 4
        };

        var texture = new RenderTexture(descriptor);
        var previousActive = RenderTexture.active;
        var previousTarget = camera.targetTexture;

        camera.targetTexture = texture;
        RenderTexture.active = texture;
        camera.Render();

        var screenshot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        screenshot.Apply();

        File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());
        Debug.Log($"Wrote Design2 asset-true target: {outputPath}");

        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        UnityEngine.Object.DestroyImmediate(screenshot);
        texture.Release();
        UnityEngine.Object.DestroyImmediate(texture);
    }
}
#endif
