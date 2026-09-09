using System;
using Game.Configs;
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
        private Button pauseButton, subtitlesButton;
        private TMP_Text pauseLabel, subtitleLabel, stateLabel, pageLabel;
        private RectTransform progressFill, progressTrack, progressHandle;
        private Action pauseHandler, subtitlesHandler;
        private bool chromeBound;

        private void Awake()
        {
            EnsureInputBinding();
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
            pauseButton = transform.Find("PauseButton")?.GetComponent<Button>();
            subtitlesButton = transform.Find("SubtitlesButton")?.GetComponent<Button>();
            pauseLabel = pauseButton?.GetComponentInChildren<TMP_Text>(true);
            subtitleLabel = subtitlesButton?.GetComponentInChildren<TMP_Text>(true);
            Transform timeline = transform.parent?.Find("ComicTimeline");
            stateLabel = timeline?.Find("State")?.GetComponent<TMP_Text>();
            pageLabel = timeline?.Find("Page")?.GetComponent<TMP_Text>();
            progressTrack = timeline?.Find("Track") as RectTransform;
            progressFill = timeline?.Find("Fill") as RectTransform;
            progressHandle = timeline?.Find("Handle") as RectTransform;
            pauseButton?.onClick.AddListener(HandlePause);
            subtitlesButton?.onClick.AddListener(HandleSubtitles);
            chromeBound = true;
        }

        public void UnbindTransport() { pauseHandler = null; subtitlesHandler = null; }

        public void ApplyTransport(int page, int total, bool paused, bool subtitles)
        {
            if (stateLabel != null) stateLabel.text = GameLocalization.Get("ui.narrative.story", "STORY");
            if (pageLabel != null)
            {
                string pages = $"{page} / {total}";
                if (GameLocalization.CurrentLocaleCode == GameLocalization.PersianLocaleCode)
                    for (int digit = 0; digit < 10; digit++) pages = pages.Replace((char)('0' + digit), (char)('\u06f0' + digit));
                pageLabel.text = pages;
            }
            if (pauseLabel != null) pauseLabel.text = GameLocalization.Get(paused ? "ui.narrative.resume" : "ui.narrative.pause", paused ? "PLAY" : "PAUSE");
            if (subtitleLabel != null) subtitleLabel.text = GameLocalization.Get(subtitles ? "ui.narrative.subtitles_on" : "ui.narrative.subtitles_off", subtitles ? "CAPTIONS ON" : "CAPTIONS OFF");
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
                skipLabel.text = string.IsNullOrWhiteSpace(accessibleLabel)
                    ? GameLocalization.Get("ui.common.skip", "SKIP")
                    : accessibleLabel;
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
