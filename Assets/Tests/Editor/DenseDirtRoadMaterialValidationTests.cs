#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseDirtRoadMaterialValidationTests
{
    private const string DemoDirtRoadMaterialPath =
        "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_A.mat";
    private const string DemoVolumeProfilePath =
        "Assets/PolygonMilitary/Scenes/Demo/Military_Demo.asset";
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string VisualQualityProfilePath =
        "Assets/Game/Rendering/VisualQualityConfig.asset";
    private const string MobileCandidateVisualQualityProfilePath =
        "Assets/Game/Rendering/VisualQualityConfig_MobileCandidate.asset";
    private const string RenderDatabaseConfigPath =
        "Assets/Game/GeneratedOperationMapEntityPresentationCandidate/VirtualizedPresentation/" +
        "OperationMapRenderDatabaseBakeConfig.asset";
    private const string RenderPrototypeReportPath =
        "Design/AgentReports/2026-07-28_dense_city_render_virtualization_prototype_recipes.json";
    private const string DemoDirtRoadAlbedoPath =
        "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png";
    private const string DemoDirtRoadNormalPath =
        "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A_Normals.png";

    private static readonly string[] RoadPrefabPaths =
    {
        "Assets/Game/Prefabs/Roads/Road_Dirt/Road_Dirt_Corner.prefab",
        "Assets/Game/Prefabs/Roads/Road_Dirt/Road_Dirt_End.prefab",
        "Assets/Game/Prefabs/Roads/Road_Dirt/Road_Dirt_Intersection.prefab",
        "Assets/Game/Prefabs/Roads/Road_Dirt/Road_Dirt_Straight.prefab",
        "Assets/Game/Prefabs/Roads/Road_Dirt/Road_Dirt_T_Intersection.prefab",
        "Assets/Game/Prefabs/Roads/Road_Dirt_With_Sidewalk/Road_Dirt_With_Sidewalk_Corner.prefab",
        "Assets/Game/Prefabs/Roads/Road_Dirt_With_Sidewalk/Road_Dirt_With_Sidewalk_End.prefab",
        "Assets/Game/Prefabs/Roads/Road_Dirt_With_Sidewalk/Road_Dirt_With_Sidewalk_Intersection.prefab",
        "Assets/Game/Prefabs/Roads/Road_Dirt_With_Sidewalk/Road_Dirt_With_Sidewalk_Straight.prefab",
        "Assets/Game/Prefabs/Roads/Road_Dirt_With_Sidewalk/Road_Dirt_With_Sidewalk_T_Intersection.prefab",
        "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/Road_Dirt_Corner_a0db9e22.prefab",
        "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/Road_Dirt_End_16612f70.prefab",
        "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/Road_Dirt_Intersection_5c9b5183.prefab",
        "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/Road_Dirt_Straight_ad3b7211.prefab",
        "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/Road_Dirt_T_Intersection_85399051.prefab",
        "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/Road_Dirt_With_Sidewalk_Corner_86153a2c.prefab",
        "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/Road_Dirt_With_Sidewalk_End_a69f7d1e.prefab",
        "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/Road_Dirt_With_Sidewalk_Intersection_3a64ece1.prefab",
        "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/Road_Dirt_With_Sidewalk_Straight_02bcd840.prefab",
        "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/Road_Dirt_With_Sidewalk_T_Intersection_7688b8f8.prefab"
    };

    public static void RunFocusedValidation()
    {
        try
        {
            DenseDirtRoadsUseExactDemoSurface();
            MatchUsesExactDemoVolumeProfile();
            Debug.Log(
                "[DenseDirtRoadMaterialValidation] result=Passed " +
                "dirtRenderers=36 packedParts=18 demoMaterial=Exact demoVolume=Exact");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[DenseDirtRoadMaterialValidation] result=Failed");
            throw;
        }
    }

    [Test]
    public static void DenseDirtRoadsUseExactDemoSurface()
    {
        Material dirt = AssetDatabase.LoadAssetAtPath<Material>(DemoDirtRoadMaterialPath);
        Assert.NotNull(dirt, $"Missing Demo dirt-road material at {DemoDirtRoadMaterialPath}.");
        Assert.AreEqual(
            DemoDirtRoadAlbedoPath,
            AssetDatabase.GetAssetPath(dirt.GetTexture("_BaseMap")),
            "Dense dirt roads must use the exact Demo dirt-road atlas and authored UV detail.");
        Assert.AreEqual(
            DemoDirtRoadNormalPath,
            AssetDatabase.GetAssetPath(dirt.GetTexture("_BumpMap")),
            "Dense dirt roads must preserve the Demo dirt-road normal map.");
        Assert.True(dirt.IsKeywordEnabled("_NORMALMAP"), "Dense dirt roads must enable their normal map.");
        Assert.That(dirt.GetFloat("_EnvironmentReflections"), Is.EqualTo(1f).Within(0.001f));
        Assert.False(dirt.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF"));
        Assert.True(dirt.enableInstancing, "Dense dirt roads must remain GPU-instancing compatible.");

        Color tint = dirt.GetColor("_BaseColor");
        Assert.That(tint.r, Is.EqualTo(1f).Within(0.001f));
        Assert.That(tint.g, Is.EqualTo(1f).Within(0.001f));
        Assert.That(tint.b, Is.EqualTo(1f).Within(0.001f));

        int dirtRendererCount = 0;
        int sidewalkRendererCount = 0;
        foreach (string path in RoadPrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.NotNull(prefab, $"Missing dense dirt-road prefab at {path}.");
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0), $"Dense dirt-road prefab has no renderers: {path}.");
            foreach (Renderer renderer in renderers)
            {
                bool isDirt = string.Equals(renderer.name, "Dirt", StringComparison.Ordinal);
                if (isDirt)
                    dirtRendererCount++;
                else
                    sidewalkRendererCount++;
                foreach (Material material in renderer.sharedMaterials)
                {
                    Assert.AreEqual(
                        DemoDirtRoadMaterialPath,
                        AssetDatabase.GetAssetPath(material),
                        $"Dense dirt-road renderer '{renderer.name}' must use the exact Demo material: {path}.");
                }
            }
        }
        Assert.AreEqual(36, dirtRendererCount, "Every source/generated Dirt renderer must be covered.");
        Assert.That(sidewalkRendererCount, Is.GreaterThan(0), "Sidewalk preservation was not exercised.");

        OperationMapRenderDatabaseBakeConfig database =
            AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(RenderDatabaseConfigPath);
        Assert.NotNull(database, $"Missing packed render database at {RenderDatabaseConfigPath}.");
        Assert.True(database.TryValidateSchema(out string schemaError), schemaError);
        int dirtMaterialIndex = -1;
        for (int index = 0; index < database.Materials.Count; index++)
        {
            if (database.Materials[index].Material == dirt)
            {
                dirtMaterialIndex = index;
                break;
            }
        }
        Assert.That(dirtMaterialIndex, Is.GreaterThanOrEqualTo(0));

        RenderPrototypeReport prototypeReport = JsonUtility.FromJson<RenderPrototypeReport>(
            File.ReadAllText(RenderPrototypeReportPath));
        Assert.NotNull(prototypeReport, $"Could not parse {RenderPrototypeReportPath}.");
        Assert.NotNull(prototypeReport.parts, "Render prototype report has no packed parts.");
        int packedDirtPartCount = 0;
        foreach (RenderPrototypePart part in prototypeReport.parts)
        {
            if (!part.rendererPath.StartsWith("Dirt[", StringComparison.Ordinal))
                continue;
            packedDirtPartCount++;
            Assert.That(part.partIndex, Is.InRange(0, database.Parts.Count - 1));
            Assert.AreEqual(
                dirtMaterialIndex,
                database.Parts[part.partIndex].MaterialIndex,
                $"Packed dirt-road part {part.partIndex} must use the exact Demo material.");
        }
        Assert.AreEqual(18, packedDirtPartCount, "Every packed Dirt renderer must be covered.");
    }

    [Test]
    public static void MatchUsesExactDemoVolumeProfile()
    {
        UnityEngine.Object demoProfile =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DemoVolumeProfilePath);
        Assert.NotNull(demoProfile, $"Missing Demo volume profile at {DemoVolumeProfilePath}.");

        AssertQualityProfileUsesDemoVolume(VisualQualityProfilePath, demoProfile);
        AssertQualityProfileUsesDemoVolume(MobileCandidateVisualQualityProfilePath, demoProfile);

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
            GameObject globalOwner = GameObject.Find("Global Volume");
            Assert.NotNull(globalOwner, "Match scene has no global volume owner.");
            Component global = globalOwner.GetComponent("Volume");
            Assert.NotNull(global, "Match global-volume owner has no Volume component.");
            SerializedProperty sharedProfile =
                new SerializedObject(global).FindProperty("sharedProfile");
            Assert.NotNull(sharedProfile, "Match global volume has no shared-profile field.");
            Assert.AreSame(
                demoProfile,
                sharedProfile.objectReferenceValue,
                "Match must use the exact Demo global volume profile before runtime quality selection.");
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static void AssertQualityProfileUsesDemoVolume(
        string profilePath,
        UnityEngine.Object demoProfile)
    {
        VisualQualityProfileAsset profile =
            AssetDatabase.LoadAssetAtPath<VisualQualityProfileAsset>(profilePath);
        Assert.NotNull(profile, $"Missing visual-quality profile at {profilePath}.");
        SerializedObject serializedProfile = new(profile);
        SerializedProperty highVolume = serializedProfile.FindProperty("highVolumeProfile");
        SerializedProperty ultraVolume = serializedProfile.FindProperty("globalVolumeProfile");
        Assert.NotNull(highVolume, $"High volume field is missing: {profilePath}.");
        Assert.NotNull(ultraVolume, $"Ultra volume field is missing: {profilePath}.");
        Assert.AreSame(
            demoProfile,
            highVolume.objectReferenceValue,
            $"High quality must use the exact Demo volume profile: {profilePath}.");
        Assert.AreSame(
            demoProfile,
            ultraVolume.objectReferenceValue,
            $"Ultra quality must use the exact Demo volume profile: {profilePath}.");
        Assert.That(
            profile.HighSunShadowStrength,
            Is.EqualTo(1f).Within(0.001f),
            $"High quality must preserve the Demo light's full shadow strength: {profilePath}.");
        Assert.That(
            profile.PremiumSunShadowStrength,
            Is.EqualTo(1f).Within(0.001f),
            $"Ultra quality must preserve the Demo light's full shadow strength: {profilePath}.");
    }

    [Serializable]
    private sealed class RenderPrototypeReport
    {
        public RenderPrototypePart[] parts;
    }

    [Serializable]
    private sealed class RenderPrototypePart
    {
        public int partIndex;
        public string rendererPath;
    }
}
#endif
