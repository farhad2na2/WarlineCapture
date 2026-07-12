using Game.Catalog.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed class NarrativePanelMotionPresentationSystemHelper
    {
        private const float PushScale = 1.035f;
        private const float DriftScale = 1.04f;
        private const float DriftPixels = 12f;
        private const float ImpactScale = 1.018f;

        private readonly RectTransform panelRoot;
        private Vector2 basePosition;
        private Vector3 baseScale;
        private NarrativeMotionPreset preset;
        private float duration;
        private float elapsed;
        private bool reducedMotion;

        public NarrativePanelMotionPresentationSystemHelper(RectTransform target)
        {
            panelRoot = target;
            if (panelRoot != null)
            {
                basePosition = panelRoot.anchoredPosition;
                baseScale = panelRoot.localScale;
            }
        }

        public bool IsReducedMotion => reducedMotion;
        public float NormalizedTime => duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        public void Start(NarrativeMotionPreset motionPreset, float durationSeconds, bool useReducedMotion)
        {
            if (panelRoot == null)
                return;

            ResetTransform();
            basePosition = panelRoot.anchoredPosition;
            baseScale = panelRoot.localScale;
            preset = motionPreset;
            duration = Mathf.Max(0.1f, durationSeconds);
            elapsed = 0f;
            reducedMotion = useReducedMotion;
            Apply(0f);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (panelRoot == null)
                return;

            elapsed = Mathf.Min(duration, elapsed + Mathf.Max(0f, unscaledDeltaTime));
            Apply(reducedMotion ? 0f : NormalizedTime);
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (panelRoot == null)
                return;
            Apply(reducedMotion ? 0f : NormalizedTime);
        }

        public void Cancel()
        {
            if (panelRoot == null)
                return;
            ResetTransform();
            elapsed = 0f;
            duration = 0f;
            preset = NarrativeMotionPreset.Static;
            reducedMotion = false;
        }

        private void Apply(float normalizedTime)
        {
            if (panelRoot == null)
                return;

            if (reducedMotion || preset == NarrativeMotionPreset.Static || preset == NarrativeMotionPreset.StaticInteractive)
            {
                panelRoot.anchoredPosition = basePosition;
                panelRoot.localScale = baseScale;
                return;
            }

            float eased = SmoothStep(normalizedTime);
            Vector2 position = basePosition;
            float scale = 1f;
            switch (preset)
            {
                case NarrativeMotionPreset.PushIn:
                    scale = Mathf.Lerp(1f, PushScale, eased);
                    break;
                case NarrativeMotionPreset.PullBack:
                    scale = Mathf.Lerp(PushScale, 1f, eased);
                    break;
                case NarrativeMotionPreset.DriftLeft:
                    scale = DriftScale;
                    position.x += Mathf.Lerp(DriftPixels, -DriftPixels, eased);
                    break;
                case NarrativeMotionPreset.DriftRight:
                    scale = DriftScale;
                    position.x += Mathf.Lerp(-DriftPixels, DriftPixels, eased);
                    break;
                case NarrativeMotionPreset.StaticImpact:
                    scale = 1f + ImpactScaleOffset(normalizedTime);
                    break;
            }

            panelRoot.anchoredPosition = position;
            panelRoot.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, baseScale.z);
        }

        private void ResetTransform()
        {
            panelRoot.anchoredPosition = basePosition;
            panelRoot.localScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
        }

        private static float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float ImpactScaleOffset(float normalizedTime)
        {
            float pulse = Mathf.Sin(Mathf.Clamp01(normalizedTime) * Mathf.PI);
            return (ImpactScale - 1f) * pulse;
        }
    }
}
