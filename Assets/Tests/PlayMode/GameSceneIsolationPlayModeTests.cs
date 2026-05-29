#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Threading.Tasks;
using Game.Scripts.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameSceneIsolationPlayModeTests
{
    private const string DefaultScenePath = "Assets/Game/Scenes/Match.unity";
    private const string DefaultSceneName = "Match";

    [TearDown]
    public void TearDown()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.BuildModeActive = false;
        InitialUnitsRuntimeState.FullscreenMapOpen = false;
        InitialUnitsRuntimeState.FullscreenMapIsoMode = false;
        InitialUnitsRuntimeState.ZoomInHeld = false;
        InitialUnitsRuntimeState.ZoomOutHeld = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = false;
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = false;
        WarlineCaptureMissionSession.Clear();
        GameRuntimeStats.Reset();
        Time.timeScale = 1f;
    }

    [Test]
    public async Task GameScene_PlayUsesPromotedDefaultCanvasWithoutOld2DRoute()
    {
        WarlineCaptureMissionSession.Clear();
        Scene defaultScene = EditorSceneManager.LoadSceneInPlayMode(DefaultScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        await NextFrame();
        await NextFrame();

        defaultScene = SceneManager.GetSceneByName(DefaultSceneName);
        Assert.IsTrue(defaultScene.IsValid(), "Match scene should be loaded by the editor PlayMode path.");
        Assert.IsTrue(defaultScene.isLoaded, "Match scene should remain loaded.");
        Assert.IsNull(FindSceneComponent<WarlineCaptureUiBootstrap>(defaultScene), "Promoted Match scene must not contain the old 2D public UI bootstrap.");
        Assert.IsNull(FindSceneComponent<WarlineCaptureRouter>(defaultScene), "Promoted Match scene must not load the old 2D/isometric app router.");

        GameObject defaultCanvas = FindRoot(defaultScene, "UI_Canvas");
        Assert.NotNull(defaultCanvas, "Promoted Match scene must keep the default UI_Canvas root.");
        Assert.IsTrue(defaultCanvas.activeInHierarchy, "Default UI_Canvas should be active when the scene is played.");

        MenuView menu = FindSceneComponent<MenuView>(defaultScene);
        Assert.NotNull(menu, "Default UI_Canvas must provide the MenuView used by the promoted prototype.");
        Assert.NotNull(menu.buttonGame, "Default MenuView must keep the scene-owned Game button.");

        GameBootstrap bootstrap = FindSceneComponent<GameBootstrap>(defaultScene);
        Assert.NotNull(bootstrap, "Promoted Match scene must keep the default GameBootstrap.");
        Assert.NotNull(bootstrap.WorldCamera);
        Assert.AreEqual("Main Camera", bootstrap.WorldCamera.name);
        Assert.NotNull(bootstrap.GlobalVolume);
        Assert.AreEqual("Global Volume", bootstrap.GlobalVolume.name);
        Assert.NotNull(bootstrap.DirectionalLight);
        Assert.AreEqual("Directional light", bootstrap.DirectionalLight.name);
        Assert.NotNull(bootstrap.DecorationRoot);
        Assert.AreEqual("Decorations", bootstrap.DecorationRoot.name);

        menu.buttonGame.onClick.Invoke();
        for (int frame = 0; frame < 20; frame++)
            await NextFrame();

        Assert.IsTrue(InitialUnitsRuntimeState.PlayRequested, "Default Game button should start the promoted prototype gameplay path.");
        Assert.IsFalse(WarlineCaptureMissionSession.HasActiveMission, "Default Match scene Play must not create a production M01 mission session.");
        Assert.IsFalse(Chapter01M01PlayableRuntime.IsActiveMission(), "Default Match scene Play must not enter the old 2D/isometric M01 runtime.");
        Assert.IsNull(FindSceneComponent<WarlineCaptureUiBootstrap>(defaultScene), "Default Game play must not instantiate the old 2D production UI bootstrap.");
        Assert.IsNull(FindSceneComponent<WarlineCaptureRouter>(defaultScene), "Default Game play must not instantiate the public app router.");
    }

    private static async Task NextFrame()
    {
        await Task.Yield();
    }

    private static GameObject FindRoot(Scene scene, string rootName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == rootName)
                return root;
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }
}
#endif
