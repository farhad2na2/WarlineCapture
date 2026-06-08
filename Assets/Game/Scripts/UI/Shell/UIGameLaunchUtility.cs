using Unity.Entities;
using UnityEngine;

public static class UIGameLaunchUtility
{
    private static readonly SceneLifecycleSystem SceneLifecycleSystem = new();
    private static readonly MatchStartSystem MatchStartSystem = new();

    public static void StartExistingGameplayAndHideRouter(Component source)
    {
        QueueMatchLoadAndStart();

        UIRouterView router = source != null ? source.GetComponentInParent<UIRouterView>() : null;
        if (router != null)
            router.gameObject.SetActive(false);
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
}
