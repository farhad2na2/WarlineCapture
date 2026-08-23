#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DenseDirtRoadMaterialValidationTests
{
    private const string DenseDirtRoadMaterialPath =
        "Assets/Game/Art/MapPrototypes/M01/Materials/M01_DirtRoad.mat";
    private const string DemoDirtRoadMaterialPath =
        "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_A.mat";
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
            DenseDirtRoadsUseWarmDemoSurface();
            Debug.Log("[DenseDirtRoadMaterialValidation] result=Passed dirtRenderers=36 packedParts=18");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[DenseDirtRoadMaterialValidation] result=Failed");
            throw;
        }
    }

    [Test]
    public static void DenseDirtRoadsUseWarmDemoSurface()
    {
        Material dirt = AssetDatabase.LoadAssetAtPath<Material>(DenseDirtRoadMaterialPath);
        Assert.NotNull(dirt, $"Missing dense dirt-road material at {DenseDirtRoadMaterialPath}.");
        Assert.AreEqual(
            DemoDirtRoadAlbedoPath,
            AssetDatabase.GetAssetPath(dirt.GetTexture("_BaseMap")),
            "Dense dirt roads must preserve the Demo dirt-road atlas and authored UV detail.");
        Assert.AreEqual(
            DemoDirtRoadNormalPath,
            AssetDatabase.GetAssetPath(dirt.GetTexture("_BumpMap")),
            "Dense dirt roads must preserve the Demo dirt-road normal map.");
        Assert.True(dirt.IsKeywordEnabled("_NORMALMAP"), "Dense dirt roads must enable their normal map.");
        Assert.That(dirt.GetFloat("_EnvironmentReflections"), Is.Zero.Within(0.001f));
        Assert.True(dirt.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF"));
        Assert.True(dirt.enableInstancing, "Dense dirt roads must remain GPU-instancing compatible.");

        Color tint = dirt.GetColor("_BaseColor");
        Assert.That(tint.r, Is.GreaterThanOrEqualTo(0.99f));
        Assert.That(tint.g, Is.GreaterThanOrEqualTo(0.85f));
        Assert.That(tint.b, Is.LessThanOrEqualTo(0.70f));
        Assert.Greater(tint.r, tint.b, "Dense dirt roads require an explicit warm sand response.");

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
                string expectedMaterialPath =
                    isDirt ? DenseDirtRoadMaterialPath : DemoDirtRoadMaterialPath;
                foreach (Material material in renderer.sharedMaterials)
                {
                    Assert.AreEqual(
                        expectedMaterialPath,
                        AssetDatabase.GetAssetPath(material),
                        $"Dense dirt-road renderer '{renderer.name}' has the wrong surface material: {path}.");
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
                $"Packed dirt-road part {part.partIndex} must use the dedicated warm material.");
        }
        Assert.AreEqual(18, packedDirtPartCount, "Every packed Dirt renderer must be covered.");
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
