using Game.Configs;
using Game.Authoring;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class CustomGameStartupConfigContractTests
{
    private static readonly string[] ConfigPaths =
    {
        "Assets/Game/Scripts/Configs/CustomGameStartupConfig.cs",
        "Assets/Game/Scripts/Configs/CustomGameUnitRosterConfig.cs",
        "Assets/Game/Scripts/Configs/CustomGameFactionConfig.cs",
        "Assets/Game/Scripts/Configs/CustomGameMapConfig.cs",
        "Assets/Game/Scripts/Configs/CustomGameVisualRegistryConfig.cs"
    };

    private static readonly string[] ForbiddenRuntimeTokens =
    {
        "UNITY_EDITOR",
        "UNITY_ANDROID",
        "Application.isEditor",
        "RuntimePlatform",
        "BuildTarget",
        "AssetDatabase",
        "SceneManager",
        "SubScene",
        "InitialUnitsSpawnerAuthoring",
        "UnitPrefabRegistryAuthoring",
        "FindObjectOfType",
        "FindObjectsOfType",
        "GameObject.Find"
    };

    public static void RunBatchValidation()
    {
        var tests = new CustomGameStartupConfigContractTests();
        tests.CustomGameConfigContractsExist();
        tests.CustomGameConfigContractsAreScriptableObjects();
        tests.CustomGameConfigContractsDoNotUseSceneAuthoringOrPlatformBranches();
        tests.CustomGameConfigContractsAreDataOnlyUnityAssets();
    }

    [Test]
    public void CustomGameConfigContractsExist()
    {
        for (int i = 0; i < ConfigPaths.Length; i++)
            Assert.IsTrue(File.Exists(ConfigPaths[i]), $"Missing Custom Game config contract: {ConfigPaths[i]}");
    }

    [Test]
    public void CustomGameConfigContractsAreScriptableObjects()
    {
        Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(typeof(CustomGameStartupConfig)));
        Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(typeof(CustomGameUnitRosterConfig)));
        Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(typeof(CustomGameFactionConfig)));
        Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(typeof(CustomGameMapConfig)));
        Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(typeof(CustomGameVisualRegistryConfig)));
    }

    [Test]
    public void CustomGameConfigContractsDoNotUseSceneAuthoringOrPlatformBranches()
    {
        for (int fileIndex = 0; fileIndex < ConfigPaths.Length; fileIndex++)
        {
            string path = ConfigPaths[fileIndex];
            string source = File.ReadAllText(path);
            for (int tokenIndex = 0; tokenIndex < ForbiddenRuntimeTokens.Length; tokenIndex++)
            {
                string token = ForbiddenRuntimeTokens[tokenIndex];
                StringAssert.DoesNotContain(token, source, $"{path} must not depend on {token}.");
            }
        }
    }

    [Test]
    public void CustomGameConfigContractsAreDataOnlyUnityAssets()
    {
        for (int fileIndex = 0; fileIndex < ConfigPaths.Length; fileIndex++)
        {
            string path = ConfigPaths[fileIndex];
            string source = File.ReadAllText(path);
            StringAssert.DoesNotContain(": MonoBehaviour", source, $"{path} must not be a scene component.");
            StringAssert.DoesNotContain("Baker<", source, $"{path} must not bake ECS data.");
            StringAssert.DoesNotContain("void Update(", source, $"{path} must not execute gameplay loops.");
            StringAssert.DoesNotContain("void Awake(", source, $"{path} must not execute scene lifecycle logic.");
            StringAssert.DoesNotContain("void Start(", source, $"{path} must not execute scene lifecycle logic.");
        }
    }
}
#endif
