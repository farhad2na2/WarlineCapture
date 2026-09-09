using UnityEngine;
using Game.UI.Contracts;
using Game.Tactical.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class UIShellContentView
    {
        private void BindMatchHudSelectionPanel(MatchHudSelectionPanelView view)
        {
            view?.HideSelection();
            _mainMenuPlayUi?.BindMatchHudSelectionPanel(view);
            _bindMatchHudSelectionPanel?.Invoke(view);
        }

        private void BindMatchHudFooter(MatchHudFooterContentView footer)
        {
            _matchHudCommandControlsView = footer != null ? footer.CommandControls : null;
            CommandWheelPanelView commandWheel = _matchHudCommandControlsView != null
                ? _matchHudCommandControlsView.CommandWheelPanel
                : null;
            MatchHudHeaderReferenceUiSystemHelper headerReferences = _matchHudHeaderContent != null
                ? MatchHudHeaderReferenceUiSystemHelper.Create(_matchHudHeaderContent.transform)
                : null;
            commandWheel?.BindRuntimeSectionReferences(
                _matchHudSelectionPanelView != null ? _matchHudSelectionPanelView.CommandWheelOpenButton : null,
                headerReferences?.ThreatJumpPanel != null ? headerReferences.ThreatJumpPanel.gameObject : null);
            if (footer != null && footer.RuntimeFeedback != null)
                footer.RuntimeFeedback.BindCurrentOrderBanner(_matchHudCurrentOrderBannerView);
            BindMatchHudCommandControls(_matchHudCommandControlsView);
            BindMatchHudRuntimeFeedback(footer != null ? footer.RuntimeFeedback : null);
            BindMatchHudMinimap(ResolveInstalledMinimap(footer));
            BindMatchHudSquadTray(footer != null ? footer.SquadTray : null);
        }

        private void BindMatchHudRightQuickRail(MatchHudRightQuickRailView view)
        {
            UnbindRightQuickRailBuildButton();
            TryBindMatchHudRightQuickRailView(view);
        }

        private void UnbindRightQuickRailBuildButton()
        {
            _rightQuickRailView?.UnbindBuildCommand();
            _mainMenuPlayUi?.BindMatchHudRightQuickRail(null);
            _rightQuickRailView = null;
            _rightQuickRailBuildButton = null;
        }

        private void OpenBuildDrawerFromRightQuickRail()
        {
            _selectionUiCommandSystem?.CaptureUiClickSequence();
            if (_buildDrawerPopupInstance != null)
            {
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyStickyCommandMode(ResolveMatchHudRuntimeFeedback(), TacticalCommandMode.Build, _gameTextResolver);
                return;
            }

            GameObject popup = InstallBuildDrawerPopup();
            if (popup == null)
            {
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(ResolveMatchHudRuntimeFeedback(), TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.BuildUnavailable,
                    _gameTextResolver.Get("build.feedback.drawer_not_ready", "Build drawer is not ready.")), _gameTextResolver);
                return;
            }

            BattleHudRuntimeFeedbackUiSystemHelper.ApplyStickyCommandMode(ResolveMatchHudRuntimeFeedback(), TacticalCommandMode.Build, _gameTextResolver);
        }

        private void BindBuildPlacementConfirmationBarInRegion()
        {
            RectTransform contentRoot = shellView != null ? shellView.transform as RectTransform : null;
            if (contentRoot == null)
                return;

            BindBuildPlacementConfirmationBar(contentRoot.gameObject);
        }

        private void BindMatchHudCommandControls(MatchOverlayCommandControlsView view)
        {
            if (view != null)
            {
                _matchOverlayCommandInputSystem.Bind(
                    view,
                    _selectionUiCommandSystem,
                    _matchHudFooterContentView != null ? _matchHudFooterContentView.RuntimeFeedback : null,
                    () => InstallBuildDrawerPopup(),
                    CloseBuildDrawerPopup,
                    _selectionDiagnosticsSink,
                    _selectionUiReadModelSystem,
                    _mainMenuPlayUi != null
                        ? new System.Action(_mainMenuPlayUi.CaptureGameplayUiClickSequence)
                        : null,
                    _gameTextResolver);
                _mainMenuPlayUi?.BindMatchHudCommandControls(view);
                RefreshMatchHudCommandControlState();
            }
        }

        private void BindMatchHudRuntimeFeedback(BattleHudRuntimeFeedbackView view)
        {
            if (view != null)
            {
                _mainMenuPlayUi?.BindMatchHudRuntimeFeedback(view);
            }
            else
            {
                _mainMenuPlayUi?.BindMatchHudRuntimeFeedback(null);
            }
        }

        private void BindMatchHudMinimap(MatchHudMinimapView view)
        {
            _mainMenuPlayUi?.BindMatchHudMinimap(view);
        }

        private void BindMatchHudSquadTray(MatchHudSquadTrayView view)
        {
            if (view != null)
                _mainMenuPlayUi?.BindMatchHudSquadTray(view);
        }

        private void BindBuildPlacementConfirmationBar(GameObject footer)
        {
            RectTransform parent = shellView != null ? shellView.transform as RectTransform : null;
            if (parent == null)
                parent = footer != null ? footer.transform as RectTransform : null;
            if (parent == null)
                return;

            _buildPlacementConfirmationBarView = BuildPlacementConfirmationBarView.Ensure(buildPlacementConfirmationBarPrefab, parent);
            if (_buildPlacementConfirmationBarView == null)
                return;

            _buildPlacementConfirmationBarView.transform.SetAsLastSibling();
            _buildPlacementConfirmationBarView?.BindRuntimeCommands(
                _buildingUiCommandSystem,
                ResolveMatchHudRuntimeFeedback(),
                _gameTextResolver);
            _mainMenuPlayUi?.BindBuildPlacementConfirmationBar(_buildPlacementConfirmationBarView);
        }

    }
}
