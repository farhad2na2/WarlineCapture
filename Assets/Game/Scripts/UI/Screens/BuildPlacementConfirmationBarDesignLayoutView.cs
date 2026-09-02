using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Scales the authored 1664x310 placement content from the footer height,
    /// then exposes any ultrawide remainder as additional design-space width.
    /// This keeps text, icons, portraits, and the three actions at the visual-lock
    /// scale without stretching the footer background.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BuildPlacementConfirmationBarDesignLayoutView : MonoBehaviour
    {
        [SerializeField] private RectTransform designContent;
        [SerializeField] private Vector2 referenceSize = new(1664f, 310f);
        [SerializeField] private float validAnchorMaxY = 245f / 941f;
        [SerializeField] private float invalidAnchorMaxY = 324f / 941f;

        private RectTransform _panel;
        private Vector2 _lastPanelSize = new(-1f, -1f);
        private bool _applying;
        private bool? _lastValidity;

        public RectTransform DesignContent => designContent;
        public Vector2 ReferenceSize => referenceSize;

        public void Configure(RectTransform content, Vector2 authoredReferenceSize)
        {
            designContent = content;
            referenceSize = new Vector2(
                Mathf.Max(1f, authoredReferenceSize.x),
                Mathf.Max(1f, authoredReferenceSize.y));
            RefreshLayout();
        }

        public void ApplyValidityState(bool isValid)
        {
            _panel ??= transform as RectTransform;
            if (_panel == null || _lastValidity == isValid)
                return;

            Vector2 anchorMax = _panel.anchorMax;
            anchorMax.y = isValid ? validAnchorMaxY : invalidAnchorMaxY;
            _panel.anchorMax = anchorMax;
            _lastValidity = isValid;
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (_applying || designContent == null)
                return;

            _panel ??= transform as RectTransform;
            if (_panel == null || _panel.rect.height <= 0f)
                return;

            _applying = true;
            try
            {
                float scale = _panel.rect.height / referenceSize.y;
                float designWidth = _panel.rect.width / Mathf.Max(0.0001f, scale);

                designContent.anchorMin = Vector2.zero;
                designContent.anchorMax = Vector2.zero;
                designContent.pivot = Vector2.zero;
                designContent.anchoredPosition = Vector2.zero;
                designContent.sizeDelta = new Vector2(
                    Mathf.Max(referenceSize.x, designWidth),
                    referenceSize.y);
                designContent.localScale = new Vector3(scale, scale, 1f);
                designContent.localRotation = Quaternion.identity;

                BuildPlacementConfirmationResponsiveLayoutView responsive =
                    designContent.GetComponent<BuildPlacementConfirmationResponsiveLayoutView>();
                responsive?.RefreshLayout();
                _lastPanelSize = _panel.rect.size;
            }
            finally
            {
                _applying = false;
            }
        }

        private void OnEnable() => RefreshLayout();
        private void Start() => RefreshLayout();
        private void OnRectTransformDimensionsChange() => RefreshLayout();

        private void LateUpdate()
        {
            _panel ??= transform as RectTransform;
            if (_panel != null && _panel.rect.size != _lastPanelSize)
                RefreshLayout();
        }
    }
}
