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
            return;

        DynamicBuffer<UiShellRouteRequestComponent> requests =
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        requests.Add(new UiShellRouteRequestComponent
        {
            Intent = intent,
            Route = route,
            PushHistory = pushHistory ? (byte)1 : (byte)0
        });
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
