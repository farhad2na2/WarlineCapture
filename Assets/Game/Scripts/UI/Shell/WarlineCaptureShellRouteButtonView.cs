using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class WarlineCaptureShellRouteButtonView : MonoBehaviour
{
    [SerializeField] private UiShellRouteIntent intent = UiShellRouteIntent.OpenMenuRoute;
    [SerializeField] private WarlineCaptureRoute route = WarlineCaptureRoute.MainMenu;
    [SerializeField] private bool pushHistory;

    private Button button;
    private readonly SceneLifecycleSystem sceneLifecycleSystem = new();
    private readonly MatchStartSystem matchStartSystem = new();
    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasBoundaryQuery;

    public UiShellRouteIntent Intent => intent;
    public WarlineCaptureRoute Route => route;
    public bool PushHistory => pushHistory;

    public void Configure(UiShellRouteIntent routeIntent, WarlineCaptureRoute targetRoute, bool shouldPushHistory)
    {
        intent = routeIntent;
        route = targetRoute;
        pushHistory = shouldPushHistory;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(SubmitRouteRequest);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(SubmitRouteRequest);
    }

    private void SubmitRouteRequest()
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
        {
            Debug.LogError($"[UiShellRoute] Missing UI shell boundary. intent={intent} route={route}");
            return;
        }

        DynamicBuffer<UiShellRouteRequestComponent> requests =
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        requests.Add(new UiShellRouteRequestComponent
        {
            Intent = intent,
            Route = route,
            PushHistory = pushHistory ? (byte)1 : (byte)0
        });

        Debug.Log($"[UiShellRoute] submitted intent={intent} route={route} pushHistory={(pushHistory ? 1 : 0)}");

        if (intent == UiShellRouteIntent.EnterMatch)
            QueueMatchLoad(entityManager);
    }

    private void QueueMatchLoad(EntityManager entityManager)
    {
        if (sceneLifecycleSystem.QueueLoadMatch(entityManager))
        {
            Debug.Log("[UiShellRoute] submitted Match scene load request.");
            if (matchStartSystem.QueueStartAfterMatchLoaded(entityManager))
                Debug.Log("[UiShellRoute] submitted Match gameplay start request.");
            else
                Debug.LogError("[UiShellRoute] failed to submit Match gameplay start request.");
        }
        else
        {
            Debug.LogError("[UiShellRoute] failed to submit Match scene load request.");
        }
    }

    private bool TryGetBoundary(out EntityManager entityManager, out Entity boundary)
    {
        entityManager = default;
        boundary = Entity.Null;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (cachedWorld != world || !hasBoundaryQuery)
        {
            cachedWorld = world;
            boundaryQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellBoundaryComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            hasBoundaryQuery = true;
        }

        if (boundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entityManager = world.EntityManager;
        boundary = boundaryQuery.GetSingletonEntity();
        return true;
    }
}
