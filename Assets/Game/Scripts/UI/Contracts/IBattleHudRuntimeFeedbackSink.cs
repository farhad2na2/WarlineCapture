public interface IBattleHudRuntimeFeedbackSink
{
    BattleHudRuntimeFeedbackState GetState();

    void ApplySelection(string displayName, string status);

    void ClearSelection();

    void ApplyCommandMode(TacticalCommandMode mode);

    void ApplyBoardCommandMode(UiBoardCommandModeDirection direction, bool boardAllInteractable);

    void ClearCommandMode();

    void ClearCommandModeTabs();

    void ApplyCommandResult(TacticalCommandResult result);

    void SetWorldMarkersVisible(bool visible);
}
