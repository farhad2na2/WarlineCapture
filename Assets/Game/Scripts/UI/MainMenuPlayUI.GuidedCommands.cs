using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MainMenuPlayUI
    {
        private readonly GuidedHudRuntime _guidedHudRuntime = new();

        private void TickGuidedHudRuntime() =>
            _guidedHudRuntime.Tick(this, _matchHudCommandControlsView);

        private void DisposeGuidedHudRuntime() => _guidedHudRuntime.Dispose();

        public void BindGuidedHudRuntime(UIShellContentView shellContent) =>
            _guidedHudRuntime.BindShellContent(shellContent);

        public void BindGuidanceWorldCamera(UnityEngine.Camera worldCamera) =>
            _matchHudAssistantUiSystem.BindWorldCamera(worldCamera);

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
    }
}
