using Game.Narrative.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEngine;

namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativeReviewPresentationSystemHelper
    {
        private FirstLaunchNarrativeSequencePresentationSystemHelper sequencePresentation;
        private NarrativeSequenceView view;
        private int lastStateIndex = -2;
        private bool lastPaused;
        private bool lastReducedMotion;
        private bool lastSubtitles;
        private bool safeArea;
        private NarrativeCompletionPayload lastCompletion;

        public bool IsEnabled { get; private set; }
        public NarrativeCompletionPayload LastCompletion => lastCompletion;

        public void Initialize(
            bool enabled,
            NarrativeSequenceView sequenceView,
            FirstLaunchNarrativeSequencePresentationSystemHelper presentation)
        {
            IsEnabled = enabled;
            view = sequenceView;
            sequencePresentation = presentation;
            if (view?.ReviewerControlsView == null)
                return;

            view.ReviewerControlsView.Bind(HandleAction);
            view.ReviewerControlsView.SetDevelopmentVisibility(enabled);
            Refresh(true);
        }

        public void Tick()
        {
            Refresh();
        }

        public void RecordHandoff(in NarrativeHandoffResult result, string nextStateId)
        {
            if (!IsEnabled)
                return;

            lastCompletion = result.Completion;
            if (!string.IsNullOrEmpty(nextStateId))
                sequencePresentation.StartAt(nextStateId);
            Refresh(true);
        }

        public void Refresh(bool force = false)
        {
            if (!IsEnabled || view?.ReviewerControlsView == null || sequencePresentation == null)
                return;

            int index = sequencePresentation.CurrentStateIndex;
            bool paused = sequencePresentation.IsPaused;
            bool reducedMotion = sequencePresentation.ReducedMotionEnabled;
            bool subtitles = sequencePresentation.SubtitlesEnabled;
            if (!force && index == lastStateIndex && paused == lastPaused &&
                reducedMotion == lastReducedMotion && subtitles == lastSubtitles)
            {
                return;
            }

            int total = sequencePresentation.StateCount;
            view.ReviewerControlsView.SetPlayingState(sequencePresentation.IsRunning && !paused);
            view.ReviewerControlsView.SetState(
                sequencePresentation.CurrentStateId,
                index >= 0 ? index + 1 : 0,
                total);
            view.ReviewerControlsView.SetProgress(index >= 0 && total > 1 ? (float)index / (total - 1) : 0f);
            view.ReviewerControlsView.SetReducedMotion(reducedMotion);
            view.ReviewerControlsView.SetSubtitles(subtitles);
            view.ReviewerControlsView.SetSafeArea(safeArea);
            view.ReviewerControlsView.SetNavigationState(index > 0, index >= 0 && index + 1 < total);
            lastStateIndex = index;
            lastPaused = paused;
            lastReducedMotion = reducedMotion;
            lastSubtitles = subtitles;
        }

        public void Shutdown()
        {
            view?.ReviewerControlsView?.Unbind();
            view?.ReviewerControlsView?.SetDevelopmentVisibility(false);
            view?.SetSafeAreaPreview(false);
            IsEnabled = false;
            sequencePresentation = null;
            view = null;
            lastStateIndex = -2;
            lastPaused = false;
            lastReducedMotion = false;
            lastSubtitles = false;
            safeArea = false;
            lastCompletion = default;
        }

        private void HandleAction(NarrativeReviewerAction action)
        {
            if (!IsEnabled)
                return;

            switch (action.Kind)
            {
                case NarrativeReviewerActionKind.TogglePlayPause:
                    if (!sequencePresentation.IsRunning)
                        sequencePresentation.Restart();
                    else if (sequencePresentation.IsPaused)
                        sequencePresentation.Resume();
                    else
                        sequencePresentation.Pause();
                    break;
                case NarrativeReviewerActionKind.Restart:
                    sequencePresentation.Restart();
                    break;
                case NarrativeReviewerActionKind.Previous:
                    sequencePresentation.PreviousState();
                    break;
                case NarrativeReviewerActionKind.Next:
                    sequencePresentation.NextState();
                    break;
                case NarrativeReviewerActionKind.Seek:
                    sequencePresentation.SeekNormalized(action.Position);
                    break;
                case NarrativeReviewerActionKind.SkipToGame:
                    sequencePresentation.StartAt(
                        sequencePresentation.FindStateId(NarrativeRouteRole.ReviewerGameplay));
                    break;
                case NarrativeReviewerActionKind.JumpToDebrief:
                    sequencePresentation.StartAt(
                        sequencePresentation.FindStateId(NarrativeRouteRole.DebriefOpening));
                    break;
                case NarrativeReviewerActionKind.SetReducedMotion:
                    sequencePresentation.SetReducedMotion(action.ReducedMotion);
                    break;
                case NarrativeReviewerActionKind.SetSubtitles:
                    sequencePresentation.SetSubtitlesEnabled(action.Enabled);
                    break;
                case NarrativeReviewerActionKind.SetSafeArea:
                    safeArea = action.Enabled;
                    view?.SetSafeAreaPreview(safeArea);
                    break;
                case NarrativeReviewerActionKind.Capture:
                    Debug.Log($"[FirstLaunchNarrativeReviewer] Capture requested at {sequencePresentation.CurrentStateId}.");
                    break;
            }
            Refresh(true);
        }
    }
}
