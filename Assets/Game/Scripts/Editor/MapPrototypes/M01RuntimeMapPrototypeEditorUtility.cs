namespace Game.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Configs;
    using Game.Runtime;
    using Unity.Entities;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class M01RuntimeMapPrototypeEditorUtility
    {
        public const string ScenePath = "Assets/Game/Scenes/MapPrototypes/Chapter01/M01_RuntimeGenerationPrototype.unity";
        public const string ConfigPath = "Assets/Game/Configs/MapPrototypes/M01_RuntimeCity_Config.asset";
        public const string VisualRecipePath = "Assets/Game/Configs/MapPrototypes/M01_RuntimeVisualRecipe.asset";
        private const string SourceConfigPath = "Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset";
        private const string VisualRecipeVersion = "M01RuntimeVisualRecipe_2026-07-17_v23_dense_district_core";
        private const string DistrictSnapshotFolder = "Assets/Game/Prefabs/MapPrototypes/M01/RuntimeParity";
        private const int MaxDistrictSliceRenderers = 64;
        private const string PremiumLightingRigPath = "Assets/Game/Rendering/Prefabs/PremiumLightingRig.prefab";
        private const string PrototypeVolumeProfilePath = "Assets/Game/Art/MapPrototypes/M01/M01_VisualVolumeProfile.asset";
        private const string DesertSkyboxMaterialPath = "Assets/Game/Art/MapPrototypes/M01/M01_DesertSkybox.mat";
        private const string SandMaterialPath = "Assets/Game/Art/MapPrototypes/M01/Materials/M01_Sand.mat";
        private const string AsphaltMaterialPath = "Assets/Game/Art/MapPrototypes/M01/Materials/M01_Asphalt.mat";
        private const string DistrictGroundMaterialPath = "Assets/Game/Art/MapPrototypes/M01/Materials/M01_DistrictGround.mat";
        private const uint PrototypeSeed = 26071501u;
        private const string RuntimeCapturePath = "Logs/M01_RuntimeGenerationBaseline.png";
        private const string RuntimeTopDownCapturePath = "Logs/M01_RuntimeGenerationTopDown.png";
        private const string RuntimeMarketRevealCapturePath = "Logs/M01_RuntimeGenerationReveal_Market.png";
        private const string EditorCapturePath = "Logs/M01_EditorCurrentReference.png";
        private const string EditorTopDownCapturePath = "Logs/M01_EditorCurrentTopDown.png";
        private const string EditorVisualManifestPath = "Logs/M01_EditorVisualManifest.txt";
        private const string RuntimeVisualManifestPath = "Logs/M01_RuntimeVisualManifest.txt";
        private const string EditorRendererPathReportPath = "Logs/M01_EditorRendererPaths.txt";
        private const string RuntimeRendererPathReportPath = "Logs/M01_RuntimeRendererPaths.txt";
        private static readonly Vector3 AcceptedGameplayCameraPosition = new(-105f, 58f, -72f);
        private static readonly Vector3 AcceptedGameplayCameraTarget = new(-24f, 1f, -8f);
        private const float AcceptedGameplayCameraFieldOfView = 49f;
        private static readonly Vector3 AcceptedTopDownCameraPosition = new(-10f, 260f, -18f);
        private const float AcceptedTopDownCameraSize = 116f;
        private static readonly string[] M01HallPrefabPaths =
        {
            "Assets/Game/Prefabs/Environment/City/Hall_01.prefab",
            "Assets/Game/Prefabs/Environment/City/Hall_02.prefab"
        };
        private static readonly string[] M01ShopPrefabPaths =
        {
            "Assets/Game/Prefabs/Environment/City/Shop_01.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_02.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_03.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_04.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_05.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_06.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_07.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_08.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_09.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_10.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_11.prefab",
            "Assets/Game/Prefabs/Environment/City/Shop_12.prefab"
        };
        private static readonly string[] M01HousePrefabPaths =
        {
            "Assets/Game/Prefabs/Environment/City/House_01.prefab",
            "Assets/Game/Prefabs/Environment/City/House_02.prefab",
            "Assets/Game/Prefabs/Environment/City/House_03.prefab",
            "Assets/Game/Prefabs/Environment/City/House_04.prefab",
            "Assets/Game/Prefabs/Environment/City/House_05.prefab",
            "Assets/Game/Prefabs/Environment/City/House_06.prefab",
            "Assets/Game/Prefabs/Environment/City/House_07.prefab"
        };
        private static readonly string[] M01AftermathDressingPrefabPaths =
        {
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_03.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_DebrisPile_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_DebrisPile_03.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Block_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Tire_01.prefab"
        };
        private static double _playModeDeadline;
        private static double _playModeStartedAt;
        private static double _playModeCaptureAfter;
        private static bool _playModeCaptureComplete;
        private static bool _playModeMarketCaptureComplete;
        private static double _playModeMarketCaptureAfter;
        private static int _playModeExitCode;
        private static double _algorithmicReviewDeadline;
        private static double _algorithmicReviewCaptureAfter;
        private static double _algorithmicReviewExitAfter;
        private static bool _algorithmicReviewCaptureComplete;
        private static int _algorithmicReviewExitAfterFrame;
        private static uint _algorithmicReviewSeed;
        private static int _algorithmicReviewExitCode;
        private static RuntimeCitySpawnerSystemConfig _algorithmicReviewConfig;
        private static double _lifecycleRecoveryDeadline;
        private static int _lifecycleRecoveryExitCode;
        private static int _lifecycleRecoveryCancelFrame;
        private static bool _lifecycleRecoveryCancellationObserved;
        private static RuntimeCitySpawnerSystemConfig _lifecycleRecoveryConfig;
        private static LifecycleRecoveryValidationPhase _lifecycleRecoveryPhase;
        private static List<string> _visualParityRendererPaths;

        private enum LifecycleRecoveryValidationPhase
        {
            WaitingForGeneration,
            WaitingForCancellation,
            WaitingForFallbackStart,
            WaitingForFallbackCompletion
        }

        [MenuItem("Game/Map Prototypes/M01/Build Runtime Generation Prototype")]
        public static void BuildPrototype()
        {
            CreateOrUpdateConfig();
            CreateOrUpdateVisualRecipe();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "M01_RuntimeGenerationPrototype";
            RuntimeCitySpawnerSystemConfig config =
                AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(ConfigPath);
            RuntimeOperationMapVisualRecipe visualRecipe =
                AssetDatabase.LoadAssetAtPath<RuntimeOperationMapVisualRecipe>(VisualRecipePath);
            if (config == null || visualRecipe == null)
                throw new InvalidOperationException("Could not reload generated M01 runtime assets after snapshot import.");

            GameObject root = new("M01_RuntimeGeneration_RnD");
            Transform environment = CreateRoot("01_Environment", root.transform);
            Transform generated = CreateRoot("02_GeneratedMap", root.transform);
            CreateLighting(environment);
            Camera camera = CreateCamera(environment);
            TextMesh statusText = CreateStatusText(camera.transform);
            CreateRuntimeView(root.transform, generated, config, visualRecipe, camera, statusText);

            EnsureDirectoryForAsset(ScenePath);
            if (!EditorSceneManager.SaveScene(scene, ScenePath, saveAsCopy: false))
                throw new InvalidOperationException($"Could not save runtime M01 prototype scene at {ScenePath}.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;
            Debug.Log(
                $"[M01RuntimeMapPrototype] result=Built version={RuntimeCityGenerationProgress.VersionTag} " +
                $"seed={PrototypeSeed} scene={ScenePath} config={ConfigPath}");
        }

        public static void BuildValidateAndExit()
        {
            try
            {
                BuildPrototype();
                ValidatePrototype();
                Debug.Log("[M01RuntimeMapPrototypeValidation] result=Passed");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[M01RuntimeMapPrototypeValidation] result=Failed");
                EditorApplication.Exit(1);
            }
        }

        public static void RunPlayModeSmokeAndExit()
        {
            try
            {
                ValidatePrototype();
                VisualParityManifest editorManifest = BuildEditorVisualParityManifest();
                WriteVisualParityManifest(EditorVisualManifestPath, editorManifest);
                CaptureEditorParityCameras();
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                _playModeStartedAt = EditorApplication.timeSinceStartup;
                _playModeDeadline = _playModeStartedAt + 120d;
                _playModeCaptureAfter = 0d;
                _playModeCaptureComplete = false;
                _playModeMarketCaptureComplete = false;
                _playModeMarketCaptureAfter = 0d;
                _playModeExitCode = 1;
                EditorApplication.update += MonitorPlayModeSmoke;
                EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[M01RuntimeMapPlayModeSmoke] result=Failed reason=startupException");
                EditorApplication.Exit(1);
            }
        }

        public static void RunAlgorithmicSeedReviewAndExit()
        {
            try
            {
                ValidatePrototype();
                _algorithmicReviewSeed = ReadCommandLineUInt("-m01ReviewSeed", PrototypeSeed);
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                RuntimeCityRAndDMapView view = UnityEngine.Object.FindAnyObjectByType<RuntimeCityRAndDMapView>();
                if (view == null)
                    throw new InvalidOperationException("Runtime M01 prototype has no map view for algorithmic review.");

                RuntimeCitySpawnerSystemConfig sourceConfig = AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(ConfigPath);
                if (sourceConfig == null)
                    throw new InvalidOperationException($"Missing runtime prototype config: {ConfigPath}");

                _algorithmicReviewConfig = ScriptableObject.CreateInstance<RuntimeCitySpawnerSystemConfig>();
                _algorithmicReviewConfig.name = $"M01_AlgorithmicReview_{_algorithmicReviewSeed}";
                EditorUtility.CopySerialized(sourceConfig, _algorithmicReviewConfig);
                var configSerialized = new SerializedObject(_algorithmicReviewConfig);
                configSerialized.FindProperty("randomSeed").longValue = _algorithmicReviewSeed;
                configSerialized.ApplyModifiedPropertiesWithoutUndo();

                var viewSerialized = new SerializedObject(view);
                viewSerialized.FindProperty("config").objectReferenceValue = _algorithmicReviewConfig;
                viewSerialized.FindProperty("visualRecipe").objectReferenceValue = null;
                viewSerialized.FindProperty("showDebugOverlay").boolValue = true;
                viewSerialized.ApplyModifiedPropertiesWithoutUndo();

                _algorithmicReviewDeadline = EditorApplication.timeSinceStartup + 120d;
                _algorithmicReviewCaptureAfter = 0d;
                _algorithmicReviewExitAfter = 0d;
                _algorithmicReviewCaptureComplete = false;
                _algorithmicReviewExitAfterFrame = 0;
                _algorithmicReviewExitCode = 1;
                EditorApplication.update += MonitorAlgorithmicSeedReview;
                EditorApplication.playModeStateChanged += HandleAlgorithmicReviewPlayModeStateChanged;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[M01AlgorithmicSeedReview] result=Failed reason=startupException");
                DestroyAlgorithmicReviewConfig();
                EditorApplication.Exit(1);
            }
        }

        public static void RunLifecycleRecoveryValidationAndExit()
        {
            try
            {
                ValidatePrototype();
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                RuntimeCityRAndDMapView view = UnityEngine.Object.FindAnyObjectByType<RuntimeCityRAndDMapView>();
                if (view == null)
                    throw new InvalidOperationException("Runtime M01 prototype has no map view for lifecycle recovery validation.");

                RuntimeCitySpawnerSystemConfig sourceConfig =
                    AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(ConfigPath);
                if (sourceConfig == null)
                    throw new InvalidOperationException($"Missing runtime prototype config: {ConfigPath}");

                _lifecycleRecoveryConfig = ScriptableObject.CreateInstance<RuntimeCitySpawnerSystemConfig>();
                _lifecycleRecoveryConfig.name = "M01_LifecycleRecoveryValidation";
                EditorUtility.CopySerialized(sourceConfig, _lifecycleRecoveryConfig);

                var viewSerialized = new SerializedObject(view);
                viewSerialized.FindProperty("config").objectReferenceValue = _lifecycleRecoveryConfig;
                viewSerialized.FindProperty("visualRecipe").objectReferenceValue = null;
                viewSerialized.FindProperty("showDebugOverlay").boolValue = true;
                viewSerialized.ApplyModifiedPropertiesWithoutUndo();

                _lifecycleRecoveryDeadline = EditorApplication.timeSinceStartup + 120d;
                _lifecycleRecoveryExitCode = 1;
                _lifecycleRecoveryCancelFrame = -1;
                _lifecycleRecoveryCancellationObserved = false;
                _lifecycleRecoveryPhase = LifecycleRecoveryValidationPhase.WaitingForGeneration;
                EditorApplication.update += MonitorLifecycleRecoveryValidation;
                EditorApplication.playModeStateChanged += HandleLifecycleRecoveryPlayModeStateChanged;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[M01RuntimeMapLifecycleRecoveryValidation] result=Failed reason=startupException");
                DestroyLifecycleRecoveryConfig();
                EditorApplication.Exit(1);
            }
        }

        public static void CaptureCurrentEditorReferenceAndExit()
        {
            try
            {
                EditorSceneManager.OpenScene(M01VisualMapPrototypeEditorUtility.ScenePath, OpenSceneMode.Single);
                CaptureEditorParityCameras();
                Debug.Log(
                    $"[M01CurrentEditorReferenceCapture] result=Passed " +
                    $"perspective={EditorCapturePath} topDown={EditorTopDownCapturePath}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[M01CurrentEditorReferenceCapture] result=Failed");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Game/Map Prototypes/M01/Validate Runtime Generation Prototype")]
        public static void ValidatePrototype()
        {
            RuntimeCitySpawnerSystemConfig config = AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(ConfigPath);
            RuntimeOperationMapVisualRecipe visualRecipe = AssetDatabase.LoadAssetAtPath<RuntimeOperationMapVisualRecipe>(VisualRecipePath);
            Assert(config != null, $"Missing runtime prototype config: {ConfigPath}");
            Assert(config.SpawnOnStart, "Runtime prototype config must enable spawn on start.");
            Assert(config.GenerateBuildings, "Runtime prototype config must generate buildings.");
            Assert(config.CityCount == 1, "The first runtime parity slice must generate exactly one city.");
            Assert(config.RandomSeed == PrototypeSeed, "Runtime prototype seed drifted from the reviewed value.");
            Assert(config.HallPrefabs.Count > 0, "Runtime prototype config needs a hall palette.");
            Assert(config.ShopPrefabs.Count > 0, "Runtime prototype config needs a shop palette.");
            Assert(config.HousePrefabs.Count > 0, "Runtime prototype config needs a house palette.");
            Assert(visualRecipe != null, $"Missing runtime visual recipe: {VisualRecipePath}");
            Assert(visualRecipe.RecipeVersion == VisualRecipeVersion, "Runtime visual recipe version drifted.");
            Assert(visualRecipe.Seed == PrototypeSeed, "Runtime visual recipe seed drifted.");
            Assert(visualRecipe.Foundation.IsConfigured, "Runtime visual recipe must declare a continuous foundation.");
            Assert(visualRecipe.Reveal.GetMinimumDuration(RuntimeOperationMapVisualStage.TerrainAndRoads) > 0f,
                "Runtime visual recipe must stage the terrain reveal over visible time.");
            Assert(visualRecipe.CameraPoses.Count == 6,
                $"Runtime visual recipe must define one camera pose per reveal stage: actual={visualRecipe.CameraPoses.Count}.");
            var cameraStages = new HashSet<RuntimeOperationMapVisualStage>();
            for (int i = 0; i < visualRecipe.CameraPoses.Count; i++)
            {
                RuntimeOperationMapCameraPose pose = visualRecipe.CameraPoses[i];
                Assert(pose.IsConfigured, $"Runtime camera pose {i} is not configured.");
                Assert(cameraStages.Add(pose.Stage), $"Runtime camera stage {pose.Stage} is duplicated.");
            }
            Assert(visualRecipe.Entries.Count >= 150, $"Runtime visual recipe is below editor-parity density: entries={visualRecipe.Entries.Count}.");
            Assert(visualRecipe.DistrictModules.Count == 3,
                $"Runtime visual recipe must define three compact M01 district modules: actual={visualRecipe.DistrictModules.Count}.");
            for (int i = 0; i < visualRecipe.DistrictModules.Count; i++)
            {
                RuntimeOperationMapDistrictModuleRecipe module = visualRecipe.DistrictModules[i];
                Assert(module != null && module.IsConfigured, $"Runtime district module {i} is not configured.");
                Assert(!module.RealizeCompletePrefab,
                    $"Runtime district module {i} must use bounded indexed slices after the exactness probe.");
                Assert(module.Slices.Count > 0,
                    $"Runtime district module {i} has no indexed slices.");
                for (int sliceIndex = 0; sliceIndex < module.Slices.Count; sliceIndex++)
                {
                    RuntimeOperationMapDistrictSliceRecipe slice = module.Slices[sliceIndex];
                    Assert(slice != null && slice.IsConfigured,
                        $"Runtime district module {i} slice {sliceIndex} is not configured.");
                }
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RuntimeCityRAndDMapView[] views = UnityEngine.Object.FindObjectsByType<RuntimeCityRAndDMapView>(FindObjectsInactive.Include);
            Assert(views.Length == 1, $"Expected one runtime map view in {ScenePath}, found {views.Length}.");
            Assert(ReferenceEquals(views[0].Config, config),
                "Runtime map view must reference the generated runtime-city config.");
            Assert(ReferenceEquals(views[0].VisualRecipe, visualRecipe),
                "Runtime map view must reference the generated visual recipe.");
            Assert(views[0].PresentationCamera != null, "Runtime map view must reference the staged presentation camera.");
            Assert(views[0].VisualRecipeFrameBudgetMilliseconds > 0f,
                "Runtime map view must declare a positive visual generation frame budget.");
            Assert(views[0].DeterministicFallbackEnabled, "Runtime map view must enable deterministic failure fallback.");
            Assert(ReferenceEquals(views[0].DeterministicFallbackRecipe, visualRecipe),
                "Runtime map view must retain the accepted M01 recipe as its deterministic fallback.");
            Assert(views[0].AlgorithmicReveal.GetMinimumDuration(RuntimeOperationMapVisualStage.Aftermath) >= 2f,
                "Algorithmic aftermath reveal must remain readable for at least two seconds.");
            Assert(views[0].AlgorithmicAftermath.FallbackAnchorSpacingInRoadCells > 0f,
                "Algorithmic aftermath must declare deterministic district fallback anchors for sparse seeds.");
            Assert(views[0].AlgorithmicAftermath.MinimumAuthoredAnchorGroups >= 2,
                "Algorithmic aftermath must reserve authored incident anchors across dense seeds.");
            UnityEngine.Object expectedVolumeProfile =
                AssetDatabase.LoadMainAssetAtPath(PrototypeVolumeProfilePath);
            Component volume = Array.Find(
                UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include),
                component => component != null && string.Equals(component.GetType().Name, "Volume", StringComparison.Ordinal));
            Assert(volume != null, "Runtime prototype must contain its global post-processing volume.");
            var volumeSerialized = new SerializedObject(volume);
            Assert(ReferenceEquals(
                    volumeSerialized.FindProperty("sharedProfile")?.objectReferenceValue,
                    expectedVolumeProfile),
                "Runtime prototype volume must reference the exact M01 visual profile.");
            Transform statusView = views[0].PresentationCamera.transform.Find("RuntimeGenerationStatusView");
            Assert(statusView != null, "Runtime generation camera must contain its loading status view.");
            RuntimeOperationMapCameraPose aftermathPose = default;
            Assert(TryFindCameraPose(views[0].AlgorithmicCameraPoses, RuntimeOperationMapVisualStage.Aftermath, out aftermathPose),
                "Algorithmic camera path must contain an aftermath pose.");
            float visibleHalfHeight = statusView.localPosition.z * Mathf.Tan(aftermathPose.FieldOfView * 0.5f * Mathf.Deg2Rad);
            Assert(Mathf.Abs(statusView.localPosition.y) < visibleHalfHeight,
                "Runtime generation status view must stay inside the narrowest authored camera frustum.");
            Transform statusBackdrop = statusView.Find("RuntimeGenerationStatusBackdrop");
            Assert(statusBackdrop != null, "Runtime generation status view must contain its backdrop.");
            float backdropBottom = statusView.localPosition.y + statusBackdrop.localPosition.y -
                                   (statusBackdrop.localScale.y * 0.5f);
            float backdropTop = statusView.localPosition.y + statusBackdrop.localPosition.y +
                                (statusBackdrop.localScale.y * 0.5f);
            float visibleHalfWidth = visibleHalfHeight * (16f / 9f);
            float backdropLeft = statusView.localPosition.x + statusBackdrop.localPosition.x -
                                 (statusBackdrop.localScale.x * 0.5f);
            float backdropRight = statusView.localPosition.x + statusBackdrop.localPosition.x +
                                  (statusBackdrop.localScale.x * 0.5f);
            Assert(backdropBottom > -visibleHalfHeight && backdropTop < visibleHalfHeight,
                "Runtime generation status backdrop must fit vertically inside the aftermath camera frustum.");
            Assert(backdropLeft > -visibleHalfWidth && backdropRight < visibleHalfWidth,
                "Runtime generation status backdrop must fit horizontally inside the aftermath camera frustum.");
            Assert(views[0].AlgorithmicCameraPoses.Count == 6,
                $"Algorithmic runtime path must define one camera pose per reveal stage: actual={views[0].AlgorithmicCameraPoses.Count}.");
            var algorithmicCameraStages = new HashSet<RuntimeOperationMapVisualStage>();
            for (int i = 0; i < views[0].AlgorithmicCameraPoses.Count; i++)
            {
                RuntimeOperationMapCameraPose pose = views[0].AlgorithmicCameraPoses[i];
                Assert(pose.IsConfigured, $"Algorithmic camera pose {i} is not configured.");
                Assert(algorithmicCameraStages.Add(pose.Stage), $"Algorithmic camera stage {pose.Stage} is duplicated.");
            }
            Assert(scene.GetRootGameObjects().Length == 1, "Runtime M01 prototype must keep one scene-owned root.");
            Assert(Array.FindIndex(EditorBuildSettings.scenes, entry => string.Equals(entry.path, ScenePath, StringComparison.Ordinal)) < 0,
                "R&D runtime prototype scene must stay out of production build settings.");

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(roots[i]);
                Assert(missingScripts == 0, $"Scene root {roots[i].name} has {missingScripts} missing script reference(s).");
            }
        }

        private static RuntimeCitySpawnerSystemConfig CreateOrUpdateConfig()
        {
            EnsureDirectoryForAsset(ConfigPath);
            RuntimeCitySpawnerSystemConfig config = AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(ConfigPath);
            if (config == null)
            {
                if (!AssetDatabase.CopyAsset(SourceConfigPath, ConfigPath))
                    throw new InvalidOperationException($"Could not clone runtime city config from {SourceConfigPath}.");
                AssetDatabase.ImportAsset(ConfigPath, ImportAssetOptions.ForceSynchronousImport);
                config = AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(ConfigPath);
            }

            if (config == null)
                throw new InvalidOperationException($"Could not load runtime city prototype config at {ConfigPath}.");

            var serialized = new SerializedObject(config);
            SetBool(serialized, "spawnOnStart", true);
            SetBool(serialized, "generateBuildings", true);
            SetInteger(serialized, "randomSeed", unchecked((int)PrototypeSeed));
            SetInteger(serialized, "cityCount", 1);
            SetVector2Int(serialized, "startCell", new Vector2Int(256, 256));
            SetInteger(serialized, "generationYieldInterval", 1);
            SetInteger(serialized, "gasStationCount", 2);
            SetInteger(serialized, "shopCount", 28);
            SetInteger(serialized, "houseCount", 46);
            SetInteger(serialized, "otherBuildingCount", 12);
            SetInteger(serialized, "cityDecorationBuildingCount", 14);
            SetInteger(serialized, "extraTownRadiusRoadCells", 0);
            SetFloat(serialized, "ruralHouseRatio", 0.30f);
            SetFloat(serialized, "houseWallChance", 0.15f);
            SetPrefabList(serialized, "hallPrefabs", M01HallPrefabPaths);
            SetPrefabList(serialized, "shopPrefabs", M01ShopPrefabPaths);
            SetPrefabList(serialized, "housePrefabs", M01HousePrefabPaths);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static RuntimeOperationMapVisualRecipe CreateOrUpdateVisualRecipe()
        {
            Scene sourceScene = EditorSceneManager.OpenScene(M01VisualMapPrototypeEditorUtility.ScenePath, OpenSceneMode.Single);
            GameObject sourceRoot = Array.Find(
                sourceScene.GetRootGameObjects(),
                candidate => string.Equals(candidate.name, "M01_VisualPrototype_Root", StringComparison.Ordinal));
            if (sourceRoot == null)
                throw new InvalidOperationException("Accepted M01 editor prototype root was not found.");

            Transform terrainAndRoadPlan =
                sourceRoot.transform.Find("_M01VisualGenerated/01_Terrain_And_RoadPlan");
            var entries = new List<RuntimeOperationMapVisualEntry>(256);
            CaptureGroup(terrainAndRoadPlan, RuntimeOperationMapVisualStage.TerrainAndRoads, entries);
            entries.RemoveAll(entry => string.Equals(entry.Name, "DesertGround", StringComparison.Ordinal));
            List<RuntimeOperationMapDistrictModuleRecipe> districtModules = CaptureDistrictModuleGroup(
                sourceRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules"));
            CaptureGroup(sourceRoot.transform.Find("_M01AuthoredStoryOverrides/03_OldMarket_StoryLayer"), RuntimeOperationMapVisualStage.Market, entries);
            CaptureGroup(sourceRoot.transform.Find("_M01AuthoredStoryOverrides/04_UtilityCompound_StoryLayer"), RuntimeOperationMapVisualStage.Compound, entries);
            CaptureGroup(sourceRoot.transform.Find("_M01AuthoredStoryOverrides/05_BombingAftermath_StoryLayer"), RuntimeOperationMapVisualStage.Aftermath, entries);
            CaptureGroup(sourceRoot.transform.Find("_M01AuthoredStoryOverrides/06_CivilianEdge_StoryLayer"), RuntimeOperationMapVisualStage.Aftermath, entries);
            CaptureGroup(sourceRoot.transform.Find("_M01VisualGenerated/07_Horizon_And_EdgeDressing"), RuntimeOperationMapVisualStage.Horizon, entries);
            RemoveStaleParitySnapshots(districtModules, entries);

            if (entries.Count < 150)
                throw new InvalidOperationException($"Accepted M01 scene produced too few runtime visual entries: {entries.Count}.");

            EnsureDirectoryForAsset(VisualRecipePath);
            RuntimeOperationMapVisualRecipe recipe = AssetDatabase.LoadAssetAtPath<RuntimeOperationMapVisualRecipe>(VisualRecipePath);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<RuntimeOperationMapVisualRecipe>();
                recipe.name = "M01_RuntimeVisualRecipe";
                AssetDatabase.CreateAsset(recipe, VisualRecipePath);
            }

            RuntimeOperationMapFoundationSettings foundation =
                CaptureEditorFoundation(terrainAndRoadPlan);
            var reveal = new RuntimeOperationMapRevealSettings(
                terrainSeconds: 0.65f,
                districtSeconds: 0.85f,
                marketRevealSeconds: 1.0f,
                compoundRevealSeconds: 0.85f,
                aftermathRevealSeconds: 0.75f,
                horizonRevealSeconds: 0.65f);
            List<RuntimeOperationMapCameraPose> cameraPoses = CreateCameraPoses();
            recipe.ReplaceGeneratedEntries(
                VisualRecipeVersion,
                PrototypeSeed,
                foundation,
                reveal,
                cameraPoses,
                districtModules,
                entries);
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        private static void RemoveStaleParitySnapshots(
            IReadOnlyList<RuntimeOperationMapDistrictModuleRecipe> districtModules,
            IReadOnlyList<RuntimeOperationMapVisualEntry> entries)
        {
            var retainedPaths = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < districtModules.Count; i++)
            {
                GameObject prefab = districtModules[i].Prefab;
                if (prefab != null)
                    retainedPaths.Add(AssetDatabase.GetAssetPath(prefab));
            }

            for (int i = 0; i < entries.Count; i++)
            {
                GameObject prefab = entries[i].Prefab;
                if (prefab == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(prefab);
                if (path.StartsWith(DistrictSnapshotFolder + "/", StringComparison.Ordinal))
                    retainedPaths.Add(path);
            }

            string[] snapshotGuids = AssetDatabase.FindAssets("t:Prefab", new[] { DistrictSnapshotFolder });
            int deleted = 0;
            for (int i = 0; i < snapshotGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(snapshotGuids[i]);
                if (retainedPaths.Contains(path))
                    continue;

                if (!AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException($"Could not remove stale M01 parity snapshot: {path}");
                deleted++;
            }

            Debug.Log($"[M01RuntimeParitySnapshots] retained={retainedPaths.Count} deleted={deleted}");
        }

        private static List<RuntimeOperationMapCameraPose> CreateCameraPoses()
        {
            return new List<RuntimeOperationMapCameraPose>
            {
                new(
                    RuntimeOperationMapVisualStage.TerrainAndRoads,
                    new Vector3(-5f, 128f, -150f),
                    new Vector3(0f, 0f, -8f),
                    48f,
                    0f),
                new(
                    RuntimeOperationMapVisualStage.DistrictModules,
                    new Vector3(-125f, 78f, -120f),
                    new Vector3(-10f, 2f, -5f),
                    50f,
                    0.7f),
                new(
                    RuntimeOperationMapVisualStage.Market,
                    new Vector3(-112f, 48f, -68f),
                    new Vector3(-48f, 3f, 10f),
                    50f,
                    0.55f),
                new(
                    RuntimeOperationMapVisualStage.Compound,
                    new Vector3(-65f, 52f, -84f),
                    new Vector3(40f, 4f, 10f),
                    50f,
                    0.5f),
                new(
                    RuntimeOperationMapVisualStage.Aftermath,
                    new Vector3(-52f, 38f, -54f),
                    new Vector3(2f, 1f, -3f),
                    44f,
                    0.45f),
                new(
                    RuntimeOperationMapVisualStage.Horizon,
                    AcceptedGameplayCameraPosition,
                    AcceptedGameplayCameraTarget,
                    AcceptedGameplayCameraFieldOfView,
                    0.5f)
            };
        }

        private static RuntimeOperationMapFoundationSettings CaptureEditorFoundation(Transform terrainAndRoadPlan)
        {
            Transform ground = terrainAndRoadPlan != null
                ? terrainAndRoadPlan.Find("DesertGround")
                : null;
            MeshRenderer renderer = ground != null ? ground.GetComponent<MeshRenderer>() : null;
            Material material = renderer != null ? renderer.sharedMaterial : null;
            if (ground == null || material == null)
                throw new InvalidOperationException("Accepted M01 editor prototype is missing its DesertGround material.");
            if (Quaternion.Angle(ground.rotation, Quaternion.identity) > 0.01f)
                throw new InvalidOperationException("Runtime foundation replay currently requires an unrotated editor DesertGround.");

            Color color = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.HasProperty("_Color")
                    ? material.color
                    : Color.white;
            return new RuntimeOperationMapFoundationSettings(
                material,
                ground.position,
                ground.lossyScale,
                color);
        }

        private static List<RuntimeOperationMapCameraPose> CreateAlgorithmicCameraPoses()
        {
            return new List<RuntimeOperationMapCameraPose>
            {
                new(
                    RuntimeOperationMapVisualStage.TerrainAndRoads,
                    new Vector3(-110f, 90f, -145f),
                    new Vector3(-10f, 0f, -15f),
                    48f,
                    0f),
                new(
                    RuntimeOperationMapVisualStage.Market,
                    new Vector3(-115f, 58f, -90f),
                    new Vector3(-38f, 3f, -2f),
                    47f,
                    0.4f),
                new(
                    RuntimeOperationMapVisualStage.DistrictModules,
                    new Vector3(-105f, 68f, -120f),
                    new Vector3(0f, 2f, -15f),
                    49f,
                    0.55f),
                new(
                    RuntimeOperationMapVisualStage.Compound,
                    new Vector3(-45f, 52f, -105f),
                    new Vector3(35f, 3f, 15f),
                    46f,
                    0.45f),
                new(
                    RuntimeOperationMapVisualStage.Aftermath,
                    new Vector3(-102f, 34f, -112f),
                    new Vector3(-48f, 1.5f, -58f),
                    38f,
                    0.45f),
                new(
                    RuntimeOperationMapVisualStage.Horizon,
                    new Vector3(-115f, 78f, -130f),
                    new Vector3(-10f, 1f, -15f),
                    46f,
                    0.65f)
            };
        }

        private static void CaptureGroup(
            Transform group,
            RuntimeOperationMapVisualStage stage,
            List<RuntimeOperationMapVisualEntry> entries)
        {
            if (group == null)
                throw new InvalidOperationException($"Accepted M01 editor prototype is missing visual group for {stage}.");

            for (int i = 0; i < group.childCount; i++)
                CaptureVisual(group.GetChild(i), stage, entries);
        }

        private static List<RuntimeOperationMapDistrictModuleRecipe> CaptureDistrictModuleGroup(
            Transform group)
        {
            if (group == null)
                throw new InvalidOperationException("Accepted M01 editor prototype is missing its district modules.");

            var modules = new List<RuntimeOperationMapDistrictModuleRecipe>(group.childCount);
            for (int moduleIndex = 0; moduleIndex < group.childCount; moduleIndex++)
            {
                Transform module = group.GetChild(moduleIndex);

                Transform sourceInstance = null;
                for (int childIndex = 0; childIndex < module.childCount; childIndex++)
                {
                    Transform child = module.GetChild(childIndex);
                    if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == null)
                        continue;

                    sourceInstance = child;
                    break;
                }
                if (sourceInstance == null)
                    throw new InvalidOperationException($"Could not find the district prefab instance under {module.name}.");

                GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(sourceInstance.gameObject);
                if (sourcePrefab == null)
                    throw new InvalidOperationException($"Could not resolve district prefab source for {module.name}.");
                GameObject modulePrefab = CreateDistrictParitySnapshot(module, sourceInstance, moduleIndex);

                var slices = new List<RuntimeOperationMapDistrictSliceRecipe>(256);
                GameObject snapshotInstance = UnityEngine.Object.Instantiate(modulePrefab);
                try
                {
                    Transform snapshotRoot = snapshotInstance.transform;
                    snapshotRoot.SetPositionAndRotation(module.position, module.rotation);
                    snapshotRoot.localScale = module.lossyScale;
                    snapshotInstance.SetActive(module.gameObject.activeSelf);
                    AssertDistrictSnapshotParity(sourceInstance, snapshotRoot, module.name);
                    for (int childIndex = 0; childIndex < snapshotRoot.childCount; childIndex++)
                    {
                        CaptureDistrictPrefabSlice(
                            snapshotRoot.GetChild(childIndex),
                            snapshotRoot,
                            modulePrefab.transform,
                            slices);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(snapshotInstance);
                }

                if (slices.Count == 0)
                    throw new InvalidOperationException($"District module {module.name} produced no indexed slices.");
                AssertDistrictSlicePartition(modulePrefab.transform, slices, module.name);

                modules.Add(new RuntimeOperationMapDistrictModuleRecipe(
                    module.name,
                    modulePrefab,
                    sourceInstance.position,
                    sourceInstance.rotation,
                    sourceInstance.lossyScale,
                    sourceInstance.gameObject.activeInHierarchy,
                    default,
                    slices));
            }

            return modules;
        }

        private static void CaptureDistrictPrefabSlice(
            Transform candidate,
            Transform sceneModuleRoot,
            Transform prefabModuleRoot,
            List<RuntimeOperationMapDistrictSliceRecipe> slices)
        {
            Renderer[] renderers = candidate.GetComponentsInChildren<Renderer>(true);
            Component[] components = candidate.GetComponents<Component>();
            bool transformOnlyContainer = components.Length == 1 && components[0] is Transform;
            if (renderers.Length > MaxDistrictSliceRenderers &&
                candidate.childCount > 0 &&
                transformOnlyContainer)
            {
                for (int childIndex = 0; childIndex < candidate.childCount; childIndex++)
                {
                    CaptureDistrictPrefabSlice(
                        candidate.GetChild(childIndex),
                        sceneModuleRoot,
                        prefabModuleRoot,
                        slices);
                }
                return;
            }

            int[] siblingIndices = CalculateSiblingIndexPath(candidate, sceneModuleRoot);
            Transform prefabSlice = ResolveSiblingIndexPath(prefabModuleRoot, siblingIndices);
            if (prefabSlice == null)
            {
                throw new InvalidOperationException(
                    $"Could not resolve indexed district prefab slice for {sceneModuleRoot.name}/{candidate.name}.");
            }

            slices.Add(new RuntimeOperationMapDistrictSliceRecipe(
                candidate.name,
                siblingIndices,
                candidate.position,
                candidate.rotation,
                candidate.lossyScale,
                candidate.gameObject.activeSelf));
        }

        private static GameObject CreateDistrictParitySnapshot(
            Transform moduleRoot,
            Transform sourceInstance,
            int moduleIndex)
        {
            string assetPath = $"{DistrictSnapshotFolder}/M01_District_{moduleIndex:00}_Parity.prefab";
            EnsureDirectoryForAsset(assetPath);
            int sourceChildIndex = sourceInstance.GetSiblingIndex();
            GameObject snapshotRoot = UnityEngine.Object.Instantiate(moduleRoot.gameObject);
            try
            {
                snapshotRoot.name = $"M01_District_{moduleIndex:00}_Parity";
                snapshotRoot.transform.SetParent(null, true);
                for (int childIndex = snapshotRoot.transform.childCount - 1; childIndex >= 0; childIndex--)
                {
                    if (childIndex != sourceChildIndex)
                        UnityEngine.Object.DestroyImmediate(snapshotRoot.transform.GetChild(childIndex).gameObject);
                }

                if (snapshotRoot.transform.childCount != 1)
                    throw new InvalidOperationException($"District parity wrapper is invalid for module {moduleIndex}.");
                GameObject retainedDistrict = snapshotRoot.transform.GetChild(0).gameObject;
                GameObject nestedPrefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(retainedDistrict);
                if (nestedPrefabRoot != null)
                {
                    PrefabUtility.UnpackPrefabInstance(
                        nestedPrefabRoot,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }

                snapshotRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                snapshotRoot.transform.localScale = Vector3.one;
                GameObject snapshotPrefab = PrefabUtility.SaveAsPrefabAsset(snapshotRoot, assetPath, out bool saved);
                if (!saved || snapshotPrefab == null)
                    throw new InvalidOperationException($"Could not save district parity snapshot: {assetPath}");

                int expectedRenderers = sourceInstance.GetComponentsInChildren<Renderer>(true).Length;
                int actualRenderers = snapshotPrefab.GetComponentsInChildren<Renderer>(true).Length;
                if (expectedRenderers != actualRenderers)
                {
                    throw new InvalidOperationException(
                        $"District parity snapshot renderer drift at {assetPath}: " +
                        $"expected={expectedRenderers} actual={actualRenderers}.");
                }

                return snapshotPrefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(snapshotRoot);
            }
        }

        private static void AssertDistrictSnapshotParity(
            Transform sourceRoot,
            Transform snapshotRoot,
            string moduleName)
        {
            Renderer[] sourceRenderers = sourceRoot.GetComponentsInChildren<Renderer>(true);
            Renderer[] snapshotRenderers = snapshotRoot.GetComponentsInChildren<Renderer>(true);
            var sourceEntries = new List<string>(sourceRenderers.Length);
            var snapshotEntries = new List<string>(snapshotRenderers.Length);
            for (int i = 0; i < sourceRenderers.Length; i++)
                sourceEntries.Add(CreateRendererManifestEntry(sourceRenderers[i]));
            for (int i = 0; i < snapshotRenderers.Length; i++)
                snapshotEntries.Add(CreateRendererManifestEntry(snapshotRenderers[i]));
            sourceEntries.Sort(StringComparer.Ordinal);
            snapshotEntries.Sort(StringComparer.Ordinal);
            if (sourceEntries.Count != snapshotEntries.Count)
            {
                throw new InvalidOperationException(
                    $"District parity snapshot count drift for {moduleName}: " +
                    $"expected={sourceEntries.Count} actual={snapshotEntries.Count}.");
            }

            for (int i = 0; i < sourceEntries.Count; i++)
            {
                if (string.Equals(sourceEntries[i], snapshotEntries[i], StringComparison.Ordinal))
                    continue;
                throw new InvalidOperationException(
                    $"District parity snapshot renderer drift for {moduleName} at sorted index {i}. " +
                    $"expected={sourceEntries[i]} actual={snapshotEntries[i]}");
            }
        }

        private static void AssertDistrictSlicePartition(
            Transform prefabRoot,
            IReadOnlyList<RuntimeOperationMapDistrictSliceRecipe> slices,
            string moduleName)
        {
            var partitionRenderers = new HashSet<Renderer>();
            for (int sliceIndex = 0; sliceIndex < slices.Count; sliceIndex++)
            {
                Transform sliceRoot = ResolveSiblingIndexPath(prefabRoot, slices[sliceIndex].SiblingIndices);
                if (sliceRoot == null)
                    throw new InvalidOperationException($"District slice {sliceIndex} no longer resolves for {moduleName}.");
                Renderer[] sliceRenderers = sliceRoot.GetComponentsInChildren<Renderer>(true);
                for (int rendererIndex = 0; rendererIndex < sliceRenderers.Length; rendererIndex++)
                {
                    if (!partitionRenderers.Add(sliceRenderers[rendererIndex]))
                    {
                        throw new InvalidOperationException(
                            $"District slice partition overlaps for {moduleName} at slice {sliceIndex}.");
                    }
                }
            }

            int expectedRenderers = prefabRoot.GetComponentsInChildren<Renderer>(true).Length;
            if (partitionRenderers.Count != expectedRenderers)
            {
                throw new InvalidOperationException(
                    $"District slice partition is incomplete for {moduleName}: " +
                    $"expected={expectedRenderers} actual={partitionRenderers.Count}.");
            }
        }

        private static int[] CalculateSiblingIndexPath(Transform candidate, Transform root)
        {
            var reverseIndices = new List<int>(8);
            Transform current = candidate;
            while (current != null && current != root)
            {
                reverseIndices.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            if (current != root || reverseIndices.Count == 0)
                throw new InvalidOperationException($"{candidate.name} is not a descendant of {root.name}.");

            var siblingIndices = new int[reverseIndices.Count];
            for (int i = 0; i < reverseIndices.Count; i++)
                siblingIndices[i] = reverseIndices[reverseIndices.Count - i - 1];
            return siblingIndices;
        }

        private static Transform ResolveSiblingIndexPath(Transform root, IReadOnlyList<int> siblingIndices)
        {
            Transform current = root;
            for (int depth = 0; depth < siblingIndices.Count; depth++)
            {
                int siblingIndex = siblingIndices[depth];
                if (siblingIndex < 0 || siblingIndex >= current.childCount)
                    return null;
                current = current.GetChild(siblingIndex);
            }

            return current;
        }

        private static void CaptureVisual(
            Transform transform,
            RuntimeOperationMapVisualStage stage,
            List<RuntimeOperationMapVisualEntry> entries)
        {
            GameObject gameObject = transform.gameObject;
            GameObject outermostPrefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            if (outermostPrefabRoot == gameObject)
            {
                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                if (prefab == null)
                    throw new InvalidOperationException($"Could not resolve prefab source for runtime recipe entry: {gameObject.name}");
                if (PrefabUtility.HasPrefabInstanceAnyOverrides(gameObject, includeDefaultOverrides: false))
                    prefab = CreateVisualEntryParitySnapshot(gameObject, stage, entries.Count);

                entries.Add(new RuntimeOperationMapVisualEntry(
                    gameObject.name,
                    stage,
                    RuntimeOperationMapVisualEntryKind.Prefab,
                    prefab,
                    null,
                    transform.position,
                    transform.rotation,
                    transform.lossyScale,
                    gameObject.activeSelf,
                    allowParticles: true));
                return;
            }

            Light light = gameObject.GetComponent<Light>();
            if (light != null && light.type == LightType.Point)
            {
                entries.Add(new RuntimeOperationMapVisualEntry(
                    gameObject.name,
                    stage,
                    RuntimeOperationMapVisualEntryKind.PointLight,
                    null,
                    null,
                    transform.position,
                    transform.rotation,
                    transform.lossyScale,
                    gameObject.activeSelf,
                    false,
                    light.color,
                    light.intensity,
                    light.range,
                    light.shadows));
                return;
            }

            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            if (meshFilter != null && renderer != null && meshFilter.sharedMesh != null)
            {
                RuntimeOperationMapVisualEntryKind kind;
                if (meshFilter.sharedMesh.name.StartsWith("Cube", StringComparison.OrdinalIgnoreCase))
                    kind = RuntimeOperationMapVisualEntryKind.Box;
                else if (meshFilter.sharedMesh.name.StartsWith("Cylinder", StringComparison.OrdinalIgnoreCase))
                    kind = RuntimeOperationMapVisualEntryKind.Cylinder;
                else if (meshFilter.sharedMesh.name.StartsWith("RuntimeIrregularSurface", StringComparison.OrdinalIgnoreCase))
                    kind = RuntimeOperationMapVisualEntryKind.IrregularSurface;
                else
                    throw new InvalidOperationException($"Unsupported non-prefab M01 recipe mesh: {meshFilter.sharedMesh.name} on {gameObject.name}");

                entries.Add(new RuntimeOperationMapVisualEntry(
                    gameObject.name,
                    stage,
                    kind,
                    null,
                    renderer.sharedMaterial,
                    transform.position,
                    transform.rotation,
                    transform.lossyScale,
                    gameObject.activeSelf,
                    false));
                return;
            }

            for (int i = 0; i < transform.childCount; i++)
                CaptureVisual(transform.GetChild(i), stage, entries);
        }

        private static GameObject CreateVisualEntryParitySnapshot(
            GameObject sourceInstance,
            RuntimeOperationMapVisualStage stage,
            int entryIndex)
        {
            string assetPath =
                $"{DistrictSnapshotFolder}/Entries/M01_Entry_{entryIndex:000}_{(int)stage}_Parity.prefab";
            EnsureDirectoryForAsset(assetPath);
            GameObject snapshotRoot = UnityEngine.Object.Instantiate(sourceInstance);
            try
            {
                snapshotRoot.name = $"M01_Entry_{entryIndex:000}_{(int)stage}_Parity";
                snapshotRoot.transform.SetParent(null, true);
                GameObject nestedPrefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(snapshotRoot);
                if (nestedPrefabRoot != null)
                {
                    PrefabUtility.UnpackPrefabInstance(
                        nestedPrefabRoot,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }

                snapshotRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                snapshotRoot.transform.localScale = Vector3.one;
                snapshotRoot.SetActive(true);
                GameObject snapshotPrefab = PrefabUtility.SaveAsPrefabAsset(snapshotRoot, assetPath, out bool saved);
                if (!saved || snapshotPrefab == null)
                    throw new InvalidOperationException($"Could not save visual-entry parity snapshot: {assetPath}");

                int expectedRenderers = sourceInstance.GetComponentsInChildren<Renderer>(true).Length;
                int actualRenderers = snapshotPrefab.GetComponentsInChildren<Renderer>(true).Length;
                if (expectedRenderers != actualRenderers)
                {
                    throw new InvalidOperationException(
                        $"Visual-entry parity snapshot renderer drift at {assetPath}: " +
                        $"expected={expectedRenderers} actual={actualRenderers}.");
                }

                return snapshotPrefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(snapshotRoot);
            }
        }

        private static void MonitorPlayModeSmoke()
        {
            if (!EditorApplication.isPlaying)
                return;

            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                RuntimeCityRAndDMapSystem runtimeSystem = world != null && world.IsCreated
                    ? world.GetExistingSystemManaged<RuntimeCityRAndDMapSystem>()
                    : null;
                if (runtimeSystem != null)
                {
                    RuntimeCityGenerationProgress progress = runtimeSystem.Progress;
                    CaptureMarketRevealWhenReady(runtimeSystem);
                    if (progress.Stage == RuntimeCityGenerationStage.Completed)
                    {
                        if (_playModeCaptureAfter <= 0d)
                        {
                            _playModeCaptureAfter = EditorApplication.timeSinceStartup + 2.5d;
                            return;
                        }
                        if (EditorApplication.timeSinceStartup < _playModeCaptureAfter)
                            return;

                        if (runtimeSystem.VisualRecipeEntryCount < 150 || runtimeSystem.VisualRecipeRendererCount < 900)
                        {
                            Debug.LogError(
                                $"[M01RuntimeMapPlayModeSmoke] result=Failed reason=belowParityDensity " +
                                $"recipeEntries={runtimeSystem.VisualRecipeEntryCount} renderers={runtimeSystem.VisualRecipeRendererCount}");
                            EditorApplication.ExitPlaymode();
                            return;
                        }

                        if (runtimeSystem.FoundationVisualCount != 1)
                        {
                            Debug.LogError(
                                $"[M01RuntimeMapPlayModeSmoke] result=Failed reason=missingFoundation " +
                                $"foundations={runtimeSystem.FoundationVisualCount}");
                            EditorApplication.ExitPlaymode();
                            return;
                        }

                        if (runtimeSystem.SuppressedObstructionCount != 0)
                        {
                            Debug.LogError(
                                $"[M01RuntimeMapPlayModeSmoke] result=Failed reason=editorContentSuppressed " +
                                $"suppressed={runtimeSystem.SuppressedObstructionCount}");
                            EditorApplication.ExitPlaymode();
                            return;
                        }

                        if (!_playModeMarketCaptureComplete)
                        {
                            Debug.LogError(
                                "[M01RuntimeMapPlayModeSmoke] result=Failed reason=marketRevealCameraStageNotObserved");
                            EditorApplication.ExitPlaymode();
                            return;
                        }

                        RuntimeCityRAndDMapView view =
                            UnityEngine.Object.FindAnyObjectByType<RuntimeCityRAndDMapView>();
                        if (view == null || view.GeneratedRoot == null)
                            throw new InvalidOperationException("Runtime parity smoke lost its generated root.");

                        VisualParityManifest expectedManifest = ReadVisualParityManifest(EditorVisualManifestPath);
                        VisualParityManifest runtimeManifest = BuildRuntimeVisualParityManifest(view.GeneratedRoot);
                        WriteVisualParityManifest(RuntimeVisualManifestPath, runtimeManifest);
                        if (!runtimeManifest.Matches(expectedManifest))
                        {
                            Debug.LogError(
                                $"[M01RuntimeMapPlayModeSmoke] result=Failed reason=visualManifestMismatch " +
                                $"expectedHash={expectedManifest.Hash} actualHash={runtimeManifest.Hash} " +
                                $"expectedRenderers={expectedManifest.RendererCount} actualRenderers={runtimeManifest.RendererCount} " +
                                $"expectedLights={expectedManifest.LightCount} actualLights={runtimeManifest.LightCount} " +
                                $"expectedParticles={expectedManifest.ParticleRendererCount} " +
                                $"actualParticles={runtimeManifest.ParticleRendererCount}");
                            EditorApplication.ExitPlaymode();
                            return;
                        }

                        CaptureRuntimeCamera();
                        VisualCaptureDelta perspectiveDelta = CompareVisualCaptures(
                            EditorCapturePath,
                            RuntimeCapturePath);
                        VisualCaptureDelta topDownDelta = CompareVisualCaptures(
                            EditorTopDownCapturePath,
                            RuntimeTopDownCapturePath);
                        if (!perspectiveDelta.IsAccepted || !topDownDelta.IsAccepted)
                        {
                            Debug.LogError(
                                $"[M01RuntimeMapPlayModeSmoke] result=Failed reason=visualCaptureDelta " +
                                $"perspectiveMae={perspectiveDelta.MeanAbsoluteError:0.000} " +
                                $"perspectiveOutliers={perspectiveDelta.OutlierFraction:P3} " +
                                $"topDownMae={topDownDelta.MeanAbsoluteError:0.000} " +
                                $"topDownOutliers={topDownDelta.OutlierFraction:P3}");
                            EditorApplication.ExitPlaymode();
                            return;
                        }
                        _playModeCaptureComplete = true;
                        _playModeExitCode = 0;
                        Debug.Log(
                            $"[M01RuntimeMapPlayModeSmoke] result=Passed version={RuntimeCityGenerationProgress.VersionTag} " +
                            $"seed={progress.Seed} cities={progress.GeneratedCityCount}/{progress.RequestedCityCount} " +
                            $"roadStrokes={runtimeSystem.RoadStrokeCount} roadCells={runtimeSystem.RoadCellCount} " +
                            $"plannedBuildings={runtimeSystem.PlannedBuildingCount} visualBuildings={runtimeSystem.VisualBuildingCount} " +
                            $"recipeEntries={runtimeSystem.VisualRecipeEntryCount} renderers={runtimeSystem.VisualRecipeRendererCount} " +
                            $"foundations={runtimeSystem.FoundationVisualCount} districtTerrainCleanups={runtimeSystem.SuppressedObstructionCount} " +
                            $"visualManifest={runtimeManifest.Hash} " +
                            $"perspectiveMae={perspectiveDelta.MeanAbsoluteError:0.000} " +
                            $"topDownMae={topDownDelta.MeanAbsoluteError:0.000} " +
                            $"maxVisualBatchMs={runtimeSystem.MaxVisualBatchMilliseconds:0.000} budgetYields={runtimeSystem.FrameBudgetYieldCount} " +
                            $"elapsed={EditorApplication.timeSinceStartup - _playModeStartedAt:0.000}s " +
                            $"capture={RuntimeCapturePath} marketRevealCapture={RuntimeMarketRevealCapturePath}");
                        EditorApplication.ExitPlaymode();
                        return;
                    }

                    if (progress.Stage == RuntimeCityGenerationStage.Failed ||
                        progress.Stage == RuntimeCityGenerationStage.Cancelled)
                    {
                        Debug.LogError($"[M01RuntimeMapPlayModeSmoke] result=Failed stage={progress.Stage} seed={progress.Seed}");
                        EditorApplication.ExitPlaymode();
                        return;
                    }
                }

                if (EditorApplication.timeSinceStartup < _playModeDeadline)
                    return;

                Debug.LogError("[M01RuntimeMapPlayModeSmoke] result=Failed reason=timeout seconds=120");
                EditorApplication.ExitPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[M01RuntimeMapPlayModeSmoke] result=Failed reason=monitorException");
                EditorApplication.ExitPlaymode();
            }
        }

        private static void MonitorAlgorithmicSeedReview()
        {
            if (!EditorApplication.isPlaying)
                return;

            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                RuntimeCityRAndDMapSystem runtimeSystem = world != null && world.IsCreated
                    ? world.GetExistingSystemManaged<RuntimeCityRAndDMapSystem>()
                    : null;
                if (runtimeSystem != null)
                {
                    RuntimeCityGenerationProgress progress = runtimeSystem.Progress;
                    if (progress.Stage == RuntimeCityGenerationStage.Completed)
                    {
                        if (_algorithmicReviewCaptureComplete)
                        {
                            if (EditorApplication.timeSinceStartup < _algorithmicReviewExitAfter ||
                                Time.frameCount < _algorithmicReviewExitAfterFrame)
                                return;

                            EditorApplication.ExitPlaymode();
                            return;
                        }

                        if (_algorithmicReviewCaptureAfter <= 0d)
                        {
                            _algorithmicReviewCaptureAfter = EditorApplication.timeSinceStartup + 1.5d;
                            return;
                        }
                        if (EditorApplication.timeSinceStartup < _algorithmicReviewCaptureAfter)
                            return;

                        if (runtimeSystem.RoadStrokeCount <= 0 ||
                            runtimeSystem.VisualBuildingCount < 30 ||
                            runtimeSystem.FoundationVisualCount != 1 ||
                            runtimeSystem.AlgorithmicDistrictSurfaceCount != 12 ||
                            runtimeSystem.AlgorithmicAftermathDressingCount < 6)
                        {
                            Debug.LogError(
                                $"[M01AlgorithmicSeedReview] result=Failed reason=insufficientPlannerOutput " +
                                $"seed={_algorithmicReviewSeed} roads={runtimeSystem.RoadStrokeCount} " +
                                $"visualBuildings={runtimeSystem.VisualBuildingCount} " +
                                $"foundations={runtimeSystem.FoundationVisualCount} " +
                                $"districtSurfaces={runtimeSystem.AlgorithmicDistrictSurfaceCount} " +
                                $"aftermathDressing={runtimeSystem.AlgorithmicAftermathDressingCount}");
                            EditorApplication.ExitPlaymode();
                            return;
                        }

                        RuntimeCityRAndDMapView view = UnityEngine.Object.FindAnyObjectByType<RuntimeCityRAndDMapView>();
                        if (view == null || view.GeneratedRoot == null)
                            throw new InvalidOperationException("Algorithmic review lost its generated root.");

                        CaptureAlgorithmicReviewCamera(_algorithmicReviewSeed);
                        string fingerprint = ComputeRuntimeTransformFingerprint(view.GeneratedRoot);
                        string fingerprintPath = WriteAlgorithmicFingerprint(_algorithmicReviewSeed, fingerprint);
                        GetAlgorithmicVisualDiagnostics(
                            view.GeneratedRoot,
                            view,
                            _algorithmicReviewConfig,
                            out int visualCount,
                            out int uniquePrefabCount,
                            out string yawBuckets,
                            out string districtCentroids,
                            out string placementGroups,
                            out string edgeOutliers,
                            out string terminalBranches);
                        _algorithmicReviewExitCode = 0;
                        Debug.Log(
                            $"[M01AlgorithmicSeedReview] result=Passed version={RuntimeCityGenerationProgress.VersionTag} " +
                            $"seed={_algorithmicReviewSeed} roads={runtimeSystem.RoadStrokeCount}/{runtimeSystem.RoadCellCount} " +
                            $"plannedBuildings={runtimeSystem.PlannedBuildingCount} visualBuildings={runtimeSystem.VisualBuildingCount} " +
                            $"visuals={visualCount} uniquePrefabs={uniquePrefabCount} " +
                            $"maxConsecutivePrefab={runtimeSystem.MaxObservedConsecutivePrefabSelections} " +
                            $"foundations={runtimeSystem.FoundationVisualCount} " +
                            $"districtSurfaces={runtimeSystem.AlgorithmicDistrictSurfaceCount} " +
                            $"aftermathDressing={runtimeSystem.AlgorithmicAftermathDressingCount} " +
                            $"yawBuckets={yawBuckets} districtCentroids={districtCentroids} " +
                            $"placementGroups={placementGroups} edgeOutliers={edgeOutliers} " +
                            $"terminalBranches={terminalBranches} " +
                            $"fingerprint={fingerprint} fingerprintPath={fingerprintPath}");
                        _algorithmicReviewCaptureComplete = true;
                        _algorithmicReviewExitAfter = EditorApplication.timeSinceStartup + 0.5d;
                        _algorithmicReviewExitAfterFrame = Time.frameCount + 8;
                        return;
                    }

                    if (progress.Stage == RuntimeCityGenerationStage.Failed ||
                        progress.Stage == RuntimeCityGenerationStage.Cancelled)
                    {
                        Debug.LogError(
                            $"[M01AlgorithmicSeedReview] result=Failed seed={_algorithmicReviewSeed} stage={progress.Stage}");
                        EditorApplication.ExitPlaymode();
                        return;
                    }
                }

                if (EditorApplication.timeSinceStartup < _algorithmicReviewDeadline)
                    return;

                Debug.LogError($"[M01AlgorithmicSeedReview] result=Failed reason=timeout seed={_algorithmicReviewSeed}");
                EditorApplication.ExitPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError($"[M01AlgorithmicSeedReview] result=Failed reason=monitorException seed={_algorithmicReviewSeed}");
                EditorApplication.ExitPlaymode();
            }
        }

        private static void HandleAlgorithmicReviewPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.update -= MonitorAlgorithmicSeedReview;
            EditorApplication.playModeStateChanged -= HandleAlgorithmicReviewPlayModeStateChanged;
            DestroyAlgorithmicReviewConfig();
            EditorApplication.Exit(_algorithmicReviewExitCode);
        }

        private static void MonitorLifecycleRecoveryValidation()
        {
            if (!EditorApplication.isPlaying)
                return;

            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                RuntimeCityRAndDMapSystem runtimeSystem = world != null && world.IsCreated
                    ? world.GetExistingSystemManaged<RuntimeCityRAndDMapSystem>()
                    : null;
                RuntimeCityRAndDMapView view = UnityEngine.Object.FindAnyObjectByType<RuntimeCityRAndDMapView>();
                if (runtimeSystem == null || view == null || view.GeneratedRoot == null)
                {
                    if (EditorApplication.timeSinceStartup >= _lifecycleRecoveryDeadline)
                        FailLifecycleRecoveryValidation("runtimeUnavailable");
                    return;
                }

                RuntimeCityGenerationProgress progress = runtimeSystem.Progress;
                switch (_lifecycleRecoveryPhase)
                {
                    case LifecycleRecoveryValidationPhase.WaitingForGeneration:
                        if (!runtimeSystem.IsGenerationActive ||
                            progress.Stage == RuntimeCityGenerationStage.Idle ||
                            progress.IsTerminal)
                        {
                            break;
                        }

                        view.RequestCancel();
                        _lifecycleRecoveryCancelFrame = Time.frameCount;
                        _lifecycleRecoveryPhase = LifecycleRecoveryValidationPhase.WaitingForCancellation;
                        Debug.Log(
                            $"[M01RuntimeMapLifecycleRecoveryValidation] action=CancelRequested " +
                            $"stage={progress.Stage} seed={progress.Seed} frame={Time.frameCount}");
                        break;

                    case LifecycleRecoveryValidationPhase.WaitingForCancellation:
                        if (progress.Stage != RuntimeCityGenerationStage.Cancelled)
                            break;
                        if (runtimeSystem.IsGenerationActive)
                        {
                            FailLifecycleRecoveryValidation("cancelledButStillActive");
                            return;
                        }

                        _lifecycleRecoveryCancellationObserved = true;
                        if (Time.frameCount < _lifecycleRecoveryCancelFrame + 2)
                            break;
                        if (view.GeneratedRoot.childCount != 0)
                        {
                            FailLifecycleRecoveryValidation(
                                $"cancelCleanupIncomplete:{view.GeneratedRoot.childCount}");
                            return;
                        }

                        var viewSerialized = new SerializedObject(view);
                        viewSerialized.FindProperty("config").objectReferenceValue = null;
                        viewSerialized.ApplyModifiedPropertiesWithoutUndo();
                        view.RequestGeneration();
                        _lifecycleRecoveryPhase = LifecycleRecoveryValidationPhase.WaitingForFallbackStart;
                        break;

                    case LifecycleRecoveryValidationPhase.WaitingForFallbackStart:
                        if (runtimeSystem.IsUsingFallback)
                        {
                            if (runtimeSystem.FallbackAttemptCount != 1 ||
                                !string.Equals(runtimeSystem.RecoveryReason, "missingConfig", StringComparison.Ordinal))
                            {
                                FailLifecycleRecoveryValidation(
                                    $"invalidFallbackStart:attempts={runtimeSystem.FallbackAttemptCount}:" +
                                    $"reason={runtimeSystem.RecoveryReason}");
                                return;
                            }

                            _lifecycleRecoveryPhase = LifecycleRecoveryValidationPhase.WaitingForFallbackCompletion;
                            break;
                        }

                        if (progress.Stage == RuntimeCityGenerationStage.Failed && !runtimeSystem.IsGenerationActive)
                        {
                            FailLifecycleRecoveryValidation("failureDidNotActivateFallback");
                            return;
                        }
                        break;

                    case LifecycleRecoveryValidationPhase.WaitingForFallbackCompletion:
                        if (progress.Stage == RuntimeCityGenerationStage.Completed)
                        {
                            if (!_lifecycleRecoveryCancellationObserved ||
                                !runtimeSystem.IsUsingFallback ||
                                runtimeSystem.FallbackAttemptCount != 1 ||
                                !string.Equals(runtimeSystem.RecoveryReason, "missingConfig", StringComparison.Ordinal) ||
                                runtimeSystem.VisualRecipeEntryCount < 150 ||
                                runtimeSystem.FoundationVisualCount != 1 ||
                                view.GeneratedRoot.childCount == 0)
                            {
                                FailLifecycleRecoveryValidation(
                                    $"invalidFallbackCompletion:cancelled={_lifecycleRecoveryCancellationObserved}:" +
                                    $"fallback={runtimeSystem.IsUsingFallback}:attempts={runtimeSystem.FallbackAttemptCount}:" +
                                    $"reason={runtimeSystem.RecoveryReason}:entries={runtimeSystem.VisualRecipeEntryCount}:" +
                                    $"foundations={runtimeSystem.FoundationVisualCount}:" +
                                    $"children={view.GeneratedRoot.childCount}");
                                return;
                            }

                            _lifecycleRecoveryExitCode = 0;
                            EditorApplication.update -= MonitorLifecycleRecoveryValidation;
                            Debug.Log(
                                $"[M01RuntimeMapLifecycleRecoveryValidation] result=Passed " +
                                $"version={RuntimeCityGenerationProgress.VersionTag} cancelled=1 " +
                                $"fallback=1 attempts={runtimeSystem.FallbackAttemptCount} " +
                                $"reason={runtimeSystem.RecoveryReason} " +
                                $"recipeEntries={runtimeSystem.VisualRecipeEntryCount} " +
                                $"foundations={runtimeSystem.FoundationVisualCount}");
                            EditorApplication.ExitPlaymode();
                            return;
                        }

                        if ((progress.Stage == RuntimeCityGenerationStage.Failed ||
                             progress.Stage == RuntimeCityGenerationStage.Cancelled) &&
                            !runtimeSystem.IsGenerationActive)
                        {
                            FailLifecycleRecoveryValidation($"fallbackTerminated:{progress.Stage}");
                            return;
                        }
                        break;
                }

                if (EditorApplication.timeSinceStartup >= _lifecycleRecoveryDeadline)
                    FailLifecycleRecoveryValidation($"timeout:{_lifecycleRecoveryPhase}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                FailLifecycleRecoveryValidation("monitorException");
            }
        }

        private static void FailLifecycleRecoveryValidation(string reason)
        {
            _lifecycleRecoveryExitCode = 1;
            EditorApplication.update -= MonitorLifecycleRecoveryValidation;
            Debug.LogError(
                $"[M01RuntimeMapLifecycleRecoveryValidation] result=Failed reason={reason} " +
                $"phase={_lifecycleRecoveryPhase}");
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
        }

        private static void HandleLifecycleRecoveryPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.update -= MonitorLifecycleRecoveryValidation;
            EditorApplication.playModeStateChanged -= HandleLifecycleRecoveryPlayModeStateChanged;
            DestroyLifecycleRecoveryConfig();
            EditorApplication.Exit(_lifecycleRecoveryExitCode);
        }

        private static void CaptureMarketRevealWhenReady(RuntimeCityRAndDMapSystem runtimeSystem)
        {
            if (_playModeMarketCaptureComplete ||
                runtimeSystem.CurrentVisualStage != RuntimeOperationMapVisualStage.Market)
                return;

            if (_playModeMarketCaptureAfter <= 0d)
            {
                _playModeMarketCaptureAfter = EditorApplication.timeSinceStartup + 0.35d;
                return;
            }

            if (EditorApplication.timeSinceStartup < _playModeMarketCaptureAfter)
                return;

            Camera camera = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null)
                throw new InvalidOperationException("Runtime prototype scene has no camera for its market reveal capture.");

            CaptureCamera(camera, RuntimeMarketRevealCapturePath);
            _playModeMarketCaptureComplete = true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.update -= MonitorPlayModeSmoke;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            if (!_playModeCaptureComplete)
                Debug.LogError("[M01RuntimeMapPlayModeSmoke] result=Failed reason=noCompletedCapture");
            EditorApplication.Exit(_playModeExitCode);
        }

        private static void CaptureRuntimeCamera()
        {
            Camera camera = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null)
                throw new InvalidOperationException("Runtime prototype scene has no camera to capture.");

            CaptureCamera(camera, RuntimeCapturePath);
            CaptureRuntimeTopDown(camera);
        }

        private static void CaptureEditorParityCameras()
        {
            GameObject cameraObject = GameObject.Find("M01_Review_GameplayOverview");
            Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
            if (camera == null)
                throw new InvalidOperationException("Accepted M01 editor scene is missing its gameplay overview camera.");

            GameObject topDownCameraObject = GameObject.Find("M01_Review_TopDownPlan");
            Camera topDownCamera = topDownCameraObject != null ? topDownCameraObject.GetComponent<Camera>() : null;
            if (topDownCamera == null)
                throw new InvalidOperationException("Accepted M01 editor scene is missing its top-down review camera.");

            CaptureCamera(camera, EditorCapturePath);
            CaptureCamera(topDownCamera, EditorTopDownCapturePath);
        }

        private static void CaptureRuntimeTopDown(Camera camera)
        {
            CaptureRuntimeTopDown(camera, RuntimeTopDownCapturePath);
        }

        private static void CaptureRuntimeTopDown(Camera camera, string outputPath)
        {
            Transform cameraTransform = camera.transform;
            Vector3 previousPosition = cameraTransform.position;
            Quaternion previousRotation = cameraTransform.rotation;
            bool previousOrthographic = camera.orthographic;
            float previousOrthographicSize = camera.orthographicSize;
            try
            {
                cameraTransform.position = AcceptedTopDownCameraPosition;
                cameraTransform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                camera.orthographic = true;
                camera.orthographicSize = AcceptedTopDownCameraSize;
                CaptureCamera(camera, outputPath);
            }
            finally
            {
                cameraTransform.position = previousPosition;
                cameraTransform.rotation = previousRotation;
                camera.orthographic = previousOrthographic;
                camera.orthographicSize = previousOrthographicSize;
            }
        }

        private static void CaptureAlgorithmicReviewCamera(uint seed)
        {
            Camera camera = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null)
                throw new InvalidOperationException("Algorithmic runtime review has no camera.");

            Transform statusView = camera.transform.Find("RuntimeGenerationStatusView");
            bool statusWasActive = statusView != null && statusView.gameObject.activeSelf;
            if (statusWasActive)
                CaptureCamera(camera, $"Logs/M01_AlgorithmicSeed_{seed}_Loading.png");

            if (statusView != null)
                statusView.gameObject.SetActive(false);
            try
            {
                CaptureCamera(camera, $"Logs/M01_AlgorithmicSeed_{seed}_Perspective.png");
                CaptureRuntimeTopDown(camera, $"Logs/M01_AlgorithmicSeed_{seed}_TopDown.png");
            }
            finally
            {
                if (statusView != null)
                    statusView.gameObject.SetActive(statusWasActive);
            }

            camera.enabled = false;
        }

        private readonly struct VisualParityManifest
        {
            public VisualParityManifest(
                string hash,
                int rendererCount,
                int lightCount,
                int particleRendererCount,
                List<string> entries)
            {
                Hash = hash ?? string.Empty;
                RendererCount = rendererCount;
                LightCount = lightCount;
                ParticleRendererCount = particleRendererCount;
                Entries = entries;
            }

            public string Hash { get; }
            public int RendererCount { get; }
            public int LightCount { get; }
            public int ParticleRendererCount { get; }
            public List<string> Entries { get; }

            public bool Matches(VisualParityManifest other)
            {
                return string.Equals(Hash, other.Hash, StringComparison.Ordinal) &&
                       RendererCount == other.RendererCount &&
                       LightCount == other.LightCount &&
                       ParticleRendererCount == other.ParticleRendererCount;
            }
        }

        private static VisualParityManifest BuildEditorVisualParityManifest()
        {
            EditorSceneManager.OpenScene(M01VisualMapPrototypeEditorUtility.ScenePath, OpenSceneMode.Single);
            GameObject sourceRoot = GameObject.Find("M01_VisualPrototype_Root");
            if (sourceRoot == null)
                throw new InvalidOperationException("Accepted M01 editor prototype root was not found for its visual manifest.");

            var roots = new List<Transform>(8);
            AddManifestRoot(roots, sourceRoot.transform.Find("_M01VisualGenerated/01_Terrain_And_RoadPlan"));
            Transform districts = sourceRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules");
            if (districts == null)
                throw new InvalidOperationException("Accepted M01 editor prototype district root was not found for its visual manifest.");
            for (int moduleIndex = 0; moduleIndex < districts.childCount; moduleIndex++)
            {
                Transform module = districts.GetChild(moduleIndex);
                Transform sourceInstance = null;
                for (int childIndex = 0; childIndex < module.childCount; childIndex++)
                {
                    Transform child = module.GetChild(childIndex);
                    if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == null)
                        continue;
                    sourceInstance = child;
                    break;
                }

                AddManifestRoot(roots, sourceInstance);
            }

            AddManifestRoot(roots, sourceRoot.transform.Find("_M01AuthoredStoryOverrides/03_OldMarket_StoryLayer"));
            AddManifestRoot(roots, sourceRoot.transform.Find("_M01AuthoredStoryOverrides/04_UtilityCompound_StoryLayer"));
            AddManifestRoot(roots, sourceRoot.transform.Find("_M01AuthoredStoryOverrides/05_BombingAftermath_StoryLayer"));
            AddManifestRoot(roots, sourceRoot.transform.Find("_M01AuthoredStoryOverrides/06_CivilianEdge_StoryLayer"));
            AddManifestRoot(roots, sourceRoot.transform.Find("_M01VisualGenerated/07_Horizon_And_EdgeDressing"));
            return BuildVisualParityManifest(roots);
        }

        private static VisualParityManifest BuildRuntimeVisualParityManifest(Transform generatedRoot)
        {
            if (generatedRoot == null)
                throw new ArgumentNullException(nameof(generatedRoot));
            return BuildVisualParityManifest(new List<Transform> { generatedRoot });
        }

        private static void AddManifestRoot(List<Transform> roots, Transform root)
        {
            if (root == null)
                throw new InvalidOperationException("Accepted M01 editor prototype is missing a visual manifest root.");
            roots.Add(root);
        }

        private static VisualParityManifest BuildVisualParityManifest(List<Transform> roots)
        {
            var entries = new List<string>(12000);
            var rendererPaths = new List<string>(12000);
            int rendererCount = 0;
            int particleRendererCount = 0;
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                Renderer[] renderers = roots[rootIndex].GetComponentsInChildren<Renderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    string entry = CreateRendererManifestEntry(renderer);
                    entries.Add(entry);
                    rendererPaths.Add(
                        entry + "|PATH|" + roots[rootIndex].name + "/" +
                        AnimationUtility.CalculateTransformPath(renderer.transform, roots[rootIndex]));
                    rendererCount++;
                    if (renderer is ParticleSystemRenderer)
                        particleRendererCount++;
                }
            }

            Light[] sceneLights = UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int lightCount = 0;
            Scene activeScene = SceneManager.GetActiveScene();
            for (int lightIndex = 0; lightIndex < sceneLights.Length; lightIndex++)
            {
                Light light = sceneLights[lightIndex];
                if (light.gameObject.scene != activeScene)
                    continue;
                entries.Add(CreateLightManifestEntry(light));
                lightCount++;
            }

            entries.Sort(StringComparer.Ordinal);
            rendererPaths.Sort(StringComparer.Ordinal);
            _visualParityRendererPaths = rendererPaths;
            var manifestText = new StringBuilder(entries.Count * 128);
            for (int i = 0; i < entries.Count; i++)
                manifestText.Append(entries[i]).Append('\n');
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(manifestText.ToString()));
            string hashText = BitConverter.ToString(hash).Replace("-", string.Empty);
            return new VisualParityManifest(
                hashText,
                rendererCount,
                lightCount,
                particleRendererCount,
                entries);
        }

        private static string CreateRendererManifestEntry(Renderer renderer)
        {
            Transform transform = renderer.transform;
            Mesh mesh = null;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null)
                mesh = filter.sharedMesh;
            else if (renderer is SkinnedMeshRenderer skinned)
                mesh = skinned.sharedMesh;

            var text = new StringBuilder(256);
            text.Append("R|").Append(renderer.GetType().Name).Append('|')
                .Append(renderer.gameObject.layer).Append('|')
                .Append(renderer.gameObject.activeSelf ? '1' : '0').Append('|')
                .Append(renderer.gameObject.activeInHierarchy ? '1' : '0').Append('|')
                .Append(renderer.enabled ? '1' : '0').Append('|')
                .Append((int)renderer.shadowCastingMode).Append('|')
                .Append(renderer.receiveShadows ? '1' : '0').Append('|')
                .Append(renderer.sortingLayerID).Append('|')
                .Append(renderer.sortingOrder).Append('|');
            AppendTransformManifest(text, transform);
            text.Append('|').Append(NormalizeManifestName(mesh != null ? mesh.name : string.Empty)).Append('|')
                .Append(mesh != null ? mesh.vertexCount : 0).Append('|')
                .Append(mesh != null ? mesh.subMeshCount : 0).Append('|');

            Material[] materials = renderer.sharedMaterials;
            int materialCount = materials.Length;
            while (materialCount > 0 && materials[materialCount - 1] == null)
                materialCount--;
            for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (materialIndex > 0)
                    text.Append(',');
                text.Append(NormalizeManifestName(material != null ? material.name : string.Empty))
                    .Append('@')
                    .Append(material != null && material.shader != null ? material.shader.name : string.Empty);
            }
            return text.ToString();
        }

        private static string CreateLightManifestEntry(Light light)
        {
            var text = new StringBuilder(192);
            text.Append("L|").Append((int)light.type).Append('|')
                .Append(light.gameObject.activeSelf ? '1' : '0').Append('|')
                .Append(light.gameObject.activeInHierarchy ? '1' : '0').Append('|')
                .Append(light.enabled ? '1' : '0').Append('|')
                .Append((int)light.shadows).Append('|');
            AppendTransformManifest(text, light.transform);
            AppendQuantized(text, light.color.r);
            AppendQuantized(text, light.color.g);
            AppendQuantized(text, light.color.b);
            AppendQuantized(text, light.color.a);
            AppendQuantized(text, light.intensity);
            AppendQuantized(text, light.range);
            AppendQuantized(text, light.spotAngle);
            return text.ToString();
        }

        private static void AppendTransformManifest(StringBuilder text, Transform transform)
        {
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            if (rotation.w < 0f)
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
            Vector3 scale = transform.lossyScale;
            AppendPositionQuantized(text, position.x);
            AppendPositionQuantized(text, position.y);
            AppendPositionQuantized(text, position.z);
            AppendQuantized(text, rotation.x);
            AppendQuantized(text, rotation.y);
            AppendQuantized(text, rotation.z);
            AppendQuantized(text, rotation.w);
            AppendQuantized(text, scale.x);
            AppendQuantized(text, scale.y);
            AppendQuantized(text, scale.z);
        }

        private static void AppendQuantized(StringBuilder text, float value)
        {
            text.Append(Mathf.RoundToInt(value * 1000f)).Append('|');
        }

        private static void AppendPositionQuantized(StringBuilder text, float value)
        {
            text.Append(Mathf.RoundToInt(value * 100f)).Append('|');
        }

        private static string NormalizeManifestName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            const string instanceSuffix = " (Instance)";
            if (value.EndsWith(instanceSuffix, StringComparison.Ordinal))
                value = value.Substring(0, value.Length - instanceSuffix.Length);
            const string foundationSuffix = "_RuntimeFoundation";
            if (value.EndsWith(foundationSuffix, StringComparison.Ordinal))
                value = value.Substring(0, value.Length - foundationSuffix.Length);
            return value;
        }

        private static void WriteVisualParityManifest(string relativePath, VisualParityManifest manifest)
        {
            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
            var text = new StringBuilder((manifest.Entries?.Count ?? 0) * 128 + 256);
            text.Append("hash=").Append(manifest.Hash).Append('\n')
                .Append("renderers=").Append(manifest.RendererCount).Append('\n')
                .Append("lights=").Append(manifest.LightCount).Append('\n')
                .Append("particles=").Append(manifest.ParticleRendererCount).Append('\n')
                .Append("---\n");
            if (manifest.Entries != null)
            {
                for (int i = 0; i < manifest.Entries.Count; i++)
                    text.Append(manifest.Entries[i]).Append('\n');
            }
            File.WriteAllText(absolutePath, text.ToString(), Encoding.UTF8);
            string pathReport = string.Equals(relativePath, EditorVisualManifestPath, StringComparison.Ordinal)
                ? EditorRendererPathReportPath
                : RuntimeRendererPathReportPath;
            string absolutePathReport = Path.GetFullPath(Path.Combine(Application.dataPath, "..", pathReport));
            File.WriteAllLines(
                absolutePathReport,
                _visualParityRendererPaths ?? new List<string>(),
                Encoding.UTF8);
            Debug.Log(
                $"[M01VisualParityManifest] hash={manifest.Hash} renderers={manifest.RendererCount} " +
                $"lights={manifest.LightCount} particles={manifest.ParticleRendererCount} path={absolutePath}");
        }

        private static VisualParityManifest ReadVisualParityManifest(string relativePath)
        {
            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
            string[] lines = File.ReadAllLines(absolutePath, Encoding.UTF8);
            if (lines.Length < 5 || !string.Equals(lines[4], "---", StringComparison.Ordinal))
                throw new InvalidDataException($"Invalid M01 visual parity manifest: {relativePath}");
            string hash = ReadManifestHeader(lines[0], "hash");
            int rendererCount = int.Parse(ReadManifestHeader(lines[1], "renderers"), CultureInfo.InvariantCulture);
            int lightCount = int.Parse(ReadManifestHeader(lines[2], "lights"), CultureInfo.InvariantCulture);
            int particleCount = int.Parse(ReadManifestHeader(lines[3], "particles"), CultureInfo.InvariantCulture);
            return new VisualParityManifest(hash, rendererCount, lightCount, particleCount, null);
        }

        private static string ReadManifestHeader(string line, string key)
        {
            string prefix = key + "=";
            if (line == null || !line.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException($"Invalid M01 visual parity manifest header: expected {key}.");
            return line.Substring(prefix.Length);
        }

        private static string ComputeRuntimeTransformFingerprint(Transform root)
        {
            var transforms = new List<Transform>(root.GetComponentsInChildren<Transform>(true));
            transforms.Sort((left, right) => string.CompareOrdinal(
                AnimationUtility.CalculateTransformPath(left, root),
                AnimationUtility.CalculateTransformPath(right, root)));
            var text = new StringBuilder(transforms.Count * 96);
            for (int i = 0; i < transforms.Count; i++)
            {
                Transform transform = transforms[i];
                Vector3 position = transform.localPosition;
                Quaternion rotation = transform.localRotation;
                Vector3 scale = transform.localScale;
                text.Append(AnimationUtility.CalculateTransformPath(transform, root)).Append('|');
                text.Append(transform.gameObject.activeSelf ? '1' : '0').Append('|');
                text.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0:R},{1:R},{2:R}|{3:R},{4:R},{5:R},{6:R}|{7:R},{8:R},{9:R}\n",
                    position.x,
                    position.y,
                    position.z,
                    rotation.x,
                    rotation.y,
                    rotation.z,
                    rotation.w,
                    scale.x,
                    scale.y,
                    scale.z);
            }

            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        private static string WriteAlgorithmicFingerprint(uint seed, string fingerprint)
        {
            string relativePath = $"Logs/M01_AlgorithmicSeed_{seed}_Transform.sha256.txt";
            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
            File.WriteAllText(absolutePath, fingerprint + Environment.NewLine, Encoding.ASCII);
            return relativePath;
        }

        private static void GetAlgorithmicVisualDiagnostics(
            Transform generatedRoot,
            RuntimeCityRAndDMapView view,
            RuntimeCitySpawnerSystemConfig config,
            out int visualCount,
            out int uniquePrefabCount,
            out string yawBuckets,
            out string districtCentroids,
            out string placementGroups,
            out string edgeOutliers,
            out string terminalBranches)
        {
            Transform visualRoot = generatedRoot.Find("RuntimeCityVisuals");
            if (visualRoot == null)
                throw new InvalidOperationException("Algorithmic review generated no RuntimeCityVisuals root.");

            var uniquePrefabs = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> marketPrefabNames = CreatePrefabNameSet(config.ShopPrefabs);
            HashSet<string> utilityPrefabNames = CreatePrefabNameSet(config.GasStationPrefabs);
            AddPrefabNames(utilityPrefabNames, config.OtherBuildingPrefabs);
            HashSet<string> residentialPrefabNames = CreatePrefabNameSet(config.HousePrefabs);
            HashSet<string> damagePrefabNames = CreateDamagePrefabNameSet(config.CityDecorationPrefabs);
            var marketCentroid = new AlgorithmicCentroidAccumulator();
            var utilityCentroid = new AlgorithmicCentroidAccumulator();
            var residentialCentroid = new AlgorithmicCentroidAccumulator();
            var damageCentroid = new AlgorithmicCentroidAccumulator();
            var otherCentroid = new AlgorithmicCentroidAccumulator();
            List<Vector3> roadCenters = CollectAlgorithmicRoadCenters(generatedRoot);
            float roadCellWorldSize = view.RoadCellSizeInGridCells * view.GridCellSize;
            Vector2Int centerRoadCell = config.StartCell / view.RoadCellSizeInGridCells;
            Vector3 cityCenter = new(
                view.GridOrigin.x + ((centerRoadCell.x + 0.5f) * roadCellWorldSize),
                view.GridOrigin.y,
                view.GridOrigin.z + ((centerRoadCell.y + 0.5f) * roadCellWorldSize));
            int totalBuildings = 1 + config.GasStationCount + config.ShopCount + config.HouseCount +
                config.OtherBuildingCount + config.CityDecorationBuildingCount;
            int townRadius = Mathf.Max(
                config.HallPlazaRadiusRoadCells + 3,
                Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(0, totalBuildings))) + config.ExtraTownRadiusRoadCells);
            float roadsideDistance = roadCellWorldSize * 1.6f;
            float edgeDistance = roadCellWorldSize * (townRadius + 1f);
            int roadsideCount = 0;
            int scatterCount = 0;
            int edgeCount = 0;
            var edgeDetails = new StringBuilder();
            int[] cardinalYawCounts = new int[4];
            visualCount = visualRoot.childCount;
            for (int i = 0; i < visualRoot.childCount; i++)
            {
                Transform visual = visualRoot.GetChild(i);
                string prefabName = visual.name.EndsWith("_Visual", StringComparison.Ordinal)
                    ? visual.name.Substring(0, visual.name.Length - "_Visual".Length)
                    : visual.name;
                uniquePrefabs.Add(prefabName);

                string category;
                if (marketPrefabNames.Contains(prefabName))
                {
                    marketCentroid.Add(visual.position);
                    category = "market";
                }
                else if (residentialPrefabNames.Contains(prefabName))
                {
                    residentialCentroid.Add(visual.position);
                    category = "residential";
                }
                else if (utilityPrefabNames.Contains(prefabName))
                {
                    utilityCentroid.Add(visual.position);
                    category = "utility";
                }
                else
                {
                    otherCentroid.Add(visual.position);
                    category = "other";
                }

                if (damagePrefabNames.Contains(prefabName))
                {
                    damageCentroid.Add(visual.position);
                    category = "damage";
                }

                bool nearRoad = IsNearAlgorithmicRoad(visual.position, roadCenters, roadsideDistance);
                if (nearRoad)
                    roadsideCount++;
                else
                    scatterCount++;

                float offsetX = Mathf.Abs(visual.position.x - cityCenter.x);
                float offsetZ = Mathf.Abs(visual.position.z - cityCenter.z);
                if (Mathf.Max(offsetX, offsetZ) > edgeDistance)
                {
                    edgeCount++;
                    if (edgeCount <= 12)
                    {
                        if (edgeDetails.Length > 0)
                            edgeDetails.Append('|');
                        edgeDetails.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "{0}:{1}:{2}@{3:0.0},{4:0.0}",
                            category,
                            nearRoad ? "roadside" : "scatter",
                            prefabName,
                            visual.position.x,
                            visual.position.z);
                    }
                }

                int yawIndex = Mathf.RoundToInt(visual.eulerAngles.y / 90f) % cardinalYawCounts.Length;
                cardinalYawCounts[yawIndex]++;
            }

            uniquePrefabCount = uniquePrefabs.Count;
            yawBuckets = $"0:{cardinalYawCounts[0]},90:{cardinalYawCounts[1]},180:{cardinalYawCounts[2]},270:{cardinalYawCounts[3]}";
            districtCentroids =
                $"market:{marketCentroid.Format()}|utility:{utilityCentroid.Format()}|" +
                $"residential:{residentialCentroid.Format()}|damage:{damageCentroid.Format()}";
            placementGroups =
                $"roadside:{roadsideCount}|scatter:{scatterCount}|" +
                $"extents=market:{marketCentroid.FormatExtents()}|utility:{utilityCentroid.FormatExtents()}|" +
                $"residential:{residentialCentroid.FormatExtents()}|damage:{damageCentroid.FormatExtents()}|" +
                $"other:{otherCentroid.FormatExtents()}";
            edgeOutliers = edgeCount == 0
                ? "none/0"
                : $"{edgeCount}:{edgeDetails}{(edgeCount > 12 ? $"|+{edgeCount - 12}" : string.Empty)}";
            terminalBranches = FormatAlgorithmicRoadTerminalBranches(
                generatedRoot,
                visualRoot,
                roadCellWorldSize);
        }

        private static string FormatAlgorithmicRoadTerminalBranches(
            Transform generatedRoot,
            Transform visualRoot,
            float roadCellWorldSize)
        {
            Transform roadRoot = generatedRoot.Find("RuntimeCityRoadVisuals");
            if (roadRoot == null)
                return "none/0";

            var roadPositions = new Dictionary<Vector2Int, Vector3>();
            for (int i = 0; i < roadRoot.childCount; i++)
            {
                Transform child = roadRoot.GetChild(i);
                if (!TryParseAlgorithmicRoadCell(child.name, out Vector2Int cell))
                    continue;

                roadPositions[cell] = child.position;
            }

            var endpoints = new List<Vector2Int>();
            foreach (Vector2Int cell in roadPositions.Keys)
            {
                if (CollectRoadNeighbors(cell, roadPositions, null) == 1)
                    endpoints.Add(cell);
            }

            endpoints.Sort((left, right) =>
            {
                int xComparison = left.x.CompareTo(right.x);
                return xComparison != 0 ? xComparison : left.y.CompareTo(right.y);
            });

            var visitedEdges = new HashSet<string>(StringComparer.Ordinal);
            var descriptions = new List<string>();
            float nearbyDistanceSquared = roadCellWorldSize * roadCellWorldSize * 2.56f;
            for (int endpointIndex = 0; endpointIndex < endpoints.Count; endpointIndex++)
            {
                Vector2Int endpoint = endpoints[endpointIndex];
                var path = new List<Vector2Int> { endpoint };
                Vector2Int previous = default;
                Vector2Int current = endpoint;
                bool hasPrevious = false;
                while (true)
                {
                    var neighbors = new List<Vector2Int>(4);
                    int degree = CollectRoadNeighbors(current, roadPositions, neighbors);
                    if (hasPrevious && degree != 2)
                        break;

                    Vector2Int next = default;
                    bool foundNext = false;
                    for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
                    {
                        if (hasPrevious && neighbors[neighborIndex] == previous)
                            continue;

                        next = neighbors[neighborIndex];
                        foundNext = true;
                        break;
                    }

                    if (!foundNext)
                        break;

                    string edgeKey = FormatRoadEdgeKey(current, next);
                    if (visitedEdges.Contains(edgeKey))
                    {
                        path.Clear();
                        break;
                    }

                    visitedEdges.Add(edgeKey);
                    previous = current;
                    current = next;
                    hasPrevious = true;
                    path.Add(current);
                }

                if (path.Count < 2)
                    continue;

                int terminalCellCount = path.Count;
                if (CollectRoadNeighbors(path[path.Count - 1], roadPositions, null) > 2)
                    terminalCellCount--;

                int nearbyVisualCount = 0;
                for (int visualIndex = 0; visualIndex < visualRoot.childCount; visualIndex++)
                {
                    Vector3 visualPosition = visualRoot.GetChild(visualIndex).position;
                    bool nearBranch = false;
                    for (int pathIndex = 0; pathIndex < terminalCellCount; pathIndex++)
                    {
                        Vector3 roadPosition = roadPositions[path[pathIndex]];
                        float x = visualPosition.x - roadPosition.x;
                        float z = visualPosition.z - roadPosition.z;
                        if ((x * x) + (z * z) > nearbyDistanceSquared)
                            continue;

                        nearBranch = true;
                        break;
                    }

                    if (nearBranch)
                        nearbyVisualCount++;
                }

                descriptions.Add(
                    $"{endpoint.x},{endpoint.y}->{current.x},{current.y}" +
                    $"/cells:{terminalCellCount}/nearby:{nearbyVisualCount}");
            }

            return descriptions.Count == 0
                ? "none/0"
                : $"{descriptions.Count}:{string.Join("|", descriptions)}";
        }

        private static bool TryParseAlgorithmicRoadCell(string objectName, out Vector2Int cell)
        {
            cell = default;
            if (!objectName.StartsWith("Road_", StringComparison.Ordinal))
                return false;

            string[] parts = objectName.Split('_');
            if (parts.Length != 3 ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
            {
                return false;
            }

            cell = new Vector2Int(x, y);
            return true;
        }

        private static int CollectRoadNeighbors(
            Vector2Int cell,
            IReadOnlyDictionary<Vector2Int, Vector3> roadPositions,
            List<Vector2Int> neighbors)
        {
            Vector2Int[] directions =
            {
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.left,
                Vector2Int.down
            };
            int count = 0;
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int neighbor = cell + directions[i];
                if (!roadPositions.ContainsKey(neighbor))
                    continue;

                count++;
                neighbors?.Add(neighbor);
            }

            return count;
        }

        private static string FormatRoadEdgeKey(Vector2Int first, Vector2Int second)
        {
            if (first.x > second.x || (first.x == second.x && first.y > second.y))
                (first, second) = (second, first);

            return $"{first.x},{first.y}:{second.x},{second.y}";
        }
        private static List<Vector3> CollectAlgorithmicRoadCenters(Transform generatedRoot)
        {
            var centers = new List<Vector3>();
            Transform roadRoot = generatedRoot.Find("RuntimeCityRoadVisuals");
            if (roadRoot == null)
                return centers;

            for (int i = 0; i < roadRoot.childCount; i++)
            {
                Transform child = roadRoot.GetChild(i);
                if (child.name.StartsWith("Road_", StringComparison.Ordinal))
                    centers.Add(child.position);
            }

            return centers;
        }

        private static bool IsNearAlgorithmicRoad(
            Vector3 position,
            IReadOnlyList<Vector3> roadCenters,
            float maximumDistance)
        {
            float maximumDistanceSquared = maximumDistance * maximumDistance;
            for (int i = 0; i < roadCenters.Count; i++)
            {
                float x = position.x - roadCenters[i].x;
                float z = position.z - roadCenters[i].z;
                if ((x * x) + (z * z) <= maximumDistanceSquared)
                    return true;
            }

            return false;
        }
        private static HashSet<string> CreateDamagePrefabNameSet(IReadOnlyList<GameObject> prefabs)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (prefabs == null)
                return names;

            for (int i = 0; i < prefabs.Count; i++)
            {
                GameObject prefab = prefabs[i];
                if (prefab == null ||
                    prefab.name.IndexOf("ClothCover", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prefab.name.IndexOf("Archway", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                names.Add(prefab.name);
            }

            return names;
        }

        private static HashSet<string> CreatePrefabNameSet(IReadOnlyList<GameObject> prefabs)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            AddPrefabNames(names, prefabs);
            return names;
        }

        private static void AddPrefabNames(HashSet<string> names, IReadOnlyList<GameObject> prefabs)
        {
            if (prefabs == null)
                return;

            for (int i = 0; i < prefabs.Count; i++)
            {
                GameObject prefab = prefabs[i];
                if (prefab != null)
                    names.Add(prefab.name);
            }
        }

        private struct AlgorithmicCentroidAccumulator
        {
            private Vector3 _sum;
            private Vector3 _minimum;
            private Vector3 _maximum;
            private int _count;

            public void Add(Vector3 position)
            {
                if (_count == 0)
                {
                    _minimum = position;
                    _maximum = position;
                }
                else
                {
                    _minimum = Vector3.Min(_minimum, position);
                    _maximum = Vector3.Max(_maximum, position);
                }

                _sum += position;
                _count++;
            }

            public string Format()
            {
                if (_count == 0)
                    return "none/0";

                Vector3 centroid = _sum / _count;
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.0},{1:0.0}/{2}",
                    centroid.x,
                    centroid.z,
                    _count);
            }

            public string FormatExtents()
            {
                if (_count == 0)
                    return "none/0";

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.0}..{1:0.0},{2:0.0}..{3:0.0}/{4}",
                    _minimum.x,
                    _maximum.x,
                    _minimum.z,
                    _maximum.z,
                    _count);
            }
        }
        private static uint ReadCommandLineUInt(string key, uint fallback)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], key, StringComparison.Ordinal) &&
                    uint.TryParse(arguments[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint value))
                {
                    return value;
                }
            }

            return fallback;
        }

        private static void DestroyAlgorithmicReviewConfig()
        {
            if (_algorithmicReviewConfig == null)
                return;

            UnityEngine.Object.DestroyImmediate(_algorithmicReviewConfig);
            _algorithmicReviewConfig = null;
        }

        private static void DestroyLifecycleRecoveryConfig()
        {
            if (_lifecycleRecoveryConfig == null)
                return;

            UnityEngine.Object.DestroyImmediate(_lifecycleRecoveryConfig);
            _lifecycleRecoveryConfig = null;
        }

        private readonly struct VisualCaptureDelta
        {
            public VisualCaptureDelta(float meanAbsoluteError, float outlierFraction)
            {
                MeanAbsoluteError = meanAbsoluteError;
                OutlierFraction = outlierFraction;
            }

            public float MeanAbsoluteError { get; }
            public float OutlierFraction { get; }
            public bool IsAccepted => MeanAbsoluteError <= 3f && OutlierFraction <= 0.01f;
        }

        private static VisualCaptureDelta CompareVisualCaptures(string expectedPath, string actualPath)
        {
            Texture2D expected = LoadCaptureTexture(expectedPath);
            Texture2D actual = LoadCaptureTexture(actualPath);
            try
            {
                if (expected.width != actual.width || expected.height != actual.height)
                {
                    throw new InvalidOperationException(
                        $"M01 visual captures have different dimensions: " +
                        $"expected={expected.width}x{expected.height} actual={actual.width}x{actual.height}.");
                }

                Color32[] expectedPixels = expected.GetPixels32();
                Color32[] actualPixels = actual.GetPixels32();
                long absoluteDifference = 0;
                int outlierCount = 0;
                for (int i = 0; i < expectedPixels.Length; i++)
                {
                    int red = Mathf.Abs(expectedPixels[i].r - actualPixels[i].r);
                    int green = Mathf.Abs(expectedPixels[i].g - actualPixels[i].g);
                    int blue = Mathf.Abs(expectedPixels[i].b - actualPixels[i].b);
                    absoluteDifference += red + green + blue;
                    if (Mathf.Max(red, Mathf.Max(green, blue)) > 8)
                        outlierCount++;
                }

                float meanAbsoluteError = expectedPixels.Length > 0
                    ? absoluteDifference / (expectedPixels.Length * 3f)
                    : float.MaxValue;
                float outlierFraction = expectedPixels.Length > 0
                    ? (float)outlierCount / expectedPixels.Length
                    : 1f;
                Debug.Log(
                    $"[M01VisualCaptureDelta] expected={expectedPath} actual={actualPath} " +
                    $"meanAbsoluteError={meanAbsoluteError:0.000} outlierFraction={outlierFraction:P3}");
                return new VisualCaptureDelta(meanAbsoluteError, outlierFraction);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(expected);
                UnityEngine.Object.DestroyImmediate(actual);
            }
        }

        private static Texture2D LoadCaptureTexture(string relativePath)
        {
            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath), markNonReadable: false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidDataException($"Could not decode M01 visual capture: {relativePath}");
            }
            return texture;
        }

        private static void CaptureCamera(Camera camera, string outputPath)
        {
            const int width = 1600;
            const int height = 900;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
                name = "M01RuntimeGenerationCapture"
            };
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            ParticleSystemRenderer[] particleRenderers =
                UnityEngine.Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsInactive.Include);
            var particleRendererStates = new bool[particleRenderers.Length];
            Texture2D texture = null;
            try
            {
                for (int i = 0; i < particleRenderers.Length; i++)
                {
                    ParticleSystemRenderer particleRenderer = particleRenderers[i];
                    particleRendererStates[i] = particleRenderer.enabled;
                    particleRenderer.enabled = false;
                }

                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                camera.Render();
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                Color32[] pixels = texture.GetPixels32();
                long luminance = 0;
                int minimumLuminance = 255 * 3;
                int maximumLuminance = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    int pixelLuminance = pixels[i].r + pixels[i].g + pixels[i].b;
                    luminance += pixelLuminance;
                    minimumLuminance = Mathf.Min(minimumLuminance, pixelLuminance);
                    maximumLuminance = Mathf.Max(maximumLuminance, pixelLuminance);
                }
                float averageLuminance = pixels.Length > 0 ? luminance / (pixels.Length * 3f) : 0f;
                int luminanceRange = maximumLuminance - minimumLuminance;
                if (averageLuminance < 2f || luminanceRange < 24)
                {
                    throw new InvalidOperationException(
                        $"Runtime generation capture is blank or uniform. " +
                        $"averageLuminance={averageLuminance:0.00} luminanceRange={luminanceRange}");
                }

                string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputPath));
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
                Debug.Log(
                    $"[M01RuntimeMapCapture] averageLuminance={averageLuminance:0.00} " +
                    $"luminanceRange={luminanceRange} path={absolutePath}");
            }
            finally
            {
                for (int i = 0; i < particleRenderers.Length; i++)
                {
                    if (particleRenderers[i] != null)
                        particleRenderers[i].enabled = particleRendererStates[i];
                }

                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void CreateRuntimeView(
            Transform parent,
            Transform generatedRoot,
            RuntimeCitySpawnerSystemConfig config,
            RuntimeOperationMapVisualRecipe visualRecipe,
            Camera presentationCamera,
            TextMesh statusText)
        {
            GameObject host = new("RuntimeCityRAndDMapView");
            host.transform.SetParent(parent, false);
            RuntimeCityRAndDMapView view = host.AddComponent<RuntimeCityRAndDMapView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("config").objectReferenceValue = config;
            serialized.FindProperty("visualRecipe").objectReferenceValue = visualRecipe;
            serialized.FindProperty("deterministicFallbackRecipe").objectReferenceValue = visualRecipe;
            serialized.FindProperty("deterministicFallbackEnabled").boolValue = true;
            serialized.FindProperty("generateOnStart").boolValue = true;
            serialized.FindProperty("showDebugOverlay").boolValue = true;
            serialized.FindProperty("visualRecipeEntriesPerFrame").intValue = 8;
            serialized.FindProperty("visualRecipeFrameBudgetMilliseconds").floatValue = 6f;
            serialized.FindProperty("gridWidth").intValue = 512;
            serialized.FindProperty("gridHeight").intValue = 512;
            serialized.FindProperty("gridCellSize").floatValue = 1f;
            serialized.FindProperty("gridOrigin").vector3Value = new Vector3(-256f, 0f, -256f);
            serialized.FindProperty("roadCellSizeInGridCells").intValue = 10;
            serialized.FindProperty("algorithmicNorthRadialTrim").intValue = 4;
            serialized.FindProperty("algorithmicEastRadialTrim").intValue = 4;
            serialized.FindProperty("algorithmicSouthRadialTrim").intValue = 3;
            serialized.FindProperty("algorithmicWestRadialTrim").intValue = 1;
            serialized.FindProperty("algorithmicMaximumOuterStreetLength").intValue = 3;
            SetAlgorithmicReveal(
                serialized.FindProperty("algorithmicReveal"),
                terrainSeconds: 0.25f,
                districtSeconds: 0.4f,
                marketSeconds: 0.35f,
                compoundSeconds: 0.35f,
                aftermathSeconds: 2.5f,
                horizonSeconds: 0.5f);
            SetCameraPoses(
                serialized.FindProperty("algorithmicCameraPoses"),
                CreateAlgorithmicCameraPoses());
            serialized.FindProperty("generatedRoot").objectReferenceValue = generatedRoot;
            serialized.FindProperty("presentationCamera").objectReferenceValue = presentationCamera;
            serialized.FindProperty("roadMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(AsphaltMaterialPath);
            serialized.FindProperty("roadShoulderMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(DistrictGroundMaterialPath);
            serialized.FindProperty("algorithmicRoadColor").colorValue = new Color(0.12f, 0.13f, 0.14f, 1f);
            serialized.FindProperty("algorithmicRoadShoulderColor").colorValue = new Color(0.56f, 0.42f, 0.28f, 1f);
            serialized.FindProperty("algorithmicGroundMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(SandMaterialPath);
            serialized.FindProperty("algorithmicGroundColor").colorValue = new Color(0.46f, 0.32f, 0.20f, 1f);
            SerializedProperty districtSurfaces = serialized.FindProperty("algorithmicDistrictSurfaces");
            districtSurfaces.arraySize = 4;
            Material districtGroundMaterial = AssetDatabase.LoadAssetAtPath<Material>(DistrictGroundMaterialPath);
            SetAlgorithmicDistrictSurface(
                districtSurfaces.GetArrayElementAtIndex(0),
                "OldMarketDistrictApron",
                districtGroundMaterial,
                new Vector2(-5.0f, 1.0f),
                new Vector2(9.5f, 6.0f),
                new Color(0.58f, 0.43f, 0.28f, 1f),
                101u);
            SetAlgorithmicDistrictSurface(
                districtSurfaces.GetArrayElementAtIndex(1),
                "UtilityCompoundDistrictApron",
                districtGroundMaterial,
                new Vector2(5.0f, 1.0f),
                new Vector2(8.0f, 5.5f),
                new Color(0.39f, 0.35f, 0.30f, 1f),
                211u);
            SetAlgorithmicDistrictSurface(
                districtSurfaces.GetArrayElementAtIndex(2),
                "ResidentialEdgeDistrictApron",
                districtGroundMaterial,
                new Vector2(4.5f, -5.0f),
                new Vector2(7.5f, 5.0f),
                new Color(0.44f, 0.34f, 0.25f, 1f),
                263u);
            SetAlgorithmicDistrictSurface(
                districtSurfaces.GetArrayElementAtIndex(3),
                "DamagedCorridorDistrictApron",
                districtGroundMaterial,
                new Vector2(-5.0f, -6.5f),
                new Vector2(5.5f, 4.5f),
                new Color(0.34f, 0.235f, 0.18f, 1f),
                307u);
            SetAlgorithmicAftermath(
                serialized.FindProperty("algorithmicAftermath"),
                "RuntimeCityAftermathDressing",
                M01AftermathDressingPrefabPaths,
                maxAnchorGroups: 4,
                itemsPerGroup: 5,
                minRadius: 3.5f,
                maxRadius: 10.5f,
                minScale: 1.35f,
                maxScale: 1.9f,
                exposureDirection: new Vector2(-1f, -1f),
                exposureArcDegrees: 70f,
                fallbackCenterOffsetInRoadCells: new Vector2(-5f, -6.5f),
                fallbackAnchorSpacingInRoadCells: 1.4f,
                minimumAuthoredAnchorGroups: 2,
                seedOffset: 401u);
            serialized.FindProperty("statusText").objectReferenceValue = statusText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!ReferenceEquals(view.Config, config) || !ReferenceEquals(view.VisualRecipe, visualRecipe))
                throw new InvalidOperationException("Runtime map view rejected its persistent config or recipe reference.");
        }

        private static void CreateLighting(Transform parent)
        {
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(DesertSkyboxMaterialPath) ??
                                    AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0022f;
            RenderSettings.fogColor = new Color(0.72f, 0.58f, 0.42f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.43f, 0.35f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.43f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.085f, 0.055f);
            RenderSettings.ambientIntensity = 1.4f;
            RenderSettings.reflectionIntensity = 0.85f;

            GameObject lightingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PremiumLightingRigPath);
            GameObject rig = PrefabUtility.InstantiatePrefab(lightingPrefab) as GameObject;
            if (rig == null)
                throw new InvalidOperationException($"Could not instantiate premium lighting rig: {PremiumLightingRigPath}");
            rig.name = "M01_Runtime_PremiumLightingRig";
            rig.transform.SetParent(parent, false);

            UnityEngine.Object profile = AssetDatabase.LoadMainAssetAtPath(PrototypeVolumeProfilePath);
            Component[] rigComponents = rig.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < rigComponents.Length; i++)
            {
                Component component = rigComponents[i];
                if (!string.Equals(component.GetType().Name, "Volume", StringComparison.Ordinal))
                    continue;

                var serialized = new SerializedObject(component);
                SerializedProperty sharedProfile = serialized.FindProperty("sharedProfile");
                if (sharedProfile != null)
                {
                    sharedProfile.objectReferenceValue = profile;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                break;
            }

            Light[] lights = rig.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light.type != LightType.Directional)
                    continue;
                light.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
                light.intensity = 1.8f;
                RenderSettings.sun = light;
                break;
            }

            GameObject fillObject = new("M01_SoftSkyFill");
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.rotation = Quaternion.Euler(58f, 145f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.60f, 0.70f, 0.82f);
            fill.intensity = 0.48f;
            fill.shadows = LightShadows.None;
        }

        private static Camera CreateCamera(Transform parent)
        {
            GameObject cameraObject = new("RuntimeGenerationCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = AcceptedGameplayCameraPosition;
            cameraObject.transform.rotation = Quaternion.LookRotation(
                (AcceptedGameplayCameraTarget - cameraObject.transform.position).normalized,
                Vector3.up);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = AcceptedGameplayCameraFieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1200f;
            camera.allowHDR = true;
            camera.allowMSAA = true;

            Type dataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (dataType != null)
            {
                Component data = cameraObject.GetComponent(dataType) ?? cameraObject.AddComponent(dataType);
                PropertyInfo renderPostProcessing = dataType.GetProperty("renderPostProcessing", BindingFlags.Instance | BindingFlags.Public);
                renderPostProcessing?.SetValue(data, true);
            }

            return camera;
        }

        private static TextMesh CreateStatusText(Transform cameraTransform)
        {
            GameObject statusObject = new("RuntimeGenerationStatusView");
            statusObject.transform.SetParent(cameraTransform, false);
            statusObject.transform.localPosition = new Vector3(-0.68f, 0.38f, 1.2f);
            statusObject.transform.localRotation = Quaternion.identity;
            TextMesh statusText = statusObject.AddComponent<TextMesh>();
            statusText.anchor = TextAnchor.UpperLeft;
            statusText.alignment = TextAlignment.Left;
            statusText.fontSize = 38;
            statusText.characterSize = 0.0048f;
            statusText.lineSpacing = 0.9f;
            statusText.color = Color.white;
            statusText.richText = false;
            statusText.text = "Runtime map generation ready";
            MeshRenderer renderer = statusObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "RuntimeGenerationStatusBackdrop";
            backdrop.transform.SetParent(statusObject.transform, false);
            backdrop.transform.localPosition = new Vector3(0.60f, -0.04f, 0.02f);
            backdrop.transform.localRotation = Quaternion.identity;
            backdrop.transform.localScale = new Vector3(1.24f, 0.08f, 1f);
            Collider backdropCollider = backdrop.GetComponent<Collider>();
            if (backdropCollider != null)
                UnityEngine.Object.DestroyImmediate(backdropCollider);
            MeshRenderer backdropRenderer = backdrop.GetComponent<MeshRenderer>();
            Material backdropMaterial = AssetDatabase.LoadAssetAtPath<Material>(AsphaltMaterialPath);
            if (backdropRenderer != null)
            {
                backdropRenderer.sharedMaterial = backdropMaterial;
                backdropRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                backdropRenderer.receiveShadows = false;
            }

            return statusText;
        }

        private static Transform CreateRoot(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root.transform;
        }

        private static void EnsureDirectoryForAsset(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
                return;

            string current = "Assets";
            string[] parts = directory.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetBool(SerializedObject serialized, string name, bool value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized config property: {name}");
            property.boolValue = value;
        }

        private static void SetInteger(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized config property: {name}");
            property.intValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string name, float value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized config property: {name}");
            property.floatValue = value;
        }

        private static void SetPrefabList(SerializedObject serialized, string name, IReadOnlyList<string> assetPaths)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null || !property.isArray)
                throw new InvalidOperationException($"Missing serialized prefab list: {name}");

            property.arraySize = assetPaths.Count;
            for (int i = 0; i < assetPaths.Count; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPaths[i]);
                if (prefab == null)
                    throw new InvalidOperationException($"Missing M01 runtime prefab: {assetPaths[i]}");
                property.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
            }
        }

        private static void SetAlgorithmicDistrictSurface(
            SerializedProperty property,
            string surfaceName,
            Material material,
            Vector2 offsetInRoadCells,
            Vector2 sizeInRoadCells,
            Color color,
            uint seedOffset)
        {
            property.FindPropertyRelative("surfaceName").stringValue = surfaceName;
            property.FindPropertyRelative("material").objectReferenceValue = material;
            property.FindPropertyRelative("offsetInRoadCells").vector2Value = offsetInRoadCells;
            property.FindPropertyRelative("sizeInRoadCells").vector2Value = sizeInRoadCells;
            property.FindPropertyRelative("color").colorValue = color;
            property.FindPropertyRelative("seedOffset").longValue = seedOffset;
        }

        private static void SetAlgorithmicReveal(
            SerializedProperty property,
            float terrainSeconds,
            float districtSeconds,
            float marketSeconds,
            float compoundSeconds,
            float aftermathSeconds,
            float horizonSeconds)
        {
            if (property == null)
                throw new InvalidOperationException("Missing algorithmic reveal settings.");

            property.FindPropertyRelative("terrainAndRoadsSeconds").floatValue = terrainSeconds;
            property.FindPropertyRelative("districtModulesSeconds").floatValue = districtSeconds;
            property.FindPropertyRelative("marketSeconds").floatValue = marketSeconds;
            property.FindPropertyRelative("compoundSeconds").floatValue = compoundSeconds;
            property.FindPropertyRelative("aftermathSeconds").floatValue = aftermathSeconds;
            property.FindPropertyRelative("horizonSeconds").floatValue = horizonSeconds;
        }

        private static void SetCameraPoses(
            SerializedProperty property,
            IReadOnlyList<RuntimeOperationMapCameraPose> poses)
        {
            if (property == null || !property.isArray)
                throw new InvalidOperationException("Missing algorithmic camera pose list.");

            property.arraySize = poses.Count;
            for (int i = 0; i < poses.Count; i++)
            {
                RuntimeOperationMapCameraPose pose = poses[i];
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("stage").enumValueIndex = (int)pose.Stage;
                element.FindPropertyRelative("position").vector3Value = pose.Position;
                element.FindPropertyRelative("target").vector3Value = pose.Target;
                element.FindPropertyRelative("fieldOfView").floatValue = pose.FieldOfView;
                element.FindPropertyRelative("transitionSeconds").floatValue = pose.TransitionSeconds;
            }
        }

        private static void SetAlgorithmicAftermath(
            SerializedProperty property,
            string groupName,
            IReadOnlyList<string> prefabPaths,
            int maxAnchorGroups,
            int itemsPerGroup,
            float minRadius,
            float maxRadius,
            float minScale,
            float maxScale,
            Vector2 exposureDirection,
            float exposureArcDegrees,
            Vector2 fallbackCenterOffsetInRoadCells,
            float fallbackAnchorSpacingInRoadCells,
            int minimumAuthoredAnchorGroups,
            uint seedOffset)
        {
            if (property == null)
                throw new InvalidOperationException("Missing algorithmic aftermath settings.");

            property.FindPropertyRelative("groupName").stringValue = groupName;
            SerializedProperty prefabs = property.FindPropertyRelative("dressingPrefabs");
            prefabs.arraySize = prefabPaths.Count;
            for (int i = 0; i < prefabPaths.Count; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab == null)
                    throw new InvalidOperationException($"Missing M01 aftermath prefab: {prefabPaths[i]}");
                prefabs.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
            }

            property.FindPropertyRelative("maxAnchorGroups").intValue = maxAnchorGroups;
            property.FindPropertyRelative("itemsPerGroup").intValue = itemsPerGroup;
            property.FindPropertyRelative("minRadius").floatValue = minRadius;
            property.FindPropertyRelative("maxRadius").floatValue = maxRadius;
            property.FindPropertyRelative("minScale").floatValue = minScale;
            property.FindPropertyRelative("maxScale").floatValue = maxScale;
            property.FindPropertyRelative("exposureDirection").vector2Value = exposureDirection;
            property.FindPropertyRelative("exposureArcDegrees").floatValue = exposureArcDegrees;
            property.FindPropertyRelative("fallbackCenterOffsetInRoadCells").vector2Value =
                fallbackCenterOffsetInRoadCells;
            property.FindPropertyRelative("fallbackAnchorSpacingInRoadCells").floatValue =
                fallbackAnchorSpacingInRoadCells;
            property.FindPropertyRelative("minimumAuthoredAnchorGroups").intValue =
                minimumAuthoredAnchorGroups;
            property.FindPropertyRelative("seedOffset").longValue = seedOffset;
        }

        private static bool TryFindCameraPose(
            IReadOnlyList<RuntimeOperationMapCameraPose> poses,
            RuntimeOperationMapVisualStage stage,
            out RuntimeOperationMapCameraPose result)
        {
            for (int i = 0; i < poses.Count; i++)
            {
                if (poses[i].Stage != stage)
                    continue;

                result = poses[i];
                return true;
            }

            result = default;
            return false;
        }

        private static void SetVector2Int(SerializedObject serialized, string name, Vector2Int value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized config property: {name}");
            property.vector2IntValue = value;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
#endif
}
