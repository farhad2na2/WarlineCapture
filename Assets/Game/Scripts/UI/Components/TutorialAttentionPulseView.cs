using UnityEngine;

namespace Game.UI.Runtime
{
    // Presentation only: the owning tutorial supplies eligibility and progress; no input is consumed.
    [DisallowMultipleComponent]
    public sealed class TutorialAttentionPulseView : MonoBehaviour
    {
        private V3GradientGraphic _border;
        private int _progress;
        private float _startedAt = -1f;

        internal static float Opacity(float elapsed, float delay, bool reducedMotion)
        {
            if (elapsed < delay) return 0f;
            if (reducedMotion) return .7f;
            float phase = ((elapsed - delay) % 2.4f) * (Mathf.PI * 2f / 2.4f);
            return .2f + .8f * (.5f - .5f * Mathf.Cos(phase));
        }

        internal static void Present(RectTransform target, float time, bool eligible, int progress, float delay)
        {
            if (target == null) return;
            var view = target.GetComponent<TutorialAttentionPulseView>();
            if (view == null && eligible) view = target.gameObject.AddComponent<TutorialAttentionPulseView>();
            if (view != null) view.Apply(time, eligible, progress, delay);
        }

        private void Apply(float time, bool eligible, int progress, float delay)
        {
            if (!eligible || !gameObject.activeInHierarchy) { ResetPulse(); return; }
            if (_startedAt < 0 || _progress != progress) { _startedAt = time; _progress = progress; }
            float opacity = Opacity(time - _startedAt, delay, SettingsService.LoadReducedMotionPreference());
            if (_border == null)
            {
                var go = new GameObject("AttentionBorder", typeof(RectTransform), typeof(V3GradientGraphic));
                go.layer = gameObject.layer;
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.SetAsFirstSibling(); // Keep the pulse behind captions and button text.
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-4, -4); rect.offsetMax = new Vector2(4, 4);
                _border = go.GetComponent<V3GradientGraphic>();
                _border.raycastTarget = false;
            }
            _border.gameObject.SetActive(opacity > 0);
            _border.ConfigureCorners(Color.clear, Color.clear, Color.clear, Color.clear,
                new Color(1f, .72f, .02f, opacity), 4f);
        }

        private void OnDisable() => ResetPulse();
        private void ResetPulse()
        {
            _startedAt = -1;
            if (_border != null) _border.gameObject.SetActive(false);
        }
    }
}
