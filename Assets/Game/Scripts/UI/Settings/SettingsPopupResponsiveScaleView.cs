using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SettingsPopupResponsiveScaleView : MonoBehaviour
    {
        [SerializeField, Range(0.5f, 0.95f)] private float targetCanvasHeight = 0.84f;
        [SerializeField, Range(0.5f, 0.95f)] private float maximumCanvasWidth = 0.76f;

        private RectTransform _rectTransform;
        private Vector2 _lastCanvasSize;

        public float TargetCanvasHeight => targetCanvasHeight;
        public float MaximumCanvasWidth => maximumCanvasWidth;
        public float LastAppliedScale { get; private set; }

        public void Configure(float heightFraction, float widthFraction)
        {
            targetCanvasHeight = Mathf.Clamp(heightFraction, 0.5f, 0.95f);
            maximumCanvasWidth = Mathf.Clamp(widthFraction, 0.5f, 0.95f);
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (_rectTransform == null)
                _rectTransform = (RectTransform)transform;

            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            Vector2 popupSize = _rectTransform.rect.size;
            if (canvasRect == null || popupSize.x <= 0f || popupSize.y <= 0f)
                return;

            Vector2 canvasSize = canvasRect.rect.size;
            RefreshForCanvasSize(canvasSize);
        }

        public void RefreshForCanvasSize(Vector2 canvasSize)
        {
            if (_rectTransform == null)
                _rectTransform = (RectTransform)transform;

            Vector2 popupSize = _rectTransform.rect.size;
            if (canvasSize.x <= 0f || canvasSize.y <= 0f || popupSize.x <= 0f || popupSize.y <= 0f)
                return;

            float heightScale = canvasSize.y * targetCanvasHeight / popupSize.y;
            float widthScale = canvasSize.x * maximumCanvasWidth / popupSize.x;
            float scale = Mathf.Max(0.01f, Mathf.Min(heightScale, widthScale));
            _rectTransform.localScale = Vector3.one * scale;
            LastAppliedScale = scale;
            _lastCanvasSize = canvasSize;
        }

        private void OnEnable()
        {
            RefreshLayout();
        }

        private void Start()
        {
            RefreshLayout();
        }

        private void LateUpdate()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            if (canvasRect != null && canvasRect.rect.size != _lastCanvasSize)
                RefreshLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshLayout();
        }
    }
}
