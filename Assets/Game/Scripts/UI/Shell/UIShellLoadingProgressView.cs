using TMPro;
using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class UIShellLoadingProgressView : MonoBehaviour
    {
        private const string DefaultStatus = "Preparing command interface";
        private static readonly string[] PercentLabels = BuildPercentLabels();

        [SerializeField] private RectTransform progressFill;
        [SerializeField] private TMP_Text percentText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private float fillWidth = 648f;

        private int lastPercent = -1;
        private bool hasLastStatus;
        private string lastStatus;

        public void Configure(RectTransform fill, TMP_Text percent, TMP_Text status, float maxFillWidth)
        {
            progressFill = fill;
            percentText = percent;
            statusText = status;
            fillWidth = Mathf.Max(1f, maxFillWidth);
            ResetPresentationCache();
            ApplyProgress(0f, DefaultStatus);
        }

        private void OnEnable()
        {
            ResetPresentationCache();
            ApplyProgress(0f, DefaultStatus);
        }

        private void Update()
        {
            if (!TryGetLoading(out UiShellLoadingProgressModel loading))
                return;

            ApplyProgress(loading.Progress01, loading.Status);
        }

        private void ApplyProgress(float progress01, string status)
        {
            float clamped = Mathf.Clamp01(progress01);
            int percent = Mathf.RoundToInt(clamped * 100f);
            if (progressFill != null)
            {
                Vector2 size = progressFill.sizeDelta;
                size.x = fillWidth * clamped;
                progressFill.sizeDelta = size;
            }

            if (percentText != null && percent != lastPercent)
                percentText.text = PercentLabels[percent];

            lastPercent = percent;

            string resolvedStatus = string.IsNullOrEmpty(status) ? DefaultStatus : status;
            if (!hasLastStatus || resolvedStatus != lastStatus)
            {
                if (statusText != null)
                    statusText.text = resolvedStatus;

                lastStatus = resolvedStatus;
                hasLastStatus = true;
            }
        }

        private void ResetPresentationCache()
        {
            lastPercent = -1;
            hasLastStatus = false;
            lastStatus = default;
        }

        private static string[] BuildPercentLabels()
        {
            string[] labels = new string[101];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = i + "%";
            return labels;
        }

        private bool TryGetLoading(out UiShellLoadingProgressModel loading)
        {
            return UiShellRuntimeGateway.TryReadLoadingProgress(out loading);
        }
    }
}
