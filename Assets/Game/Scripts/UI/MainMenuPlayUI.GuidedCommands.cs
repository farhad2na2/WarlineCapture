using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MainMenuPlayUI
    {
        private readonly GuidedHudRuntime _guidedHudRuntime = new();
        private UnityEngine.Camera _guidanceWorldCamera;
        private UnityEngine.RectTransform _guidedResourceStrip;

        private void TickGuidedHudRuntime()
        {
            UnityEngine.Camera activeCamera = _selectionUiCameraSystem?.WorldCamera;
            if (activeCamera != null)
                _guidanceWorldCamera = activeCamera;
            _matchHudAssistantUiSystem.BindWorldCamera(_guidanceWorldCamera);
            UnityEngine.RectTransform resourceStrip =
                _matchHudHeaderReferences?.ResourceStrip as UnityEngine.RectTransform;
            if (_guidedResourceStrip != resourceStrip)
            {
                _guidedResourceStrip = resourceStrip;
                _matchHudAssistantUiSystem.BindResourceStrip(_guidedResourceStrip);
            }
            _guidedHudRuntime.Tick(this, _matchHudCommandControlsView);
        }

        private void DisposeGuidedHudRuntime()
        {
            _guidedHudRuntime.Dispose();
            _guidanceWorldCamera = null;
            _guidedResourceStrip = null;
            _matchHudAssistantUiSystem.BindResourceStrip(null);
            _matchHudAssistantUiSystem.BindBuildingPlacementStepExecutor(null);
        }

        public void BindGuidedHudRuntime(UIShellContentView shellContent)
        {
            _matchHudAssistantUiSystem.BindBuildingPlacementStepExecutor(
                TryExecuteGuidedBuildingPlacement);
            if (!_guidedHudRuntime.BindShellContent(shellContent))
                return;

            _matchHudAssistantUiSystem.ResetForMissionAttempt();
            _matchHudSquadTrayView?.ClearActiveSlot();
        }

        public void BindGuidanceWorldCamera(UnityEngine.Camera worldCamera)
        {
            _guidanceWorldCamera = worldCamera;
            _matchHudAssistantUiSystem.BindWorldCamera(worldCamera);
        }

        public void BindMatchHudCommandControls(
            MatchOverlayCommandControlsView commandControlsView)
        {
            _matchHudCommandControlsView = commandControlsView;
            _matchHudAssistantUiSystem.BindCommandControls(commandControlsView);
        }

        public void ApplyMatchHudCommandMode(TacticalCommandMode mode)
        {
            _matchHudAssistantUiSystem.ApplyCommandMode(mode);
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(
                _matchHudRuntimeFeedbackView, mode, _gameTextResolver);
        }

        public void AcknowledgeMatchHudGuidedCommandMode(TacticalCommandMode mode)
        {
            _matchHudAssistantUiSystem.AcknowledgeCommandMode(mode);
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(
                _matchHudRuntimeFeedbackView, mode, _gameTextResolver);
        }

        public void CompleteMatchHudGuidedWorldTarget(TacticalCommandMode mode)
        {
            _matchHudAssistantUiSystem.CompleteWorldTarget(mode);
        }

        public void ClearMatchHudCommandMode()
        {
            _matchHudAssistantUiSystem.ApplyCommandMode(TacticalCommandMode.None);
            BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(
                _matchHudRuntimeFeedbackView, _gameTextResolver);
        }

        public void ConfigureMatchHudSquadTrayBinding(
            System.Action<IMatchHudSquadTrayView> bindMatchHudSquadTray)
        {
            _bindMatchHudSquadTray = bindMatchHudSquadTray;
            if (_matchHudSquadTrayView == null)
                return;

            _bindMatchHudSquadTray?.Invoke(_matchHudSquadTrayView);
            // ARIA listens after the real selection listener so it observes accepted input.
            _matchHudAssistantUiSystem.BindSquadTray(_matchHudSquadTrayView);
        }

        public void BindMatchHudSquadTray(MatchHudSquadTrayView squadTrayView)
        {
            _matchHudAssistantUiSystem.BindSquadTray(null);
            _matchHudSquadTrayView?.Unbind();
            _matchHudSquadTrayView = squadTrayView;
            _bindMatchHudSquadTray?.Invoke(_matchHudSquadTrayView);
            // Bind() replaces button listeners, so ARIA must register afterward.
            _matchHudAssistantUiSystem.BindSquadTray(_matchHudSquadTrayView);
        }

        private bool TryExecuteGuidedBuildingPlacement()
        {
            if (_buildPlacementConfirmationBarView?.HasPendingPlacement == true)
                return _buildPlacementConfirmationBarView.TryInvokeConfirmFromGuidance();

            BuildDrawerCatalogRuntimeView catalog =
                _buildDrawerView != null
                    ? _buildDrawerView.GetComponent<BuildDrawerCatalogRuntimeView>()
                    : null;
            if (catalog == null || !catalog.TryInvokePrimaryActionFromGuidance())
                return false;

            // Starting placement and confirming it are separate player-facing steps. Returning
            // here lets the real confirmation bar render before either the player or a later
            // guidance invocation accepts the placement.
            return _buildPlacementConfirmationBarView != null &&
                   _buildPlacementConfirmationBarView.HasPendingPlacement;
        }
    }
}
