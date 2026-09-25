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
        public bool CanAffordRifle, CanAffordAntiAir, PadPresent, ReadinessEligible, CanQueueAir;
        public bool CanAffordLogisticsTruck, LogisticsTruckCommitted, RifleRecruitPending, AirProfile;
        public bool CanBuildAirPad;
        public bool AirRecruitPending;
        public int OwnAttackAirLive, OwnAttackAirActive, OwnAttackAirLanded, OwnAirFuel;
        public int OwnMaterials, VisibleHostileCombat, VisibleHostileAir;
        public string PlayerBase, EnemyBase, Clock, Objective, ResultTitle, ResultDetail, Statistics;
    }
    public struct UiExpandedSquadPage
    {
        public bool Expanded;
        public int PageIndex;
        public int AssaultMask;
        public int SelectedMask;
        public int StructureMask;
        public int AttackOrderMask;
        public int AirMask;
        public bool NextPage;
    }

    public struct UiSkirmishReadinessModel
    {
        public bool Visible, CanUpgrade, InProgress, CanCancel;
        public byte Stage;
        public int MaterialsCost;
        public float RemainingSeconds;
        public uint ResearchId;
        public string Reason;
    }
    public interface IUiSkirmishReadinessGateway
    {
        bool TryReadSkirmishReadiness(out UiSkirmishReadinessModel model);
        bool TryRequestSkirmishReadiness(uint cancelResearchId);
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
    }
}
