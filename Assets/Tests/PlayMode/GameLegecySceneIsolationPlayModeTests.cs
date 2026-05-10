#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Threading.Tasks;
using Game.Scripts.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameLegecySceneIsolationPlayModeTests
{
    private const string LegacyScenePath = "Assets/Game/Scenes/Game_Legecy.unity";
    private const string LegacySceneName = "Game_Legecy";

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
    public async Task GameLegecyScene_PlayUsesLegacyCanvasWithoutProductionRoute()
    {
        WarlineCaptureMissionSession.Clear();
        Scene legacyScene = EditorSceneManager.LoadSceneInPlayMode(LegacyScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        await NextFrame();
        await NextFrame();

        legacyScene = SceneManager.GetSceneByName(LegacySceneName);
        Assert.IsTrue(legacyScene.IsValid(), "Game_Legecy scene should be loaded by the editor PlayMode path.");
        Assert.IsTrue(legacyScene.isLoaded, "Game_Legecy scene should remain loaded.");
        Assert.IsNull(FindSceneComponent<WarlineCaptureUiBootstrap>(legacyScene), "Game_Legecy must not contain the production public UI bootstrap.");
        Assert.IsNull(FindSceneComponent<WarlineCaptureRouter>(legacyScene), "Game_Legecy must not load the public 2D/isometric app router.");
        Assert.IsNull(FindSceneComponent<Chapter01MissionTacticalRuntimeBinder>(legacyScene), "Game_Legecy must not contain the M01 production tactical binder.");

        GameObject legacyCanvas = FindRoot(legacyScene, "UI_Canvas");
        Assert.NotNull(legacyCanvas, "Game_Legecy must keep the legacy UI_Canvas root.");
        Assert.IsTrue(legacyCanvas.activeInHierarchy, "Legacy UI_Canvas should be active when the scene is played.");

        MenuView menu = FindSceneComponent<MenuView>(legacyScene);
        Assert.NotNull(menu, "Legacy UI_Canvas must provide the MenuView used by the old prototype.");
        Assert.NotNull(menu.buttonGame, "Legacy MenuView must keep the scene-owned Game button.");

        GameBootstrap bootstrap = FindSceneComponent<GameBootstrap>(legacyScene);
        Assert.NotNull(bootstrap, "Game_Legecy must keep the legacy GameBootstrap.");
        Assert.IsNull(bootstrap.Chapter01TacticalBinder, "Legacy GameBootstrap must not reference the M01 production binder.");
        Assert.NotNull(bootstrap.WorldCamera);
        Assert.AreEqual("Main Camera_Experiment", bootstrap.WorldCamera.name);
        Assert.NotNull(bootstrap.GlobalVolume);
        Assert.AreEqual("Global Volume_Experiment", bootstrap.GlobalVolume.name);
        Assert.NotNull(bootstrap.DirectionalLight);
        Assert.AreEqual("Directional Light (1)", bootstrap.DirectionalLight.name);
        Assert.NotNull(bootstrap.DecorationRoot);
        Assert.AreEqual("Decorations", bootstrap.DecorationRoot.name);

        menu.buttonGame.onClick.Invoke();
        for (int frame = 0; frame < 20; frame++)
            await NextFrame();

        Assert.IsTrue(InitialUnitsRuntimeState.PlayRequested, "Legacy Game button should start the old prototype gameplay path.");
        Assert.IsFalse(WarlineCaptureMissionSession.HasActiveMission, "Legacy scene Play must not create a production M01 mission session.");
        Assert.IsFalse(Chapter01M01PlayableRuntime.IsActiveMission(), "Legacy scene Play must not enter the new 2D/isometric M01 runtime.");
        Assert.IsNull(FindSceneComponent<WarlineCaptureUiBootstrap>(legacyScene), "Legacy play must not instantiate the production UI bootstrap.");
        Assert.IsNull(FindSceneComponent<WarlineCaptureRouter>(legacyScene), "Legacy play must not instantiate the public app router.");
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
