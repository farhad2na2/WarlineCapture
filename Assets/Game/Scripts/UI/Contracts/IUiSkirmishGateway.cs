namespace Game.UI.Contracts
{
    public enum UiSkirmishAction : byte { FocusPlayer, FocusEnemy, Surrender, Replay, AdjustSetup, MainMenu, Restart }
    public struct UiSkirmishModel
    {
        public bool Finished, Paused, StartupFailed;
        public string Infantry;
        // Numeric mirrors of the displayed counts, at the same visible precision.
        public int ScenarioIndex;
        public int InfantryCount, PlayerHealth, EnemyHealth;
        public bool Expanded, PlayerDesignatedAlive, EnemyDesignatedAlive;
        public string PlayerBase, EnemyBase, Clock, Objective, ResultTitle, ResultDetail, Statistics;
    }
    public struct UiExpandedSquadPage
    {
        public bool Expanded;
        public int PageIndex;
        public int AssaultMask;
        public int SelectedMask;
        public bool NextPage;
    }

    public interface IUiSkirmishGateway
    {
        bool TryReadSkirmish(out UiSkirmishModel model);
        bool TryRequestSkirmish(UiSkirmishAction action);
    }

    public interface IUiExpandedSkirmishCommandGateway
    {
        bool TryReadExpandedSquadPage(out UiExpandedSquadPage page);
        bool TrySelectExpandedPresentedSlot(int slotIndex);
        bool TryHoldExpandedSelection();
        bool TryAttackExpandedEnemyBase();
    }
}
