using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed class BattleHudRuntimeFeedbackSink : IBattleHudRuntimeFeedbackSink
    {
        private readonly IBattleHudRuntimeFeedbackView _view;
        private readonly IGameTextResolver _gameTextResolver;

        public BattleHudRuntimeFeedbackSink(
            IBattleHudRuntimeFeedbackView view,
            IGameTextResolver gameTextResolver = null)
        {
            _view = view;
            _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;
        }

        public BattleHudRuntimeFeedbackState GetState()
        {
            return BattleHudRuntimeFeedbackUiSystemHelper.GetState(_view);
        }

        public void ApplySelection(string displayName, string status)
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ApplySelection(_view, displayName, status);
        }

        public void ClearSelection()
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ClearSelection(_view);
        }

        public void ApplyCommandMode(TacticalCommandMode mode)
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(_view, mode, _gameTextResolver);
        }

        public void ApplyBoardCommandMode(UiBoardCommandModeDirection direction, bool boardAllInteractable)
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyBoardCommandMode(
                _view,
                direction,
                boardAllInteractable,
                _gameTextResolver);
        }

        public void ClearCommandMode()
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(_view, _gameTextResolver);
        }

        public void ClearCommandModeTabs()
        {
            _view?.ClearCommandModeTabs();
        }

        public void ApplyCommandResult(TacticalCommandResult result)
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_view, result, _gameTextResolver);
        }

        public void SetWorldMarkersVisible(bool visible)
        {
            BattleHudRuntimeFeedbackUiSystemHelper.SetWorldMarkersVisible(_view, visible);
        }
    }
}
