using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class ScreenRouteButton : MonoBehaviour
{
    [SerializeField] private WarlineCaptureRouter router;
    [SerializeField] private WarlineCaptureRoute route;
    [SerializeField] private bool useBackNavigation;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (router == null)
            router = GetComponentInParent<WarlineCaptureRouter>();

        if (router == null)
            return;

        if (useBackNavigation)
            router.Back();
        else
            router.GoTo(route);
    }
}
