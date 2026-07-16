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
        private const string VisualRecipeVersion = "M01RuntimeVisualRecipe_2026-07-16_v12";
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

        [MenuItem("Game/Map Prototypes/M01/Build Runtime Generation Prototype")]
        public static void BuildPrototype()
        {
            RuntimeCitySpawnerSystemConfig config = CreateOrUpdateConfig();
            RuntimeOperationMapVisualRecipe visualRecipe = CreateOrUpdateVisualRecipe();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "M01_RuntimeGenerationPrototype";

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

        public static void CaptureCurrentEditorReferenceAndExit()
        {
            try
            {
                EditorSceneManager.OpenScene(M01VisualMapPrototypeEditorUtility.ScenePath, OpenSceneMode.Single);
                GameObject cameraObject = GameObject.Find("M01_Review_GameplayOverview");
                Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
                if (camera == null)
                    throw new InvalidOperationException("Accepted M01 editor scene is missing its gameplay overview camera.");

                CaptureCamera(camera, "Logs/M01_EditorCurrentReference.png");
                Debug.Log("[M01CurrentEditorReferenceCapture] result=Passed capture=Logs/M01_EditorCurrentReference.png");
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
            int districtSliceCount = 0;
            for (int i = 0; i < visualRecipe.DistrictModules.Count; i++)
            {
                RuntimeOperationMapDistrictModuleRecipe module = visualRecipe.DistrictModules[i];
                Assert(module != null && module.IsConfigured, $"Runtime district module {i} is not configured.");
                districtSliceCount += module.SlicePaths.Count;
            }
            Assert(districtSliceCount >= 3,
                $"Runtime visual recipe must define bounded slices for all three M01 districts: actual={districtSliceCount}.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RuntimeCityRAndDMapView[] views = UnityEngine.Object.FindObjectsByType<RuntimeCityRAndDMapView>(FindObjectsInactive.Include);
            Assert(views.Length == 1, $"Expected one runtime map view in {ScenePath}, found {views.Length}.");
            Assert(views[0].PresentationCamera != null, "Runtime map view must reference the staged presentation camera.");
            Assert(views[0].VisualRecipeFrameBudgetMilliseconds > 0f,
                "Runtime map view must declare a positive visual generation frame budget.");
            Assert(views[0].AlgorithmicReveal.GetMinimumDuration(RuntimeOperationMapVisualStage.Aftermath) >= 2f,
                "Algorithmic aftermath reveal must remain readable for at least two seconds.");
            Assert(views[0].AlgorithmicAftermath.FallbackAnchorSpacingInRoadCells > 0f,
                "Algorithmic aftermath must declare deterministic district fallback anchors for sparse seeds.");
            Assert(views[0].AlgorithmicAftermath.MinimumAuthoredAnchorGroups >= 2,
                "Algorithmic aftermath must reserve authored incident anchors across dense seeds.");
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

            var entries = new List<RuntimeOperationMapVisualEntry>(256);
            CaptureGroup(sourceRoot.transform.Find("_M01VisualGenerated/01_Terrain_And_RoadPlan"), RuntimeOperationMapVisualStage.TerrainAndRoads, entries);
            entries.RemoveAll(entry => string.Equals(entry.Name, "DesertGround", StringComparison.Ordinal));
            List<RuntimeOperationMapDistrictModuleRecipe> districtModules = CaptureDistrictModuleGroup(
                sourceRoot.transform.Find("_M01VisualGenerated/02_DemoAuthored_DistrictModules"));
            CaptureGroup(sourceRoot.transform.Find("_M01AuthoredStoryOverrides/03_OldMarket_StoryLayer"), RuntimeOperationMapVisualStage.Market, entries);
            CaptureGroup(sourceRoot.transform.Find("_M01AuthoredStoryOverrides/04_UtilityCompound_StoryLayer"), RuntimeOperationMapVisualStage.Compound, entries);
            CaptureGroup(sourceRoot.transform.Find("_M01AuthoredStoryOverrides/05_BombingAftermath_StoryLayer"), RuntimeOperationMapVisualStage.Aftermath, entries);
            CaptureGroup(sourceRoot.transform.Find("_M01VisualGenerated/07_Horizon_And_EdgeDressing"), RuntimeOperationMapVisualStage.Horizon, entries);

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

            var foundation = new RuntimeOperationMapFoundationSettings(
                AssetDatabase.LoadAssetAtPath<Material>(SandMaterialPath),
                new Vector3(0f, -0.65f, 16f),
                new Vector3(1200f, 1.2f, 900f),
                new Color(0.54f, 0.40f, 0.26f, 1f));
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
                    new Vector3(-75f, 42f, -82f),
                    new Vector3(12f, 1f, -40f),
                    48f,
                    0.45f),
                new(
                    RuntimeOperationMapVisualStage.Horizon,
                    new Vector3(-105f, 54f, -80f),
                    new Vector3(-20f, 1.5f, 0f),
                    53f,
                    0.5f)
            };
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
                RuntimeOperationMapVisualCleanupSettings cleanup = GetCleanupSettings(
                    module.name,
                    RuntimeOperationMapVisualStage.DistrictModules);
                if (!cleanup.IsConfigured)
                    throw new InvalidOperationException($"M01 district module {module.name} is missing cleanup settings.");

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

                GameObject modulePrefab = PrefabUtility.GetCorrespondingObjectFromSource(sourceInstance.gameObject);
                if (modulePrefab == null)
                    throw new InvalidOperationException($"Could not resolve district prefab source for {module.name}.");

                var slicePaths = new List<string>(256);
                for (int childIndex = 0; childIndex < sourceInstance.childCount; childIndex++)
                    CaptureDistrictPrefabSlicePath(sourceInstance.GetChild(childIndex), sourceInstance, slicePaths);

                modules.Add(new RuntimeOperationMapDistrictModuleRecipe(
                    module.name,
                    modulePrefab,
                    sourceInstance.position,
                    sourceInstance.rotation,
                    sourceInstance.lossyScale,
                    sourceInstance.gameObject.activeInHierarchy,
                    cleanup,
                    slicePaths));
            }

            return modules;
        }

        private static void CaptureDistrictPrefabSlicePath(
            Transform candidate,
            Transform module,
            List<string> slicePaths)
        {
            Renderer[] renderers = candidate.GetComponentsInChildren<Renderer>(true);
            Component[] components = candidate.GetComponents<Component>();
            bool transformOnlyContainer = components.Length == 1 && components[0] is Transform;
            if (renderers.Length > MaxDistrictSliceRenderers &&
                candidate.childCount > 0 &&
                transformOnlyContainer)
            {
                for (int childIndex = 0; childIndex < candidate.childCount; childIndex++)
                    CaptureDistrictPrefabSlicePath(candidate.GetChild(childIndex), module, slicePaths);
                return;
            }

            GameObject prefabSlice = PrefabUtility.GetCorrespondingObjectFromSource(candidate.gameObject);
            if (prefabSlice == null)
            {
                throw new InvalidOperationException(
                    $"Could not resolve district prefab slice source for {module.name}/{candidate.name}.");
            }

            string relativePath = AnimationUtility.CalculateTransformPath(candidate, module);
            slicePaths.Add(relativePath);
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
                    stage == RuntimeOperationMapVisualStage.Aftermath,
                    cleanupSettings: GetCleanupSettings(gameObject.name, stage)));
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

        private static RuntimeOperationMapVisualCleanupSettings GetCleanupSettings(
            string entryName,
            RuntimeOperationMapVisualStage stage)
        {
            if (stage != RuntimeOperationMapVisualStage.DistrictModules)
                return default;

            if (entryName.StartsWith("OldMarket_West_DemoAuthored", StringComparison.Ordinal))
                return new RuntimeOperationMapVisualCleanupSettings(new Vector2(-67f, 12f), new Vector2(104f, 100f));
            if (entryName.StartsWith("UtilityCompound_East_DemoAuthored", StringComparison.Ordinal))
                return new RuntimeOperationMapVisualCleanupSettings(new Vector2(69f, 13f), new Vector2(96f, 112f));
            if (entryName.StartsWith("Residential_South_DemoAuthored", StringComparison.Ordinal))
                return new RuntimeOperationMapVisualCleanupSettings(new Vector2(-5f, -70f), new Vector2(106f, 106f));

            return default;
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

                        if (!_playModeMarketCaptureComplete)
                        {
                            Debug.LogError(
                                "[M01RuntimeMapPlayModeSmoke] result=Failed reason=marketRevealCameraStageNotObserved");
                            EditorApplication.ExitPlaymode();
                            return;
                        }

                        CaptureRuntimeCamera();
                        _playModeCaptureComplete = true;
                        _playModeExitCode = 0;
                        Debug.Log(
                            $"[M01RuntimeMapPlayModeSmoke] result=Passed version={RuntimeCityGenerationProgress.VersionTag} " +
                            $"seed={progress.Seed} cities={progress.GeneratedCityCount}/{progress.RequestedCityCount} " +
                            $"roadStrokes={runtimeSystem.RoadStrokeCount} roadCells={runtimeSystem.RoadCellCount} " +
                            $"plannedBuildings={runtimeSystem.PlannedBuildingCount} visualBuildings={runtimeSystem.VisualBuildingCount} " +
                            $"recipeEntries={runtimeSystem.VisualRecipeEntryCount} renderers={runtimeSystem.VisualRecipeRendererCount} " +
                            $"foundations={runtimeSystem.FoundationVisualCount} districtTerrainCleanups={runtimeSystem.SuppressedObstructionCount} " +
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
                cameraTransform.position = new Vector3(0f, 280f, 0f);
                cameraTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
                camera.orthographic = true;
                camera.orthographicSize = 145f;
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
            Texture2D texture = null;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                camera.Render();
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                Color32[] pixels = texture.GetPixels32();
                long luminance = 0;
                for (int i = 0; i < pixels.Length; i++)
                    luminance += pixels[i].r + pixels[i].g + pixels[i].b;
                float averageLuminance = pixels.Length > 0 ? luminance / (pixels.Length * 3f) : 0f;
                if (averageLuminance < 2f)
                    throw new InvalidOperationException($"Runtime generation capture is blank. averageLuminance={averageLuminance:0.00}");

                string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputPath));
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
                Debug.Log($"[M01RuntimeMapCapture] averageLuminance={averageLuminance:0.00} path={absolutePath}");
            }
            finally
            {
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
                if (string.Equals(component.GetType().Name, "Volume", StringComparison.Ordinal))
                {
                    var serialized = new SerializedObject(component);
                    SerializedProperty sharedProfile = serialized.FindProperty("sharedProfile");
                    if (sharedProfile != null)
                    {
                        sharedProfile.objectReferenceValue = profile;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                Light light = component as Light;
                if (light == null || light.type != LightType.Directional)
                    continue;
                light.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
                light.intensity = 1.8f;
                RenderSettings.sun = light;
            }

            GameObject fillObject = new("SkyFill");
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.rotation = Quaternion.Euler(55f, 145f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.55f, 0.68f, 0.86f);
            fill.intensity = 0.32f;
            fill.shadows = LightShadows.None;
        }

        private static Camera CreateCamera(Transform parent)
        {
            GameObject cameraObject = new("RuntimeGenerationCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(-105f, 54f, -80f);
            Vector3 target = new(-20f, 1.5f, 0f);
            cameraObject.transform.rotation = Quaternion.LookRotation((target - cameraObject.transform.position).normalized, Vector3.up);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 53f;
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
