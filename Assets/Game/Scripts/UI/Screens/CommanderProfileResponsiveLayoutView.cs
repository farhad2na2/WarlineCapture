using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    public enum CommanderProfileResponsiveSection : byte
    {
        Middle,
        Right,
        Footer
    }

    [DisallowMultipleComponent]
    public sealed class CommanderProfileResponsiveLayoutView : MonoBehaviour
    {
        private const float WideCanvasHeight = 2160f;
        private const float NarrowCanvasHeight = 2700f;

        [SerializeField] private CommanderProfileResponsiveSection section;
        [SerializeField] private RectTransform[] targets = Array.Empty<RectTransform>();
        [SerializeField] private float[] wideTopOffsets = Array.Empty<float>();
        [SerializeField] private float[] narrowTopOffsets = Array.Empty<float>();
        [SerializeField] private float[] wideHeights = Array.Empty<float>();
        [SerializeField] private float[] narrowHeights = Array.Empty<float>();

        public CommanderProfileResponsiveSection Section => section;
        public float LastAppliedCanvasHeight { get; private set; }

        public void Configure(
            CommanderProfileResponsiveSection responsiveSection,
            RectTransform[] layoutTargets,
            float[] wideOffsets,
            float[] narrowOffsets,
            float[] wideTargetHeights = null,
            float[] narrowTargetHeights = null)
        {
            section = responsiveSection;
            targets = layoutTargets ?? Array.Empty<RectTransform>();
            wideTopOffsets = wideOffsets ?? Array.Empty<float>();
            narrowTopOffsets = narrowOffsets ?? Array.Empty<float>();
            wideHeights = wideTargetHeights ?? Array.Empty<float>();
            narrowHeights = narrowTargetHeights ?? Array.Empty<float>();
        }

        public void ApplyLayout(float canvasHeightOverride = 0f)
        {
            float canvasHeight = canvasHeightOverride > 0f ? canvasHeightOverride : ResolveCanvasHeight();
            if (canvasHeight <= 0f)
                return;

            LastAppliedCanvasHeight = canvasHeight;
            float narrowWeight = Mathf.InverseLerp(WideCanvasHeight, NarrowCanvasHeight, canvasHeight);
            int count = Mathf.Min(targets.Length, Mathf.Min(wideTopOffsets.Length, narrowTopOffsets.Length));
            for (int i = 0; i < count; i++)
            {
                RectTransform target = targets[i];
                if (target == null)
                    continue;

                float top = Mathf.Lerp(wideTopOffsets[i], narrowTopOffsets[i], narrowWeight);
                Vector2 anchoredPosition = target.anchoredPosition;
                float desiredY = -top;
                if (Mathf.Abs(anchoredPosition.y - desiredY) > 0.01f)
                {
                    anchoredPosition.y = desiredY;
                    target.anchoredPosition = anchoredPosition;
                }

                if (i >= wideHeights.Length || i >= narrowHeights.Length ||
                    wideHeights[i] <= 0f || narrowHeights[i] <= 0f)
                    continue;

                float desiredHeight = Mathf.Lerp(wideHeights[i], narrowHeights[i], narrowWeight);
                Vector2 sizeDelta = target.sizeDelta;
                if (Mathf.Abs(sizeDelta.y - desiredHeight) <= 0.01f)
                    continue;

                sizeDelta.y = desiredHeight;
                target.sizeDelta = sizeDelta;
            }
        }

        private void OnEnable()
        {
            ApplyLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

        private float ResolveCanvasHeight()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            return canvasRect != null ? canvasRect.rect.height : 0f;
        }
    }
}
