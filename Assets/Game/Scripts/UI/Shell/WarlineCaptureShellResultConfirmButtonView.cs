using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class WarlineCaptureShellResultConfirmButtonView : MonoBehaviour
{
    private Button button;
    private readonly SceneLifecycleSystem sceneLifecycleSystem = new();
    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasBoundaryQuery;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(ConfirmResult);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(ConfirmResult);
    }

    private void ConfirmResult()
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return;

        DynamicBuffer<UiShellPopupRequestComponent> popupRequests =
            entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        popupRequests.Add(new UiShellPopupRequestComponent
        {
            PopupKind = UiShellPopupKind.MissionResult,
            Intent = UiShellPopupIntent.Hide,
            PayloadId = 0
        });

        DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        routeRequests.Add(new UiShellRouteRequestComponent
        {
            Intent = UiShellRouteIntent.ReturnToMainMenu,
            Route = WarlineCaptureRoute.MainMenu,
            PushHistory = 0
        });

        if (sceneLifecycleSystem.QueueUnloadMatch(entityManager))
            Debug.Log("[UiShellResult] submitted Match scene unload request.");
        else
            Debug.LogError("[UiShellResult] failed to submit Match scene unload request.");
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
                ComponentType.ReadWrite<UiShellPopupRequestComponent>(),
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
