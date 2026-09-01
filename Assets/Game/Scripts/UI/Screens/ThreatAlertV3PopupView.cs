using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Owns the two visual states of POP-01 without duplicating its runtime
    /// prefab: the blocking threat alert and the non-blocking route preview.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThreatAlertV3PopupView : MonoBehaviour
    {
        [SerializeField] private GameObject scrim;
        [SerializeField] private GameObject alertSurface;
        [SerializeField] private GameObject routePreviewSurface;
        [SerializeField] private GameObject routePreviewStrip;
        [SerializeField] private GameObject routeWorldOverlay;
        [SerializeField] private Button jumpToThreatButton;
        [SerializeField] private Button alertCloseButton;
        [SerializeField] private Button routeCloseButton;

        private GameObject _suppressedThreatPanel;
        private bool _restoreSuppressedThreatPanel;

        public GameObject Scrim => scrim;
        public GameObject AlertSurface => alertSurface;
        public GameObject RoutePreviewSurface => routePreviewSurface;
        public GameObject RoutePreviewStrip => routePreviewStrip;
        public GameObject RouteWorldOverlay => routeWorldOverlay;
        public Button JumpToThreatButton => jumpToThreatButton;
        public Button AlertCloseButton => alertCloseButton;
        public Button RouteCloseButton => routeCloseButton;
        public bool IsRoutePreview => routePreviewSurface != null && routePreviewSurface.activeSelf;

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            SuppressLegacyThreatBanner();
        }

        private void OnDisable()
        {
            RestoreLegacyThreatBanner();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            RestoreLegacyThreatBanner();
        }

        public void ShowAlert()
        {
            gameObject.SetActive(true);
            SuppressLegacyThreatBanner();
            SetState(routePreview: false);
        }

        public void ShowRoutePreview()
        {
            gameObject.SetActive(true);
            SuppressLegacyThreatBanner();
            SetState(routePreview: true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void SetState(bool routePreview)
        {
            if (scrim != null)
                scrim.SetActive(!routePreview);
            if (alertSurface != null)
                alertSurface.SetActive(!routePreview);
            if (routePreviewSurface != null)
                routePreviewSurface.SetActive(routePreview);
            if (routePreviewStrip != null)
                routePreviewStrip.SetActive(routePreview);
            if (routeWorldOverlay != null)
                routeWorldOverlay.SetActive(routePreview);
        }

        private void BindButtons()
        {
            if (jumpToThreatButton != null)
                jumpToThreatButton.onClick.AddListener(ShowRoutePreview);
            if (alertCloseButton != null)
                alertCloseButton.onClick.AddListener(Close);
            if (routeCloseButton != null)
                routeCloseButton.onClick.AddListener(Close);
        }

        private void UnbindButtons()
        {
            if (jumpToThreatButton != null)
                jumpToThreatButton.onClick.RemoveListener(ShowRoutePreview);
            if (alertCloseButton != null)
                alertCloseButton.onClick.RemoveListener(Close);
            if (routeCloseButton != null)
                routeCloseButton.onClick.RemoveListener(Close);
        }

        private void SuppressLegacyThreatBanner()
        {
            if (_suppressedThreatPanel != null)
                return;

            Transform found = FindDeepChild(transform.root, "ThreatJumpPanel");
            if (found == null || found.IsChildOf(transform))
                return;

            _suppressedThreatPanel = found.gameObject;
            _restoreSuppressedThreatPanel = _suppressedThreatPanel.activeSelf;
            if (_restoreSuppressedThreatPanel)
                _suppressedThreatPanel.SetActive(false);
        }

        private void RestoreLegacyThreatBanner()
        {
            if (_suppressedThreatPanel != null && _restoreSuppressedThreatPanel)
                _suppressedThreatPanel.SetActive(true);
            _suppressedThreatPanel = null;
            _restoreSuppressedThreatPanel = false;
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            if (root == null)
                return null;
            if (root.name == childName)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }
            return null;
        }

#if UNITY_EDITOR
        public void Configure(
            GameObject configuredScrim,
            GameObject configuredAlertSurface,
            GameObject configuredRoutePreviewSurface,
            GameObject configuredRoutePreviewStrip,
            GameObject configuredRouteWorldOverlay,
            Button configuredJumpToThreatButton,
            Button configuredAlertCloseButton,
            Button configuredRouteCloseButton)
        {
            UnbindButtons();
            scrim = configuredScrim;
            alertSurface = configuredAlertSurface;
            routePreviewSurface = configuredRoutePreviewSurface;
            routePreviewStrip = configuredRoutePreviewStrip;
            routeWorldOverlay = configuredRouteWorldOverlay;
            jumpToThreatButton = configuredJumpToThreatButton;
            alertCloseButton = configuredAlertCloseButton;
            routeCloseButton = configuredRouteCloseButton;
            BindButtons();
            SetState(routePreview: false);
        }
#endif
    }
}
