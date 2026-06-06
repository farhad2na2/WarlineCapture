using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureGameLaunchUtility
{
    private static readonly SceneLifecycleSystem SceneLifecycleSystem = new();
    private static readonly MatchStartSystem MatchStartSystem = new();

    public static void StartExistingGameplayAndHideRouter(Component source)
    {
        Scene preferredScene = source != null ? source.gameObject.scene : default;
        if (ShouldUseWarlineCaptureProductionRoute())
        {
            StartM01ProductionRoute(source, preferredScene);
            return;
        }

        AISettingsRuntimeState.ApplyToWorld(World.DefaultGameObjectInjectionWorld);
        QueueMatchLoadAndStart();

        WarlineCaptureRouter router = source != null ? source.GetComponentInParent<WarlineCaptureRouter>() : null;
        if (router != null)
            router.gameObject.SetActive(false);
    }

    public static bool ShouldUseWarlineCaptureProductionRoute()
    {
        return new ActiveMissionSession().HasActiveMission &&
            new ActiveMissionSession().ActiveMissionId == ChapterOneMissionCatalog.FirstContactMissionId;
    }

    private static void StartM01ProductionRoute(Component source, Scene preferredScene)
    {
        GameObject legacyCanvas = FindLoadedSceneObject("UI_Canvas", preferredScene);
        if (legacyCanvas != null)
            legacyCanvas.SetActive(false);

        AISettingsRuntimeState.ApplyToWorld(World.DefaultGameObjectInjectionWorld);
        QueueMatchLoadAndStart();

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

    private static void QueueMatchLoadAndStart()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogError("[GameLaunch] Cannot queue Match start because the default ECS world is missing.");
            return;
        }

        EntityManager entityManager = world.EntityManager;
        bool loadQueued = SceneLifecycleSystem.QueueLoadMatch(entityManager);
        bool startQueued = MatchStartSystem.QueueStartAfterMatchLoaded(entityManager);
        if (!loadQueued || !startQueued)
            Debug.LogError($"[GameLaunch] Failed to queue Match start. loadQueued={(loadQueued ? 1 : 0)} startQueued={(startQueued ? 1 : 0)}");
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
