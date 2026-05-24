using Unity.Entities;

public sealed class SelectionHudFeedbackSystem
{
    private BattleHudGameplayBridge _battleHudBridge;

    public void ResetBridgeCache()
    {
        _battleHudBridge = null;
    }

    public void ApplySelection(EntityManager em, Entity entity, SelectionUiQuerySystem selectionUiQuerySystem)
    {
        if (entity == Entity.Null || !em.Exists(entity))
        {
            ClearSelection();
            return;
        }

        BattleHudGameplayBridge bridge = ResolveBattleHudBridge();
        if (bridge == null)
            return;

        bridge.ApplySelection(
            selectionUiQuerySystem.ResolveFocusedUnitName(em, entity),
            selectionUiQuerySystem.ResolveHudSelectionStatus(em, entity));
    }

    public void ApplySquadSelection(int selectedCount)
    {
        BattleHudGameplayBridge bridge = ResolveBattleHudBridge();
        if (bridge == null)
            return;

        if (selectedCount <= 0)
        {
            bridge.ClearSelection();
            return;
        }

        string unitLabel = selectedCount == 1 ? "UNIT" : "UNITS";
        bridge.ApplySelection($"{selectedCount} {unitLabel}", "SQUAD SELECTED");
    }

    public void ClearSelection()
    {
        ResolveBattleHudBridge()?.ClearSelection();
    }

    public void ApplyCommandMode(TacticalCommandMode mode)
    {
        ResolveBattleHudBridge()?.ApplyCommandMode(mode);
    }

    public void ClearCommandMode()
    {
        ResolveBattleHudBridge()?.ClearCommandMode();
    }

    public void ApplyCommandResult(TacticalCommandResult result)
    {
        ResolveBattleHudBridge()?.ApplyCommandResult(result);
    }

    public void SetWorldMarkersVisible(bool visible)
    {
        ResolveBattleHudBridge()?.SetWorldMarkersVisible(visible);
    }

    private BattleHudGameplayBridge ResolveBattleHudBridge()
    {
        if (_battleHudBridge != null)
            return _battleHudBridge;

        _battleHudBridge = BattleHudGameplayBridge.ResolveActive();
        return _battleHudBridge;
    }
}
