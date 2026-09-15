using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    // Passive presentation, driven by the HUD after its controls have reflowed.
    [DisallowMultipleComponent]
    public sealed class TutorialAttentionPulseView : MonoBehaviour
    {
        private TutorialFocusFrameGraphic border;
        private Action prepareFrame;
        private int progress;
        private float startedAt = -1f, delay;
        private bool eligible;

        internal static float Opacity(float elapsed, float delay, bool reducedMotion)
        {
            if (elapsed < delay) return 0f;
            if (reducedMotion) return .7f;
            float phase = ((elapsed - delay) % 2.4f) * (Mathf.PI * 2f / 2.4f);
            return .2f + .8f * (.5f - .5f * Mathf.Cos(phase));
        }

        internal static void BindFrame(RectTransform target, Action prepare)
        {
            var view = target.GetComponent<TutorialAttentionPulseView>() ?? target.gameObject.AddComponent<TutorialAttentionPulseView>();
            view.prepareFrame = prepare;
        }

        internal static void Present(RectTransform target, float time, bool eligible, int progress, float delay)
        {
            if (target == null) return;
            var view = target.GetComponent<TutorialAttentionPulseView>();
            if (view == null && eligible) view = target.gameObject.AddComponent<TutorialAttentionPulseView>();
            if (view == null) return;
            if (!eligible) { view.ResetPulse(); return; }
            if (!view.eligible || view.progress != progress) view.startedAt = time;
            view.eligible = true; view.progress = progress; view.delay = delay;
            view.RenderAt(time);
        }

        private void OnEnable() => Canvas.willRenderCanvases += RenderBeforeCanvas;
        private void OnDisable()
        {
            Canvas.willRenderCanvases -= RenderBeforeCanvas;
            ResetPulse();
        }
        // Placement validity, responsive sections and the HUD camera can all move
        // controls after Update. Follow their final rendered geometry, not that snapshot.
        private void RenderBeforeCanvas() => RenderAt(Time.unscaledTime);
        internal void RenderAt(float time)
        {
            if (!eligible || !gameObject.activeInHierarchy) return;
            prepareFrame?.Invoke();
            if (!gameObject.activeInHierarchy) return;
            float elapsed = time - startedAt;
            float opacity = Opacity(elapsed, delay, SettingsService.LoadReducedMotionPreference());
            if (border == null)
            {
                var go = new GameObject("AttentionDoubleBorder", typeof(RectTransform), typeof(TutorialFocusFrameGraphic));
                go.layer = gameObject.layer;
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false); rect.SetAsFirstSibling();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                border = go.GetComponent<TutorialFocusFrameGraphic>(); border.raycastTarget = false;
            }
            border.gameObject.SetActive(opacity > 0);
            border.SetIntensity(.85f + .15f * opacity);
            TutorialTapPointerView.Present((RectTransform)transform, elapsed - delay, opacity > 0);
        }

        private void ResetPulse()
        {
            eligible = false; startedAt = -1;
            if (border != null) border.gameObject.SetActive(false);
            TutorialTapPointerView.Present((RectTransform)transform, 0, false);
        }
    }
}
