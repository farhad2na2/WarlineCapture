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
            _buildDrawerOpenRequested = false;
            _buildDrawerView = buildDrawerView;
            _buildDrawerCatalogRuntimeView =
                _buildDrawerView != null
                    ? _buildDrawerView.GetComponent<BuildDrawerCatalogRuntimeView>()
                    : null;
            ResolveBarracksGuidanceButton();
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
                if (!EnsureBuildDrawerOpen())
                    return false;

                if (_buildDrawerCatalogRuntimeView == null ||
                    !_buildDrawerCatalogRuntimeView.TryInvokeRifleProductionFromGuidance())
                {
                    return false;
                }

                ClearUiSurfaceCue();
                _uiSurfaceAcknowledged?.Invoke(ProduceRecommendationKind);
                return true;
            }
            if (recommendationKind == SelectRecommendationKind && !EnsureBuildDrawerOpen())
                return false;
            Button target = recommendationKind switch
            {
                BuildRecommendationKind => _buildGuidanceButton,
                SelectRecommendationKind => ResolveBarracksGuidanceButton(),
                _ => null
            };
            if (target == null || !target.IsActive() || !target.IsInteractable())
                return false;
            target.onClick.Invoke();
            return true;
        }

        private bool EnsureBuildDrawerOpen()
        {
            if (_buildDrawerView != null && _buildDrawerView.IsOpen)
            {
                _buildDrawerOpenRequested = false;
                return true;
            }
            if (_buildGuidanceButton == null || !_buildGuidanceButton.IsActive() ||
                !_buildGuidanceButton.IsInteractable() || _buildDrawerOpenRequested)
            {
                return false;
            }

            _buildGuidanceButton.onClick.Invoke();
            bool open = _buildDrawerView != null && _buildDrawerView.IsOpen;
            _buildDrawerOpenRequested = !open;
            return open;
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
                    SelectRecommendationKind => ResolveBarracksGuidanceButton(),
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

        private Button ResolveBarracksGuidanceButton()
        {
            Button resolved = _buildDrawerCatalogRuntimeView != null
                ? _buildDrawerCatalogRuntimeView.ResolveBarracksGuidanceButton()
                : _buildDrawerView?.ItemTemplate?.SelectionButton;
            if (resolved == _barracksGuidanceButton)
                return resolved;

            _barracksGuidanceButton?.onClick.RemoveListener(AcknowledgeBarracksSelection);
            _barracksGuidanceButton = resolved;
            _barracksGuidanceButton?.onClick.AddListener(AcknowledgeBarracksSelection);
            return resolved;
        }

        private static string ResolveIndicatorText(UiAssistantHighlightModel model, bool commandCue)
        {
            if (model.TargetKind == UiSurfaceTargetKind &&
                model.RecommendationKind == BuildRecommendationKind)
                return "OPEN BUILD";
            if (model.TargetKind == UiSurfaceTargetKind &&
                model.RecommendationKind == SelectRecommendationKind)
                return "SELECT BARRACKS";
            if (model.TargetKind == UiSurfaceTargetKind &&
                model.RecommendationKind == ExplainRecommendationKind)
                return "RESOURCE SPEND";
            if (model.TargetKind == UiSurfaceTargetKind &&
                model.RecommendationKind == ProduceRecommendationKind)
                return "QUEUE RIFLE";
            if (model.RecommendationKind == SelectRecommendationKind)
                return "SELECT SQUAD";
            if (model.RecommendationKind == MoveRecommendationKind)
                return commandCue ? "PRESS MOVE" : "CLICK DESTINATION";
            if (model.RecommendationKind == AttackRecommendationKind)
                return commandCue ? "PRESS ATTACK" : "CLICK ENEMY";
            return "ARIA TARGET";
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
            _buildDrawerOpenRequested = false;
        }

        private void DetachResourceGuidanceTarget()
        {
            _resourceGuidanceTarget = null;
        }
    }
}
