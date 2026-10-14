using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Pulls the SCN-13 composition inside the safe area after the shared section layout
    /// has placed it. 16:9, 20:9 and 4:3 still come from that layout. A capture can simulate
    /// a notch so the inset is visible without a device cutout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkirmishLibrarySafeFrame : MonoBehaviour
    {
        private MainMenuV3SectionLayoutView layout;
        private RectTransform rect;
        private bool _simulated;
        private float _canvasWidth;
        private float _canvasHeight;
        private float _left;
        private float _top;
        private float _right;
        private float _bottom;
        private RectTransform _notch;

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            layout = GetComponent<MainMenuV3SectionLayoutView>();
            if (layout != null)
                layout.LayoutApplied += Apply;
        }

        private void OnDisable()
        {
            if (layout != null)
                layout.LayoutApplied -= Apply;
        }

        /// <summary>Pixel insets in the capture canvas. Top is the notch.</summary>
        public void SetSimulatedInsets(float canvasWidth, float canvasHeight, float left, float top, float right, float bottom)
        {
            _simulated = canvasWidth > 2f && canvasHeight > 2f;
            _canvasWidth = canvasWidth;
            _canvasHeight = canvasHeight;
            _left = left;
            _top = top;
            _right = right;
            _bottom = bottom;
        }

        public void ClearSimulatedInsets()
        {
            _simulated = false;
            if (_notch != null)
                _notch.gameObject.SetActive(false);
        }

        public void Apply()
        {
            if (rect == null)
                return;

            float canvasWidth;
            float canvasHeight;
            float left;
            float right;
            float top;
            float bottom;
            if (_simulated)
            {
                canvasWidth = _canvasWidth;
                canvasHeight = _canvasHeight;
                left = _left;
                right = _right;
                top = _top;
                bottom = _bottom;
            }
            else
            {
                if (Screen.width < 2 || Screen.height < 2)
                    return;
                Rect safe = Screen.safeArea;
                canvasWidth = Screen.width;
                canvasHeight = Screen.height;
                left = safe.xMin;
                right = Screen.width - safe.xMax;
                bottom = safe.yMin;
                top = Screen.height - safe.yMax;
            }

            float horizontal = (left + right) / Mathf.Max(1f, canvasWidth);
            float vertical = (top + bottom) / Mathf.Max(1f, canvasHeight);
            bool inset = horizontal + vertical >= 0.004f;
            ShowEdges(inset);
            ShowNotch(inset && _simulated && top >= 24f);
            if (!inset)
                return;

            float fit = Mathf.Clamp(Mathf.Min(1f - horizontal, 1f - vertical), 0.72f, 1f);
            float scale = layout != null && layout.LastAppliedScale > 0f
                ? layout.LastAppliedScale * fit
                : rect.localScale.x * fit;
            rect.localScale = new Vector3(scale, scale, 1f);

            RectTransform canvas = rect.parent as RectTransform;
            if (canvas == null)
                return;
            Vector3 shift = canvas.TransformVector(new Vector3(
                (left - right) / canvasWidth * canvas.rect.width * 0.5f,
                (bottom - top) / canvasHeight * canvas.rect.height * 0.5f,
                0f));
            rect.position += shift;
        }

        private void ShowEdges(bool visible)
        {
            EnsureEdge("SafeAreaEdgeTop", 0f, 1f, 1f, 1f, 0f, -4f, 0f, 0f, visible);
            EnsureEdge("SafeAreaEdgeBottom", 0f, 0f, 1f, 0f, 0f, 0f, 0f, 4f, visible);
            EnsureEdge("SafeAreaEdgeLeft", 0f, 0f, 0f, 1f, 0f, 0f, 4f, 0f, visible);
            EnsureEdge("SafeAreaEdgeRight", 1f, 0f, 1f, 1f, -4f, 0f, 0f, 0f, visible);
        }

        private void EnsureEdge(string name, float minX, float minY, float maxX, float maxY, float left, float bottom, float right, float top, bool visible)
        {
            Transform existing = transform.Find(name);
            RectTransform edge = existing as RectTransform;
            if (edge == null)
            {
                var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(transform, false);
                image.color = new Color(0.93f, 0.78f, 0.38f, 1f);
                image.raycastTarget = false;
                edge = image.rectTransform;
            }

            edge.gameObject.SetActive(visible);
            edge.anchorMin = new Vector2(minX, minY);
            edge.anchorMax = new Vector2(maxX, maxY);
            edge.offsetMin = new Vector2(left, bottom);
            edge.offsetMax = new Vector2(right, top);
            edge.SetAsLastSibling();
        }

        private void ShowNotch(bool visible)
        {
            RectTransform canvas = rect.parent as RectTransform;
            if (canvas == null)
                return;
            if (_notch == null)
            {
                var image = new GameObject("SafeAreaNotch", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(canvas, false);
                image.color = new Color(0.02f, 0.02f, 0.03f, 1f);
                image.raycastTarget = false;
                _notch = image.rectTransform;
                _notch.anchorMin = _notch.anchorMax = new Vector2(0.5f, 1f);
                _notch.pivot = new Vector2(0.5f, 1f);
                _notch.sizeDelta = new Vector2(220f, 36f);
                _notch.anchoredPosition = Vector2.zero;
            }

            _notch.gameObject.SetActive(visible);
            if (visible)
                _notch.SetAsLastSibling();
        }
    }
}
