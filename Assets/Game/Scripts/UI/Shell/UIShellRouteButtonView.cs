using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UIShellRouteButtonView : MonoBehaviour
{
    [SerializeField] private UiShellRouteIntent intent = UiShellRouteIntent.OpenMenuRoute;
    [SerializeField] private UIRoute route = UIRoute.MainMenu;
    [SerializeField] private bool pushHistory;

    private Button button;

    public UiShellRouteIntent Intent => intent;
    public UIRoute Route => route;
    public bool PushHistory => pushHistory;

    public void Configure(UiShellRouteIntent routeIntent, UIRoute targetRoute, bool shouldPushHistory)
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
        if (!UiShellEcsGateway.TryEnqueueRouteRequest(intent, route, pushHistory))
        {
            Debug.LogError($"[UiShellRoute] Missing UI shell boundary. intent={intent} route={route}");
        }
    }
}
