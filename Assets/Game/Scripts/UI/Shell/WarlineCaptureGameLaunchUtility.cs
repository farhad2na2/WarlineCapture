using Game.Scripts.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureGameLaunchUtility
{
    public static void StartExistingGameplayAndHideRouter(Component source)
    {
        Scene preferredScene = source != null ? source.gameObject.scene : default;
        if (ShouldUseWarlineCaptureProductionRoute())
        {
            StartM01ProductionRoute(source, preferredScene);
            return;
        }

        GameObject legacyCanvas = FindLoadedSceneObject("UI_Canvas", preferredScene);
        if (legacyCanvas != null)
            legacyCanvas.SetActive(true);

        AISettingsRuntimeState.ApplyToWorld(World.DefaultGameObjectInjectionWorld);

        MenuView menuView = FindLoadedSceneComponent<MenuView>();
        if (menuView != null)
            menuView.RequestGameStart();
        else
            FindLoadedSceneComponent<GameBootstrap>()?.BeginGameplay();

        WarlineCaptureRouter router = source != null ? source.GetComponentInParent<WarlineCaptureRouter>() : null;
        if (router != null)
            router.gameObject.SetActive(false);
    }

    public static bool ShouldUseWarlineCaptureProductionRoute()
    {
        return WarlineCaptureMissionSession.HasActiveMission &&
            WarlineCaptureMissionSession.ActiveMissionId == ChapterOneMissionCatalog.FirstContactMissionId;
    }

    private static void StartM01ProductionRoute(Component source, Scene preferredScene)
    {
        GameObject legacyCanvas = FindLoadedSceneObject("UI_Canvas", preferredScene);
        if (legacyCanvas != null)
            legacyCanvas.SetActive(false);

        AISettingsRuntimeState.ApplyToWorld(World.DefaultGameObjectInjectionWorld);

        GameBootstrap bootstrap = FindLoadedSceneComponent<GameBootstrap>();
        if (bootstrap != null)
            bootstrap.BeginGameplay();

        WarlineCaptureRouter router = source != null ? source.GetComponentInParent<WarlineCaptureRouter>(true) : null;
        if (router != null)
        {
            router.gameObject.SetActive(true);
            router.Initialize();
            router.GoTo(WarlineCaptureRoute.Match, false);
        }

        if (legacyCanvas != null)
            legacyCanvas.SetActive(false);
    }

    private static T FindLoadedSceneComponent<T>() where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded)
                return component;
        }

        return null;
    }

    private static GameObject FindLoadedSceneObject(string objectName, Scene preferredScene)
    {
        GameObject fallback = null;
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded || gameObject.name != objectName)
                continue;

            if (preferredScene.IsValid() && scene == preferredScene)
                return gameObject;

            fallback ??= gameObject;
        }

        return fallback;
    }
}
