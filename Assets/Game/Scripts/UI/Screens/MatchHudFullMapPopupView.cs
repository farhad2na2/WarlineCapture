using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchHudFullMapPopupView : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private MatchHudMinimapView minimap;
    [SerializeField] private Button closeAction;

    public GameObject PopupRoot => popupRoot != null ? popupRoot : gameObject;
    public MatchHudMinimapView Minimap => minimap;
    public bool IsOpen => PopupRoot != null && PopupRoot.activeInHierarchy;

    public event System.Action CloseRequested;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;
    }

    private void OnEnable()
    {
        if (closeAction != null)
            closeAction.onClick.AddListener(RequestClose);
    }

    private void OnDisable()
    {
        if (closeAction != null)
            closeAction.onClick.RemoveListener(RequestClose);
    }

    public void Show()
    {
        if (PopupRoot != null && !PopupRoot.activeSelf)
            PopupRoot.SetActive(true);
    }

    public void Hide()
    {
        if (PopupRoot != null && PopupRoot.activeSelf)
            PopupRoot.SetActive(false);
    }

    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        RectTransform rect = PopupRoot != null ? PopupRoot.transform as RectTransform : transform as RectTransform;
        if (rect == null || !rect.gameObject.activeInHierarchy)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, ResolveEventCamera(rect));
    }

    private void RequestClose()
    {
        CloseRequested?.Invoke();
    }

    private static Camera ResolveEventCamera(RectTransform rect)
    {
        Canvas canvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }
}
