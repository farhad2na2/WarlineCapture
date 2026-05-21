using System.IO;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class Chapter01TacticalRuntimeBindingTests
{
    private const string GameScenePath = "Assets/Game/Scenes/Game2D.unity";
    private const string DefinitionPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset";
    private const string GridConfigPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.grid.asset";
    private const string MissionId = "saga.ch01.m01.first_contact";

    [Test]
    public void TacticalMapDefinition_M01AnchorsAreInsideWalkableGrid()
    {
        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        GridAuthoringConfig gridConfig = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(GridConfigPath);

        Assert.NotNull(definition);
        Assert.NotNull(gridConfig);
        Assert.AreEqual(MissionId, definition.MissionId);
        Assert.AreEqual(definition.GridWidth, gridConfig.Width);
        Assert.AreEqual(definition.GridHeight, gridConfig.Height);
        Assert.AreEqual(definition.CellSize, gridConfig.CellSize, 0.0001f);

        AssertAnchorIsOpen(definition, gridConfig, "player_spawn.command_squad");
        AssertAnchorIsOpen(definition, gridConfig, "enemy_spawn.patrol_start");
        AssertAnchorIsOpen(definition, gridConfig, "tutorial.move_target.cover_01");
    }

    [Test]
    public void GameScene_Chapter01TacticalRuntimeBinderIsWired()
    {
        string sceneText = File.ReadAllText(GameScenePath);
        string definitionGuid = AssetDatabase.AssetPathToGUID(DefinitionPath);
        string gridGuid = AssetDatabase.AssetPathToGUID(GridConfigPath);

        StringAssert.Contains("chapter01TacticalBinder: {fileID:", sceneText);
        StringAssert.Contains("m_Name: Chapter01_TacticalMissionRuntime", sceneText);
        StringAssert.Contains("m_EditorClassIdentifier: Assembly-CSharp::Chapter01MissionTacticalRuntimeBinder", sceneText);
        StringAssert.Contains($"missionDefinitions:\n  - {{fileID: 11400000, guid: {definitionGuid}, type: 2}}", sceneText);
        StringAssert.Contains($"missionGridConfigs:\n  - {{fileID: 11400000, guid: {gridGuid}, type: 2}}", sceneText);
        StringAssert.Contains("m_EditorClassIdentifier: Assembly-CSharp::TacticalMapRuntimeLoader", sceneText);
        StringAssert.Contains($"definition: {{fileID: 11400000, guid: {definitionGuid}, type: 2}}", sceneText);
        StringAssert.Contains($"gridConfig: {{fileID: 11400000, guid: {gridGuid}, type: 2}}", sceneText);
        StringAssert.Contains("loadOnAwake: 0", sceneText);
    }

    [Test]
    public void MissionCatalog_M01CarriesScenarioAndMapContractIds()
    {
        MissionConfig mission = ChapterOneMissionCatalog.GetMission(MissionId);

        Assert.AreEqual("scenario.ch01.m01.first_contact", mission.ScenarioSetupId);
        Assert.AreEqual("level.ch01.district_edge_01", mission.LevelId);
        Assert.AreEqual("iso.ch01.district_edge_01", mission.IsoMapId);
        Assert.AreEqual("preview.ch01.first_contact", mission.MapPreviewArtId);
        Assert.AreEqual("minimap.ch01.first_contact", mission.MinimapArtId);

        WarlineCaptureMissionSession.BeginMission(MissionId, WarlineCaptureRoute.SagaMap);
        Assert.AreEqual(mission.ScenarioSetupId, WarlineCaptureMissionSession.ActiveScenarioSetupId);
        Assert.AreEqual(mission.LevelId, WarlineCaptureMissionSession.ActiveLevelId);
        Assert.AreEqual(mission.IsoMapId, WarlineCaptureMissionSession.ActiveIsoMapId);
        Assert.AreEqual(mission.MapPreviewArtId, WarlineCaptureMissionSession.ActiveMapPreviewArtId);
        Assert.AreEqual(mission.MinimapArtId, WarlineCaptureMissionSession.ActiveMinimapArtId);
    }

    [Test]
    public void TacticalMapRuntimeLoader_UsesGameplayXZPlaneForGridAndCamera()
    {
        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        GridAuthoringConfig gridConfig = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(GridConfigPath);
        Assert.NotNull(definition);
        Assert.NotNull(gridConfig);

        GameObject root = new("RuntimeLoaderTestRoot");
        GameObject cameraObject = new("RuntimeLoaderTestCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(99f, 12f, -99f);

            TacticalMapRuntimeLoader loader = root.AddComponent<TacticalMapRuntimeLoader>();
            loader.Configure(definition, gridConfig, camera);
            loader.Load();

            Assert.AreEqual(TacticalMapRuntimePlane.GameplayXZ, loader.RuntimePlane);
            Assert.NotNull(loader.GridAuthoring);
            Assert.AreEqual(definition.WorldOrigin.x, loader.GridAuthoring.transform.localPosition.x, 0.0001f);
            Assert.AreEqual(0f, loader.GridAuthoring.transform.localPosition.y, 0.0001f);
            Assert.AreEqual(definition.WorldOrigin.y, loader.GridAuthoring.transform.localPosition.z, 0.0001f);
            Assert.AreEqual(90f, loader.GroundRenderer.transform.localEulerAngles.x, 0.0001f);

            Vector2 clampedCenter = loader.ClampWorldToCameraBounds(definition.CameraDefaultCenter);
            Assert.AreEqual(clampedCenter.x, camera.transform.position.x, 0.0001f);
            Assert.AreEqual(12f, camera.transform.position.y, 0.0001f);
            Assert.AreEqual(clampedCenter.y, camera.transform.position.z, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
            WarlineCaptureMissionSession.Clear();
        }
    }

    [Test]
    public void RuntimeBinder_MarksM01RoadsFromTacticalSurfaceMetadataOnly()
    {
        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        GridAuthoringConfig gridConfig = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(GridConfigPath);
        Assert.NotNull(definition);
        Assert.NotNull(gridConfig);

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("Chapter01TacticalRuntimeBindingTests_RoadMetadata");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = em.CreateEntity(typeof(GridConfig));
            em.AddBuffer<GridWalkable>(gridEntity);
            em.AddBuffer<GridRoad>(gridEntity);
            em.AddBuffer<GridRoadSidewalk>(gridEntity);
            em.AddBuffer<GridRoadDirt>(gridEntity);
            DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
            DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
            DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
            DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity);
            int seededSize = 8 * 8;
            walkable.ResizeUninitialized(seededSize);
            roads.ResizeUninitialized(seededSize);
            sidewalks.ResizeUninitialized(seededSize);
            dirtRoads.ResizeUninitialized(seededSize);
            for (int i = 0; i < seededSize; i++)
            {
                walkable[i] = new GridWalkable { Value = 0 };
                roads[i] = new GridRoad { Value = 1 };
                sidewalks[i] = new GridRoadSidewalk { Value = 1 };
                dirtRoads[i] = new GridRoadDirt { Value = 1 };
            }

            MethodInfo applyGrid = typeof(Chapter01MissionTacticalRuntimeBinder).GetMethod(
                "ApplyGridToEcsWorld",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(applyGrid);
            applyGrid.Invoke(null, new object[] { definition, gridConfig });

            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            Assert.AreEqual(definition.GridWidth, grid.Width);
            Assert.AreEqual(definition.GridHeight, grid.Height);
            Assert.AreEqual(definition.CellSize, grid.CellSize, 0.0001f);

            roads = em.GetBuffer<GridRoad>(gridEntity);
            sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
            HashSet<int> authoredRoads = CollectSurfaceIndices(definition, TacticalMapSurfaceType.MainRoad);
            HashSet<int> authoredShoulders = CollectSurfaceIndices(definition, TacticalMapSurfaceType.RoadShoulder);
            Assert.That(authoredRoads.Count, Is.GreaterThan(0), "M01 must define authored main-road cells.");
            Assert.That(authoredShoulders.Count, Is.GreaterThan(0), "M01 must define authored road-shoulder cells.");
            Assert.That(authoredRoads.Count, Is.LessThan(definition.GridWidth * definition.GridHeight), "Road metadata must not be a full-grid procedural fill.");

            Assert.AreEqual(authoredRoads.Count, CountRoadCells(roads), "Runtime road buffer should match authored TacticalMapDefinition MainRoad surfaces exactly.");
            Assert.AreEqual(authoredShoulders.Count, CountSidewalkCells(sidewalks), "Runtime sidewalk buffer should match authored TacticalMapDefinition RoadShoulder surfaces exactly.");

            for (int index = 0; index < roads.Length; index++)
            {
                Assert.AreEqual(authoredRoads.Contains(index) ? 1 : 0, roads[index].Value, $"road index {index}");
                Assert.AreEqual(authoredShoulders.Contains(index) ? 1 : 0, sidewalks[index].Value, $"sidewalk index {index}");
            }
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void RuntimeCitySpawner_DoesNotMutateRoadCellsForM01FixedTacticalMission()
    {
        WarlineCaptureMissionSession.BeginMission(MissionId, WarlineCaptureRoute.SagaMap);
        InitialUnitsRuntimeState.PlayRequested = true;

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("Chapter01TacticalRuntimeBindingTests_RuntimeCitySpawner");
        GameObject runtimeRoot = new("RuntimeCitySpawnerTestRoot");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerData));
            em.SetComponentData(gridEntity, new GridConfig { Width = 4, Height = 4, CellSize = 1f });
            DynamicBuffer<GridRoad> roads = em.AddBuffer<GridRoad>(gridEntity);
            roads.ResizeUninitialized(16);
            for (int i = 0; i < roads.Length; i++)
                roads[i] = new GridRoad { Value = (byte)(i % 2) };

            RuntimeCitySpawnerSystem spawner = new();
            spawner.Init(null, null, null, runtimeRoot.transform);
            spawner.Update();

            Assert.IsTrue(spawner.HasSpawned, "M01 fixed tactical missions should bypass random city generation and unblock gameplay startup.");
            Assert.IsFalse(spawner.IsGenerating, "M01 fixed tactical missions should not start a city generation coroutine.");
            for (int i = 0; i < roads.Length; i++)
                Assert.AreEqual(i % 2, roads[i].Value, $"RuntimeCitySpawner should not rewrite M01 road cell {i}.");
            spawner.Dispose();
        }
        finally
        {
            Object.DestroyImmediate(runtimeRoot);
            World.DefaultGameObjectInjectionWorld = previousWorld;
            InitialUnitsRuntimeState.PlayRequested = false;
            InitialUnitsRuntimeState.BuildModeActive = false;
            WarlineCaptureMissionSession.Clear();
        }
    }

    private static void AssertAnchorIsOpen(TacticalMapDefinition definition, GridAuthoringConfig gridConfig, string anchorId)
    {
        Assert.IsTrue(definition.TryGetAnchor(anchorId, out TacticalMapAnchor anchor), $"{anchorId} anchor must exist.");
        Vector2Int cell = definition.NormalizedToCell(anchor.NormalizedPosition);
        Assert.That(cell.x, Is.InRange(0, definition.GridWidth - 1), $"{anchorId} x cell must be in bounds.");
        Assert.That(cell.y, Is.InRange(0, definition.GridHeight - 1), $"{anchorId} y cell must be in bounds.");

        if (gridConfig.BlockedCells == null)
            return;

        foreach (Vector2Int blockedCell in gridConfig.BlockedCells)
            Assert.AreNotEqual(blockedCell, cell, $"{anchorId} must not be on a blocked cell.");
    }

    private static HashSet<int> CollectSurfaceIndices(TacticalMapDefinition definition, TacticalMapSurfaceType type)
    {
        HashSet<int> indices = new();
        foreach (TacticalMapSurface surface in definition.Surfaces)
        {
            if (surface.Type != type)
                continue;

            int minX = Mathf.Clamp(Mathf.FloorToInt(surface.NormalizedBounds.xMin * definition.GridWidth), 0, definition.GridWidth - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(surface.NormalizedBounds.xMax * definition.GridWidth), 0, definition.GridWidth);
            int minY = Mathf.Clamp(Mathf.FloorToInt(surface.NormalizedBounds.yMin * definition.GridHeight), 0, definition.GridHeight - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(surface.NormalizedBounds.yMax * definition.GridHeight), 0, definition.GridHeight);
            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                    indices.Add(x + y * definition.GridWidth);
            }
        }

        return indices;
    }

    private static int CountRoadCells(DynamicBuffer<GridRoad> roads)
    {
        int count = 0;
        for (int i = 0; i < roads.Length; i++)
        {
            if (roads[i].Value != 0)
                count++;
        }

        return count;
    }

    private static int CountSidewalkCells(DynamicBuffer<GridRoadSidewalk> sidewalks)
    {
        int count = 0;
        for (int i = 0; i < sidewalks.Length; i++)
        {
            if (sidewalks[i].Value != 0)
                count++;
        }

        return count;
    }
}
