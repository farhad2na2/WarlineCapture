using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Pulls the SCN-13 composition inside <see cref="Screen.safeArea"/> after the shared
    /// section layout has placed it. 16:9, 20:9 and 4:3 still come from that layout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkirmishLibrarySafeFrame : MonoBehaviour
    {
        private MainMenuV3SectionLayoutView layout;
        private RectTransform rect;

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

        public void Apply()
        {
            if (rect == null || Screen.width < 2 || Screen.height < 2)
                return;

            Rect safe = Screen.safeArea;
            float left = safe.xMin / Screen.width;
            float right = (Screen.width - safe.xMax) / Screen.width;
            float bottom = safe.yMin / Screen.height;
            float top = (Screen.height - safe.yMax) / Screen.height;
            float horizontal = left + right;
            float vertical = top + bottom;
            if (horizontal + vertical < 0.004f)
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
                (left - right) * canvas.rect.width * 0.5f,
                (bottom - top) * canvas.rect.height * 0.5f,
                0f));
            rect.position += shift;
        }
    }
}
