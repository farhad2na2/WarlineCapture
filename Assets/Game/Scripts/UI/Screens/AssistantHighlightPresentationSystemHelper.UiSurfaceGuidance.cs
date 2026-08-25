using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed partial class AssistantHighlightPresentationSystemHelper
    {
        private const byte ExplainRecommendationKind = 9;
        private RectTransform _resourceGuidanceTarget;

        public void BindBuildButton(Button buildButton)
        {
            DetachBuildGuidanceButton();
            _buildGuidanceButton = buildButton;
            _buildGuidanceButton?.onClick.AddListener(AcknowledgeBuildButton);
            ApplyVisual(LastAppliedModel);
        }

        public void BindBuildDrawer(BuildDrawerView buildDrawerView)
        {
            DetachBarracksGuidanceButton();
            _buildDrawerView = buildDrawerView;
            _buildDrawerCatalogRuntimeView =
                _buildDrawerView != null
                    ? _buildDrawerView.GetComponent<BuildDrawerCatalogRuntimeView>()
                    : null;
            _barracksGuidanceButton = _buildDrawerView?.ItemTemplate?.SelectionButton;
            _barracksGuidanceButton?.onClick.AddListener(AcknowledgeBarracksSelection);
            ApplyVisual(LastAppliedModel);
        }

        public void BindResourceStrip(RectTransform resourceStrip)
        {
            _resourceGuidanceTarget = resourceStrip;
            ApplyVisual(LastAppliedModel);
        }

        public bool TryExecuteUiSurface(byte recommendationKind, byte targetKind)
        {
            if (targetKind != UiSurfaceTargetKind)
                return false;
            if (recommendationKind == ExplainRecommendationKind)
            {
                if (_resourceGuidanceTarget == null || !_resourceGuidanceTarget.gameObject.activeInHierarchy)
                    return false;
                AcknowledgeResourceSpend();
                return true;
            }
            if (recommendationKind == ProduceRecommendationKind)
            {
                if ((_buildDrawerView == null || !_buildDrawerView.IsOpen) &&
                    _buildGuidanceButton != null && _buildGuidanceButton.IsActive() &&
                    _buildGuidanceButton.IsInteractable())
                {
                    _buildGuidanceButton.onClick.Invoke();
                }

                if (_buildDrawerCatalogRuntimeView == null ||
                    !_buildDrawerCatalogRuntimeView.TryInvokeRifleProductionFromGuidance())
                {
                    return false;
                }

                ClearUiSurfaceCue();
                _uiSurfaceAcknowledged?.Invoke(ProduceRecommendationKind);
                return true;
            }
            Button target = recommendationKind switch
            {
                BuildRecommendationKind => _buildGuidanceButton,
                SelectRecommendationKind => _barracksGuidanceButton,
                _ => null
            };
            if (target == null || !target.IsActive() || !target.IsInteractable())
                return false;
            target.onClick.Invoke();
            return true;
        }

        public void ClearUiSurfaceCue()
        {
            _localUiCueActive = false;
            _pendingFirstShowMe = false;
            if (LastAppliedModel.TargetKind == UiSurfaceTargetKind)
            {
                LastAppliedModel = UiAssistantHighlightModel.Empty;
                ApplyVisual(LastAppliedModel);
            }
        }

        private RectTransform ResolveGuidedCommandButton(UiAssistantHighlightModel model)
        {
            if (model.TargetKind == UiSurfaceTargetKind)
            {
                Button uiButton = model.RecommendationKind switch
                {
                    BuildRecommendationKind => _buildGuidanceButton,
                    SelectRecommendationKind => _barracksGuidanceButton,
                    _ => null
                };
                if (model.RecommendationKind == ProduceRecommendationKind)
                {
                    return _buildDrawerCatalogRuntimeView?.ResolveRifleProductionGuidanceTarget() ??
                           (_buildGuidanceButton != null
                               ? _buildGuidanceButton.transform as RectTransform
                               : null);
                }
                if (model.RecommendationKind == ExplainRecommendationKind)
                    return _resourceGuidanceTarget;
                return uiButton != null ? uiButton.transform as RectTransform : null;
            }
            if (model.RecommendationKind == SelectRecommendationKind)
                return _squadTrayView?.AssistantGuidanceTarget;

            Button button = model.RecommendationKind == MoveRecommendationKind
                ? _commandControlsView != null ? _commandControlsView.MoveButton : null
                : model.RecommendationKind == AttackRecommendationKind
                    ? _commandControlsView != null ? _commandControlsView.AttackButton : null
                    : null;
            return button != null ? button.transform as RectTransform : null;
        }

        private static string ResolveIndicatorText(UiAssistantHighlightModel model, bool commandCue)
        {
            if (model.TargetKind == UiSurfaceTargetKind &&
                model.RecommendationKind == BuildRecommendationKind)
                return "OPEN BUILD\n\u25bc";
            if (model.TargetKind == UiSurfaceTargetKind &&
                model.RecommendationKind == SelectRecommendationKind)
                return "SELECT BARRACKS\n\u25bc";
            if (model.TargetKind == UiSurfaceTargetKind &&
                model.RecommendationKind == ExplainRecommendationKind)
                return "RESOURCE SPEND\n\u25bc";
            if (model.TargetKind == UiSurfaceTargetKind &&
                model.RecommendationKind == ProduceRecommendationKind)
                return "QUEUE RIFLE\n\u25bc";
            if (model.RecommendationKind == SelectRecommendationKind)
                return "SELECT SQUAD\n\u25bc";
            if (model.RecommendationKind == MoveRecommendationKind)
                return commandCue ? "PRESS MOVE\n\u25bc" : "CLICK DESTINATION\n\u25bc";
            if (model.RecommendationKind == AttackRecommendationKind)
                return commandCue ? "PRESS ATTACK\n\u25bc" : "CLICK ENEMY\n\u25bc";
            return "ARIA TARGET\n\u25bc";
        }

        private void AcknowledgeBuildButton()
        {
            if (!MatchesUiSurface(BuildRecommendationKind))
                return;
            ClearUiSurfaceCue();
            _uiSurfaceAcknowledged?.Invoke(BuildRecommendationKind);
        }

        private void AcknowledgeBarracksSelection()
        {
            if (!MatchesUiSurface(SelectRecommendationKind))
                return;
            ClearUiSurfaceCue();
            _uiSurfaceAcknowledged?.Invoke(SelectRecommendationKind);
        }

        private void AcknowledgeResourceSpend()
        {
            if (!MatchesUiSurface(ExplainRecommendationKind))
                return;
            ClearUiSurfaceCue();
            _uiSurfaceAcknowledged?.Invoke(ExplainRecommendationKind);
        }

        private bool MatchesUiSurface(byte recommendationKind) =>
            LastAppliedModel.Active && LastAppliedModel.TargetKind == UiSurfaceTargetKind &&
            LastAppliedModel.RecommendationKind == recommendationKind;

        private void DetachBuildGuidanceButton()
        {
            _buildGuidanceButton?.onClick.RemoveListener(AcknowledgeBuildButton);
            _buildGuidanceButton = null;
        }

        private void DetachBarracksGuidanceButton()
        {
            _barracksGuidanceButton?.onClick.RemoveListener(AcknowledgeBarracksSelection);
            _barracksGuidanceButton = null;
            _buildDrawerView = null;
            _buildDrawerCatalogRuntimeView = null;
        }

        private void DetachResourceGuidanceTarget()
        {
            _resourceGuidanceTarget = null;
        }
    }
}
