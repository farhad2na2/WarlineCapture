using System;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private const float M02DoItReadyTimeoutSeconds = 3f;
        private const byte RecommendationSelect = 1;
        private const byte RecommendationBuild = 4;
        private const byte RecommendationProduce = 5;
        private const byte RecommendationExplain = 9;
        private const byte TargetWorldPosition = 3;
        private const byte TargetUiSurface = 4;
        private Func<bool> _executeBuildingPlacementStep;
        private RectTransform _boundResourceStrip;
        private byte _pendingM02DoItStep;
        private float _pendingM02DoItUntilUnscaledTime = -1f;

        private readonly struct RebindState
        {
            public readonly bool Preserve;
            public readonly UiAssistantPanelModel Panel;
            public readonly UiAssistantHighlightModel Highlight;
            public readonly TacticalCommandMode CommandMode;
            public readonly bool WorldTargetCompleted;
            public readonly Func<bool> ExecuteBuildingPlacementStep;
            public readonly RectTransform ResourceStrip;
            public readonly byte PendingDoItStep;
            public readonly float PendingDoItUntil;
            public readonly byte PendingTutorialStep;
            public readonly byte CompletedTutorialStep;
            public readonly byte DisplayedTutorialStep;
            public readonly UiTutorialNarrationPhase PendingTutorialPhase;
            public readonly UiTutorialNarrationPhase DisplayedTutorialPhase;
            public readonly ushort NarratedTutorialCues;
            public readonly ushort AutoShownTutorialCues;
            public readonly float TutorialShowAt;
            public readonly bool TutorialCinematicSuspended;
            public readonly bool FinalTutorialSuppressed;

            public RebindState(MatchHudAssistantUiSystemHelper owner, bool preserve)
            {
                Preserve = preserve;
                Panel = owner._lastPanelModel;
                Highlight = owner._lastHighlightModel;
                CommandMode = owner._activeCommandMode;
                WorldTargetCompleted = owner._tutorialWorldTargetCompleted;
                ExecuteBuildingPlacementStep = owner._executeBuildingPlacementStep;
                ResourceStrip = owner._boundResourceStrip;
                PendingDoItStep = owner._pendingM02DoItStep;
                PendingDoItUntil = owner._pendingM02DoItUntilUnscaledTime;
                PendingTutorialStep = owner._pendingTutorialStep;
                CompletedTutorialStep = owner._completedTutorialStep;
                DisplayedTutorialStep = owner._displayedTutorialStep;
                PendingTutorialPhase = owner._pendingTutorialPhase;
                DisplayedTutorialPhase = owner._displayedTutorialPhase;
                NarratedTutorialCues = owner._narratedTutorialCues;
                AutoShownTutorialCues = owner._autoShownTutorialCues;
                TutorialShowAt = owner._tutorialShowAtUnscaledTime;
                TutorialCinematicSuspended = owner._tutorialCinematicSuspended;
                FinalTutorialSuppressed = owner._finalTutorialSuppressed;
            }
        }

        private RebindState CaptureRebindState(bool preserve) => new(this, preserve);

        private void RestoreRebindState(in RebindState state)
        {
            if (!state.Preserve)
                return;

            _lastPanelModel = state.Panel;
            _lastHighlightModel = state.Highlight;
            _activeCommandMode = state.CommandMode;
            _tutorialWorldTargetCompleted = state.WorldTargetCompleted;
            _executeBuildingPlacementStep = state.ExecuteBuildingPlacementStep;
            _boundResourceStrip = state.ResourceStrip;
            _pendingM02DoItStep = state.PendingDoItStep;
            _pendingM02DoItUntilUnscaledTime = state.PendingDoItUntil;
            _pendingTutorialStep = state.PendingTutorialStep;
            _completedTutorialStep = state.CompletedTutorialStep;
            _displayedTutorialStep = state.DisplayedTutorialStep;
            _pendingTutorialPhase = state.PendingTutorialPhase;
            _displayedTutorialPhase = state.DisplayedTutorialPhase;
            _narratedTutorialCues = state.NarratedTutorialCues;
            _autoShownTutorialCues = state.AutoShownTutorialCues;
            _tutorialShowAtUnscaledTime = state.TutorialShowAt;
            _tutorialCinematicSuspended = state.TutorialCinematicSuspended;
            _finalTutorialSuppressed = state.FinalTutorialSuppressed;
        }

        public void BindResourceStrip(RectTransform resourceStrip)
        {
            _boundResourceStrip = resourceStrip;
            _highlightPresentationSystem.BindResourceStrip(resourceStrip);
        }

        public void BindBuildingPlacementStepExecutor(Func<bool> executeBuildingPlacementStep) =>
            _executeBuildingPlacementStep = executeBuildingPlacementStep;

        internal static bool IsM02DoItStep(in UiAssistantPanelModel model) =>
            model.TutorialStepCount == 9 && (model.TutorialStep switch
            {
                2 => model.RecommendationKind == RecommendationBuild &&
                     model.RecommendationTargetKind == TargetUiSurface,
                3 => model.RecommendationKind == RecommendationSelect &&
                     model.RecommendationTargetKind == TargetUiSurface,
                4 => model.RecommendationKind == RecommendationBuild &&
                     model.RecommendationTargetKind == TargetWorldPosition,
                5 => model.RecommendationKind == RecommendationExplain &&
                     model.RecommendationTargetKind == TargetUiSurface,
                6 => model.RecommendationKind == RecommendationProduce &&
                     model.RecommendationTargetKind == TargetUiSurface,
                _ => false
            });

        internal static UiCampaignGuidanceTargetKind ResolveM02AcknowledgementTarget(
            byte recommendationKind) => recommendationKind switch
        {
            RecommendationBuild => UiCampaignGuidanceTargetKind.BuildButton,
            RecommendationSelect => UiCampaignGuidanceTargetKind.BarracksCatalogItem,
            RecommendationExplain => UiCampaignGuidanceTargetKind.ResourceStrip,
            RecommendationProduce => UiCampaignGuidanceTargetKind.RifleProduction,
            _ => UiCampaignGuidanceTargetKind.None
        };

        private bool TryExecuteM02DoIt(in UiAssistantPanelModel model)
        {
            if (model.TutorialStep == 4)
                return _executeBuildingPlacementStep?.Invoke() == true;
            return _highlightPresentationSystem.TryExecuteUiSurface(
                model.RecommendationKind,
                model.RecommendationTargetKind);
        }

        private void QueueM02DoItRetry(byte step, float unscaledTime)
        {
            _pendingM02DoItStep = step;
            _pendingM02DoItUntilUnscaledTime = unscaledTime + M02DoItReadyTimeoutSeconds;
        }

        private void TickPendingM02DoIt(float unscaledTime)
        {
            if (_pendingM02DoItStep == 0)
                return;
            if (_lastPanelModel.TutorialStep != _pendingM02DoItStep ||
                !IsM02DoItStep(in _lastPanelModel) ||
                unscaledTime > _pendingM02DoItUntilUnscaledTime)
            {
                ClearPendingM02DoIt();
                return;
            }
            if (!TryExecuteM02DoIt(in _lastPanelModel))
                return;

            ClearPendingM02DoIt();
            ClosePanelWithoutInputCapture();
        }

        private void ClearPendingM02DoIt()
        {
            _pendingM02DoItStep = 0;
            _pendingM02DoItUntilUnscaledTime = -1f;
        }

        public bool IsBuildDrawerSelectionGuidance =>
            _lastPanelModel.HasRecommendation &&
            ((_lastPanelModel.RecommendationTargetKind == 4 &&
              _lastPanelModel.RecommendationKind == 1) ||
             (_lastPanelModel.TutorialStepCount == 9 &&
              _lastPanelModel.TutorialStep == 4 &&
              _lastPanelModel.RecommendationKind == 4));
    }
}
