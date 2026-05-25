#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WarlineCaptureGc16WalkabilityContractBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/Generated/GC16_WalkabilityContract_2048.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC16_WalkabilityContract_2048";
    private const string ReportPath = "Design/AgentReports/2026-05-21_gameplay_gc16-walkability-contract.md";
    private const float MapSize = 2048f;

    private static readonly List<Zone> MacroRoads = new();
    private static readonly List<Zone> SoldierWalkable = new();
    private static readonly List<Zone> VehicleWalkable = new();
    private static readonly List<Zone> SoldierBlockers = new();
    private static readonly List<Zone> VehicleBlockers = new();
    private static readonly List<Connection> Connections = new();
    private static readonly List<Route> SoldierRoutes = new();
    private static readonly List<Route> VehicleRoutes = new();
    private static readonly List<string> ValidationLog = new();

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

    private readonly struct Connection
    {
        public readonly string Name;
        public readonly Vector3 Position;
        public readonly string LocalZone;
        public readonly string RoadZone;
        public readonly bool VehicleAllowed;

        public Connection(string name, Vector3 position, string localZone, string roadZone, bool vehicleAllowed)
        {
            Name = name;
            Position = position;
            LocalZone = localZone;
            RoadZone = roadZone;
            VehicleAllowed = vehicleAllowed;
        }
    }

    private readonly struct Route
    {
        public readonly string Name;
        public readonly Vector3[] Points;

        public Route(string name, params Vector3[] points)
        {
            Name = name;
            Points = points;
        }
    }

    [MenuItem("WarlineCapture/Design/Build GC16 Walkability Contract 2048")]
    public static void BuildGc16WalkabilityContract2048()
    {
        MacroRoads.Clear();
        SoldierWalkable.Clear();
        VehicleWalkable.Clear();
        SoldierBlockers.Clear();
        VehicleBlockers.Clear();
        Connections.Clear();
        SoldierRoutes.Clear();
        VehicleRoutes.Clear();
        ValidationLog.Clear();

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        GameObject root = new("GC16_WalkabilityContract_2048_Root");
        BuildEnvironment(root);
        DefineMacroRoads();
        DefineLocalWalkability();
        DefineRoutes();
        BuildProofSurfaces(root);
        BuildCameras(root);
        ValidateContract();

        EditorSceneManager.SaveScene(scene, ScenePath);
        CaptureScene();
        WriteReport();
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_GC16_WALKABILITY_CONTRACT_BUILT scene={ScenePath} report={ReportPath}");
        EditorApplication.Exit(0);
    }

    private static void DefineMacroRoads()
    {
        AddMacroRoad("MacroRoad_MainNorthSouth_West", 258f, 0f, 60f, 890f);
        AddMacroRoad("MacroRoad_MainNorthSouth_East", 440f, 0f, 58f, 890f);
        AddMacroRoad("MacroRoad_NorthCityToAirfield", 62f, 224f, 766f, 54f);
        AddMacroRoad("MacroRoad_CentralCityToCommand", 130f, 426f, 686f, 54f);
        AddMacroRoad("MacroRoad_SouthPlayerToFuel", 38f, 578f, 790f, 86f);
    }

    private static void DefineLocalWalkability()
    {
        AddCity("CityCore_NorthWest", 58f, 68f, 142f, 136f, "MacroRoad_NorthCityToAirfield", true);
        AddCity("CityCentral_WestInner", 304f, 84f, 108f, 118f, "MacroRoad_NorthCityToAirfield", false);
        AddCity("CityMarket_West", 102f, 320f, 150f, 136f, "MacroRoad_CentralCityToCommand", true);
        AddCity("CitySouth_West", 76f, 714f, 150f, 136f, "MacroRoad_SouthPlayerToFuel", true);
        AddCamp("SouthGate_PlayerCamp", 64f, 686f, 94f, 102f, "MacroRoad_SouthPlayerToFuel", true);
        AddCamp("CentralTentBarracks", 310f, 500f, 104f, 96f, "MacroRoad_SouthPlayerToFuel", true);
        AddCamp("WestTentBarracks", 350f, 724f, 102f, 102f, "MacroRoad_SouthPlayerToFuel", true);
        AddCamp("Airfield_Apron", 718f, 114f, 118f, 148f, "MacroRoad_NorthCityToAirfield", true);
        AddCamp("CommandDepot_CentralEast", 548f, 478f, 114f, 94f, "MacroRoad_CentralCityToCommand", true);
        AddCamp("VehicleYard_NorthEast", 596f, 286f, 88f, 82f, "MacroRoad_CentralCityToCommand", true);
        AddCamp("VehicleYard_SouthEast", 656f, 666f, 112f, 112f, "MacroRoad_SouthPlayerToFuel", true);
        AddCamp("FuelUtility_East", 754f, 558f, 72f, 104f, "MacroRoad_SouthPlayerToFuel", true);
    }

    private static void AddCity(string name, float x, float y, float width, float height, string roadZone, bool vehiclePocket)
    {
        Rect outer = BlueprintRect(x, y, width, height);
        Zone local = new(name + "_SoldierLocalWalk", outer);
        SoldierWalkable.Add(local);

        float lane = Mathf.Min(width, height) * 0.22f;
        SoldierWalkable.Add(new Zone(name + "_Alley_NorthSouth", BlueprintRect(x + width * 0.43f, y + height * 0.08f, lane, height * 0.84f)));
        SoldierWalkable.Add(new Zone(name + "_Alley_EastWest", BlueprintRect(x + width * 0.08f, y + height * 0.44f, width * 0.84f, lane)));
        SoldierBlockers.Add(new Zone(name + "_BuildingBlock_NW", BlueprintRect(x + width * 0.08f, y + height * 0.08f, width * 0.28f, height * 0.30f)));
        SoldierBlockers.Add(new Zone(name + "_BuildingBlock_NE", BlueprintRect(x + width * 0.62f, y + height * 0.08f, width * 0.28f, height * 0.30f)));
        SoldierBlockers.Add(new Zone(name + "_BuildingBlock_SW", BlueprintRect(x + width * 0.08f, y + height * 0.62f, width * 0.28f, height * 0.28f)));
        SoldierBlockers.Add(new Zone(name + "_BuildingBlock_SE", BlueprintRect(x + width * 0.62f, y + height * 0.62f, width * 0.28f, height * 0.28f)));

        if (vehiclePocket)
        {
            VehicleWalkable.Add(new Zone(name + "_VehiclePocket", BlueprintRect(x + width * 0.35f, y + height * 0.36f, width * 0.30f, height * 0.28f)));
        }

        AddConnection(name, BlueprintPoint(x + width * 0.5f, y + height + 3f), local.Name, roadZone, vehiclePocket);
    }

    private static void AddCamp(string name, float x, float y, float width, float height, string roadZone, bool vehicleAllowed)
    {
        Rect yard = BlueprintRect(x, y, width, height);
        SoldierWalkable.Add(new Zone(name + "_SoldierYard", yard));
        SoldierWalkable.Add(new Zone(name + "_InternalFootLane", BlueprintRect(x + width * 0.12f, y + height * 0.42f, width * 0.76f, height * 0.18f)));
        SoldierBlockers.Add(new Zone(name + "_TentOrPropBlock_NW", BlueprintRect(x + width * 0.08f, y + height * 0.10f, width * 0.30f, height * 0.26f)));
        SoldierBlockers.Add(new Zone(name + "_TentOrPropBlock_SE", BlueprintRect(x + width * 0.60f, y + height * 0.62f, width * 0.30f, height * 0.26f)));

        if (vehicleAllowed)
        {
            VehicleWalkable.Add(new Zone(name + "_VehicleYard", BlueprintRect(x + width * 0.16f, y + height * 0.20f, width * 0.68f, height * 0.58f)));
            VehicleBlockers.Add(new Zone(name + "_VehicleStaticPropBlock", BlueprintRect(x + width * 0.38f, y + height * 0.34f, width * 0.24f, height * 0.22f)));
        }

        AddConnection(name, BlueprintPoint(x + width * 0.5f, y - 3f), name + "_SoldierYard", roadZone, vehicleAllowed);
    }

    private static void AddConnection(string prefix, Vector3 position, string localZone, string roadZone, bool vehicleAllowed)
    {
        Connections.Add(new Connection(prefix + "_RoadConnection", position, localZone, roadZone, vehicleAllowed));
    }

    private static void DefineRoutes()
    {
        SoldierRoutes.Add(new Route("Soldier_PlayerToMarket",
            BlueprintPoint(78f, 642f),
            BlueprintPoint(178f, 626f),
            BlueprintPoint(250f, 606f),
            BlueprintPoint(360f, 452f),
            BlueprintPoint(176f, 392f)));
        SoldierRoutes.Add(new Route("Soldier_MarketToCommand",
            BlueprintPoint(176f, 392f),
            BlueprintPoint(360f, 452f),
            BlueprintPoint(620f, 450f),
            BlueprintPoint(604f, 520f)));
        SoldierRoutes.Add(new Route("Soldier_CommandToAirfield",
            BlueprintPoint(604f, 520f),
            BlueprintPoint(620f, 450f),
            BlueprintPoint(704f, 250f),
            BlueprintPoint(776f, 188f)));
        VehicleRoutes.Add(new Route("Vehicle_PlayerToVehicleYard",
            BlueprintPoint(246f, 606f),
            BlueprintPoint(390f, 552f),
            BlueprintPoint(620f, 502f),
            BlueprintPoint(680f, 720f)));
    }

    private static void BuildProofSurfaces(GameObject root)
    {
        Material baseMat = CreateMaterial("GC16_BaseSand", new Color(0.48f, 0.41f, 0.30f, 1f));
        Material roadMat = CreateMaterial("GC16_MacroRoadWalkable_Blue", new Color(0.05f, 0.22f, 0.95f, 0.82f));
        Material soldierMat = CreateMaterial("GC16_SoldierWalkable_Green", new Color(0.04f, 0.72f, 0.20f, 0.78f));
        Material vehicleMat = CreateMaterial("GC16_VehicleWalkable_Cyan", new Color(0.05f, 0.76f, 0.95f, 0.72f));
        Material blockerMat = CreateMaterial("GC16_Blocker_Red", new Color(0.95f, 0.04f, 0.02f, 0.84f));
        Material connectionMat = CreateMaterial("GC16_Connection_Yellow", new Color(1f, 0.9f, 0.02f, 1f));
        Material soldierRouteMat = CreateMaterial("GC16_SoldierRoute_White", Color.white);
        Material vehicleRouteMat = CreateMaterial("GC16_VehicleRoute_Purple", new Color(0.72f, 0.2f, 1f, 1f));

        Surface(root, "FlatMapBase", Vector3.zero, new Vector2(MapSize, MapSize), baseMat, -0.04f);
        GameObject roads = Child(root, "MacroRoadWalkable_Blue");
        foreach (Zone road in MacroRoads)
            Surface(roads, road.Name, Center(road.Rect, 0.01f), new Vector2(road.Rect.width, road.Rect.height), roadMat, 0.01f);

        GameObject soldier = Child(root, "CityLocalWalkable_Green");
        foreach (Zone zone in SoldierWalkable)
            Surface(soldier, zone.Name, Center(zone.Rect, 0.03f), new Vector2(zone.Rect.width, zone.Rect.height), soldierMat, 0.03f);

        GameObject vehicle = Child(root, "VehicleWalkable_Cyan");
        foreach (Zone zone in VehicleWalkable)
            Surface(vehicle, zone.Name, Center(zone.Rect, 0.045f), new Vector2(zone.Rect.width, zone.Rect.height), vehicleMat, 0.045f);

        GameObject blockers = Child(root, "Blockers_Red");
        foreach (Zone zone in SoldierBlockers.Concat(VehicleBlockers))
            Surface(blockers, zone.Name, Center(zone.Rect, 0.07f), new Vector2(zone.Rect.width, zone.Rect.height), blockerMat, 0.07f);

        GameObject connections = Child(root, "ConnectionPoints_Yellow");
        foreach (Connection connection in Connections)
            Cylinder(connections, connection.Name, connection.Position, connection.VehicleAllowed ? 16f : 11f, connectionMat, 0.11f);

        GameObject routes = Child(root, "SampleRoutes_WhiteSoldier_PurpleVehicle");
        foreach (Route route in SoldierRoutes)
            Polyline(routes, route.Name, route.Points, soldierRouteMat, 7f, 0.13f);
        foreach (Route route in VehicleRoutes)
            Polyline(routes, route.Name, route.Points, vehicleRouteMat, 11f, 0.14f);
    }

    private static void ValidateContract()
    {
        foreach (Connection connection in Connections)
        {
            Zone local = SoldierWalkable.FirstOrDefault(zone => zone.Name == connection.LocalZone);
            Zone road = MacroRoads.FirstOrDefault(zone => zone.Name == connection.RoadZone);
            if (local.Name == null)
                ValidationLog.Add($"ERROR: connection {connection.Name} references missing local zone {connection.LocalZone}.");
            if (road.Name == null)
                ValidationLog.Add($"ERROR: connection {connection.Name} references missing road zone {connection.RoadZone}.");
            if (local.Name != null && road.Name != null && !Expanded(local.Rect, 165f).Overlaps(road.Rect))
                ValidationLog.Add($"ERROR: connection {connection.Name} local zone is not adjacent to macro road {connection.RoadZone}.");
        }

        foreach (Route route in SoldierRoutes)
            ValidateRoute(route, true);
        foreach (Route route in VehicleRoutes)
            ValidateRoute(route, false);

        if (ValidationLog.Count == 0)
            ValidationLog.Add($"PASS: GC16 defines {MacroRoads.Count} macro roads, {SoldierWalkable.Count} soldier walk zones, {VehicleWalkable.Count} vehicle zones, {SoldierBlockers.Count + VehicleBlockers.Count} blocker zones, and {Connections.Count} road-to-local connection points.");
    }

    private static void ValidateRoute(Route route, bool soldier)
    {
        List<Zone> walkables = soldier ? SoldierWalkable.Concat(MacroRoads).ToList() : VehicleWalkable.Concat(MacroRoads).ToList();
        List<Zone> blockers = soldier ? SoldierBlockers : VehicleBlockers.Concat(SoldierBlockers).ToList();
        foreach (Vector3 point in route.Points)
        {
            Vector2 p = new(point.x, point.z);
            if (!walkables.Any(zone => zone.Rect.Contains(p)))
                ValidationLog.Add($"ERROR: {route.Name} point {Format(point)} is not inside {(soldier ? "soldier" : "vehicle")} walkable space.");
            if (blockers.Any(zone => zone.Rect.Contains(p)))
                ValidationLog.Add($"ERROR: {route.Name} point {Format(point)} intersects blocker space.");
        }
    }

    private static void BuildEnvironment(GameObject root)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.78f, 0.78f, 0.72f, 1f);
        Light light = Child(root, "DirectionalLight_Debug").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.9f;
        light.shadows = LightShadows.None;
        light.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static void BuildCameras(GameObject root)
    {
        Camera top = CameraObject(root, "Camera_GC16_TopDownWalkabilityProof");
        top.orthographic = true;
        top.orthographicSize = 1035f;
        top.transform.position = new Vector3(0f, 1600f, 0f);
        top.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera city = CameraObject(root, "Camera_GC16_CityWalkabilityClose");
        city.orthographic = true;
        city.orthographicSize = 250f;
        city.transform.position = BlueprintPoint(168f, 382f) + new Vector3(0f, 760f, 0f);
        city.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera baseView = CameraObject(root, "Camera_GC16_BaseWalkabilityClose");
        baseView.orthographic = true;
        baseView.orthographicSize = 310f;
        baseView.transform.position = BlueprintPoint(624f, 520f) + new Vector3(0f, 760f, 0f);
        baseView.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static Camera CameraObject(GameObject root, string name)
    {
        GameObject cameraObject = Child(root, name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.25f, 0.23f, 0.19f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 3000f;
        camera.allowHDR = false;
        UniversalAdditionalCameraData data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;
        data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        return camera;
    }

    private static void CaptureScene()
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (camera.name == "Camera_GC16_TopDownWalkabilityProof")
                Render(camera, ProjectPath(CaptureRoot + "/gc16_topdown_walkability_contract_2048x2048.png"), 2048, 2048);
            if (camera.name == "Camera_GC16_CityWalkabilityClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc16_city_walkability_close_1920x1080.png"), 1920, 1080);
            if (camera.name == "Camera_GC16_BaseWalkabilityClose")
                Render(camera, ProjectPath(CaptureRoot + "/gc16_base_walkability_close_1920x1080.png"), 1920, 1080);
        }
    }

    private static void WriteReport()
    {
        StringBuilder report = new();
        report.AppendLine("# GC16 Walkability Contract");
        report.AppendLine();
        report.AppendLine("Lane: Gameplay");
        report.AppendLine("Task: Define the gameplay walkability contract missing from GC15: macro roads, local city/camp soldier walkable areas, vehicle walkable pockets, blocker masks, connection points, and sample routes.");
        report.AppendLine();
        report.AppendLine("Files changed:");
        report.AppendLine("- `Assets/Game/Scripts/Editor/WarlineCaptureGc16WalkabilityContractBuilder.cs`");
        report.AppendLine("- `Assets/Game/Scenes/Generated/GC16_WalkabilityContract_2048.unity`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC16_WalkabilityContract_2048/gc16_topdown_walkability_contract_2048x2048.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC16_WalkabilityContract_2048/gc16_city_walkability_close_1920x1080.png`");
        report.AppendLine("- `Design/AgentReports/Captures/GeneratedScenes/GC16_WalkabilityContract_2048/gc16_base_walkability_close_1920x1080.png`");
        report.AppendLine();
        report.AppendLine("Contracts touched: new GC16 walkability layers on top of the GC14/GC15 macro layout: MacroRoadWalkable, CityLocalWalkable, SoldierBlocker, VehicleWalkable, VehicleBlocker, ConnectionPoint, SampleRoute.");
        report.AppendLine("User-visible behavior: no shipped runtime behavior changed; generated proof scene shows where soldiers/vehicles may move inside cities/camps.");
        report.AppendLine("Validation run: Unity batchmode `WarlineCaptureGc16WalkabilityContractBuilder.BuildGc16WalkabilityContract2048`.");
        report.AppendLine("Validation result: " + (ValidationLog.Any(line => line.StartsWith("ERROR:", StringComparison.Ordinal)) ? "failed; see validation log below." : "passed walkability contract validation."));
        report.AppendLine("Known gaps: contract proof only; it still needs export into runtime ECS/pathfinding data and reconciliation against final accepted visual clusters.");
        report.AppendLine("Cross-lane impacts: Design/Art must keep authored visual clusters compatible with these local walkable corridors and blocker footprints.");
        report.AppendLine("Next recommended task: convert the accepted GC16 zones into a reusable data asset and make GC17 generate visual clusters around these masks instead of after-the-fact fitting.");
        report.AppendLine();
        report.AppendLine("Layer counts:");
        report.AppendLine($"- MacroRoadWalkable: {MacroRoads.Count}");
        report.AppendLine($"- CityLocalWalkable/SoldierWalkable: {SoldierWalkable.Count}");
        report.AppendLine($"- VehicleWalkable: {VehicleWalkable.Count}");
        report.AppendLine($"- SoldierBlocker: {SoldierBlockers.Count}");
        report.AppendLine($"- VehicleBlocker: {VehicleBlockers.Count}");
        report.AppendLine($"- ConnectionPoint: {Connections.Count}");
        report.AppendLine($"- Soldier sample routes: {SoldierRoutes.Count}");
        report.AppendLine($"- Vehicle sample routes: {VehicleRoutes.Count}");
        report.AppendLine();
        report.AppendLine("Validation log:");
        foreach (string line in ValidationLog)
            report.AppendLine("- " + line);
        File.WriteAllText(ProjectPath(ReportPath), report.ToString(), Encoding.UTF8);
    }

    private static void AddMacroRoad(string name, float x, float y, float width, float height)
    {
        MacroRoads.Add(new Zone(name, BlueprintRect(x, y, width, height)));
    }

    private static Rect Expanded(Rect rect, float amount)
    {
        return new Rect(rect.xMin - amount, rect.yMin - amount, rect.width + amount * 2f, rect.height + amount * 2f);
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

    private static Vector3 Center(Rect rect, float y)
    {
        return new Vector3(rect.center.x, y, rect.center.y);
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

    private static void Cylinder(GameObject parent, string name, Vector3 position, float radius, Material material, float y)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = name;
        marker.transform.SetParent(parent.transform, true);
        marker.transform.position = new Vector3(position.x, y, position.z);
        marker.transform.localScale = new Vector3(radius, 0.04f, radius);
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void Polyline(GameObject parent, string name, Vector3[] points, Material material, float width, float y)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 from = points[i];
            Vector3 to = points[i + 1];
            Vector3 delta = to - from;
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"{name}_{i:00}";
            segment.transform.SetParent(parent.transform, true);
            segment.transform.position = new Vector3((from.x + to.x) * 0.5f, y, (from.z + to.z) * 0.5f);
            segment.transform.localScale = new Vector3(width, 0.035f, delta.magnitude);
            segment.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            Object.DestroyImmediate(segment.GetComponent<Collider>());
            segment.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
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
