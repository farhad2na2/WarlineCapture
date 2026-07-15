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

    public static partial class M01VisualMapPrototypeBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/MapPrototypes/Chapter01/M01_VisualPrototype.unity";
        public const string CaptureDirectory = "Design/ArtReview/OperationMaps/M01";
        public const int GenerationSeed = 26071501;
        public const string GeneratorVersion = "M01VisualPrototype_2026-07-15_v6";

        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const string MaterialDirectory = "Assets/Game/Art/MapPrototypes/M01/Materials";
        private const string PremiumLightingRigPath = "Assets/Game/Rendering/Prefabs/PremiumLightingRig.prefab";
        private const string PremiumVolumeProfilePath = "Assets/Game/Rendering/Profiles/PremiumGlobalVolumeProfile.asset";
        private const string PrototypeVolumeProfilePath = "Assets/Game/Art/MapPrototypes/M01/M01_VisualVolumeProfile.asset";
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

        private static readonly string[] RequiredPrefabPaths =
        {
            PremiumLightingRigPath,
            TownMarketModulePath,
            BaseCommandModulePath,
            SouthTownModulePath,
            "Assets/Game/Prefabs/Environment/City/Clock_Tower_01.prefab",
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Ruins_03.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Stall_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_ClothCover_Large_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Truck_01_Destroyed.prefab",
            "Assets/PolygonMilitary/Prefabs/FX/FX_Fire_01.prefab",
            "Assets/PolygonMilitary/Prefabs/FX/FX_Smoke_Large_01.prefab",
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
        }

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
            CreateAuthoredDistrictModules(generatedRoot.transform);
            CreateOldMarketStoryLayer(authoredRoot.transform, palette);
            CreateCompoundStoryLayer(authoredRoot.transform, palette);
            CreateBombingAftermath(authoredRoot.transform);
            CreateHorizonAndEdgeDressing(generatedRoot.transform);
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
                Sand = CreateOrUpdateMaterial(SandMaterialPath, new Color(0.30f, 0.22f, 0.14f), 0f, 0.12f),
                Asphalt = CreateOrUpdateMaterial(AsphaltMaterialPath, new Color(0.055f, 0.06f, 0.065f), 0f, 0.2f),
                Concrete = CreateOrUpdateMaterial(ConcreteMaterialPath, new Color(0.35f, 0.33f, 0.29f), 0f, 0.18f),
                Curb = CreateOrUpdateMaterial(CurbMaterialPath, new Color(0.69f, 0.62f, 0.50f), 0f, 0.22f),
                WhitePaint = CreateOrUpdateMaterial(WhitePaintMaterialPath, new Color(0.88f, 0.84f, 0.70f), 0f, 0.25f),
                AmberPaint = CreateOrUpdateMaterial(AmberPaintMaterialPath, new Color(0.95f, 0.52f, 0.08f), 0f, 0.27f),
                Turquoise = CreateOrUpdateMaterial(TurquoiseMaterialPath, new Color(0.035f, 0.36f, 0.36f), 0f, 0.25f),
                Rust = CreateOrUpdateMaterial(RustMaterialPath, new Color(0.38f, 0.11f, 0.045f), 0.05f, 0.16f)
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
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
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

        private static void CreateTerrainAndRoadPlan(Transform parent, Palette palette)
        {
            Transform terrainRoot = CreateRoot("01_Terrain_And_RoadPlan", parent).transform;
            CreateBox("DesertGround", new Vector3(0f, -1.15f, 0f), new Vector3(310f, 2.2f, 238f), palette.Sand, terrainRoot);

            CreateBox("MainRoad_NS", new Vector3(0f, 0.03f, 0f), new Vector3(21f, 0.18f, 230f), palette.Asphalt, terrainRoot);
            CreateBox("MainRoad_EW", new Vector3(0f, 0.04f, -18f), new Vector3(300f, 0.2f, 19f), palette.Asphalt, terrainRoot);
            CreateBox("MarketServiceRoad", new Vector3(-64f, 0.02f, 48f), new Vector3(104f, 0.14f, 9f), palette.Concrete, terrainRoot);
            CreateBox("CompoundAccessRoad", new Vector3(66f, 0.02f, 50f), new Vector3(105f, 0.14f, 10f), palette.Concrete, terrainRoot);

            CreateVerticalCurbs(terrainRoot, palette.Curb, -11.3f);
            CreateVerticalCurbs(terrainRoot, palette.Curb, 11.3f);
            CreateHorizontalCurbs(terrainRoot, palette.Curb, -28.3f);
            CreateHorizontalCurbs(terrainRoot, palette.Curb, -7.7f);
            CreateRoadMarkings(terrainRoot, palette);
            CreateIntersectionDetails(terrainRoot, palette);
        }

        private static void CreateVerticalCurbs(Transform parent, Material material, float x)
        {
            CreateBox($"Curb_NS_{x:0}_North", new Vector3(x, 0.18f, 55f), new Vector3(1.5f, 0.34f, 90f), material, parent);
            CreateBox($"Curb_NS_{x:0}_South", new Vector3(x, 0.18f, -78f), new Vector3(1.5f, 0.34f, 55f), material, parent);
        }

        private static void CreateHorizontalCurbs(Transform parent, Material material, float z)
        {
            CreateBox($"Curb_EW_{z:0}_West", new Vector3(-79f, 0.17f, z), new Vector3(126f, 0.34f, 1.5f), material, parent);
            CreateBox($"Curb_EW_{z:0}_East", new Vector3(79f, 0.17f, z), new Vector3(126f, 0.34f, 1.5f), material, parent);
        }

        private static void CreateRoadMarkings(Transform parent, Palette palette)
        {
            for (int z = -103; z <= 103; z += 13)
            {
                if (z > -34 && z < 2)
                    continue;
                CreateBox($"NS_Dash_{z}", new Vector3(0f, 0.16f, z), new Vector3(0.35f, 0.05f, 5.4f), palette.AmberPaint, parent);
            }

            for (int x = -139; x <= 139; x += 14)
            {
                if (x > -18 && x < 18)
                    continue;
                CreateBox($"EW_Dash_{x}", new Vector3(x, 0.17f, -18f), new Vector3(5.6f, 0.05f, 0.34f), palette.WhitePaint, parent);
            }

            CreateBox("NS_Edge_West", new Vector3(-8.6f, 0.15f, 0f), new Vector3(0.25f, 0.04f, 226f), palette.WhitePaint, parent);
            CreateBox("NS_Edge_East", new Vector3(8.6f, 0.15f, 0f), new Vector3(0.25f, 0.04f, 226f), palette.WhitePaint, parent);
        }

        private static void CreateIntersectionDetails(Transform parent, Palette palette)
        {
            for (int i = -4; i <= 4; i++)
            {
                float x = i * 1.8f;
                CreateBox($"Crosswalk_N_{i}", new Vector3(x, 0.19f, -5.6f), new Vector3(0.9f, 0.04f, 3.2f), palette.WhitePaint, parent);
                CreateBox($"Crosswalk_S_{i}", new Vector3(x, 0.19f, -30.4f), new Vector3(0.9f, 0.04f, 3.2f), palette.WhitePaint, parent);
            }

            CreateCylinder("OldMarketBollard_W", new Vector3(-12.6f, 0.65f, -5.8f), new Vector3(0.55f, 0.65f, 0.55f), palette.Turquoise, parent);
            CreateCylinder("OldMarketBollard_E", new Vector3(12.6f, 0.65f, -5.8f), new Vector3(0.55f, 0.65f, 0.55f), palette.Rust, parent);
        }

        private static void CreateAuthoredDistrictModules(Transform parent)
        {
            Transform modulesRoot = CreateRoot("02_DemoAuthored_DistrictModules", parent).transform;
            PlaceModule(TownMarketModulePath, "OldMarket_West_DemoAuthored", new Vector3(-68f, 0f, 12f), 0f, 0.82f, modulesRoot);
            PlaceModule(BaseCommandModulePath, "UtilityCompound_East_DemoAuthored", new Vector3(69f, 0f, 13f), 180f, 0.76f, modulesRoot);
            PlaceModule(SouthTownModulePath, "Residential_South_DemoAuthored", new Vector3(-5f, 0f, -68f), 0f, 0.58f, modulesRoot);
        }

        private static void CreateOldMarketStoryLayer(Transform parent, Palette palette)
        {
            Transform root = CreateRoot("03_OldMarket_StoryLayer", parent).transform;

            PlacePrefab("Assets/Game/Prefabs/Environment/City/Clock_Tower_01.prefab", "OldMarketClockTower", new Vector3(-42f, 0f, 42f), 18f, 1.1f, root);
            PlacePrefab("Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_ArchwayBridge_01.prefab", "OldMarketArchway", new Vector3(-19f, 0f, 24f), 90f, 1.05f, root);

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

            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Barrier_Base_Group_03.prefab", "CompoundCheckpoint", new Vector3(23f, 0f, 28f), 90f, 1f, root);
            PlacePrefab("Assets/Game/Prefabs/Buildings/Building_GuardTower.prefab", "CompoundGuardTower", new Vector3(31f, 0f, 46f), 180f, 0.9f, root);
            PlacePrefab("Assets/Game/Prefabs/Buildings/Building_WaterTank.prefab", "CompoundWaterTank", new Vector3(55f, 0f, 51f), 20f, 0.9f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Crate_Stack_Cover_02.prefab", "CompoundSupplies_A", new Vector3(31f, 0f, 16f), 12f, 1f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Crate_Stack_03.prefab", "CompoundSupplies_B", new Vector3(38f, 0f, 18f), 84f, 1f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_LightPole_01.prefab", "CompoundLightPole", new Vector3(24f, 0f, 39f), 0f, 1f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/Signs/SM_Prop_Sign_Safety_03.prefab", "CompoundWarningSign", new Vector3(20.5f, 2.2f, 31f), 90f, 1.15f, root);

            CreateBox("CompoundSecurityStripe_A", new Vector3(15f, 0.34f, 32f), new Vector3(0.4f, 0.08f, 12f), palette.AmberPaint, root);
            CreateBox("CompoundSecurityStripe_B", new Vector3(18f, 0.34f, 32f), new Vector3(0.4f, 0.08f, 12f), palette.Rust, root);
            CreatePointLight("CompoundSecurityLight", new Vector3(28f, 9f, 31f), new Color(0.62f, 0.78f, 1f), 2.2f, 28f, root);
        }

        private static void CreateBombingAftermath(Transform parent)
        {
            Transform root = CreateRoot("05_BombingAftermath_StoryLayer", parent).transform;

            PlacePrefab("Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Crater_02.prefab", "ImpactCrater", new Vector3(19f, 0.2f, -47f), 12f, 1.65f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Vehicles/Destroyed/SM_Veh_Truck_01_Destroyed.prefab", "DestroyedAidTruck", new Vector3(21f, 0f, -43f), 24f, 1.05f, root);
            PlacePrefab("Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Ruins_03.prefab", "BombedCornerRuin", new Vector3(45f, 0f, -49f), 182f, 1.15f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/FX/FX_Fire_01.prefab", "AftermathFire", new Vector3(18f, 0.7f, -43f), 0f, 0.9f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/FX/FX_Smoke_Large_01.prefab", "AftermathSmoke", new Vector3(13f, 0f, -38f), 0f, 0.68f, root);

            string[] debris =
            {
                "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_01.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Rubble_Pile_03.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_DebrisPile_02.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Vehicle_Debris_04.prefab",
                "Assets/PolygonMilitary/Prefabs/Props/Debris/SM_Prop_Block_02.prefab"
            };
            var random = new System.Random(GenerationSeed + 77);
            for (int i = 0; i < 22; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = 5f + (float)random.NextDouble() * 18f;
                Vector3 position = new(20f + Mathf.Cos(angle) * radius, 0f, -45f + Mathf.Sin(angle) * radius * 0.65f);
                PlacePrefab(debris[i % debris.Length], $"AftermathDebris_{i + 1:00}", position, random.Next(0, 360), 0.8f + (float)random.NextDouble() * 0.7f, root);
            }

            for (int i = 0; i < 5; i++)
            {
                PlacePrefab("Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Barrier_Tall_02.prefab", $"EmergencyBarrier_{i + 1:00}", new Vector3(-1f + i * 4f, 0f, -39f + (i % 2) * 1.2f), 8f, 1f, root);
            }

            CreatePointLight("AftermathFireLight", new Vector3(19f, 4f, -43f), new Color(1f, 0.21f, 0.045f), 5.5f, 31f, root);
        }

        private static void CreateHorizonAndEdgeDressing(Transform parent)
        {
            Transform root = CreateRoot("07_Horizon_And_EdgeDressing", parent).transform;
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_03.prefab", "HorizonDunes_West", new Vector3(-177f, -1f, 128f), 18f, 1.8f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_01.prefab", "HorizonDunes_Center", new Vector3(0f, -1f, 142f), 0f, 2.1f, root);
            PlacePrefab("Assets/PolygonMilitary/Prefabs/Environment/SM_Env_SandDunes_02.prefab", "HorizonDunes_East", new Vector3(178f, -1f, 126f), -24f, 1.8f, root);

            string[] edgeProps =
            {
                "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Cactus_01.prefab",
                "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Cactus_02.prefab",
                "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Bush_Large_02.prefab",
                "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_03.prefab",
                "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Pebbles_02.prefab"
            };
            var random = new System.Random(GenerationSeed + 190);
            for (int i = 0; i < 58; i++)
            {
                bool horizontalEdge = i % 2 == 0;
                float x = horizontalEdge ? -142f + (float)random.NextDouble() * 284f : (random.Next(0, 2) == 0 ? -143f : 143f);
                float z = horizontalEdge ? (random.Next(0, 2) == 0 ? -101f : 101f) : -96f + (float)random.NextDouble() * 192f;
                PlacePrefab(edgeProps[i % edgeProps.Length], $"EdgeAccent_{i + 1:00}", new Vector3(x, 0f, z), random.Next(0, 360), 0.75f + (float)random.NextDouble() * 0.9f, root);
            }
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
