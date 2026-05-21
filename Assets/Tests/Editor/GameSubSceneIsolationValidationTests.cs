#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameSubSceneIsolationValidationTests
{
    private const string DefaultScenePath = "Assets/Game/Scenes/Game.unity";
    private const string Old2DScenePath = "Assets/Game/Scenes/Game2D.unity";
    private const string DefaultSubScenePath = "Assets/Game/Scenes/Game/GameSubScene.unity";
    private const string Old2DSubScenePath = "Assets/Game/Scenes/Game2D/GameSubScene.unity";

    [Test]
    public void DefaultGameAndOld2DScenesUseDistinctSubSceneAssets()
    {
        Scene defaultScene = EditorSceneManager.OpenScene(DefaultScenePath, OpenSceneMode.Single);
        SubScene defaultSubScene = FindRootSubScene(defaultScene);
        Assert.NotNull(defaultSubScene, "Game.unity must keep its promoted 3D GameSubScene root.");
        string defaultSubScenePath = AssetDatabase.GetAssetPath(defaultSubScene.SceneAsset);
        Assert.AreEqual(DefaultSubScenePath, defaultSubScenePath);

        Scene old2DScene = EditorSceneManager.OpenScene(Old2DScenePath, OpenSceneMode.Single);
        SubScene old2DSubScene = FindRootSubScene(old2DScene);
        Assert.NotNull(old2DSubScene, "Game2D.unity must keep the old 2D GameSubScene root.");
        string old2DSubScenePath = AssetDatabase.GetAssetPath(old2DSubScene.SceneAsset);
        Assert.AreEqual(Old2DSubScenePath, old2DSubScenePath);

        Assert.AreNotEqual(
            defaultSubScenePath,
            old2DSubScenePath,
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
