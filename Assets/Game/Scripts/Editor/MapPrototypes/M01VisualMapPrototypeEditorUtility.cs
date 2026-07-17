namespace Game.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public static partial class M01VisualMapPrototypeEditorUtility
    {
        public const string ScenePath = "Assets/Game/Scenes/MapPrototypes/Chapter01/M01_VisualPrototype.unity";
        public const string CaptureDirectory = "Design/ArtReview/OperationMaps/M01";
        public const int GenerationSeed = 26071501;
        public const string GeneratorVersion = "M01VisualPrototype_2026-07-17_v28_horizon_belt";

        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const string MaterialDirectory = "Assets/Game/Art/MapPrototypes/M01/Materials";
        private const string PremiumLightingRigPath = "Assets/Game/Rendering/Prefabs/PremiumLightingRig.prefab";
        private const string PremiumVolumeProfilePath = "Assets/Game/Rendering/Profiles/PremiumGlobalVolumeProfile.asset";
        private const string PrototypeVolumeProfilePath = "Assets/Game/Art/MapPrototypes/M01/M01_VisualVolumeProfile.asset";
        private const string DesertSkyboxMaterialPath = "Assets/Game/Art/MapPrototypes/M01/M01_DesertSkybox.mat";
        private const string TownMarketModulePath = "Assets/Game/Prefabs/Generated/GC04Modules/TownMarket_DemoAuthored.prefab";
        private const string BaseCommandModulePath = "Assets/Game/Prefabs/Generated/GC04Modules/BaseCommand_DemoAuthored.prefab";
        private const string SouthTownModulePath = "Assets/Game/Prefabs/Generated/GC04Modules/TownBlock_SouthCenter_DemoAuthored.prefab";

        private const string SandMaterialPath = MaterialDirectory + "/M01_Sand.mat";
        private const string AsphaltMaterialPath = MaterialDirectory + "/M01_Asphalt.mat";
        private const string ConcreteMaterialPath = MaterialDirectory + "/M01_Concrete.mat";
        private const string CurbMaterialPath = MaterialDirectory + "/M01_Curb.mat";
        private const string WhitePaintMaterialPath = MaterialDirectory + "/M01_RoadPaint_White.mat";
        private const string AmberPaintMaterialPath = MaterialDirectory + "/M01_RoadPaint_Amber.mat";
        private const string TurquoiseMaterialPath = MaterialDirectory + "/M01_Market_Turquoise.mat";
        private const string RustMaterialPath = MaterialDirectory + "/M01_Rust.mat";
        private const string DistrictGroundMaterialPath = MaterialDirectory + "/M01_DistrictGround.mat";
        private const string TransitionGroundMaterialPath = MaterialDirectory + "/M01_TransitionGround.mat";
        private const string DirtRoadMaterialPath = MaterialDirectory + "/M01_DirtRoad.mat";

        private static readonly string[] RequiredPrefabPaths =
        {
            PremiumLightingRigPath,
            TownMarketModulePath,
            BaseCommandModulePath,
            SouthTownModulePath,
            "Assets/Game/Prefabs/Environment/City/Clock_Tower_01.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Ruins_03.prefab",
            "Assets/Game/Prefabs/Buildings/Building_Hall.prefab",
            "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab",
            "Assets/Game/Prefabs/Buildings/Building_GuardTower.prefab",
            "Assets/Game/Prefabs/Buildings/Building_WaterTank.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Generator_Large_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_03.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_05.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Fence_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Wood_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Stall_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_ClothCover_Large_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Refugee_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Refugee_Damaged_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Truck_01_Destroyed.prefab",
            "Assets/PolygonMilitary/Prefabs/FX/FX_Fire_01.prefab",
            "Assets/PolygonMilitary/Prefabs/FX/FX_Smoke_Large_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Signs/SM_Prop_Sign_Medical_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_MedicalBox_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Bed_Medical_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_03.prefab"
        };

        private sealed class Palette
        {
            public Material Sand;
            public Material Asphalt;
            public Material Concrete;
            public Material Curb;
            public Material WhitePaint;
            public Material AmberPaint;
            public Material Turquoise;
            public Material Rust;
            public Material DistrictGround;
            public Material TransitionGround;
            public Material DirtRoad;
        }

        private readonly struct LocalRoadSegmentDefinition
        {
            public LocalRoadSegmentDefinition(string name, Vector3 start, Vector3 end, bool dusty)
            {
                Name = name;
                Start = start;
                End = end;
                Dusty = dusty;
            }

            public string Name { get; }
            public Vector3 Start { get; }
            public Vector3 End { get; }
            public bool Dusty { get; }
        }

        private readonly struct DistrictCurationDefinition
        {
            public DistrictCurationDefinition(
                string moduleName,
                Vector2 minimum,
                Vector2 maximum,
                bool excludeAirfield,
                bool excludeRoads = false,
                float remoteRoadDistance = 0f,
                float remoteRoadSize = 0f)
            {
                ModuleName = moduleName;
                Minimum = minimum;
                Maximum = maximum;
                ExcludeAirfield = excludeAirfield;
                ExcludeRoads = excludeRoads;
                RemoteRoadDistance = remoteRoadDistance;
                RemoteRoadSize = remoteRoadSize;
            }

            public string ModuleName { get; }
            public Vector2 Minimum { get; }
            public Vector2 Maximum { get; }
            public bool ExcludeAirfield { get; }
            public bool ExcludeRoads { get; }
            public float RemoteRoadDistance { get; }
            public float RemoteRoadSize { get; }

            public bool Contains(Vector3 worldPosition)
            {
                return worldPosition.x >= Minimum.x &&
                       worldPosition.x <= Maximum.x &&
                       worldPosition.z >= Minimum.y &&
                       worldPosition.z <= Maximum.y;
            }
        }

        private readonly struct CompositionBoundsRecord
        {
            public CompositionBoundsRecord(Transform owner, Bounds bounds)
            {
                Owner = owner;
                Bounds = bounds;
            }

            public Transform Owner { get; }
            public Bounds Bounds { get; }
        }

        private readonly struct TerrainClearanceZone
        {
            public TerrainClearanceZone(Vector2 center, float radius)
            {
                Center = center;
                Radius = radius;
            }

            public Vector2 Center { get; }
            public float Radius { get; }
        }

        private static readonly LocalRoadSegmentDefinition[] LocalRoadSegments =
        {
            new("MarketStreet_West", new Vector3(-64f, 0f, 6f), new Vector3(-38f, 0f, 6f), true),
            new("MarketStreet_Bend", new Vector3(-38f, 0f, 6f), new Vector3(-15f, 0f, 10f), true),
            new("CompoundApproach", new Vector3(-15f, 0f, 10f), new Vector3(15.5f, 0f, 18f), true)
        };

        private const float LocalRoadWidth = 3.2f;
        private const float LocalRoadShoulderAllowance = 1.6f;
        private const float MinimumLocalRoadLength = 2f;
        private const float MaximumLocalRoadLength = 36f;
        private const float MaximumLocalRoadTotalLength = 90f;
        private const int MaximumLocalRoadSegmentCount = 4;
        private const float LocalRoadEndpointTolerance = 0.05f;

        private static readonly TerrainClearanceZone[] FrontageTerrainClearanceZones =
        {
            new(new Vector2(-39f, -15f), 8f),
            new(new Vector2(-22f, -16f), 8f),
            new(new Vector2(26f, -17f), 8f)
        };

        private static readonly string[] AuthoredRoadClearanceObstacleNames =
        {
            "DestroyedAidTruck",
            "BombedCornerRuin",
            "CivilianAidTent",
            "DamagedCivilianTent",
            "CivilianFrontageHouse_West",
            "CivilianFrontageHouse_Center",
            "CivilianFrontageHouse_East"
        };

        private static readonly string[] AuthoredTransitionStructureNames =
        {
            "ResidentialTransitionHouse",
            "CivilianFrontageHouse_West",
            "CivilianFrontageHouse_Center",
            "CivilianFrontageHouse_East"
        };

        private static readonly string[] OldMarketBuildingSupportGroundNames =
        {
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_02 (4)",
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_02 (5)",
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_02 (6)",
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_Flat_01 (2)",
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_Flat_01 (3)",
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_Flat_01 (4)",
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_Flat_01 (5)",
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_Flat_01 (6)",
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_Flat_01 (7)",
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_Flat_01 (8)",
            "TownMarket_DemoAuthored_SM_Env_Ground_Hill_Flat_01 (9)"
        };

        private static readonly string[] UnsupportedUtilityGroundNames =
        {
            "BaseCommand_DemoAuthored_SM_Env_Ground_Hill_Flat_Square_01 (8)",
            "BaseCommand_DemoAuthored_SM_Env_Ground_Hill_Flat_Square_01 (9)"
        };

        private static readonly DistrictCurationDefinition[] DistrictCurationDefinitions =
        {
            new(
                "OldMarket_West_DemoAuthored",
                new Vector2(-113f, -33f),
                new Vector2(-14f, 69f),
                false),
            new(
                "UtilityCompound_East_DemoAuthored",
                new Vector2(0f, 6f),
                new Vector2(44f, 52f),
                true,
                true),
            new(
                "Residential_South_DemoAuthored",
                new Vector2(-43f, -93f),
                new Vector2(35f, -17f),
                false)
        };

        [MenuItem("Game/Operation Maps/M01/Generate Visual Prototype")]
        public static void GenerateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            GenerateScene();
        }

        [MenuItem("Game/Operation Maps/M01/Generate And Capture Visual Prototype")]
        public static void GenerateAndCaptureFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            GenerateScene();
            CaptureReviewSet();
        }

        public static void GenerateAndCaptureBatch()
        {
            try
            {
                GenerateScene();
                string contactSheet = CaptureReviewSet();
                Debug.Log($"[M01VisualMap] result=Passed version={GeneratorVersion} seed={GenerationSeed} scene={ScenePath} contactSheet={contactSheet}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[M01VisualMap] result=Failed version={GeneratorVersion} seed={GenerationSeed}\n{exception}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }

        public static void ValidateDeterministicRegenerationBatch()
        {
            try
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                GameObject existingRoot = FindSceneRoot(scene);
                string before = ComputeSceneFingerprint(existingRoot);

                GenerateScene();

                GameObject regeneratedRoot = FindSceneRoot(SceneManager.GetActiveScene());
                string after = ComputeSceneFingerprint(regeneratedRoot);
                if (!string.Equals(before, after, StringComparison.Ordinal))
                    throw new InvalidOperationException($"M01 semantic regeneration mismatch: before={before} after={after}");

                Debug.Log($"[M01VisualMapDeterminism] result=Passed fingerprint={after} version={GeneratorVersion} seed={GenerationSeed}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[M01VisualMapDeterminism] result=Failed version={GeneratorVersion} seed={GenerationSeed}\n{exception}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }

        public static void AnalyzeDistrictModuleEdgesBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = FindSceneRoot(scene);
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                throw new InvalidOperationException("M01 district module root is missing.");

            int candidateCount = 0;
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                Renderer[] renderers = module.GetComponentsInChildren<Renderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (!IsDistrictEdgeCandidateName(renderer.name))
                        continue;

                    Bounds bounds = renderer.bounds;
                    Vector2 offset = new(bounds.center.x - module.position.x, bounds.center.z - module.position.z);
                    float distance = offset.magnitude;
                    if (distance < 42f)
                        continue;

                    candidateCount++;
                    Debug.Log(
                        $"[M01DistrictEdgeCandidate] module={module.name} name={renderer.name} " +
                        $"distance={distance:0.00} center={bounds.center} size={bounds.size}");
                }
            }

            Debug.Log($"[M01DistrictEdgeAnalysis] result=Passed candidates={candidateCount}");
        }

        public static void AnalyzeDistrictLayoutBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = FindSceneRoot(scene);
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                throw new InvalidOperationException("M01 district module root is missing.");

            var moduleBounds = new List<CompositionBoundsRecord>(modulesRoot.childCount);
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                Renderer[] renderers = module.GetComponentsInChildren<Renderer>(true);
                bool assigned = false;
                Bounds occupiedBounds = default;
                int occupiedRendererCount = 0;
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                        renderer.bounds.size.y < 1.2f)
                    {
                        continue;
                    }

                    if (!assigned)
                    {
                        occupiedBounds = renderer.bounds;
                        assigned = true;
                    }
                    else
                    {
                        occupiedBounds.Encapsulate(renderer.bounds);
                    }

                    occupiedRendererCount++;
                }

                if (!assigned)
                    continue;

                moduleBounds.Add(new CompositionBoundsRecord(module, occupiedBounds));
                Debug.Log(
                    $"[M01DistrictLayout] module={module.name} position={module.position} " +
                    $"occupiedCenter={occupiedBounds.center} occupiedSize={occupiedBounds.size} " +
                    $"occupiedRenderers={occupiedRendererCount}");
            }

            for (int firstIndex = 0; firstIndex < moduleBounds.Count; firstIndex++)
            {
                CompositionBoundsRecord first = moduleBounds[firstIndex];
                for (int secondIndex = firstIndex + 1; secondIndex < moduleBounds.Count; secondIndex++)
                {
                    CompositionBoundsRecord second = moduleBounds[secondIndex];
                    float gapX = Mathf.Max(0f, Mathf.Max(first.Bounds.min.x, second.Bounds.min.x) -
                        Mathf.Min(first.Bounds.max.x, second.Bounds.max.x));
                    float gapZ = Mathf.Max(0f, Mathf.Max(first.Bounds.min.z, second.Bounds.min.z) -
                        Mathf.Min(first.Bounds.max.z, second.Bounds.max.z));
                    float planarGap = Mathf.Sqrt(gapX * gapX + gapZ * gapZ);
                    Debug.Log(
                        $"[M01DistrictLayoutPair] first={first.Owner.name} second={second.Owner.name} " +
                        $"gapX={gapX:0.00} gapZ={gapZ:0.00} planarGap={planarGap:0.00}");
                }
            }

            Debug.Log($"[M01DistrictLayoutAnalysis] result=Passed modules={moduleBounds.Count}");
        }

        public static void AnalyzeDistrictBuildingOrientationBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = FindSceneRoot(scene);
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                throw new InvalidOperationException("M01 district module root is missing.");

            int candidateCount = 0;
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                Transform[] transforms = module.GetComponentsInChildren<Transform>(true);
                for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    Transform candidate = transforms[transformIndex];
                    if (!IsDistrictBuildingAssemblyCandidate(candidate, module))
                        continue;

                    Renderer[] renderers = candidate.GetComponentsInChildren<Renderer>(true);
                    if (!TryGetCombinedBounds(renderers, out Bounds bounds))
                        continue;

                    candidateCount++;
                    Debug.Log(
                        $"[M01DistrictBuildingCandidate] module={module.name} name={candidate.name} " +
                        $"parent={candidate.parent.name} children={candidate.childCount} renderers={renderers.Length} " +
                        $"position={candidate.position} yaw={candidate.eulerAngles.y:0.0} " +
                        $"boundsCenter={bounds.center} boundsSize={bounds.size}");
                }
            }

            Debug.Log($"[M01DistrictBuildingAnalysis] result=Passed candidates={candidateCount}");
        }

        public static void AnalyzeDistrictCompositionOwnersBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = FindSceneRoot(scene);
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                throw new InvalidOperationException("M01 district module root is missing.");

            int candidateCount = 0;
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                Transform compositionRoot = FindDistrictCompositionRoot(module);
                for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
                {
                    Transform owner = compositionRoot.GetChild(ownerIndex);
                    Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
                    if (!TryGetCombinedBounds(renderers, out Bounds bounds))
                        continue;

                    Vector2 offset = new(bounds.center.x - module.position.x, bounds.center.z - module.position.z);
                    float distance = offset.magnitude;
                    float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
                    string category = ClassifyDistrictCompositionOwner(owner);
                    if (bounds.size.sqrMagnitude < 0.01f ||
                        (distance < 45f && horizontalSize < 18f && string.Equals(category, "other", StringComparison.Ordinal)))
                        continue;

                    candidateCount++;
                    Debug.Log(
                        $"[M01DistrictCompositionOwner] module={module.name} owner={owner.name} " +
                        $"category={category} distance={distance:0.00} " +
                        $"center={bounds.center} size={bounds.size} renderers={renderers.Length}");
                }
            }

            Debug.Log($"[M01DistrictCompositionAnalysis] result=Passed candidates={candidateCount}");
        }

        public static void AnalyzeFrontageCompositionOwnersBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = FindSceneRoot(scene);
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                throw new InvalidOperationException("M01 district module root is missing.");

            int candidateCount = 0;
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                Transform compositionRoot = FindDistrictCompositionRoot(module);
                for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
                {
                    Transform owner = compositionRoot.GetChild(ownerIndex);
                    if (!owner.gameObject.activeInHierarchy)
                        continue;

                    Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
                    if (!TryGetCombinedBounds(renderers, out Bounds bounds) ||
                        bounds.center.x < -10f || bounds.center.x > 42f ||
                        bounds.center.z < -36f || bounds.center.z > 2f ||
                        Mathf.Max(bounds.size.x, bounds.size.z) < 1.5f)
                    {
                        continue;
                    }

                    candidateCount++;
                    Debug.Log(
                        $"[M01FrontageCompositionOwner] module={module.name} owner={owner.name} " +
                        $"center={bounds.center} size={bounds.size} renderers={renderers.Length}");
                }
            }

            Debug.Log($"[M01FrontageCompositionAnalysis] result=Passed candidates={candidateCount}");
        }

        public static void AnalyzeTerrainStructurePenetrationsBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = FindSceneRoot(scene);
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                throw new InvalidOperationException("M01 district module root is missing.");

            int totalCandidates = 0;
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                Transform compositionRoot = FindDistrictCompositionRoot(module);
                var structures = new List<CompositionBoundsRecord>();
                var terrain = new List<CompositionBoundsRecord>();

                for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
                {
                    Transform owner = compositionRoot.GetChild(ownerIndex);
                    if (!owner.gameObject.activeInHierarchy)
                        continue;

                    Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
                    if (!TryGetCombinedBounds(renderers, out Bounds bounds))
                        continue;

                    if (ContainsDescendantName(owner, "_Bld_"))
                    {
                        structures.Add(new CompositionBoundsRecord(owner, bounds));
                    }
                    else if (ContainsPenetratingTerrainName(owner) && Mathf.Max(bounds.size.x, bounds.size.z) >= 3f)
                    {
                        terrain.Add(new CompositionBoundsRecord(owner, bounds));
                    }
                }

                int moduleCandidates = 0;
                for (int structureIndex = 0; structureIndex < structures.Count; structureIndex++)
                {
                    CompositionBoundsRecord structure = structures[structureIndex];
                    for (int terrainIndex = 0; terrainIndex < terrain.Count; terrainIndex++)
                    {
                        CompositionBoundsRecord terrainRecord = terrain[terrainIndex];
                        if (!HasMeaningfulTerrainStructureOverlap(structure.Bounds, terrainRecord.Bounds))
                            continue;

                        moduleCandidates++;
                        Debug.Log(
                            $"[M01TerrainStructurePenetration] module={module.name} " +
                            $"structure={structure.Owner.name} terrain={terrainRecord.Owner.name} " +
                            $"structureCenter={structure.Bounds.center} structureSize={structure.Bounds.size} " +
                            $"terrainCenter={terrainRecord.Bounds.center} terrainSize={terrainRecord.Bounds.size}");
                    }
                }

                totalCandidates += moduleCandidates;
                Debug.Log(
                    $"[M01TerrainStructurePenetrationModule] module={module.name} structures={structures.Count} " +
                    $"terrain={terrain.Count} candidates={moduleCandidates}");
            }

            Debug.Log($"[M01TerrainStructurePenetrationAnalysis] result=Recorded candidates={totalCandidates}");
        }

        private static bool ContainsDescendantName(Transform owner, string fragment)
        {
            Transform[] transforms = owner.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (ContainsName(transforms[index].name, fragment))
                    return true;
            }

            return false;
        }

        private static bool ContainsPenetratingTerrainName(Transform owner)
        {
            Transform[] transforms = owner.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                string objectName = transforms[index].name;
                if (ContainsName(objectName, "_Env_Rock_") ||
                    ContainsName(objectName, "_Env_Boulder_") ||
                    ContainsName(objectName, "_Env_Cliff_"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMeaningfulTerrainStructureOverlap(Bounds structure, Bounds terrain)
        {
            float overlapX = Mathf.Min(structure.max.x, terrain.max.x) - Mathf.Max(structure.min.x, terrain.min.x);
            float overlapZ = Mathf.Min(structure.max.z, terrain.max.z) - Mathf.Max(structure.min.z, terrain.min.z);
            if (overlapX < 0.65f || overlapZ < 0.65f || overlapX * overlapZ < 1.5f)
                return false;

            float overlapY = Mathf.Min(structure.max.y, terrain.max.y) - Mathf.Max(structure.min.y, terrain.min.y);
            return overlapY >= 0.35f && terrain.max.y >= structure.min.y + 0.35f;
        }

        private static bool IsPrimaryStructureOwner(Transform owner)
        {
            string objectName = owner.name;
            return ContainsName(objectName, "_Bld_Hall_") ||
                   ContainsName(objectName, "_Bld_Shop_") ||
                   ContainsName(objectName, "_Bld_Village_House_") ||
                   ContainsName(objectName, "_Bld_GasStation_") ||
                   ContainsName(objectName, "_Bld_Ruins_") ||
                   ContainsName(objectName, "_Bld_Mosque_") ||
                   ContainsName(objectName, "_Bld_Barracks_") ||
                   ContainsName(objectName, "_Bld_Office_") ||
                   ContainsName(objectName, "_Bld_Tower_");
        }

        private static bool HasHighConfidenceTerrainStructurePenetration(Bounds structure, Bounds terrain)
        {
            bool terrainCenterInsideStructure =
                Mathf.Abs(terrain.center.x - structure.center.x) <= structure.size.x * 0.38f &&
                Mathf.Abs(terrain.center.z - structure.center.z) <= structure.size.z * 0.38f;
            return terrainCenterInsideStructure &&
                   terrain.max.y - structure.min.y >= 0.8f &&
                   HasMeaningfulTerrainStructureOverlap(structure, terrain);
        }

        private static Transform FindDistrictCompositionRoot(Transform module)
        {
            Transform[] transforms = module.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (string.Equals(transforms[index].name, "Art", StringComparison.Ordinal))
                    return transforms[index];
            }

            return module.childCount > 0 ? module.GetChild(0) : module;
        }

        private static string ClassifyDistrictCompositionOwner(Transform owner)
        {
            Transform[] transforms = owner.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                string objectName = transforms[index].name;
                if (ContainsName(objectName, "Runway") ||
                    ContainsName(objectName, "_Veh_Jet_") ||
                    ContainsName(objectName, "_Veh_TransportPlane_") ||
                    ContainsName(objectName, "_Bld_Hangar_"))
                {
                    return "airfield";
                }
            }

            for (int index = 0; index < transforms.Length; index++)
            {
                string objectName = transforms[index].name;
                if (ContainsName(objectName, "_Env_Road_"))
                    return "major-road";
                if (ContainsName(objectName, "_Env_DirtRoad_") ||
                    ContainsName(objectName, "_Env_Sidewalk_"))
                {
                    return "road";
                }
            }

            return "other";
        }

        public static void AnalyzeLocalRoadClearanceBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject sceneRoot = FindSceneRoot(scene);
            int importedIntersectionCount = CountLocalRoadClearanceIntersections(sceneRoot, true);
            int authoredIntersectionCount = CountAuthoredRoadClearanceIntersections(sceneRoot, true);
            Debug.Log(
                $"[M01LocalRoadClearance] result=Passed intersections={importedIntersectionCount + authoredIntersectionCount} " +
                $"imported={importedIntersectionCount} authored={authoredIntersectionCount}");
        }

        private static int CountLocalRoadClearanceIntersections(GameObject sceneRoot, bool logDetails)
        {
            Renderer[] renderers = sceneRoot.GetComponentsInChildren<Renderer>(true);
            var analyzedOwners = new HashSet<int>();
            int intersectionCount = 0;

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Transform owner = FindRoadClearanceOwner(renderers[rendererIndex].transform, sceneRoot.transform);
                if (owner == null || !analyzedOwners.Add(owner.GetEntityId().GetHashCode()))
                    continue;

                Renderer[] ownerRenderers = owner.GetComponentsInChildren<Renderer>(true);
                if (!TryGetCombinedBounds(ownerRenderers, out Bounds bounds))
                    continue;

                for (int segmentIndex = 0; segmentIndex < LocalRoadSegments.Length; segmentIndex++)
                {
                    LocalRoadSegmentDefinition segment = LocalRoadSegments[segmentIndex];
                    if (!IntersectsRoadCorridor(bounds, segment, 3.2f))
                        continue;

                    intersectionCount++;
                    if (logDetails)
                    {
                        Debug.Log(
                            $"[M01LocalRoadClearance] road={segment.Name} obstacle={GetTransformPath(owner, sceneRoot.transform)} " +
                            $"center={bounds.center} size={bounds.size}");
                    }
                }
            }

            return intersectionCount;
        }

        private static int CountAuthoredRoadClearanceIntersections(GameObject sceneRoot, bool logDetails)
        {
            Transform[] transforms = sceneRoot.GetComponentsInChildren<Transform>(true);
            int intersectionCount = 0;
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (!candidate.gameObject.activeInHierarchy || !IsAuthoredRoadClearanceObstacle(candidate.name))
                    continue;

                Renderer[] renderers = candidate.GetComponentsInChildren<Renderer>(true);
                if (!TryGetCombinedBounds(renderers, out Bounds bounds) || !IntersectsAnyLocalRoad(bounds, 3.2f))
                    continue;

                intersectionCount++;
                if (logDetails)
                {
                    Debug.Log(
                        $"[M01AuthoredRoadClearance] obstacle={GetTransformPath(candidate, sceneRoot.transform)} " +
                        $"center={bounds.center} size={bounds.size}");
                }
            }

            return intersectionCount;
        }

        private static bool IsAuthoredRoadClearanceObstacle(string objectName)
        {
            for (int index = 0; index < AuthoredRoadClearanceObstacleNames.Length; index++)
            {
                if (string.Equals(objectName, AuthoredRoadClearanceObstacleNames[index], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static Transform FindRoadClearanceOwner(Transform candidate, Transform sceneRoot)
        {
            Transform terrainOwner = null;
            Transform buildingOwner = null;
            for (Transform current = candidate; current != null && current != sceneRoot; current = current.parent)
            {
                if (IsRoadTerrainObstacleName(current.name))
                    terrainOwner = current;
                if (ContainsName(current.name, "_Bld_") || ContainsName(current.name, "Building_"))
                    buildingOwner = current;
            }

            Transform owner = buildingOwner != null ? buildingOwner : terrainOwner;
            if (owner == null)
                return null;

            Renderer[] ownerRenderers = owner.GetComponentsInChildren<Renderer>(true);
            if (!TryGetCombinedBounds(ownerRenderers, out Bounds bounds))
                return null;

            bool terrainIsLargeEnough = terrainOwner == null || Mathf.Max(bounds.size.x, bounds.size.z) >= 3f;
            return terrainIsLargeEnough ? owner : null;
        }

        private static bool IsRoadTerrainObstacleName(string objectName)
        {
            return ContainsName(objectName, "_Env_Rock_") ||
                   ContainsName(objectName, "_Env_Boulder_") ||
                   ContainsName(objectName, "_Env_Cliff_") ||
                   ContainsName(objectName, "_Env_Hill_") ||
                   ContainsName(objectName, "_Env_SandDunes_");
        }

        private static bool IntersectsRoadCorridor(Bounds bounds, LocalRoadSegmentDefinition segment, float halfWidth)
        {
            Vector3 delta = segment.End - segment.Start;
            float length = delta.magnitude;
            if (length <= Mathf.Epsilon)
                return false;

            Vector3 forward = delta / length;
            Vector3 right = new(forward.z, 0f, -forward.x);
            Vector3 offset = bounds.center - (segment.Start + segment.End) * 0.5f;
            float centerAcross = Vector3.Dot(offset, right);
            float centerAlong = Vector3.Dot(offset, forward);
            float extentAcross = Mathf.Abs(right.x) * bounds.extents.x + Mathf.Abs(right.z) * bounds.extents.z;
            float extentAlong = Mathf.Abs(forward.x) * bounds.extents.x + Mathf.Abs(forward.z) * bounds.extents.z;
            return Mathf.Abs(centerAcross) <= halfWidth + extentAcross &&
                   Mathf.Abs(centerAlong) <= length * 0.5f + extentAlong;
        }

        private static bool IntersectsAnyLocalRoad(Bounds bounds, float halfWidth)
        {
            for (int segmentIndex = 0; segmentIndex < LocalRoadSegments.Length; segmentIndex++)
            {
                if (IntersectsRoadCorridor(bounds, LocalRoadSegments[segmentIndex], halfWidth))
                    return true;
            }

            return false;
        }

        private static bool ContainsRoadTerrainObstacleName(Transform owner)
        {
            Transform[] transforms = owner.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (IsRoadTerrainObstacleName(transforms[index].name))
                    return true;
            }

            return false;
        }

        private static bool ContainsImportedGroundSurfaceName(Transform owner)
        {
            Transform[] transforms = owner.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                string objectName = transforms[index].name;
                if (ContainsName(objectName, "_Env_Ground_") ||
                    ContainsName(objectName, "_Env_SandDunes_"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsExactOwnerName(string ownerName, string[] exactNames)
        {
            for (int index = 0; index < exactNames.Length; index++)
            {
                if (string.Equals(ownerName, exactNames[index], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool IsUnsupportedImportedGround(string moduleName, string ownerName)
        {
            return string.Equals(moduleName, "UtilityCompound_East_DemoAuthored", StringComparison.Ordinal) &&
                   IsExactOwnerName(ownerName, UnsupportedUtilityGroundNames);
        }

        private static Material SelectImportedGroundMaterial(string moduleName, string ownerName, Palette palette)
        {
            bool oldMarketBuildingSupport =
                string.Equals(moduleName, "OldMarket_West_DemoAuthored", StringComparison.Ordinal) &&
                IsExactOwnerName(ownerName, OldMarketBuildingSupportGroundNames);
            return oldMarketBuildingSupport ? palette.DistrictGround : palette.TransitionGround;
        }

        private static int OverrideRendererMaterials(Transform owner, Material material)
        {
            Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
            int overrideCount = 0;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    if (materials[materialIndex] == material)
                        continue;

                    materials[materialIndex] = material;
                    changed = true;
                }

                if (!changed)
                    continue;

                renderer.sharedMaterials = materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                overrideCount++;
            }

            return overrideCount;
        }

        private static string GetTransformPath(Transform transform, Transform stop)
        {
            string path = transform.name;
            for (Transform current = transform.parent; current != null && current != stop; current = current.parent)
                path = current.name + "/" + path;
            return path;
        }

        private static bool IsDistrictBuildingAssemblyCandidate(Transform candidate, Transform module)
        {
            if (candidate == null || candidate == module || candidate.childCount == 0)
                return false;

            string candidateName = candidate.name;
            bool buildingShell = ContainsName(candidateName, "_Bld_Shop_") ||
                                 ContainsName(candidateName, "_Bld_Village_House_") ||
                                 ContainsName(candidateName, "_Bld_GasStation_") ||
                                 ContainsName(candidateName, "_Bld_Ruins_") ||
                                 ContainsName(candidateName, "_Bld_Mosque_");
            if (!buildingShell)
                return false;

            Transform parent = candidate.parent;
            while (parent != null && parent != module)
            {
                if (ContainsName(parent.name, "_Bld_") && parent.childCount > 0)
                    return false;
                parent = parent.parent;
            }

            return parent == module;
        }

        private static bool TryGetCombinedBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            if (renderers == null || renderers.Length == 0)
                return false;

            bool assigned = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!assigned)
                {
                    bounds = renderer.bounds;
                    assigned = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return assigned;
        }

        private static bool IsDistrictEdgeCandidateName(string objectName)
        {
            return ContainsName(objectName, "_Env_Road_") ||
                   ContainsName(objectName, "_Env_Sidewalk_") ||
                   ContainsName(objectName, "_Prop_Powerpole_") ||
                   ContainsName(objectName, "_Prop_Street_Light_") ||
                   ContainsName(objectName, "_Prop_Washingline_") ||
                   ContainsName(objectName, "_Prop_Wire_Lights_") ||
                   ContainsName(objectName, "_Prop_Fence_");
        }

        private static bool ContainsName(string value, string fragment)
        {
            return value?.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static IReadOnlyList<string> ValidateRequiredAssets()
        {
            var missing = new List<string>();
            for (int i = 0; i < RequiredPrefabPaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(RequiredPrefabPaths[i]) == null)
                    missing.Add(RequiredPrefabPaths[i]);
            }

            return missing;
        }

        private static void GenerateScene()
        {
            IReadOnlyList<string> missing = ValidateRequiredAssets();
            if (missing.Count > 0)
                throw new InvalidOperationException("M01 visual palette is missing:\n" + string.Join("\n", missing));

            EnsureAssetDirectory("Assets/Game/Scenes/MapPrototypes/Chapter01");
            EnsureAssetDirectory(MaterialDirectory);

            Palette palette = CreateOrUpdateMaterials();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "M01_VisualPrototype";

            ConfigureRenderSettings();

            GameObject sceneRoot = new("M01_VisualPrototype_Root");
            GameObject generatedRoot = CreateRoot("_M01VisualGenerated", sceneRoot.transform);
            GameObject authoredRoot = CreateRoot("_M01AuthoredStoryOverrides", sceneRoot.transform);
            GameObject cameraRoot = CreateRoot("_M01ReviewCameras", sceneRoot.transform);
            CreateRoot($"GENERATOR_{GeneratorVersion}_SEED_{GenerationSeed}", sceneRoot.transform);

            CreateTerrainAndRoadPlan(generatedRoot.transform, palette);
            CreateAuthoredDistrictModules(generatedRoot.transform, palette);
            CreateOldMarketStoryLayer(authoredRoot.transform, palette);
            CreateCompoundStoryLayer(authoredRoot.transform, palette);
            CreateBombingAftermath(authoredRoot.transform);
            CreateCivilianEdgeStoryLayer(authoredRoot.transform, palette);
            CreateHorizonAndEdgeDressing(generatedRoot.transform, palette);
            CreateLighting(sceneRoot.transform);
            CreateReviewCameras(cameraRoot.transform);

            SimulateParticles();
            ValidateGeneratedScene(sceneRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException($"Could not save M01 visual prototype: {ScenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = sceneRoot;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log($"[M01VisualMap] generated version={GeneratorVersion} seed={GenerationSeed} fingerprint={ComputeSceneFingerprint(sceneRoot)} scene={ScenePath}");
        }

        private static Palette CreateOrUpdateMaterials()
        {
            return new Palette
            {
                Sand = CreateOrUpdateMaterial(SandMaterialPath, new Color(0.38f, 0.285f, 0.19f), 0f, 0.12f),
                Asphalt = CreateOrUpdateMaterial(AsphaltMaterialPath, new Color(0.09f, 0.085f, 0.08f), 0f, 0.18f),
                Concrete = CreateOrUpdateMaterial(ConcreteMaterialPath, new Color(0.35f, 0.33f, 0.29f), 0f, 0.18f),
                Curb = CreateOrUpdateMaterial(CurbMaterialPath, new Color(0.69f, 0.62f, 0.50f), 0f, 0.22f),
                WhitePaint = CreateOrUpdateMaterial(WhitePaintMaterialPath, new Color(0.88f, 0.84f, 0.70f), 0f, 0.25f),
                AmberPaint = CreateOrUpdateMaterial(AmberPaintMaterialPath, new Color(0.95f, 0.52f, 0.08f), 0f, 0.27f),
                Turquoise = CreateOrUpdateMaterial(TurquoiseMaterialPath, new Color(0.035f, 0.36f, 0.36f), 0f, 0.25f),
                Rust = CreateOrUpdateMaterial(RustMaterialPath, new Color(0.38f, 0.11f, 0.045f), 0.05f, 0.16f),
                DistrictGround = CreateOrUpdateMaterial(DistrictGroundMaterialPath, new Color(0.405f, 0.31f, 0.21f), 0f, 0.1f),
                TransitionGround = CreateOrUpdateMaterial(TransitionGroundMaterialPath, new Color(0.39f, 0.295f, 0.20f), 0f, 0.1f),
                DirtRoad = CreateOrUpdateMaterial(DirtRoadMaterialPath, new Color(0.32f, 0.235f, 0.145f), 0f, 0.08f)
            };
        }

        private static Material CreateOrUpdateMaterial(string path, Color color, float metallic, float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null)
                    throw new InvalidOperationException("No compatible lit shader was found for M01 prototype materials.");

                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.skybox = CreateOrUpdateSkyboxMaterial();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0022f;
            RenderSettings.fogColor = new Color(0.72f, 0.58f, 0.42f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.43f, 0.35f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.43f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.085f, 0.055f);
            RenderSettings.ambientIntensity = 1.4f;
            RenderSettings.reflectionIntensity = 0.85f;
        }

        private static Material CreateOrUpdateSkyboxMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DesertSkyboxMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader == null)
                    throw new InvalidOperationException("The procedural skybox shader is unavailable for M01.");
                material = new Material(shader) { name = "M01_DesertSkybox" };
                AssetDatabase.CreateAsset(material, DesertSkyboxMaterialPath);
            }

            material.SetColor("_SkyTint", new Color(0.43f, 0.57f, 0.68f, 1f));
            material.SetColor("_GroundColor", new Color(0.56f, 0.43f, 0.30f, 1f));
            material.SetFloat("_Exposure", 1.24f);
            material.SetFloat("_AtmosphereThickness", 0.72f);
            material.SetFloat("_SunSize", 0.035f);
            material.SetFloat("_SunSizeConvergence", 5f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateTerrainAndRoadPlan(Transform parent, Palette palette)
        {
            Transform terrainRoot = CreateRoot("01_Terrain_And_RoadPlan", parent).transform;
            CreateBox("DesertGround", new Vector3(0f, -1.05f, 16f), new Vector3(1200f, 2f, 900f), palette.Sand, terrainRoot);

            CreateIrregularSurface("CentralOperationGround", new Vector3(-18f, -0.065f, -5f), new Vector2(176f, 142f), palette.TransitionGround, terrainRoot, 2f);
            CreateIrregularSurface("OldMarketUtilityGroundLink", new Vector3(-29f, -0.035f, 9f), new Vector2(86f, 25f), palette.TransitionGround, terrainRoot, 3f);
            CreateIrregularSurface("CivilianFrontageTransition", new Vector3(-2f, -0.034f, -5f), new Vector2(62f, 28f), palette.TransitionGround, terrainRoot, -4f);
            CreateIrregularSurface("CivilianFrontageApron", new Vector3(-2f, -0.005f, -5f), new Vector2(50f, 21f), palette.DistrictGround, terrainRoot, 2f);

            CreateLocalRoadNetwork(terrainRoot, palette);
        }

        private static void CreateLocalRoadNetwork(Transform parent, Palette palette)
        {
            var roadSurfaces = new GameObject[LocalRoadSegments.Length];
            for (int i = 0; i < LocalRoadSegments.Length; i++)
                roadSurfaces[i] = CreateLocalRoadSegment(LocalRoadSegments[i], parent, palette);

            CreateLocalRoadNode(
                "MarketStreet_WestEnd",
                LocalRoadSegments[0].Start,
                parent,
                roadSurfaces[0].transform,
                palette);
            for (int i = 0; i < LocalRoadSegments.Length; i++)
            {
                CreateLocalRoadNode(
                    $"{LocalRoadSegments[i].Name}_End",
                    LocalRoadSegments[i].End,
                    parent,
                    roadSurfaces[i].transform,
                    palette);
            }
        }

        private static GameObject CreateLocalRoadSegment(
            LocalRoadSegmentDefinition segment,
            Transform parent,
            Palette palette)
        {
            Vector3 delta = segment.End - segment.Start;
            float length = delta.magnitude;
            Vector3 center = (segment.Start + segment.End) * 0.5f;
            Quaternion rotation = Quaternion.Euler(0f, Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 0f);
            float roadWidth = segment.Dusty ? LocalRoadWidth : 4.2f;

            CreateBox(
                $"{segment.Name}_Shoulder",
                center + Vector3.down * 0.025f,
                new Vector3(roadWidth + LocalRoadShoulderAllowance, 0.07f, length),
                palette.TransitionGround,
                parent,
                rotation);
            return CreateBox(
                segment.Name,
                center + Vector3.up * 0.035f,
                new Vector3(roadWidth, 0.12f, length),
                segment.Dusty ? palette.DirtRoad : palette.Asphalt,
                parent,
                rotation);
        }

        private static void CreateLocalRoadNode(
            string name,
            Vector3 position,
            Transform terrainParent,
            Transform roadOwner,
            Palette palette)
        {
            float shoulderWidth = LocalRoadWidth + LocalRoadShoulderAllowance;
            GameObject shoulder = CreateCylinder(
                $"{name}_ShoulderBlend",
                position + Vector3.down * 0.025f,
                new Vector3(shoulderWidth, 0.035f, shoulderWidth),
                palette.TransitionGround,
                terrainParent);
            shoulder.transform.SetParent(roadOwner, true);

            GameObject dirt = CreateCylinder(
                $"{name}_DirtBlend",
                position + Vector3.up * 0.035f,
                new Vector3(LocalRoadWidth, 0.06f, LocalRoadWidth),
                palette.DirtRoad,
                terrainParent);
            dirt.transform.SetParent(roadOwner, true);
        }

        private static void CreateAuthoredDistrictModules(Transform parent, Palette palette)
        {
            Transform modulesRoot = CreateRoot("02_DemoAuthored_DistrictModules", parent).transform;
            ApplyDistrictCuration(PlaceModule(TownMarketModulePath, "OldMarket_West_DemoAuthored", new Vector3(-68f, 0f, 12f), 0f, 0.82f, modulesRoot), palette);
            ApplyDistrictCuration(PlaceModule(BaseCommandModulePath, "UtilityCompound_East_DemoAuthored", new Vector3(23f, 0f, 13f), 180f, 0.76f, modulesRoot), palette);
            ApplyDistrictCuration(PlaceModule(SouthTownModulePath, "Residential_South_DemoAuthored", new Vector3(-5f, 0f, -54f), 0f, 0.58f, modulesRoot), palette);
        }

        private static void ApplyDistrictCuration(GameObject moduleObject, Palette palette)
        {
            if (!TryGetDistrictCurationDefinition(moduleObject.name, out DistrictCurationDefinition definition))
                throw new InvalidOperationException($"M01 district curation is missing for {moduleObject.name}.");

            Transform compositionRoot = FindDistrictCompositionRoot(moduleObject.transform);
            int envelopeExclusions = 0;
            int airfieldExclusions = 0;
            int majorRoadExclusions = 0;
            int remoteRoadExclusions = 0;
            int roadStructureExclusions = 0;
            int roadTerrainExclusions = 0;
            int frontageTerrainExclusions = 0;
            int unsupportedGroundExclusions = 0;
            int terrainMaterialOverrides = 0;
            int disabledRenderers = 0;
            for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
            {
                Transform owner = compositionRoot.GetChild(ownerIndex);
                if (!owner.gameObject.activeSelf)
                    continue;

                Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
                if (!TryGetCombinedBounds(renderers, out Bounds bounds))
                    continue;

                string category = ClassifyDistrictCompositionOwner(owner);
                bool outsideEnvelope = !definition.Contains(bounds.center);
                bool airfieldContent = definition.ExcludeAirfield &&
                                       string.Equals(category, "airfield", StringComparison.Ordinal);
                bool excludedRoadContent = definition.ExcludeRoads &&
                                           (string.Equals(category, "road", StringComparison.Ordinal) ||
                                            string.Equals(category, "major-road", StringComparison.Ordinal));
                bool majorRoadContent = string.Equals(category, "major-road", StringComparison.Ordinal);
                bool remoteRoadContent = IsRemoteLongRoad(moduleObject.transform, bounds, category, definition);
                bool roadStructureContent = ContainsDescendantName(owner, "_Bld_") && IntersectsAnyLocalRoad(bounds, 3.2f);
                bool roadTerrainContent = ContainsRoadTerrainObstacleName(owner) && IntersectsAnyLocalRoad(bounds, 3.2f);
                bool frontageObstacleContent = IntersectsFrontageObstacleClearance(owner, bounds);
                bool unsupportedGround = IsUnsupportedImportedGround(moduleObject.name, owner.name);
                if (!outsideEnvelope && !airfieldContent && !excludedRoadContent && !majorRoadContent && !remoteRoadContent && !roadStructureContent && !roadTerrainContent && !frontageObstacleContent && !unsupportedGround)
                {
                    if (ContainsImportedGroundSurfaceName(owner))
                    {
                        Material groundMaterial = SelectImportedGroundMaterial(moduleObject.name, owner.name, palette);
                        terrainMaterialOverrides += OverrideRendererMaterials(owner, groundMaterial);
                    }
                    continue;
                }

                owner.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(owner.gameObject);
                disabledRenderers += renderers.Length;
                if (outsideEnvelope)
                    envelopeExclusions++;
                if (airfieldContent)
                    airfieldExclusions++;
                if (excludedRoadContent && !majorRoadContent)
                    majorRoadExclusions++;
                if (majorRoadContent)
                    majorRoadExclusions++;
                if (remoteRoadContent)
                    remoteRoadExclusions++;
                if (roadStructureContent)
                    roadStructureExclusions++;
                if (roadTerrainContent)
                    roadTerrainExclusions++;
                if (frontageObstacleContent)
                    frontageTerrainExclusions++;
                if (unsupportedGround)
                    unsupportedGroundExclusions++;
            }

            int terrainClearanceAdjustments = ApplyTerrainStructureClearance(moduleObject.transform);

            Debug.Log(
                $"[M01DistrictCuration] module={moduleObject.name} envelopeExclusions={envelopeExclusions} " +
                $"airfieldExclusions={airfieldExclusions} majorRoadExclusions={majorRoadExclusions} " +
                $"remoteRoadExclusions={remoteRoadExclusions} roadStructureExclusions={roadStructureExclusions} " +
                $"roadTerrainExclusions={roadTerrainExclusions} frontageTerrainExclusions={frontageTerrainExclusions} " +
                $"unsupportedGroundExclusions={unsupportedGroundExclusions} " +
                $"terrainMaterialOverrides={terrainMaterialOverrides} terrainClearanceAdjustments={terrainClearanceAdjustments} " +
                $"disabledRenderers={disabledRenderers} " +
                $"bounds={definition.Minimum}->{definition.Maximum}");
        }

        private static int ApplyTerrainStructureClearance(Transform module)
        {
            Transform compositionRoot = FindDistrictCompositionRoot(module);
            var structures = new List<CompositionBoundsRecord>();
            var terrain = new List<CompositionBoundsRecord>();
            for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
            {
                Transform owner = compositionRoot.GetChild(ownerIndex);
                if (!owner.gameObject.activeInHierarchy)
                    continue;

                Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
                if (!TryGetCombinedBounds(renderers, out Bounds bounds))
                    continue;

                if (IsPrimaryStructureOwner(owner))
                    structures.Add(new CompositionBoundsRecord(owner, bounds));
                else if (ContainsPenetratingTerrainName(owner) && Mathf.Max(bounds.size.x, bounds.size.z) >= 3f)
                    terrain.Add(new CompositionBoundsRecord(owner, bounds));
            }

            int adjustmentCount = 0;
            for (int terrainIndex = 0; terrainIndex < terrain.Count; terrainIndex++)
            {
                CompositionBoundsRecord terrainRecord = terrain[terrainIndex];
                float requiredLowering = 0f;
                for (int structureIndex = 0; structureIndex < structures.Count; structureIndex++)
                {
                    Bounds structureBounds = structures[structureIndex].Bounds;
                    if (!HasHighConfidenceTerrainStructurePenetration(structureBounds, terrainRecord.Bounds))
                        continue;

                    requiredLowering = Mathf.Max(
                        requiredLowering,
                        terrainRecord.Bounds.max.y - structureBounds.min.y + 0.08f);
                }

                if (requiredLowering <= 0f)
                    continue;

                terrainRecord.Owner.position += Vector3.down * requiredLowering;
                PrefabUtility.RecordPrefabInstancePropertyModifications(terrainRecord.Owner);
                adjustmentCount++;
                Debug.Log(
                    $"[M01TerrainStructureClearance] module={module.name} terrain={terrainRecord.Owner.name} " +
                    $"lowered={requiredLowering:0.00}");
            }

            return adjustmentCount;
        }

        private static int CountHighConfidenceTerrainStructurePenetrations(GameObject sceneRoot, bool logDetails)
        {
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                return 1;

            int penetrationCount = 0;
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                Transform compositionRoot = FindDistrictCompositionRoot(module);
                var structures = new List<CompositionBoundsRecord>();
                var terrain = new List<CompositionBoundsRecord>();
                for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
                {
                    Transform owner = compositionRoot.GetChild(ownerIndex);
                    if (!owner.gameObject.activeInHierarchy)
                        continue;

                    Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
                    if (!TryGetCombinedBounds(renderers, out Bounds bounds))
                        continue;

                    if (IsPrimaryStructureOwner(owner))
                        structures.Add(new CompositionBoundsRecord(owner, bounds));
                    else if (ContainsPenetratingTerrainName(owner) && Mathf.Max(bounds.size.x, bounds.size.z) >= 3f)
                        terrain.Add(new CompositionBoundsRecord(owner, bounds));
                }

                for (int structureIndex = 0; structureIndex < structures.Count; structureIndex++)
                {
                    for (int terrainIndex = 0; terrainIndex < terrain.Count; terrainIndex++)
                    {
                        if (!HasHighConfidenceTerrainStructurePenetration(
                                structures[structureIndex].Bounds,
                                terrain[terrainIndex].Bounds))
                        {
                            continue;
                        }

                        penetrationCount++;
                        if (logDetails)
                        {
                            Debug.Log(
                                $"[M01TerrainStructureClearanceViolation] module={module.name} " +
                                $"structure={structures[structureIndex].Owner.name} terrain={terrain[terrainIndex].Owner.name}");
                        }
                    }
                }
            }

            return penetrationCount;
        }

        private static int CountCrossModulePrimaryStructureOverlaps(GameObject sceneRoot, bool logDetails)
        {
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                return 1;

            var moduleStructures = new List<List<CompositionBoundsRecord>>(modulesRoot.childCount);
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                Transform compositionRoot = FindDistrictCompositionRoot(module);
                var structures = new List<CompositionBoundsRecord>();
                for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
                {
                    Transform owner = compositionRoot.GetChild(ownerIndex);
                    if (!owner.gameObject.activeInHierarchy || !IsPrimaryStructureOwner(owner))
                        continue;

                    Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
                    if (TryGetCombinedBounds(renderers, out Bounds bounds))
                        structures.Add(new CompositionBoundsRecord(owner, bounds));
                }

                moduleStructures.Add(structures);
            }

            int overlapCount = 0;
            for (int firstModuleIndex = 0; firstModuleIndex < moduleStructures.Count; firstModuleIndex++)
            {
                List<CompositionBoundsRecord> firstStructures = moduleStructures[firstModuleIndex];
                for (int secondModuleIndex = firstModuleIndex + 1; secondModuleIndex < moduleStructures.Count; secondModuleIndex++)
                {
                    List<CompositionBoundsRecord> secondStructures = moduleStructures[secondModuleIndex];
                    for (int firstIndex = 0; firstIndex < firstStructures.Count; firstIndex++)
                    {
                        CompositionBoundsRecord first = firstStructures[firstIndex];
                        for (int secondIndex = 0; secondIndex < secondStructures.Count; secondIndex++)
                        {
                            CompositionBoundsRecord second = secondStructures[secondIndex];
                            float overlapX = Mathf.Min(first.Bounds.max.x, second.Bounds.max.x) -
                                             Mathf.Max(first.Bounds.min.x, second.Bounds.min.x);
                            float overlapY = Mathf.Min(first.Bounds.max.y, second.Bounds.max.y) -
                                             Mathf.Max(first.Bounds.min.y, second.Bounds.min.y);
                            float overlapZ = Mathf.Min(first.Bounds.max.z, second.Bounds.max.z) -
                                             Mathf.Max(first.Bounds.min.z, second.Bounds.min.z);
                            if (overlapX < 0.75f || overlapY < 0.5f || overlapZ < 0.75f || overlapX * overlapZ < 1.5f)
                                continue;

                            overlapCount++;
                            if (logDetails)
                            {
                                Debug.Log(
                                    $"[M01CrossModuleStructureOverlap] first={first.Owner.name} second={second.Owner.name} " +
                                    $"overlap=({overlapX:0.00},{overlapY:0.00},{overlapZ:0.00})");
                            }
                        }
                    }
                }
            }

            return overlapCount;
        }

        private static int CountAuthoredTransitionStructureOverlaps(GameObject sceneRoot, bool logDetails)
        {
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                return 1;

            var moduleStructures = new List<CompositionBoundsRecord>();
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform compositionRoot = FindDistrictCompositionRoot(modulesRoot.GetChild(moduleIndex));
                for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
                {
                    Transform owner = compositionRoot.GetChild(ownerIndex);
                    if (!owner.gameObject.activeInHierarchy || !IsPrimaryStructureOwner(owner))
                        continue;

                    Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
                    if (TryGetCombinedBounds(renderers, out Bounds bounds))
                        moduleStructures.Add(new CompositionBoundsRecord(owner, bounds));
                }
            }

            var authoredStructures = new List<CompositionBoundsRecord>(AuthoredTransitionStructureNames.Length);
            for (int nameIndex = 0; nameIndex < AuthoredTransitionStructureNames.Length; nameIndex++)
            {
                GameObject structureObject = GameObject.Find(AuthoredTransitionStructureNames[nameIndex]);
                if (structureObject == null)
                    return 1;

                Renderer[] renderers = structureObject.GetComponentsInChildren<Renderer>(true);
                if (TryGetCombinedBounds(renderers, out Bounds bounds))
                    authoredStructures.Add(new CompositionBoundsRecord(structureObject.transform, bounds));
            }

            int overlapCount = 0;
            for (int authoredIndex = 0; authoredIndex < authoredStructures.Count; authoredIndex++)
            {
                CompositionBoundsRecord authored = authoredStructures[authoredIndex];
                for (int moduleIndex = 0; moduleIndex < moduleStructures.Count; moduleIndex++)
                {
                    if (HasMeaningfulStructureOverlap(authored.Bounds, moduleStructures[moduleIndex].Bounds))
                    {
                        overlapCount++;
                        if (logDetails)
                        {
                            Debug.Log(
                                $"[M01AuthoredTransitionOverlap] authored={authored.Owner.name} " +
                                $"module={moduleStructures[moduleIndex].Owner.name}");
                        }
                    }
                }

                for (int secondAuthoredIndex = authoredIndex + 1; secondAuthoredIndex < authoredStructures.Count; secondAuthoredIndex++)
                {
                    if (!HasMeaningfulStructureOverlap(authored.Bounds, authoredStructures[secondAuthoredIndex].Bounds))
                        continue;

                    overlapCount++;
                    if (logDetails)
                    {
                        Debug.Log(
                            $"[M01AuthoredTransitionOverlap] authored={authored.Owner.name} " +
                            $"other={authoredStructures[secondAuthoredIndex].Owner.name}");
                    }
                }
            }

            return overlapCount;
        }

        private static bool HasMeaningfulStructureOverlap(Bounds first, Bounds second)
        {
            float overlapX = Mathf.Min(first.max.x, second.max.x) - Mathf.Max(first.min.x, second.min.x);
            float overlapY = Mathf.Min(first.max.y, second.max.y) - Mathf.Max(first.min.y, second.min.y);
            float overlapZ = Mathf.Min(first.max.z, second.max.z) - Mathf.Max(first.min.z, second.min.z);
            return overlapX >= 0.75f && overlapY >= 0.5f && overlapZ >= 0.75f && overlapX * overlapZ >= 1.5f;
        }

        private static bool IntersectsFrontageObstacleClearance(Transform owner, Bounds bounds)
        {
            bool penetratingTerrain = ContainsPenetratingTerrainName(owner);
            bool largeVegetation = ContainsDescendantName(owner, "_Env_Tree_Big_") &&
                                   Mathf.Max(bounds.size.x, bounds.size.z) >= 1.5f;
            if (!penetratingTerrain && !largeVegetation)
                return false;

            Vector2 center = new(bounds.center.x, bounds.center.z);
            if ((penetratingTerrain || largeVegetation) &&
                center.x >= -48f && center.x <= 35f &&
                center.y >= -28f && center.y <= -7f)
            {
                return true;
            }

            if (!penetratingTerrain)
                return false;

            for (int zoneIndex = 0; zoneIndex < FrontageTerrainClearanceZones.Length; zoneIndex++)
            {
                TerrainClearanceZone zone = FrontageTerrainClearanceZones[zoneIndex];
                if ((center - zone.Center).sqrMagnitude <= zone.Radius * zone.Radius)
                    return true;
            }

            return false;
        }

        private static bool IsRemoteLongRoad(
            Transform module,
            Bounds bounds,
            string category,
            DistrictCurationDefinition definition)
        {
            if (definition.RemoteRoadDistance <= 0f ||
                definition.RemoteRoadSize <= 0f ||
                !string.Equals(category, "road", StringComparison.Ordinal))
            {
                return false;
            }

            Vector2 offset = new(bounds.center.x - module.position.x, bounds.center.z - module.position.z);
            float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
            return offset.magnitude >= definition.RemoteRoadDistance &&
                   horizontalSize >= definition.RemoteRoadSize;
        }

        private static bool TryGetDistrictCurationDefinition(
            string moduleName,
            out DistrictCurationDefinition definition)
        {
            for (int index = 0; index < DistrictCurationDefinitions.Length; index++)
            {
                if (!string.Equals(DistrictCurationDefinitions[index].ModuleName, moduleName, StringComparison.Ordinal))
                    continue;

                definition = DistrictCurationDefinitions[index];
                return true;
            }

            definition = default;
            return false;
        }

        private static int CountDistrictCurationViolations(GameObject sceneRoot, bool logDetails)
        {
            Transform modulesRoot = sceneRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (modulesRoot == null)
                return 1;

            int violationCount = 0;
            for (int moduleIndex = 0; moduleIndex < modulesRoot.childCount; moduleIndex++)
            {
                Transform module = modulesRoot.GetChild(moduleIndex);
                if (!TryGetDistrictCurationDefinition(module.name, out DistrictCurationDefinition definition))
                {
                    violationCount++;
                    continue;
                }

                Transform compositionRoot = FindDistrictCompositionRoot(module);
                for (int ownerIndex = 0; ownerIndex < compositionRoot.childCount; ownerIndex++)
                {
                    Transform owner = compositionRoot.GetChild(ownerIndex);
                    if (!owner.gameObject.activeInHierarchy)
                        continue;

                    Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
                    if (!TryGetCombinedBounds(renderers, out Bounds bounds))
                        continue;

                    string category = ClassifyDistrictCompositionOwner(owner);
                    bool outsideEnvelope = !definition.Contains(bounds.center);
                    bool airfieldContent = definition.ExcludeAirfield &&
                                           string.Equals(category, "airfield", StringComparison.Ordinal);
                    bool excludedRoadContent = definition.ExcludeRoads &&
                                               (string.Equals(category, "road", StringComparison.Ordinal) ||
                                                string.Equals(category, "major-road", StringComparison.Ordinal));
                    bool majorRoadContent = string.Equals(category, "major-road", StringComparison.Ordinal);
                    bool remoteRoadContent = IsRemoteLongRoad(module, bounds, category, definition);
                    bool frontageObstacleContent = IntersectsFrontageObstacleClearance(owner, bounds);
                    if (!outsideEnvelope && !airfieldContent && !excludedRoadContent && !majorRoadContent && !remoteRoadContent && !frontageObstacleContent)
                        continue;

                    violationCount++;
                    if (logDetails)
                    {
                        Debug.Log(
                            $"[M01DistrictCurationViolation] module={module.name} owner={owner.name} " +
                            $"center={bounds.center} outsideEnvelope={outsideEnvelope} airfield={airfieldContent} " +
                            $"excludedRoad={excludedRoadContent} majorRoad={majorRoadContent} remoteRoad={remoteRoadContent} " +
                            $"frontageObstacle={frontageObstacleContent}");
                    }
                }
            }

            return violationCount;
        }

        private static void CreateOldMarketStoryLayer(Transform parent, Palette palette)
        {
            Transform root = CreateRoot("03_OldMarket_StoryLayer", parent).transform;

            PlacePrefab("Assets/Game/Prefabs/Environment/City/Clock_Tower_01.prefab", "OldMarketClockTower", new Vector3(-42f, 0f, 42f), 18f, 1.1f, root);
            PlacePrefab("Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_ArchwayBridge_01.prefab", "OldMarketArchway", new Vector3(-19f, 0f, 24f), 90f, 1.05f, root);
            CreateIrregularSurface("ResidentialTransitionCourtyard", new Vector3(-23f, -0.01f, -30f), new Vector2(24f, 16f), palette.DistrictGround, root, -8f);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_03.prefab", "ResidentialTransitionHouse", new Vector3(-29f, 0f, -31f), 8f, 0.88f, root);

            string[] stalls =
            {
                "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Stall_01.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/SM_Bld_Shops_ClothCover_Large_01.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/SM_Bld_Shops_ClothCover_Small_01.prefab"
            };
            Vector3[] stallPositions =
            {
                new(-30f, 0f, -1f), new(-43f, 0f, -3f), new(-57f, 0f, -2f),
                new(-31f, 0f, 13f), new(-46f, 0f, 14f), new(-60f, 0f, 13f)
            };
            for (int i = 0; i < stallPositions.Length; i++)
                PlacePrefab(stalls[i % stalls.Length], $"MarketStall_{i + 1:00}", stallPositions[i], i < 3 ? 0f : 180f, 1f, root);

            string[] marketProps =
            {
                "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Basket_01.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Basket_03.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Crate_Wood_01.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Crate_Wood_03.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/Rugs/SM_Prop_Rug_Rolls_01.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/Rugs/SM_Prop_Rug_Pile_01.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Copper_Pot_Small_01.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_TeaPot_01.prefab"
            };
            var random = new System.Random(GenerationSeed);
            for (int i = 0; i < 34; i++)
            {
                float x = -67f + (float)random.NextDouble() * 44f;
                float z = -8f + (float)random.NextDouble() * 35f;
                float yaw = random.Next(0, 360);
                float scale = 0.82f + (float)random.NextDouble() * 0.34f;
                PlacePrefab(marketProps[i % marketProps.Length], $"MarketMicroProp_{i + 1:00}", new Vector3(x, 0f, z), yaw, scale, root);
            }

            for (int i = 0; i < 5; i++)
            {
                PlacePrefab($"Assets/PolygonMilitary/Prefabs/Props/Signs/SM_Prop_Sign_Shop_0{(i % 7) + 1}.prefab", $"MarketSign_{i + 1:00}", new Vector3(-31f - i * 9f, 3.2f, 5.8f + (i % 2) * 12f), i % 2 == 0 ? 0f : 180f, 1f, root);
            }

            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Wire_Lights_01.prefab", "MarketWireLights_A", new Vector3(-42f, 5.8f, 5f), 0f, 1.15f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Wire_Lights_02.prefab", "MarketWireLights_B", new Vector3(-52f, 5.5f, 17f), 90f, 1.1f, root);
            CreatePointLight("MarketWarmLight_A", new Vector3(-42f, 7f, 2f), new Color(1f, 0.56f, 0.24f), 3.4f, 23f, root);
            CreatePointLight("MarketWarmLight_B", new Vector3(-54f, 6f, 17f), new Color(1f, 0.64f, 0.32f), 2.6f, 19f, root);

            CreateBox("MarketTurquoiseAwning", new Vector3(-26f, 3.9f, 14f), new Vector3(9f, 0.18f, 6f), palette.Turquoise, root, Quaternion.Euler(0f, 0f, -4f));
        }

        private static void CreateCompoundStoryLayer(Transform parent, Palette palette)
        {
            Transform root = CreateRoot("04_UtilityCompound_StoryLayer", parent).transform;

            CreateIrregularSurface("CompoundCourtyard", new Vector3(25f, -0.005f, 35f), new Vector2(38f, 37f), palette.DistrictGround, root, -2f);
            PlacePrefab("Assets/Game/Prefabs/Buildings/Building_Hall.prefab", "CompoundOperationsHall", new Vector3(20f, 0f, 35f), 180f, 0.72f, root);
            PlacePrefab("Assets/Game/Prefabs/Buildings/Building_Barrack.prefab", "CompoundServiceBuilding", new Vector3(34f, 0f, 36f), 180f, 0.62f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Group_03.prefab", "CompoundCheckpoint", new Vector3(16.5f, 0f, 18.5f), 78f, 0.9f, root);
            PlacePrefab("Assets/Game/Prefabs/Buildings/Building_GuardTower.prefab", "CompoundGuardTower", new Vector3(39f, 0f, 49f), 180f, 0.78f, root);
            PlacePrefab("Assets/Game/Prefabs/Buildings/Building_WaterTank.prefab", "CompoundWaterTank", new Vector3(36f, 0f, 23f), 20f, 0.76f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Crate_Stack_Cover_02.prefab", "CompoundSupplies_A", new Vector3(16f, 0f, 23f), 12f, 0.9f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Crate_Stack_03.prefab", "CompoundSupplies_B", new Vector3(22f, 0f, 23f), 84f, 0.9f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Generator_Large_01.prefab", "CompoundGenerator", new Vector3(29f, 0f, 23f), 90f, 0.85f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_LightPole_01.prefab", "CompoundLightPole", new Vector3(12f, 0f, 39f), 0f, 0.9f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Signs/SM_Prop_Sign_Safety_03.prefab", "CompoundWarningSign", new Vector3(10f, 2.2f, 20f), 78f, 1f, root);

            CreateBox("CompoundWall_West", new Vector3(7f, 1.2f, 36f), new Vector3(0.75f, 2.4f, 34f), palette.Concrete, root);
            CreateBox("CompoundWall_East", new Vector3(44f, 1.2f, 36f), new Vector3(0.75f, 2.4f, 34f), palette.Concrete, root);
            CreateBox("CompoundWall_North", new Vector3(25.5f, 1.2f, 53f), new Vector3(37.75f, 2.4f, 0.75f), palette.Concrete, root);
            CreateBox("CompoundWall_SouthWest", new Vector3(10f, 1.2f, 19f), new Vector3(6f, 2.4f, 0.75f), palette.Concrete, root);
            CreateBox("CompoundWall_SouthEast", new Vector3(32f, 1.2f, 19f), new Vector3(24f, 2.4f, 0.75f), palette.Concrete, root);
            Vector3[] pillarPositions =
            {
                new(7f, 1.55f, 19f), new(14f, 1.55f, 19f), new(20f, 1.55f, 19f), new(44f, 1.55f, 19f),
                new(7f, 1.55f, 53f), new(44f, 1.55f, 53f)
            };
            for (int i = 0; i < pillarPositions.Length; i++)
                CreateBox($"CompoundWallPillar_{i + 1:00}", pillarPositions[i], new Vector3(1.2f, 3.1f, 1.2f), palette.Curb, root);

            CreateBox("CompoundSecurityStripe_A", new Vector3(13f, 0.08f, 22f), new Vector3(0.35f, 0.08f, 5f), palette.AmberPaint, root, Quaternion.Euler(0f, -12f, 0f));
            CreateBox("CompoundSecurityStripe_B", new Vector3(15f, 0.08f, 22.5f), new Vector3(0.35f, 0.08f, 5f), palette.Rust, root, Quaternion.Euler(0f, -12f, 0f));
            CreatePointLight("CompoundSecurityLight", new Vector3(15f, 8f, 31f), new Color(0.62f, 0.78f, 1f), 2.2f, 24f, root);
        }

        private static void CreateBombingAftermath(Transform parent)
        {
            Transform root = CreateRoot("05_BombingAftermath_StoryLayer", parent).transform;

            PlacePrefab("Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Crater_02.prefab", "ImpactCrater", new Vector3(5f, 0.2f, -5f), 10f, 1.25f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Truck_01_Destroyed.prefab", "DestroyedAidTruck", new Vector3(4f, 0f, -1.5f), 12f, 1.05f, root);
            PlacePrefab("Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Ruins_03.prefab", "BombedCornerRuin", new Vector3(16f, 0f, -4f), 188f, 1.05f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/FX/FX_Fire_01.prefab", "AftermathFire", new Vector3(4.5f, 0.7f, -2f), 0f, 0.72f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/FX/FX_Smoke_Large_01.prefab", "AftermathSmoke", new Vector3(5.5f, 0f, -3f), 0f, 0.32f, root);

            string[] debris =
            {
                "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_01.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_03.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_DebrisPile_02.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Vehicle_Debris_04.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Block_02.prefab"
            };
            var random = new System.Random(GenerationSeed + 77);
            for (int i = 0; i < 18; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = 3f + (float)random.NextDouble() * 7f;
                Vector3 position = new(6f + Mathf.Cos(angle) * radius, 0f, -4f + Mathf.Sin(angle) * radius * 0.55f);
                PlacePrefab(debris[i % debris.Length], $"AftermathDebris_{i + 1:00}", position, random.Next(0, 360), 0.8f + (float)random.NextDouble() * 0.7f, root);
            }

            for (int i = 0; i < 4; i++)
            {
                PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Barrier_Tall_02.prefab", $"EmergencyBarrier_{i + 1:00}", new Vector3(-2f + i * 2.4f, 0f, 5.1f + (i % 2) * 0.35f), 0f, 1f, root);
            }

            CreatePointLight("AftermathFireLight", new Vector3(5f, 4f, -2f), new Color(1f, 0.21f, 0.045f), 5.5f, 24f, root);
        }

        private static void CreateCivilianEdgeStoryLayer(Transform parent, Palette palette)
        {
            Transform root = CreateRoot("06_CivilianEdge_StoryLayer", parent).transform;

            CreateIrregularSurface("CivilianFrontageCourtyard_West", new Vector3(-30f, -0.006f, -15f), new Vector2(29f, 15f), palette.DistrictGround, root, -2f);
            CreateIrregularSurface("CivilianFrontageCourtyard_East", new Vector3(26f, -0.006f, -17f), new Vector2(19f, 15f), palette.DistrictGround, root, 4f);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_02.prefab", "CivilianFrontageHouse_West", new Vector3(-39f, 0f, -15f), 8f, 0.82f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_05.prefab", "CivilianFrontageHouse_Center", new Vector3(-22f, 0f, -16f), 352f, 0.76f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_House_02.prefab", "CivilianFrontageHouse_East", new Vector3(26f, 0f, -17f), 185f, 0.76f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Fence_02.prefab", "CivilianFrontageFence_West", new Vector3(-39f, 0f, -20f), 90f, 0.9f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Fence_02.prefab", "CivilianFrontageFence_Center", new Vector3(-22f, 0f, -21f), 90f, 0.82f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_Fence_02.prefab", "CivilianFrontageFence_East", new Vector3(26f, 0f, -22f), 90f, 0.88f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Wood_01.prefab", "CivilianFrontageCart_West", new Vector3(-31f, 0f, -10f), 22f, 0.92f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Wood_01.prefab", "CivilianFrontageCart_East", new Vector3(19f, 0f, -11f), 338f, 0.88f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_LightPole_01.prefab", "CivilianFrontageLightPole_West", new Vector3(-46f, 0f, -8f), 0f, 0.88f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_LightPole_01.prefab", "CivilianFrontageLightPole_East", new Vector3(34f, 0f, -9f), 0f, 0.88f, root);

            PlacePrefab("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Refugee_01.prefab", "CivilianAidTent", new Vector3(-8f, 0f, -1f), 92f, 0.92f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Tent_Refugee_Damaged_01.prefab", "DamagedCivilianTent", new Vector3(-16f, 0f, -6f), 108f, 0.82f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Signs/SM_Prop_Sign_Medical_01.prefab", "CivilianAidMedicalSign", new Vector3(-4f, 0f, -5f), 8f, 1.08f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_MedicalBox_01.prefab", "CivilianAidMedicalBox", new Vector3(-9f, 0f, -5f), 30f, 1f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Bed_Medical_01.prefab", "CivilianAidBed", new Vector3(-11f, 0f, -1f), 86f, 0.95f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Crate_Stack_Cover_02.prefab", "CivilianAidSupplies", new Vector3(-15f, 0f, -1f), 22f, 0.82f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_LightPole_01.prefab", "CivilianAidLightPole", new Vector3(-6f, 0f, -7f), 0f, 0.9f, root);
            CreatePointLight("CivilianAidWarmLight", new Vector3(-10f, 6f, -1f), new Color(1f, 0.62f, 0.34f), 2.2f, 18f, root);
        }

        private static void CreateHorizonAndEdgeDressing(Transform parent, Palette palette)
        {
            Transform root = CreateRoot("07_Horizon_And_EdgeDressing", parent).transform;
            GameObject westDunes = PlacePrefab("Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_03.prefab", "HorizonDunes_West", new Vector3(-142f, -2.8f, 154f), 18f, 0.9f, root);
            GameObject westCenterDunes = PlacePrefab("Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_02.prefab", "HorizonDunes_WestCenter", new Vector3(-72f, -3f, 162f), -12f, 0.78f, root);
            GameObject centerDunes = PlacePrefab("Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_01.prefab", "HorizonDunes_Center", new Vector3(0f, -2.9f, 168f), 0f, 1.02f, root);
            GameObject eastCenterDunes = PlacePrefab("Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_03.prefab", "HorizonDunes_EastCenter", new Vector3(74f, -3f, 160f), 16f, 0.78f, root);
            GameObject eastDunes = PlacePrefab("Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_02.prefab", "HorizonDunes_East", new Vector3(144f, -2.8f, 152f), -24f, 0.9f, root);
            OverrideRendererMaterials(westDunes.transform, palette.TransitionGround);
            OverrideRendererMaterials(westCenterDunes.transform, palette.TransitionGround);
            OverrideRendererMaterials(centerDunes.transform, palette.TransitionGround);
            OverrideRendererMaterials(eastCenterDunes.transform, palette.TransitionGround);
            OverrideRendererMaterials(eastDunes.transform, palette.TransitionGround);
        }

        private static void ValidateGeneratedScene(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length < 900)
                throw new InvalidOperationException($"M01 visual prototype is below the expected reference density: renderers={renderers.Length}");

            Bounds bounds = CalculateBounds(root);
            if (bounds.size.x < 180f || bounds.size.z < 150f)
                throw new InvalidOperationException($"M01 visual prototype bounds are too small: {bounds.size}");

            if (GameObject.Find("OldMarketClockTower") == null || GameObject.Find("DestroyedAidTruck") == null || GameObject.Find("M01_Review_GameplayOverview") == null)
                throw new InvalidOperationException("M01 visual prototype is missing a required visual anchor or review camera.");

            int roadPolicyViolations = CountLocalRoadPolicyViolations(true);
            if (roadPolicyViolations != 0)
                throw new InvalidOperationException($"M01 road plan violates {roadPolicyViolations} narrow local-route constraints.");

            int roadClearanceIntersections = CountLocalRoadClearanceIntersections(root, true);
            if (roadClearanceIntersections != 0)
                throw new InvalidOperationException($"M01 local roads intersect {roadClearanceIntersections} building or large-terrain bounds.");

            int authoredRoadClearanceIntersections = CountAuthoredRoadClearanceIntersections(root, true);
            if (authoredRoadClearanceIntersections != 0)
                throw new InvalidOperationException($"M01 local roads intersect {authoredRoadClearanceIntersections} authored story structures.");

            int districtCurationViolations = CountDistrictCurationViolations(root, true);
            if (districtCurationViolations != 0)
                throw new InvalidOperationException($"M01 district curation left {districtCurationViolations} active edge, airfield, or major-road objects.");

            int terrainStructurePenetrations = CountHighConfidenceTerrainStructurePenetrations(root, true);
            if (terrainStructurePenetrations != 0)
                throw new InvalidOperationException($"M01 district curation left {terrainStructurePenetrations} high-confidence terrain/structure penetrations.");

            int crossModuleStructureOverlaps = CountCrossModulePrimaryStructureOverlaps(root, true);
            if (crossModuleStructureOverlaps != 0)
                throw new InvalidOperationException($"M01 district placement left {crossModuleStructureOverlaps} cross-module primary-structure overlaps.");

            int authoredTransitionOverlaps = CountAuthoredTransitionStructureOverlaps(root, true);
            if (authoredTransitionOverlaps != 0)
                throw new InvalidOperationException($"M01 transition composition left {authoredTransitionOverlaps} authored structure overlaps.");
        }

        private static void SimulateParticles()
        {
            GameObject aftermathRootObject = GameObject.Find("05_BombingAftermath_StoryLayer");
            Transform aftermathRoot = aftermathRootObject != null ? aftermathRootObject.transform : null;
            ParticleSystem[] particleSystems = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                bool isAuthoredAftermath = aftermathRoot != null && particleSystems[i].transform.IsChildOf(aftermathRoot);
                if (!isAuthoredAftermath)
                {
                    particleSystems[i].gameObject.SetActive(false);
                    continue;
                }

                try
                {
                    particleSystems[i].Simulate(1.6f, true, true, true);
                    particleSystems[i].Pause(true);
                }
                catch (InvalidOperationException)
                {
                    // Some nested particle systems cannot be simulated independently in edit mode.
                }
            }
        }

        private static GameObject PlaceModule(string prefabPath, string name, Vector3 center, float yaw, float scale, Transform parent)
        {
            GameObject wrapper = CreateRoot(name, parent);

            GameObject prefab = RequirePrefab(prefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate authored M01 module: {prefabPath}");

            instance.name = name + "_Source";
            instance.transform.SetParent(wrapper.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            Bounds bounds = CalculateBounds(instance);
            instance.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            wrapper.transform.position = center;
            wrapper.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            wrapper.transform.localScale = Vector3.one * scale;
            return wrapper;
        }

        private static GameObject PlacePrefab(string prefabPath, string name, Vector3 position, float yaw, float scale, Transform parent)
        {
            GameObject prefab = RequirePrefab(prefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate M01 palette prefab: {prefabPath}");

            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one * scale;
            return instance;
        }

        private static GameObject RequirePrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                throw new InvalidOperationException($"Missing M01 visual palette prefab: {path}");
            return prefab;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static GameObject CreateRoot(string name, Transform parent)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            return root;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Quaternion? rotation = null)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.rotation = rotation ?? Quaternion.identity;
            box.transform.localScale = scale;
            Renderer renderer = box.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            Object.DestroyImmediate(box.GetComponent<Collider>());
            GameObjectUtility.SetStaticEditorFlags(box, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            return box;
        }

        private static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.position = position;
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            return cylinder;
        }

        private static GameObject CreateIrregularSurface(
            string name,
            Vector3 position,
            Vector2 size,
            Material material,
            Transform parent,
            float yaw = 0f)
        {
            GameObject surface = Game.Runtime.RuntimeOperationMapSurfaceGeometrySystemHelper.CreateIrregularSurface(
                name,
                unchecked((uint)GenerationSeed),
                parent);
            surface.transform.position = position;
            surface.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            surface.transform.localScale = new Vector3(size.x, 1f, size.y);
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(surface, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            return surface;
        }

        private static Light CreatePointLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent)
        {
            GameObject lightObject = new(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            return light;
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
        }
    }
#endif
}
