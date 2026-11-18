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
        public bool CanAffordBreacher, BreacherRecruitPending, BreacherFielded;
        public int CoverageAffordMask, CoveragePendingMask, CoverageFieldedMask;
        public int ResearchNeedMask, BuildingNeedMask, BuildingPresentMask, BuildingAffordMask;
        public int CoverageObjectiveAffordMask, BuildingObjectiveAffordMask;
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
        public bool SlippingHostile;
        public float SlippingHostileX, SlippingHostileY, SlippingHostileZ;
        public string PlayerBase, EnemyBase, Clock, Objective, ResultTitle, ResultDetail, Statistics;
        public byte ResultStars;
        public string ResultElapsed;
        public int PlayerUnitsLost, EnemyUnitsLost, PlayerBuildingsLost, EnemyBuildingsLost;
        public bool PlayerBaseHeld, EnemyBaseDestroyed;
        // First medal earned by the settlement that produced this result. Empty unless IsFirstAward.
        public string MedalScenarioId;
        public byte MedalDifficulty;
        public bool MedalIsFirstAward;
        // Expanded session catalog id, including repeats that earn no new medal.
        public string CatalogId;
        // Fog-respecting knowledge of the enemy main base (0 hidden, 1 last seen, 2 visible),
        // published only when the session supplies it. Age is seconds since it was last seen.
        public bool EnemyBaseKnowledgeKnown;
        public byte EnemyBaseKnowledge;
        public float EnemyBaseLastSeenAgeSeconds;
        // Base under fire (public only): own base position, the latest attacker only while it is
        // fog-visible to the player, and the nearest own ground squad to the base.
        public bool PlayerBaseWorldKnown;
        public UnityEngine.Vector3 PlayerBaseWorld;
        public bool PlayerBaseRecentlyHit;
        public bool BaseAttackerVisible;
        public UnityEngine.Vector3 BaseAttackerWorld;
        public uint BaseDefenderGroupId;
        public string BaseDefenderLabel;
        public bool BaseDefenderIsHomeGuard;
        // Raised by SkirmishBaseThreatAlertSystem: revision increments on perimeter entry (reason 1)
        // and on the first damage of an attack (reason 2). The perimeter equals the base reveal radius.
        public int BaseThreatRevision;
        public byte BaseThreatReason;
        public float BasePerimeterRadius;
        public bool BaseIntruderInside;
        // The latest hit on the player's main base (public: own base, own damage readout). The event
        // id changes on every new hit; the world point is where the hit landed.
        public int BaseHitEventId;
        public UnityEngine.Vector3 BaseHitWorld;
        // Per-mission guidance (SkirmishMissionHudGuidanceCatalog): first-order prompt during the
        // opening seconds and a rendezvous marker on an existing layout anchor.
        public string FirstOrderText;
        public bool RendezvousKnown;
        public UnityEngine.Vector3 RendezvousWorld;
        public string RendezvousLabel;
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
        // Support unit kind per slot (3 bits per slot: 1 radar, 2 drone, 3 transport heli, 4 AA); not assault roles,
        // ordered once by AriaSupportOrderPolicy.
        public int SupportSlots;
        // Cards whose presented role fights armour (rocketeer, tank, siege): home defence against a shelling tank.
        public int AntiArmorMask;
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
