using UnityEngine;

namespace Game.UI.Runtime
{
    // Presentation only: the owning tutorial supplies eligibility and progress; no input is consumed.
    [DisallowMultipleComponent]
    public sealed class TutorialAttentionPulseView : MonoBehaviour
    {
        private V3GradientGraphic _border;
        private V3GradientGraphic _contrast;
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
            if (_contrast == null)
            {
                var go = new GameObject("AttentionContrast", typeof(RectTransform), typeof(V3GradientGraphic));
                go.layer = gameObject.layer;
                var rect = (RectTransform)go.transform; rect.SetParent(transform, false); rect.SetAsFirstSibling();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-7, -7); rect.offsetMax = new Vector2(7, 7);
                _contrast = go.GetComponent<V3GradientGraphic>(); _contrast.raycastTarget = false;
                _contrast.ConfigureCorners(Color.clear, Color.clear, Color.clear, Color.clear,
                    new Color(.025f, .035f, .04f, 1), 16);
            }
            _contrast.gameObject.SetActive(opacity > 0);
            _border.ConfigureCorners(Color.clear, Color.clear, Color.clear, Color.clear,
                new Color(1f, .85f, .02f, .85f + .15f * opacity), 8f + 4f * opacity);
            TutorialTapPointerView.Present((RectTransform)transform, time - _startedAt, opacity > 0);
        }

        private void OnDisable() => ResetPulse();
        private void ResetPulse()
        {
            _startedAt = -1;
            if (_border != null) _border.gameObject.SetActive(false);
            if (_contrast != null) _contrast.gameObject.SetActive(false);
            TutorialTapPointerView.Present((RectTransform)transform, 0, false);
        }
    }
}
