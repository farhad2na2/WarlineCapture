using System;
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
