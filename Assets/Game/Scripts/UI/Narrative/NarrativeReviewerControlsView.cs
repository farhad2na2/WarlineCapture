using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public enum NarrativeReviewerActionKind
    {
        TogglePlayPause = 0,
        Restart = 1,
        Previous = 2,
        Next = 3,
        Seek = 4,
        SkipToGame = 5,
        JumpToDebrief = 6,
        SetReducedMotion = 7,
        Capture = 8,
        SetSubtitles = 9,
        SetSafeArea = 10
    }

    public readonly struct NarrativeReviewerAction
    {
        public NarrativeReviewerAction(NarrativeReviewerActionKind kind, float position = 0f, bool reducedMotion = false)
        {
            Kind = kind;
            Position = position;
            ReducedMotion = reducedMotion;
        }

        public NarrativeReviewerActionKind Kind { get; }
        public float Position { get; }
        public bool ReducedMotion { get; }
        public bool Enabled => ReducedMotion;
    }

    [DisallowMultipleComponent]
    public sealed class NarrativeReviewerControlsView : MonoBehaviour
    {
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipToGameButton;
        [SerializeField] private Button jumpToDebriefButton;
        [SerializeField] private Button captureButton;
        [SerializeField] private Slider timelineSlider;
        [SerializeField] private TMP_Text playPauseLabel;
        [SerializeField] private TMP_Text stateIdLabel;
        [SerializeField] private TMP_Text positionLabel;
        [SerializeField] private Toggle reducedMotionToggle;
        [SerializeField] private Toggle subtitlesToggle;
        [SerializeField] private Toggle safeAreaToggle;
        [SerializeField] private CanvasGroup visibilityGroup;

        private Action<NarrativeReviewerAction> actionHandler;
        private bool eventsWired;

        private void Awake()
        {
            EnsureEventWiring();
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            ApplyVisibility(false);
#endif
        }

        private void OnDestroy()
        {
            RemoveEventWiring();
            actionHandler = null;
        }

        public void Bind(Action<NarrativeReviewerAction> handler)
        {
            EnsureEventWiring();
            actionHandler = handler;
        }

        public void Unbind()
        {
            actionHandler = null;
        }

        public void SetPlayingState(bool isPlaying)
        {
            if (playPauseLabel != null)
                playPauseLabel.text = isPlaying ? "PAUSE" : "PLAY";
        }

        public void SetPosition(float position)
        {
            SetProgress(position);
        }

        public void SetPosition(float position, string displayText)
        {
            if (timelineSlider != null)
                timelineSlider.SetValueWithoutNotify(Mathf.Clamp(position, timelineSlider.minValue, timelineSlider.maxValue));
            if (positionLabel != null)
                positionLabel.text = displayText ?? string.Empty;
        }

        public void SetProgress(float normalizedPosition)
        {
            if (timelineSlider != null)
                timelineSlider.SetValueWithoutNotify(Mathf.Clamp01(normalizedPosition));
        }

        public void SetState(string stateId, int currentState, int totalStates)
        {
            int safeTotal = Mathf.Max(0, totalStates);
            int safeCurrent = safeTotal == 0 ? 0 : Mathf.Clamp(currentState, 1, safeTotal);

            SetStateId(stateId);
            if (positionLabel != null)
                positionLabel.SetText("{0} / {1}", safeCurrent, safeTotal);
        }

        public void SetStateId(string stateId)
        {
            if (stateIdLabel != null)
                stateIdLabel.text = stateId ?? string.Empty;
        }

        public void SetReducedMotion(bool reducedMotion)
        {
            reducedMotionToggle?.SetIsOnWithoutNotify(reducedMotion);
        }

        public void SetSubtitles(bool visible) => subtitlesToggle?.SetIsOnWithoutNotify(visible);
        public void SetSafeArea(bool visible) => safeAreaToggle?.SetIsOnWithoutNotify(visible);

        public void SetDevelopmentVisibility(bool visible)
        {
            ApplyVisibility(visible && IsDevelopmentContext);
        }

        public void SetNavigationState(bool hasPrevious, bool hasNext)
        {
            if (previousButton != null)
                previousButton.interactable = hasPrevious;
            if (nextButton != null)
                nextButton.interactable = hasNext;
        }

        public void SetVisible(bool visible)
        {
            SetDevelopmentVisibility(visible);
        }

        private static bool IsDevelopmentContext
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        private void ApplyVisibility(bool visible)
        {
            if (visibilityGroup == null)
                return;

            visibilityGroup.alpha = visible ? 1f : 0f;
            visibilityGroup.interactable = visible;
            visibilityGroup.blocksRaycasts = visible;
        }

        private void EnsureEventWiring()
        {
            if (eventsWired)
                return;

            playPauseButton?.onClick.AddListener(HandlePlayPause);
            restartButton?.onClick.AddListener(HandleRestart);
            previousButton?.onClick.AddListener(HandlePrevious);
            nextButton?.onClick.AddListener(HandleNext);
            skipToGameButton?.onClick.AddListener(HandleSkipToGame);
            jumpToDebriefButton?.onClick.AddListener(HandleJumpToDebrief);
            captureButton?.onClick.AddListener(HandleCapture);
            timelineSlider?.onValueChanged.AddListener(HandleSeek);
            reducedMotionToggle?.onValueChanged.AddListener(HandleReducedMotionChanged);
            subtitlesToggle?.onValueChanged.AddListener(HandleSubtitlesChanged);
            safeAreaToggle?.onValueChanged.AddListener(HandleSafeAreaChanged);
            eventsWired = true;
        }

        private void RemoveEventWiring()
        {
            if (!eventsWired)
                return;

            playPauseButton?.onClick.RemoveListener(HandlePlayPause);
            restartButton?.onClick.RemoveListener(HandleRestart);
            previousButton?.onClick.RemoveListener(HandlePrevious);
            nextButton?.onClick.RemoveListener(HandleNext);
            skipToGameButton?.onClick.RemoveListener(HandleSkipToGame);
            jumpToDebriefButton?.onClick.RemoveListener(HandleJumpToDebrief);
            captureButton?.onClick.RemoveListener(HandleCapture);
            timelineSlider?.onValueChanged.RemoveListener(HandleSeek);
            reducedMotionToggle?.onValueChanged.RemoveListener(HandleReducedMotionChanged);
            subtitlesToggle?.onValueChanged.RemoveListener(HandleSubtitlesChanged);
            safeAreaToggle?.onValueChanged.RemoveListener(HandleSafeAreaChanged);
            eventsWired = false;
        }

        private void HandlePlayPause() => Emit(NarrativeReviewerActionKind.TogglePlayPause);
        private void HandleRestart() => Emit(NarrativeReviewerActionKind.Restart);
        private void HandlePrevious() => Emit(NarrativeReviewerActionKind.Previous);
        private void HandleNext() => Emit(NarrativeReviewerActionKind.Next);
        private void HandleSkipToGame() => Emit(NarrativeReviewerActionKind.SkipToGame);
        private void HandleJumpToDebrief() => Emit(NarrativeReviewerActionKind.JumpToDebrief);
        private void HandleCapture() => Emit(NarrativeReviewerActionKind.Capture);
        private void HandleSeek(float position) => Emit(NarrativeReviewerActionKind.Seek, position);
        private void HandleReducedMotionChanged(bool reducedMotion) =>
            Emit(NarrativeReviewerActionKind.SetReducedMotion, reducedMotion: reducedMotion);
        private void HandleSubtitlesChanged(bool visible) => Emit(NarrativeReviewerActionKind.SetSubtitles, reducedMotion: visible);
        private void HandleSafeAreaChanged(bool visible) => Emit(NarrativeReviewerActionKind.SetSafeArea, reducedMotion: visible);

        private void Emit(NarrativeReviewerActionKind kind, float position = 0f, bool reducedMotion = false)
        {
            actionHandler?.Invoke(new NarrativeReviewerAction(kind, position, reducedMotion));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (playPauseButton == null || restartButton == null || previousButton == null || nextButton == null ||
                skipToGameButton == null || jumpToDebriefButton == null || timelineSlider == null ||
                playPauseLabel == null || stateIdLabel == null || positionLabel == null ||
                reducedMotionToggle == null || subtitlesToggle == null || safeAreaToggle == null || visibilityGroup == null)
            {
                Debug.LogWarning($"[{nameof(NarrativeReviewerControlsView)}] Missing required serialized reference on {name}.", this);
            }
        }
#endif
    }
}
