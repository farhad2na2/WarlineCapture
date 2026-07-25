using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class DenseCityDeterministicFixtureTests
{
    private const string CanonicalMapScenePath =
        "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
    private const string SourceConfigPath =
        "Assets/Game/Configs/MapPrototypes/M01_RuntimeCity_Config.asset";
    private const string RoadMaterialPath =
        "Assets/Game/Art/MapPrototypes/M01/Materials/M01_DirtRoad.mat";
    private const string RoadShoulderMaterialPath =
        "Assets/Game/Art/MapPrototypes/M01/Materials/M01_TransitionGround.mat";
    private const string GroundMaterialPath =
        "Assets/Game/Art/MapPrototypes/M01/Materials/M01_DistrictGround.mat";

    public static void RunFocusedValidation()
    {
        try
        {
            new DenseCityDeterministicFixtureTests()
                .RunSmallFixture(forceReplaceUntitledScene: true);
            Debug.Log("[DenseCityDeterministicFixtureValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[DenseCityDeterministicFixtureValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void SmallFixture_BuildsDeterministicallyWithoutMutatingCanonicalMap() =>
        RunSmallFixture(forceReplaceUntitledScene: false);

    private void RunSmallFixture(bool forceReplaceUntitledScene)
    {
        string canonicalHashBefore = ComputeFileSha256(CanonicalMapScenePath);
        Scene previousActiveScene = SceneManager.GetActiveScene();
        bool hasOccupiedUntitledScene =
            previousActiveScene.IsValid() &&
            previousActiveScene.isLoaded &&
            string.IsNullOrEmpty(previousActiveScene.path) &&
            previousActiveScene.rootCount != 0;
        if (hasOccupiedUntitledScene && !Application.isBatchMode && !forceReplaceUntitledScene)
        {
            Assert.Ignore(
                "Cannot create an additive fixture while an unsaved untitled scene contains objects.");
        }

        bool replaceBatchUntitledScene =
            hasOccupiedUntitledScene && (Application.isBatchMode || forceReplaceUntitledScene);
        bool closeFixtureScene =
            !replaceBatchUntitledScene && !CanUseEmptyUntitledScene(previousActiveScene);
        Scene fixtureScene = replaceBatchUntitledScene
            ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)
            : closeFixtureScene
                ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive)
                : previousActiveScene;
        RuntimeCitySpawnerSystemConfig fixtureConfig = null;
        GameObject host = null;

        try
        {
            if (SceneManager.GetActiveScene().handle != fixtureScene.handle)
                Assert.That(EditorSceneManager.SetActiveScene(fixtureScene), Is.True);
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(fixtureScene.handle));
            host = new GameObject("DenseCityDeterministicFixture");
            RuntimeCityRAndDMapView view = host.AddComponent<RuntimeCityRAndDMapView>();
            fixtureConfig = CreateSmallFixtureConfig();
            ConfigureFixtureView(view, fixtureConfig);

            RuntimeCityRAndDEditModeBuilder.Build(view);
            Assert.That(view.GeneratedRoot, Is.Not.Null);
            Assert.That(view.GeneratedRoot.childCount, Is.GreaterThan(0));
            string firstHash = ComputeHierarchyHash(view.GeneratedRoot);

            RuntimeCityRAndDEditModeBuilder.Clear(view);
            Assert.That(view.GeneratedRoot.childCount, Is.Zero);

            RuntimeCityRAndDEditModeBuilder.Build(view);
            string secondHash = ComputeHierarchyHash(view.GeneratedRoot);

            Assert.That(secondHash, Is.EqualTo(firstHash));
            Assert.That(
                ComputeFileSha256(CanonicalMapScenePath),
                Is.EqualTo(canonicalHashBefore));
            Assert.That(
                AssetDatabase.GetAssetPath(fixtureConfig),
                Is.Empty,
                "The fixture config must remain an in-memory clone.");
        }
        finally
        {
            if (fixtureConfig != null)
                Object.DestroyImmediate(fixtureConfig);
            if (replaceBatchUntitledScene)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else if (closeFixtureScene && fixtureScene.IsValid() && fixtureScene.isLoaded)
                EditorSceneManager.CloseScene(fixtureScene, true);
            else if (host != null)
                Object.DestroyImmediate(host);
            if (closeFixtureScene && previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                EditorSceneManager.SetActiveScene(previousActiveScene);
        }
    }

    private static bool CanUseEmptyUntitledScene(Scene scene) =>
        scene.IsValid() &&
        scene.isLoaded &&
        string.IsNullOrEmpty(scene.path) &&
        scene.rootCount == 0;

    private static RuntimeCitySpawnerSystemConfig CreateSmallFixtureConfig()
    {
        RuntimeCitySpawnerSystemConfig source =
            AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(SourceConfigPath);
        Assert.That(source, Is.Not.Null, $"Missing fixture source config: {SourceConfigPath}");

        RuntimeCitySpawnerSystemConfig config = Object.Instantiate(source);
        config.name = "DenseCityDeterministicFixtureConfig";
        config.hideFlags = HideFlags.HideAndDontSave;

        var serialized = new SerializedObject(config);
        SetInteger(serialized, "cityCount", 1);
        SetLong(serialized, "randomSeed", 26072101);
        SetVector2Int(serialized, "startCell", new Vector2Int(96, 96));
        SetInteger(serialized, "generationYieldInterval", 0);
        SetInteger(serialized, "gasStationCount", 1);
        SetInteger(serialized, "shopCount", 2);
        SetInteger(serialized, "houseCount", 4);
        SetInteger(serialized, "otherBuildingCount", 2);
        SetInteger(serialized, "cityDecorationBuildingCount", 1);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return config;
    }

    private static void ConfigureFixtureView(
        RuntimeCityRAndDMapView view,
        RuntimeCitySpawnerSystemConfig config)
    {
        Material road = RequireAsset<Material>(RoadMaterialPath);
        Material shoulder = RequireAsset<Material>(RoadShoulderMaterialPath);
        Material ground = RequireAsset<Material>(GroundMaterialPath);

        var serialized = new SerializedObject(view);
        SetObject(serialized, "config", config);
        SetObject(serialized, "visualRecipe", null);
        SetObject(serialized, "deterministicFallbackRecipe", null);
        SetBoolean(serialized, "deterministicFallbackEnabled", false);
        SetBoolean(serialized, "runtimeGenerationEnabled", false);
        SetBoolean(serialized, "generateOnStart", false);
        SetBoolean(serialized, "showDebugOverlay", false);
        SetBoolean(serialized, "createAlgorithmicFoundation", false);
        SetBoolean(serialized, "cloneGeneratedMaterials", false);
        SetInteger(serialized, "gridWidth", 256);
        SetInteger(serialized, "gridHeight", 256);
        SetFloat(serialized, "gridCellSize", 1f);
        SetVector3(serialized, "gridOrigin", new Vector3(-128f, 0f, -128f));
        SetInteger(serialized, "roadCellSizeInGridCells", 10);
        SetObject(serialized, "roadMaterial", road);
        SetObject(serialized, "roadShoulderMaterial", shoulder);
        SetObject(serialized, "algorithmicGroundMaterial", ground);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string ComputeHierarchyHash(Transform root)
    {
        var builder = new StringBuilder(16384);
        AppendTransform(builder, root, string.Empty);
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return ToLowerHex(hash);
    }

    private static void AppendTransform(StringBuilder builder, Transform transform, string parentPath)
    {
        string path = string.IsNullOrEmpty(parentPath)
            ? transform.name
            : $"{parentPath}/{transform.name}[{transform.GetSiblingIndex()}]";
        Vector3 position = transform.localPosition;
        Quaternion rotation = transform.localRotation;
        Vector3 scale = transform.localScale;
        builder
            .Append(path).Append('|')
            .Append(transform.gameObject.activeSelf ? '1' : '0').Append('|')
            .Append(BitConverter.SingleToInt32Bits(position.x)).Append(',')
            .Append(BitConverter.SingleToInt32Bits(position.y)).Append(',')
            .Append(BitConverter.SingleToInt32Bits(position.z)).Append('|')
            .Append(BitConverter.SingleToInt32Bits(rotation.x)).Append(',')
            .Append(BitConverter.SingleToInt32Bits(rotation.y)).Append(',')
            .Append(BitConverter.SingleToInt32Bits(rotation.z)).Append(',')
            .Append(BitConverter.SingleToInt32Bits(rotation.w)).Append('|')
            .Append(BitConverter.SingleToInt32Bits(scale.x)).Append(',')
            .Append(BitConverter.SingleToInt32Bits(scale.y)).Append(',')
            .Append(BitConverter.SingleToInt32Bits(scale.z));

        Component[] components = transform.GetComponents<Component>();
        for (int index = 0; index < components.Length; index++)
            builder.Append('|').Append(components[index]?.GetType().FullName ?? "<missing>");
        builder.Append('\n');

        for (int index = 0; index < transform.childCount; index++)
            AppendTransform(builder, transform.GetChild(index), path);
    }

    private static string ComputeFileSha256(string projectRelativePath)
    {
        using SHA256 sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(Path.GetFullPath(projectRelativePath));
        return ToLowerHex(sha256.ComputeHash(stream));
    }

    private static string ToLowerHex(byte[] bytes) =>
        BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();

    private static T RequireAsset<T>(string path)
        where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.That(asset, Is.Not.Null, $"Missing fixture asset: {path}");
        return asset;
    }

    private static SerializedProperty RequireProperty(SerializedObject serialized, string name)
    {
        SerializedProperty property = serialized.FindProperty(name);
        Assert.That(property, Is.Not.Null, $"Missing serialized property: {name}");
        return property;
    }

    private static void SetInteger(SerializedObject serialized, string name, int value) =>
        RequireProperty(serialized, name).intValue = value;

    private static void SetLong(SerializedObject serialized, string name, long value) =>
        RequireProperty(serialized, name).longValue = value;

    private static void SetBoolean(SerializedObject serialized, string name, bool value) =>
        RequireProperty(serialized, name).boolValue = value;

    private static void SetFloat(SerializedObject serialized, string name, float value) =>
        RequireProperty(serialized, name).floatValue = value;

    private static void SetVector2Int(
        SerializedObject serialized,
        string name,
        Vector2Int value) =>
        RequireProperty(serialized, name).vector2IntValue = value;

    private static void SetVector3(SerializedObject serialized, string name, Vector3 value) =>
        RequireProperty(serialized, name).vector3Value = value;

    private static void SetObject(
        SerializedObject serialized,
        string name,
        Object value) =>
        RequireProperty(serialized, name).objectReferenceValue = value;
}
