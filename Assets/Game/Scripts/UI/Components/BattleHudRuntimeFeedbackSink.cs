public sealed class BattleHudRuntimeFeedbackSink : IBattleHudRuntimeFeedbackSink
{
    private readonly IBattleHudRuntimeFeedbackView _view;

    public BattleHudRuntimeFeedbackSink(IBattleHudRuntimeFeedbackView view)
    {
        _view = view;
    }

    public BattleHudRuntimeFeedbackState GetState()
    {
        return BattleHudRuntimeFeedbackBoundary.GetState(_view);
    }

    public void ApplySelection(string displayName, string status)
    {
        BattleHudRuntimeFeedbackBoundary.ApplySelection(_view, displayName, status);
    }

    public void ClearSelection()
    {
        BattleHudRuntimeFeedbackBoundary.ClearSelection(_view);
    }

    public void ApplyCommandMode(TacticalCommandMode mode)
    {
        BattleHudRuntimeFeedbackBoundary.ApplyCommandMode(_view, mode);
    }

    public void ApplyBoardCommandMode(UiBoardCommandModeDirection direction, bool boardAllInteractable)
    {
        BattleHudRuntimeFeedbackBoundary.ApplyBoardCommandMode(_view, direction, boardAllInteractable);
    }

    public void ClearCommandMode()
    {
        BattleHudRuntimeFeedbackBoundary.ClearCommandMode(_view);
    }

    public void ClearCommandModeTabs()
    {
        _view?.ClearCommandModeTabs();
    }

    public void ApplyCommandResult(TacticalCommandResult result)
    {
        BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(_view, result);
    }

    public void SetWorldMarkersVisible(bool visible)
    {
        BattleHudRuntimeFeedbackBoundary.SetWorldMarkersVisible(_view, visible);
    }
}
