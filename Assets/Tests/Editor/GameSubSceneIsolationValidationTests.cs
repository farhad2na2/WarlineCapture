#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameSubSceneIsolationValidationTests
{
    private const string ProductionScenePath = "Assets/Game/Scenes/Game.unity";
    private const string LegacyScenePath = "Assets/Game/Scenes/Game_Legecy.unity";
    private const string ProductionSubScenePath = "Assets/Game/Scenes/Game/GameSubScene.unity";
    private const string LegacySubScenePath = "Assets/Game/Scenes/Game_Legecy/GameSubScene.unity";

    [Test]
    public void ProductionAndLegacyScenesUseDistinctSubSceneAssets()
    {
        Scene productionScene = EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);
        SubScene productionSubScene = FindRootSubScene(productionScene);
        Assert.NotNull(productionSubScene, "Game.unity must keep its GameSubScene root.");
        string productionSubScenePath = AssetDatabase.GetAssetPath(productionSubScene.SceneAsset);
        Assert.AreEqual(ProductionSubScenePath, productionSubScenePath);

        Scene legacyScene = EditorSceneManager.OpenScene(LegacyScenePath, OpenSceneMode.Single);
        SubScene legacySubScene = FindRootSubScene(legacyScene);
        Assert.NotNull(legacySubScene, "Game_Legecy.unity must keep its GameSubScene root.");
        string legacySubScenePath = AssetDatabase.GetAssetPath(legacySubScene.SceneAsset);
        Assert.AreEqual(LegacySubScenePath, legacySubScenePath);

        Assert.AreNotEqual(
            productionSubScenePath,
            legacySubScenePath,
            "Unity.Entities does not support multiple active SubScene components referencing the same SceneAsset.");
    }

    private static SubScene FindRootSubScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "GameSubScene")
                return root.GetComponent<SubScene>();
        }

        return null;
    }
}
#endif
