namespace Game.UI.Contracts
{
    public enum UiSkirmishAction : byte { FocusPlayer, FocusEnemy, Surrender, Replay, AdjustSetup, MainMenu, Restart, SaveExit }
    public struct UiSkirmishModel
    {
        public Game.Skirmish.Contracts.SkirmishPublicSelectedApproach SelectedApproach;
        public bool Finished, Paused, StartupFailed;
        public bool CanSaveMatch, SaveFailed;
        public string Infantry;
        public string MissionTitle;
        // Numeric mirrors of the displayed counts, at the same visible precision.
        public int ScenarioIndex;
        public int InfantryCount, PlayerHealth, EnemyHealth;
        public bool Expanded, PlayerDesignatedAlive, EnemyDesignatedAlive;
        public bool CanAffordRifle, CanAffordAntiAir, PadPresent, ReadinessEligible, CanQueueAir;
        public bool CombinedArmsField, CanAffordRocketeer, RocketeerRecruitPending;
        public int OwnStructureAttackUnits;
        public bool CanAffordLogisticsTruck, LogisticsTruckCommitted, RifleRecruitPending, AirProfile;
        public bool CanAffordTanker;
        public int TankersCommitted;
        public bool CanBuildAirPad;
        public bool AirRecruitPending;
        public int OwnAttackAirLive, OwnAttackAirActive, OwnAttackAirLanded, OwnAirFuel;
        public int OwnDefenseTowers;
        public bool AntiAirCommitted;
        public int OwnMaterials, VisibleHostileCombat, VisibleHostileAir;
        public string PlayerBase, EnemyBase, Clock, Objective, ResultTitle, ResultDetail, Statistics;
        public byte ResultStars;
        public string ResultElapsed;
        public int PlayerUnitsLost, EnemyUnitsLost, PlayerBuildingsLost, EnemyBuildingsLost;
        public bool PlayerBaseHeld, EnemyBaseDestroyed;
    }
    public struct UiExpandedPrecisionAttackResult
    {
        public bool Presented;
        public uint Revision, GroupId;
        public int Frame, RequestId, PublicTargetId, AcceptedMembers, SourceMembers;
        public UnityEngine.Vector2 ScreenPosition;
    }
    public struct UiExpandedSquadPage
    {
        public bool Expanded;
        public int PageIndex;
        public int PageCount, TotalGroups, SelectedGroups, SelectedOffPage;
        public uint RosterRevision;
        public bool PreviousPage;
        public int AssaultMask;
        public int SelectedMask;
        public int StructureMask;
        public int AttackOrderMask;
        public bool BaseAssault;
        public uint Group0, Group1, Group2, Group3, Group4;
        public UiExpandedPrecisionAttackResult PrecisionAttack;
        public uint Group(int slot) => slot switch { 0=>Group0,1=>Group1,2=>Group2,3=>Group3,4=>Group4,_=>0 };
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
        // Context-only consumers must not require the full economy/ARIA projection.
        bool TryReadSkirmishContext(out bool expanded)
        {
            bool admitted = TryReadSkirmish(out var model);
            expanded = admitted && model.Expanded;
            return admitted;
        }
        bool TryRequestSkirmish(UiSkirmishAction action);
        bool HasSavedSkirmish(string catalogId) => false;
        bool TryConsumeSkirmishResumeFailure() => false;
        bool TryResumeSavedSkirmish(string catalogId) => false;
        bool TryDiscardSavedSkirmish(string catalogId) => false;
    }

    public interface IUiExpandedSkirmishCommandGateway
    {
        bool TryReadExpandedSquadPage(out UiExpandedSquadPage page);
        bool TrySelectExpandedPresentedSlot(int slotIndex);
        bool TrySelectExpandedGroup(uint groupId);
        bool TryChangeExpandedSquadPage(int delta);
        bool TryClearExpandedSquadSelection();
        bool TryHoldExpandedSelection();
    }
}
