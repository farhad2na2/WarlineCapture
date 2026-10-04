using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class NarrativePlaybackControlsView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup skipGroup;
        [SerializeField] private Button skipButton;
        [SerializeField] private TMP_Text skipLabel;

        private Action skipHandler;
        private bool inputBound;
        [SerializeField] private Button pauseButton, subtitlesButton;
        [SerializeField] private TMP_Text pauseLabel, subtitleLabel, stateLabel, pageLabel;
        [SerializeField] private RectTransform progressFill, progressTrack, progressHandle;
        private Action pauseHandler, subtitlesHandler;
        private bool chromeBound;

        private void Awake()
        {
            EnsureInputBinding();
            foreach(var label in new[]{skipLabel,pauseLabel,subtitleLabel})
            {
                if(label==null)continue;label.textWrappingMode=TextWrappingModes.Normal;label.overflowMode=TextOverflowModes.Overflow;
                var binding=label.GetComponent<V3LocalizedTextBindingView>()??label.gameObject.AddComponent<V3LocalizedTextBindingView>();
                binding.ConfigureFontBounds(22,22);
            }
        }

        private void OnDestroy()
        {
            if (skipButton != null && inputBound)
                skipButton.onClick.RemoveListener(HandleSkip);
            inputBound = false;
            skipHandler = null;
            if (pauseButton != null) pauseButton.onClick.RemoveListener(HandlePause);
            if (subtitlesButton != null) subtitlesButton.onClick.RemoveListener(HandleSubtitles);
        }

        public void BindSkip(Action handler)
        {
            EnsureInputBinding();
            skipHandler = handler;
        }

        public void UnbindSkip()
        {
            skipHandler = null;
        }

        public void BindTransport(Action onPause, Action onSubtitles)
        {
            pauseHandler = onPause; subtitlesHandler = onSubtitles;
            if (chromeBound) return;
            pauseButton?.onClick.AddListener(HandlePause);
            subtitlesButton?.onClick.AddListener(HandleSubtitles);
            chromeBound = true;
        }

        public void UnbindTransport() { pauseHandler = null; subtitlesHandler = null; }

        public void ApplyTransport(int page, int total, bool paused, bool subtitles)
        {
            if (stateLabel != null) UiLocalizedText.Set(stateLabel,UiShellRuntimeGateway.Localization.Get("ui.narrative.story", "STORY"));
            if (pageLabel != null)
            {
                string pages = $"{page} / {total}";
                if (UiShellRuntimeGateway.Localization.CurrentLocaleCode == Game.UI.Contracts.UiLocaleCodes.Persian)
                    for (int digit = 0; digit < 10; digit++) pages = pages.Replace((char)('0' + digit), (char)('\u06f0' + digit));
                UiLocalizedText.Set(pageLabel,pages);
            }
            if (pauseLabel != null) UiLocalizedText.Set(pauseLabel,UiShellRuntimeGateway.Localization.Get(paused ? "ui.narrative.resume" : "ui.narrative.pause", paused ? "PLAY" : "PAUSE"));
            if (subtitleLabel != null)
            {
                string caption=UiShellRuntimeGateway.Localization.Get(subtitles ? "ui.narrative.subtitles_on" : "ui.narrative.subtitles_off", subtitles ? "CAPTIONS ON" : "CAPTIONS OFF");
                if(UiShellRuntimeGateway.Localization.IsRightToLeft) caption=caption.Replace(" ","\n");
                UiLocalizedText.Set(subtitleLabel,caption);
            }
            if (progressTrack == null) return;
            float width = progressTrack.rect.width * Mathf.Clamp01(total > 0 ? (float)page / total : 0);
            if (progressFill != null) progressFill.sizeDelta = new Vector2(width, progressFill.sizeDelta.y);
            if (progressHandle != null) progressHandle.anchoredPosition = new Vector2(progressTrack.anchoredPosition.x + width, progressHandle.anchoredPosition.y);
        }

        private void HandlePause() => pauseHandler?.Invoke();
        private void HandleSubtitles() => subtitlesHandler?.Invoke();

        public void SetSkipState(bool visible, bool interactable, string accessibleLabel)
        {
            if (skipGroup != null)
            {
                skipGroup.alpha = visible ? 1f : 0f;
                skipGroup.blocksRaycasts = visible && interactable;
                skipGroup.interactable = visible && interactable;
            }

            if (skipButton != null)
                skipButton.interactable = interactable;
            if (skipLabel != null)
                UiLocalizedText.Set(skipLabel,string.IsNullOrWhiteSpace(accessibleLabel)
                    ? UiShellRuntimeGateway.Localization.Get("ui.common.skip", "SKIP")
                    : accessibleLabel);
        }

        public bool HandleBack()
        {
            if(skipButton==null||!skipButton.IsActive()||!skipButton.IsInteractable())return false;
            HandleSkip();return true;
        }

        private void HandleSkip()
        {
            skipHandler?.Invoke();
        }

        private void EnsureInputBinding()
        {
            if (inputBound || skipButton == null)
                return;
            skipButton.onClick.AddListener(HandleSkip);
            inputBound = true;
        }
    }
}
