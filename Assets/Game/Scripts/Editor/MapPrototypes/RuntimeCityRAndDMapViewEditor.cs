using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    [CustomEditor(typeof(RuntimeCityRAndDMapView))]
    public sealed class RuntimeCityRAndDMapViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Edit-mode city generation is unavailable while the game is playing.",
                    MessageType.Info);
                return;
            }

            var view = (RuntimeCityRAndDMapView)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Edit-Mode Deterministic City", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Builds the algorithmic city path used when Visual Recipe is empty. " +
                "The generated hierarchy is saved with the scene and is inert at runtime when Runtime Generation Enabled is disabled.",
                MessageType.None);

            bool hasConfiguration = view.Config != null && view.VisualRecipe == null;
            using (new EditorGUI.DisabledScope(!hasConfiguration))
            {
                if (GUILayout.Button("Build Deterministic City"))
                    RuntimeCityRAndDEditModeBuilder.Build(view);

                if (GUILayout.Button("Build Giant Dense City"))
                    RuntimeCityRAndDEditModeBuilder.BuildDenseMapWide(view);
            }

            if (!hasConfiguration)
            {
                EditorGUILayout.HelpBox(
                    view.Config == null
                        ? "Assign a Runtime City Spawner Config before building."
                        : "Clear Visual Recipe to use the deterministic city path.",
                    MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(view.GeneratedRoot == null || view.GeneratedRoot.childCount == 0))
            {
                if (GUILayout.Button("Clear Generated City"))
                    RuntimeCityRAndDEditModeBuilder.Clear(view);
            }
        }
    }

    internal static class RuntimeCityRAndDEditModeBuilder
    {
        private const int MaximumGenerationSteps = 100000;
        private static readonly Vector2Int[] MapWideDistrictStartCells =
        {
            new(300, 280),
            new(780, 280),
            new(1260, 280),
            new(1740, 280),
            new(300, 740),
            new(780, 740),
            new(1260, 740),
            new(1740, 740)
        };

        public static void Build(RuntimeCityRAndDMapView view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (Application.isPlaying)
                throw new InvalidOperationException("Edit-mode city generation cannot run while playing.");
            if (view.Config == null)
                throw new InvalidOperationException("Runtime City Spawner Config is required.");
            if (view.VisualRecipe != null)
                throw new InvalidOperationException("Clear Visual Recipe to build the deterministic city path.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Deterministic City");
            try
            {
                Transform root = EnsureGeneratedRoot(view);
                ClearChildren(root);

                var runtimeMap = new RuntimeCityRAndDMapCompositionSystemHelper();
                runtimeMap.Configure(view, view.RoadMaterial);
                runtimeMap.RequestGeneration();

                for (int frame = 0; frame < MaximumGenerationSteps; frame++)
                {
                    runtimeMap.Tick(frame);
                    if (runtimeMap.IsGenerationActive)
                        continue;

                    RuntimeCityGenerationProgress progress = runtimeMap.Progress;
                    if (progress.Stage == RuntimeCityGenerationStage.Completed)
                    {
                        RegisterGeneratedChildrenForUndo(root);
                        MarkSceneDirty(view);
                        Debug.Log(
                            $"[DeterministicCityEditor] result=Built seed={progress.Seed} " +
                            $"cities={progress.GeneratedCityCount}/{progress.RequestedCityCount} " +
                            $"children={root.childCount}",
                            view);
                        return;
                    }

                    throw new InvalidOperationException(
                        $"Deterministic city generation stopped at {progress.Stage}. " +
                        $"Check the assigned city config and generated-city settings.");
                }

                throw new InvalidOperationException(
                    $"Deterministic city generation exceeded {MaximumGenerationSteps} editor steps.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, view);
                throw;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        public static void Clear(RuntimeCityRAndDMapView view)
        {
            if (view == null || view.GeneratedRoot == null)
                return;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Clear Deterministic City");
            try
            {
                ClearChildren(view.GeneratedRoot);
                MarkSceneDirty(view);
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        public static void BuildMapWide(RuntimeCityRAndDMapView view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (Application.isPlaying)
                throw new InvalidOperationException("Edit-mode city generation cannot run while playing.");
            if (view.VisualRecipe != null)
                throw new InvalidOperationException("Clear Visual Recipe to build the deterministic city path.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Map-Wide Deterministic City");
            try
            {
                RuntimeCitySpawnerSystemConfig sourceConfig =
                    SkirmishDesertBaseDeterministicCityBuilder.EnsureMapWideConfig(view);
                AssignConfig(view, sourceConfig);

                Transform root = EnsureGeneratedRoot(view);
                root.name = "Generated_MapWideDeterministicCity";
                ClearChildren(root);

                for (int index = 0; index < MapWideDistrictStartCells.Length; index++)
                    BuildMapWideDistrict(view, root, sourceConfig, index);

                RegisterGeneratedChildrenForUndo(root);
                MarkSceneDirty(view);
                Debug.Log(
                    $"[DeterministicCityEditor] result=BuiltMapWide districts={MapWideDistrictStartCells.Length} " +
                    $"children={root.childCount}",
                    view);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, view);
                throw;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        public static void BuildDenseMapWide(RuntimeCityRAndDMapView view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (Application.isPlaying)
                throw new InvalidOperationException("Edit-mode city generation cannot run while playing.");
            if (view.VisualRecipe != null)
                throw new InvalidOperationException("Clear Visual Recipe to build the deterministic city path.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Giant Dense City");
            try
            {
                RuntimeCitySpawnerSystemConfig config =
                    SkirmishDesertBaseDeterministicCityBuilder.EnsureMapWideConfig(view);
                AssignConfig(view, config);

                Transform root = EnsureGeneratedRoot(view);
                root.name = "Generated_GiantDenseMiddleEasternCity";
                ClearChildren(root);

                DenseMiddleEasternCityEditModeBuilder.Result result =
                    DenseMiddleEasternCityEditModeBuilder.Build(view, root, config);

                RegisterGeneratedChildrenForUndo(root);
                MarkSceneDirty(view);
                Debug.Log(
                    $"[DeterministicCityEditor] result=BuiltGiantDenseMiddleEasternCity " +
                    $"buildings={result.Buildings} parks={result.Parks} " +
                    $"roadTiles={result.RoadTiles} roadChunks={result.RoadChunks} " +
                    $"authoredCoreRenderers={result.AuthoredCoreRenderers}",
                    view);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, view);
                throw;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static List<float> CreateStreetPositions(float min, float max, float spacing)
        {
            var positions = new List<float> { min };
            for (float position = min + spacing; position < max - spacing * 0.5f; position += spacing)
                positions.Add(position);
            if (positions[positions.Count - 1] < max)
                positions.Add(max);
            return positions;
        }

        private static void CreateDenseRoadNetwork(
            RuntimeCityRAndDMapView view,
            Transform roadRoot,
            List<float> streetsX,
            List<float> streetsZ,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float streetWidth)
        {
            float centerX = (minX + maxX) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;
            for (int index = 0; index < streetsZ.Count; index++)
            {
                CreateRoadSegment(
                    roadRoot,
                    $"EastWest_{index + 1}",
                    new Vector3(centerX, view.GridOrigin.y, streetsZ[index]),
                    new Vector3(maxX - minX + streetWidth, 0.12f, streetWidth),
                    view.RoadMaterial,
                    view.RoadShoulderMaterial);
            }

            for (int index = 0; index < streetsX.Count; index++)
            {
                CreateRoadSegment(
                    roadRoot,
                    $"NorthSouth_{index + 1}",
                    new Vector3(streetsX[index], view.GridOrigin.y, centerZ),
                    new Vector3(streetWidth, 0.12f, maxZ - minZ + streetWidth),
                    view.RoadMaterial,
                    view.RoadShoulderMaterial);
            }
        }

        private static void CreateRoadSegment(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material roadMaterial,
            Material shoulderMaterial)
        {
            GameObject shoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shoulder.name = name + "_Shoulder";
            shoulder.transform.SetParent(parent, false);
            shoulder.transform.position = position + Vector3.down * 0.035f;
            shoulder.transform.localScale = new Vector3(scale.x + 3f, 0.08f, scale.z + 3f);
            ApplySharedMaterial(shoulder, shoulderMaterial);
            RemoveCollider(shoulder);

            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = name;
            road.transform.SetParent(parent, false);
            road.transform.position = position + Vector3.up * 0.03f;
            road.transform.localScale = scale;
            ApplySharedMaterial(road, roadMaterial);
            RemoveCollider(road);
        }

        private static int CreateDenseBuildingBlocks(
            RuntimeCityVisualPresentationSystemHelper visualSystem,
            RuntimeCitySpawnerSystemConfig config,
            GridConfig grid,
            List<float> streetsX,
            List<float> streetsZ,
            float streetWidth,
            float blockInset,
            System.Random random)
        {
            int buildingCount = 0;
            int centerX = (streetsX.Count - 2) / 2;
            int centerZ = (streetsZ.Count - 2) / 2;
            for (int x = 0; x < streetsX.Count - 1; x++)
            {
                for (int z = 0; z < streetsZ.Count - 1; z++)
                {
                    if (x == centerX && z == centerZ)
                        continue;

                    float minX = streetsX[x] + streetWidth * 0.5f + blockInset;
                    float maxX = streetsX[x + 1] - streetWidth * 0.5f - blockInset;
                    float minZ = streetsZ[z] + streetWidth * 0.5f + blockInset;
                    float maxZ = streetsZ[z + 1] - streetWidth * 0.5f - blockInset;
                    if (maxX <= minX || maxZ <= minZ)
                        continue;

                    for (int column = 0; column < 2; column++)
                    {
                        for (int row = 0; row < 2; row++)
                        {
                            if (random.NextDouble() > 0.88d)
                                continue;

                            GameObject prefab = SelectDenseBuildingPrefab(config, random);
                            if (prefab == null)
                                continue;

                            float positionX = Mathf.Lerp(minX, maxX, (column + 0.5f) * 0.5f);
                            float positionZ = Mathf.Lerp(minZ, maxZ, (row + 0.5f) * 0.5f);
                            if (SpawnDenseBuilding(
                                    visualSystem,
                                    prefab,
                                    new Vector3(positionX, grid.Origin.y, positionZ),
                                    grid,
                                    random))
                                buildingCount++;
                        }
                    }
                }
            }

            return buildingCount;
        }

        private static void CreateCivicCenter(
            RuntimeCityVisualPresentationSystemHelper visualSystem,
            RuntimeCitySpawnerSystemConfig config,
            GridConfig grid,
            List<float> streetsX,
            List<float> streetsZ,
            System.Random random)
        {
            int centerX = (streetsX.Count - 2) / 2;
            int centerZ = (streetsZ.Count - 2) / 2;
            Vector3 center = new(
                (streetsX[centerX] + streetsX[centerX + 1]) * 0.5f,
                grid.Origin.y,
                (streetsZ[centerZ] + streetsZ[centerZ + 1]) * 0.5f);
            GameObject hall = PickPrefab(config.HallPrefabs, random) ?? SelectDenseBuildingPrefab(config, random);
            if (hall != null)
                SpawnDenseBuilding(visualSystem, hall, center, grid, random);

            for (int index = 0; index < 4; index++)
            {
                GameObject fountain = PickPrefab(config.FountainPrefabs, random);
                if (fountain == null)
                    break;

                float offsetX = index % 2 == 0 ? -22f : 22f;
                float offsetZ = index < 2 ? -22f : 22f;
                SpawnDenseBuilding(
                    visualSystem,
                    fountain,
                    center + new Vector3(offsetX, 0f, offsetZ),
                    grid,
                    random);
            }
        }

        private static bool SpawnDenseBuilding(
            RuntimeCityVisualPresentationSystemHelper visualSystem,
            GameObject prefab,
            Vector3 position,
            GridConfig grid,
            System.Random random)
        {
            int cellX = Mathf.Clamp(
                Mathf.FloorToInt((position.x - grid.Origin.x) / grid.CellSize),
                0,
                grid.Width - 1);
            int cellZ = Mathf.Clamp(
                Mathf.FloorToInt((position.z - grid.Origin.z) / grid.CellSize),
                0,
                grid.Height - 1);
            return visualSystem.SpawnVisualOnlyPrefab(
                       prefab,
                       new Vector2Int(cellX, cellZ),
                       Vector2Int.one,
                       Quaternion.Euler(0f, random.Next(0, 4) * 90f, 0f),
                       grid) != null;
        }

        private static GameObject SelectDenseBuildingPrefab(
            RuntimeCitySpawnerSystemConfig config,
            System.Random random)
        {
            int roll = random.Next(100);
            GameObject prefab = roll < 62
                ? PickPrefab(config.HousePrefabs, random)
                : roll < 87
                    ? PickPrefab(config.ShopPrefabs, random)
                    : PickPrefab(config.OtherBuildingPrefabs, random);
            return prefab ?? PickPrefab(config.HousePrefabs, random) ??
                PickPrefab(config.ShopPrefabs, random) ??
                PickPrefab(config.OtherBuildingPrefabs, random);
        }

        private static GameObject PickPrefab(List<GameObject> prefabs, System.Random random)
        {
            if (prefabs == null || prefabs.Count == 0)
                return null;

            int startIndex = random.Next(prefabs.Count);
            for (int offset = 0; offset < prefabs.Count; offset++)
            {
                GameObject candidate = prefabs[(startIndex + offset) % prefabs.Count];
                if (candidate != null)
                    return candidate;
            }

            return null;
        }

        private static GridConfig CreateViewGrid(RuntimeCityRAndDMapView view)
        {
            Vector3 origin = view.GridOrigin;
            return new GridConfig
            {
                Width = view.GridWidth,
                Height = view.GridHeight,
                CellSize = view.GridCellSize,
                Origin = new Unity.Mathematics.float3(origin.x, origin.y, origin.z)
            };
        }

        private static void ApplySharedMaterial(GameObject target, Material material)
        {
            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
        }

        private static void BuildMapWideDistrict(
            RuntimeCityRAndDMapView sourceView,
            Transform mapWideRoot,
            RuntimeCitySpawnerSystemConfig sourceConfig,
            int index)
        {
            RuntimeCitySpawnerSystemConfig districtConfig = UnityEngine.Object.Instantiate(sourceConfig);
            districtConfig.name = $"{sourceConfig.name}_District_{index + 1}";
            districtConfig.hideFlags = HideFlags.HideAndDontSave;

            GameObject districtHost = null;
            try
            {
                ConfigureDistrictConfig(districtConfig, MapWideDistrictStartCells[index], index);

                var districtRoot = new GameObject($"District_{index % 4 + 1}_{index / 4 + 1}");
                Undo.RegisterCreatedObjectUndo(districtRoot, "Build Map-Wide Deterministic City");
                districtRoot.transform.SetParent(mapWideRoot, false);

                districtHost = new GameObject("MapWideCityGenerationScratch");
                districtHost.hideFlags = HideFlags.HideAndDontSave;
                RuntimeCityRAndDMapView districtView = districtHost.AddComponent<RuntimeCityRAndDMapView>();
                EditorUtility.CopySerialized(sourceView, districtView);
                districtView.AssignGeneratedRootForEditor(districtRoot.transform);
                AssignConfig(districtView, districtConfig);
                Build(districtView);
            }
            finally
            {
                if (districtHost != null)
                    UnityEngine.Object.DestroyImmediate(districtHost);

                UnityEngine.Object.DestroyImmediate(districtConfig);
            }
        }

        private static void ConfigureDistrictConfig(
            RuntimeCitySpawnerSystemConfig config,
            Vector2Int startCell,
            int districtIndex)
        {
            var serialized = new SerializedObject(config);
            uint seed = config.RandomSeed == 0 ? 26071501u : config.RandomSeed;
            serialized.FindProperty("cityCount").intValue = 1;
            serialized.FindProperty("randomSeed").longValue = seed + (uint)(districtIndex * 104729);
            serialized.FindProperty("startCell").vector2IntValue = startCell;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignConfig(
            RuntimeCityRAndDMapView view,
            RuntimeCitySpawnerSystemConfig config)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("config").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static Transform EnsureGeneratedRoot(RuntimeCityRAndDMapView view)
        {
            if (view.GeneratedRoot != null)
                return view.GeneratedRoot;

            Undo.RecordObject(view, "Assign Deterministic City Root");
            var root = new GameObject("Generated_DeterministicCity");
            Undo.RegisterCreatedObjectUndo(root, "Create Deterministic City Root");
            root.transform.SetParent(view.transform, false);
            view.AssignGeneratedRootForEditor(root.transform);
            EditorUtility.SetDirty(view);
            return root.transform;
        }

        private static void ClearChildren(Transform root)
        {
            for (int index = root.childCount - 1; index >= 0; index--)
                Undo.DestroyObjectImmediate(root.GetChild(index).gameObject);
        }

        private static void RegisterGeneratedChildrenForUndo(Transform root)
        {
            for (int index = 0; index < root.childCount; index++)
                Undo.RegisterCreatedObjectUndo(root.GetChild(index).gameObject, "Build Deterministic City");
        }

        private static void MarkSceneDirty(RuntimeCityRAndDMapView view)
        {
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            SceneView.RepaintAll();
        }
    }

    public static class SkirmishDesertBaseDeterministicCityBuilder
    {
        private const string ScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
        private const string CityConfigPath =
            "Assets/Game/Configs/MapPrototypes/M01_RuntimeCity_Config.asset";
        private const string MapWideConfigFolder =
            "Assets/Game/Configs/OperationMaps/Skirmish";
        private const string MapWideConfigPath =
            MapWideConfigFolder + "/SkirmishDesertBase_MapWideCity_Config.asset";
        private const string RoadMaterialPath =
            "Assets/Game/Art/MapPrototypes/M01/Materials/M01_DirtRoad.mat";
        private const string RoadShoulderMaterialPath =
            "Assets/Game/Art/MapPrototypes/M01/Materials/M01_TransitionGround.mat";
        private const string GroundMaterialPath =
            "Assets/Game/Art/MapPrototypes/M01/Materials/M01_DistrictGround.mat";

        [MenuItem("Tools/Warline Capture/Maps/Skirmish Desert Base/Install Deterministic City Builder")]
        public static void InstallInScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            OperationMapSceneView mapView = FindSingleMapView(scene);
            RuntimeCityRAndDMapView builder = FindOrCreateBuilder(mapView);
            ConfigureBuilder(builder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = builder.gameObject;
            Debug.Log($"[DeterministicCityEditor] result=Installed scene={ScenePath}", builder);
        }

        [MenuItem("Tools/Warline Capture/Maps/Skirmish Desert Base/Validate Deterministic City Builder")]
        public static void ValidateEditModeBuild()
        {
            Scene validationScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var host = new GameObject("DeterministicCityValidation");
                RuntimeCityRAndDMapView view = host.AddComponent<RuntimeCityRAndDMapView>();
                ConfigureBuilder(view);
                RuntimeCityRAndDEditModeBuilder.Build(view);
                if (view.GeneratedRoot == null || view.GeneratedRoot.childCount == 0)
                    throw new InvalidOperationException("Edit-mode deterministic city build created no generated hierarchy.");

                RuntimeCityRAndDEditModeBuilder.Clear(view);
                if (view.GeneratedRoot.childCount != 0)
                    throw new InvalidOperationException("Edit-mode deterministic city clear left generated objects behind.");

                Debug.Log("[DeterministicCityEditor] result=ValidationPassed");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [MenuItem("Tools/Warline Capture/Maps/Skirmish Desert Base/Build Giant Dense City")]
        public static void BuildMapWideInScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            OperationMapSceneView mapView = FindSingleMapView(scene);
            RuntimeCityRAndDMapView builder = FindOrCreateBuilder(mapView);
            ConfigureBuilder(builder);
            RuntimeCityRAndDEditModeBuilder.BuildDenseMapWide(builder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = builder.gameObject;
            Debug.Log($"[DeterministicCityEditor] result=BuiltDenseMapWide scene={ScenePath}", builder);
        }

        [MenuItem("Tools/Warline Capture/Maps/Skirmish Desert Base/Capture Dense City Visual Proof")]
        public static void CaptureDenseCityVisualProof()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            OperationMapSceneView mapView = FindSingleMapView(scene);
            RuntimeCityRAndDMapView builder = FindOrCreateBuilder(mapView);
            DenseMiddleEasternCityEditModeBuilder.CaptureVisualProof(builder);
        }

        [MenuItem("Tools/Warline Capture/Maps/Skirmish Desert Base/Audit Terrain Mesh Sources")]
        public static void AuditTerrainMeshSources()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MapBakeGroupAuthoring[] groups = UnityEngine.Object.FindObjectsByType<MapBakeGroupAuthoring>(
                FindObjectsInactive.Include);
            int terrainGroups = 0;
            int ownedFilters = 0;
            for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
            {
                MapBakeGroupAuthoring group = groups[groupIndex];
                if (group == null || group.gameObject.scene != scene || group.Role != MapBakeGroupRole.Terrain)
                    continue;

                terrainGroups++;
                MeshFilter[] filters = group.GetComponentsInChildren<MeshFilter>(group.IncludeInactiveChildren);
                for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
                {
                    MeshFilter filter = filters[filterIndex];
                    if (filter == null || filter.sharedMesh == null ||
                        filter.GetComponentInParent<MapBakeGroupAuthoring>(true) != group)
                    {
                        continue;
                    }

                    ownedFilters++;
                    Renderer renderer = filter.GetComponent<Renderer>();
                    Bounds bounds = renderer != null
                        ? renderer.bounds
                        : new Bounds(filter.transform.position, Vector3.zero);
                    string meshPath = AssetDatabase.GetAssetPath(filter.sharedMesh);
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(filter.gameObject);
                    Debug.Log(
                        $"[DenseCityTerrainSource] group={GetHierarchyPath(group.transform)} " +
                        $"filter={GetHierarchyPath(filter.transform)} mesh={filter.sharedMesh.name} " +
                        $"meshPath={meshPath} prefabPath={prefabPath} vertices={filter.sharedMesh.vertexCount} " +
                        $"boundsCenter={bounds.center:F2} boundsSize={bounds.size:F2}");
                }
            }

            Terrain[] terrains = UnityEngine.Object.FindObjectsByType<Terrain>(
                FindObjectsInactive.Include);
            int sceneTerrains = 0;
            for (int index = 0; index < terrains.Length; index++)
            {
                Terrain terrain = terrains[index];
                if (terrain == null || terrain.gameObject.scene != scene)
                    continue;

                sceneTerrains++;
                Debug.Log(
                    $"[DenseCityTerrainSource] unityTerrain={GetHierarchyPath(terrain.transform)} " +
                    $"dataPath={AssetDatabase.GetAssetPath(terrain.terrainData)} " +
                    $"size={(terrain.terrainData != null ? terrain.terrainData.size : Vector3.zero):F2}");
            }

            Debug.Log(
                $"[DenseCityTerrainSource] summary terrainGroups={terrainGroups} " +
                $"ownedMeshFilters={ownedFilters} unityTerrains={sceneTerrains}");
        }

        [MenuItem("Tools/Warline Capture/Maps/Skirmish Desert Base/Audit Expansion Obstacles")]
        public static void AuditExpansionObstacles()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            OperationMapSceneView mapView = FindSingleMapView(scene);
            Transform mapRoot = mapView.MapRoot;
            if (mapRoot == null)
                throw new InvalidOperationException("Operation map has no Map Root.");

            int logged = 0;
            for (int categoryIndex = 0; categoryIndex < mapRoot.childCount; categoryIndex++)
            {
                Transform category = mapRoot.GetChild(categoryIndex);
                if (category == null || IsGeneratedCityHierarchy(category))
                    continue;

                LogExpansionObstacle(category, 0, ref logged);
                for (int childIndex = 0; childIndex < category.childCount; childIndex++)
                {
                    Transform child = category.GetChild(childIndex);
                    if (child == null || IsGeneratedCityHierarchy(child))
                        continue;
                    LogExpansionObstacle(child, 1, ref logged);
                }
            }

            Debug.Log($"[DenseCityExpansionObstacle] summary logged={logged} mapRoot={GetHierarchyPath(mapRoot)}");
        }

        private static void LogExpansionObstacle(Transform transform, int depth, ref int logged)
        {
            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            Debug.Log(
                $"[DenseCityExpansionObstacle] depth={depth} path={GetHierarchyPath(transform)} " +
                $"active={transform.gameObject.activeInHierarchy} children={transform.childCount} " +
                $"renderers={renderers.Length} center={bounds.center:F2} size={bounds.size:F2}");
            logged++;
        }

        private static bool IsGeneratedCityHierarchy(Transform transform) =>
            transform.name == "SkirmishDeterministicCityBuilder" ||
            transform.name.StartsWith("Generated_GiantDenseMiddleEasternCity", StringComparison.Ordinal) ||
            transform.GetComponentInParent<RuntimeCityRAndDMapView>(true) != null;

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            string path = transform.name;
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }

        internal static RuntimeCitySpawnerSystemConfig EnsureMapWideConfig(RuntimeCityRAndDMapView view)
        {
            RuntimeCitySpawnerSystemConfig existing =
                AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(MapWideConfigPath);
            if (existing != null)
                return existing;

            RuntimeCitySpawnerSystemConfig source = view != null ? view.Config : null;
            if (source == null)
                source = AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(CityConfigPath);
            if (source == null)
                throw new InvalidOperationException("Missing source Runtime City Spawner Config for the map-wide city preset.");

            if (!AssetDatabase.IsValidFolder("Assets/Game/Configs/OperationMaps"))
                AssetDatabase.CreateFolder("Assets/Game/Configs", "OperationMaps");
            if (!AssetDatabase.IsValidFolder(MapWideConfigFolder))
                AssetDatabase.CreateFolder("Assets/Game/Configs/OperationMaps", "Skirmish");

            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(sourcePath) || !AssetDatabase.CopyAsset(sourcePath, MapWideConfigPath))
                throw new InvalidOperationException("Could not create the Skirmish map-wide city config asset.");

            RuntimeCitySpawnerSystemConfig created =
                AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(MapWideConfigPath);
            if (created == null)
                throw new InvalidOperationException("Created Skirmish map-wide city config could not be loaded.");

            ConfigureMapWideConfig(created);
            AssetDatabase.SaveAssets();
            return created;
        }

        private static OperationMapSceneView FindSingleMapView(Scene scene)
        {
            OperationMapSceneView found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                OperationMapSceneView[] candidates = root.GetComponentsInChildren<OperationMapSceneView>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    if (found != null)
                        throw new InvalidOperationException("Expected exactly one OperationMapSceneView in the skirmish scene.");
                    found = candidates[index];
                }
            }

            return found ?? throw new InvalidOperationException("Skirmish scene has no OperationMapSceneView.");
        }

        private static RuntimeCityRAndDMapView FindOrCreateBuilder(OperationMapSceneView mapView)
        {
            Transform mapRoot = mapView.MapRoot;
            if (mapRoot == null)
                throw new InvalidOperationException("OperationMapSceneView has no Map Root.");

            RuntimeCityRAndDMapView existing = mapRoot.GetComponentInChildren<RuntimeCityRAndDMapView>(true);
            if (existing != null)
                return existing;

            var builderObject = new GameObject("SkirmishDeterministicCityBuilder");
            builderObject.transform.SetParent(mapRoot, false);
            return builderObject.AddComponent<RuntimeCityRAndDMapView>();
        }

        private static void ConfigureMapWideConfig(RuntimeCitySpawnerSystemConfig config)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty("cityCount").intValue = 1;
            serialized.FindProperty("randomSeed").longValue = 26071501;
            serialized.FindProperty("startCell").vector2IntValue = new Vector2Int(300, 280);
            serialized.FindProperty("generationYieldInterval").intValue = 0;
            serialized.FindProperty("gasStationCount").intValue = 6;
            serialized.FindProperty("shopCount").intValue = 55;
            serialized.FindProperty("houseCount").intValue = 140;
            serialized.FindProperty("otherBuildingCount").intValue = 35;
            serialized.FindProperty("cityDecorationBuildingCount").intValue = 35;
            serialized.FindProperty("extraTownRadiusRoadCells").intValue = 8;
            serialized.FindProperty("cityMinSpacingRoadCells").intValue = 50;
            serialized.FindProperty("ruralHouseRatio").floatValue = 0.2f;
            serialized.FindProperty("houseWallChance").floatValue = 0.05f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void ConfigureBuilder(RuntimeCityRAndDMapView view)
        {
            RuntimeCitySpawnerSystemConfig config =
                AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(CityConfigPath);
            Material road = AssetDatabase.LoadAssetAtPath<Material>(RoadMaterialPath);
            Material shoulder = AssetDatabase.LoadAssetAtPath<Material>(RoadShoulderMaterialPath);
            Material ground = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (config == null || road == null || shoulder == null || ground == null)
                throw new InvalidOperationException("Missing deterministic-city config or M01 presentation material.");

            var serialized = new SerializedObject(view);
            serialized.FindProperty("config").objectReferenceValue = config;
            serialized.FindProperty("visualRecipe").objectReferenceValue = null;
            serialized.FindProperty("deterministicFallbackRecipe").objectReferenceValue = null;
            serialized.FindProperty("deterministicFallbackEnabled").boolValue = false;
            serialized.FindProperty("runtimeGenerationEnabled").boolValue = false;
            serialized.FindProperty("generateOnStart").boolValue = false;
            serialized.FindProperty("showDebugOverlay").boolValue = false;
            serialized.FindProperty("createAlgorithmicFoundation").boolValue = false;
            serialized.FindProperty("cloneGeneratedMaterials").boolValue = false;
            serialized.FindProperty("gridWidth").intValue = 2048;
            serialized.FindProperty("gridHeight").intValue = 1024;
            serialized.FindProperty("gridCellSize").floatValue = 1f;
            serialized.FindProperty("gridOrigin").vector3Value = new Vector3(768f, 0f, 256f);
            serialized.FindProperty("roadCellSizeInGridCells").intValue = 10;
            serialized.FindProperty("roadMaterial").objectReferenceValue = road;
            serialized.FindProperty("roadShoulderMaterial").objectReferenceValue = shoulder;
            serialized.FindProperty("algorithmicGroundMaterial").objectReferenceValue = ground;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }
    }
}
