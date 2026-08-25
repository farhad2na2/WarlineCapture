using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private Func<bool> _executeBuildingPlacementStep;
        private RectTransform _boundResourceStrip;

        public void BindResourceStrip(RectTransform resourceStrip)
        {
            _boundResourceStrip = resourceStrip;
            _highlightPresentationSystem.BindResourceStrip(resourceStrip);
        }

        public void BindBuildingPlacementStepExecutor(Func<bool> executeBuildingPlacementStep) =>
            _executeBuildingPlacementStep = executeBuildingPlacementStep;

        public bool IsBuildDrawerSelectionGuidance =>
            _lastPanelModel.HasRecommendation &&
            ((_lastPanelModel.RecommendationTargetKind == 4 &&
              _lastPanelModel.RecommendationKind == 1) ||
             (_lastPanelModel.TutorialStepCount == 9 &&
              _lastPanelModel.TutorialStep == 4 &&
              _lastPanelModel.RecommendationKind == 4));
    }
}
