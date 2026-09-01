using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchHudFullMapPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private MatchHudMinimapView minimap;
        [SerializeField] private Button closeAction;
        [SerializeField] private Button centerOnHqAction;
        [SerializeField] private Vector2 hqNormalizedPosition = new(0.5f, 0.5f);

        private Canvas _cachedCanvas;

        public GameObject PopupRoot => popupRoot != null ? popupRoot : gameObject;
        public MatchHudMinimapView Minimap => minimap;
        public Button CloseAction => closeAction;
        public Button CenterOnHqAction => centerOnHqAction;
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
            if (centerOnHqAction != null)
                centerOnHqAction.onClick.AddListener(RequestCenterOnHq);
        }

        private void OnDisable()
        {
            if (closeAction != null)
                closeAction.onClick.RemoveListener(RequestClose);
            if (centerOnHqAction != null)
                centerOnHqAction.onClick.RemoveListener(RequestCenterOnHq);
        }

        private void OnTransformParentChanged()
        {
            _cachedCanvas = null;
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

        private void RequestCenterOnHq()
        {
            minimap?.RequestFocus(hqNormalizedPosition);
        }

        private Camera ResolveEventCamera(RectTransform rect)
        {
            Canvas canvas = ResolveCanvas(rect);
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private Canvas ResolveCanvas(RectTransform rect)
        {
            if (_cachedCanvas == null && rect != null)
                _cachedCanvas = rect.GetComponentInParent<Canvas>();
            return _cachedCanvas;
        }
    }
}
