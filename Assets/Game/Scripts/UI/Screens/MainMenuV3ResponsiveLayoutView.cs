using UnityEngine;

namespace Game.UI.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MainMenuV3ResponsiveLayoutView : MonoBehaviour
    {
        public enum RegionLayoutKind
        {
            Header,
            Left,
            Right,
            Footer
        }

        [SerializeField] private RegionLayoutKind layoutKind;
        [SerializeField] private RectTransform layoutRoot;
        [SerializeField] private float headerHeight = 400f;
        [SerializeField] private float footerHeight = 330f;
        [SerializeField] private float sideRegionTop = 280f;
        [SerializeField] private float leftWidth = 2000f;
        [SerializeField] private float rightWidth = 950f;
        [SerializeField] private float horizontalMargin = 30f;

        public void Configure(RegionLayoutKind kind, RectTransform root)
        {
            layoutKind = kind;
            layoutRoot = root;
            ApplyLayout();
        }

        private void OnEnable()
        {
            ApplyLayout();
            Canvas.preWillRenderCanvases -= ApplyBeforeRender;
            Canvas.preWillRenderCanvases += ApplyBeforeRender;
        }

        private void OnDisable()
        {
            Canvas.preWillRenderCanvases -= ApplyBeforeRender;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                ApplyLayout();
        }

        private void ApplyBeforeRender()
        {
            Canvas.preWillRenderCanvases -= ApplyBeforeRender;
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (layoutRoot == null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            float canvasHeight = canvasRect != null && canvasRect.rect.height > 1f
                ? canvasRect.rect.height
                : 2160f;
            float tallLayoutBlend = Mathf.InverseLerp(2160f, 2700f, canvasHeight);
            float resolvedHeaderHeight = Mathf.Lerp(340f, headerHeight, tallLayoutBlend);
            float resolvedFooterHeight = Mathf.Lerp(280f, footerHeight, tallLayoutBlend);
            float compactFooterClearance = Mathf.Lerp(120f, 0f, tallLayoutBlend);
            float sideHeight = Mathf.Max(
                900f,
                canvasHeight - resolvedHeaderHeight - resolvedFooterHeight - compactFooterClearance);
            float sideTopOffset = Mathf.Max(0f, resolvedHeaderHeight - sideRegionTop);

            switch (layoutKind)
            {
                case RegionLayoutKind.Header:
                    SetTopStretch(layoutRoot, resolvedHeaderHeight);
                    break;
                case RegionLayoutKind.Left:
                    SetTopSide(layoutRoot, true, sideTopOffset, leftWidth, sideHeight);
                    break;
                case RegionLayoutKind.Right:
                    SetTopSide(layoutRoot, false, sideTopOffset, rightWidth, sideHeight);
                    break;
                case RegionLayoutKind.Footer:
                    SetBottomStretch(layoutRoot, resolvedFooterHeight);
                    break;
            }
        }

        private static void SetTopStretch(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        private void SetTopSide(RectTransform rect, bool left, float topOffset, float width, float height)
        {
            Vector2 anchor = new(left ? 0f : 1f, 1f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = new Vector2(left ? horizontalMargin : -horizontalMargin, -topOffset);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetBottomStretch(RectTransform rect, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }
    }
}
