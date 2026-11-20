using Game.Skirmish.Contracts;
using UnityEngine;
namespace Game.UI.Contracts
{
    // Only displayed controls/objective markers and HUD values cross this boundary.
    [System.Serializable]
    public struct AriaTouchTarget
    {
        public int Id;
        public Vector2 Position;
        public bool Available;
        public bool Drag;
        public Vector2 DragEnd;
        // Presented off-screen catalog and squad cards provide scroll progress.
        public int ScrollTargetId;
        public float ScrollDistanceViewports;
    }
    // Only an actually rendered map surface supplies these contacts/provenance.
    [System.Serializable]
    public struct AriaAssaultMapObservation
    {
        public AriaTouchTarget OpenMap;
        public int Identity, MarkerFrame, ObjectiveHudIdentity;
        public uint MarkerRevision;
        public float MarkerTime;
        public Vector3 ObjectiveWorld;
        public bool ExpectedIdentity, FullMap, Complete, ProjectionValid;
        public bool IsCurrent(int frame, float now) =>
            ExpectedIdentity && Identity != 0 && ObjectiveHudIdentity != 0 &&
            float.IsFinite(ObjectiveWorld.x) && float.IsFinite(ObjectiveWorld.y) && float.IsFinite(ObjectiveWorld.z) &&
            Complete && ProjectionValid && MarkerRevision != 0 && MarkerFrame > 0 &&
            MarkerFrame <= frame && float.IsFinite(MarkerTime) &&
            float.IsFinite(now) && now >= MarkerTime && now - MarkerTime <= 1f;
    }
    public enum AriaSkirmishIntent : byte
    {
        Recruit, SelectSquad, FindBase, Attack, TargetBase, ObserveBattle, FindThreat, TargetThreat,
        GroupForce, DefendBase, BuildDefense, Advance, Hold, Inspect, Scout, Handback,
        BuildAirPad, WaitForAirPadSite, ReturnAircraft, ServiceAircraft,
        ControlZone, OpenObjectiveCorridor, EvacuateOriginals, EscortOriginalTruck, InspectObjective, ObserveObjective, UnloadOriginals, RepairVehicle, ServiceOriginalTruck, StopDamagedTruck
    }
    [System.Serializable]
    public struct AriaSkirmishObservation
    {
        public int SourceScenarioIndex;
        public AriaObjectiveTouchObservation PublicObjective;
        public AriaObjectiveSupportObservation ObjectiveSupport;
        public AriaAircraftServiceObservation AircraftService;
        public AriaPublicScanObservation PublicScan;
        public AriaPublicBoardingObservation PublicBoarding;
        public AriaOffensiveAirObservation OffensiveAir;
        public AriaAirRecruitObservation AirRecruit;
        // FC/BT/CE objective skill (AriaSkirmishPlanSystem.ObjectiveSkill). ObjectiveType: 0 none, 1 Frontline
        // Control, 2 Breakthrough, 3 Convoy Escort (last presented goal perspective). ObjectiveFrameReason: why
        // the rendered objective frame is missing this frame (SkirmishMatchView.PublicObjectiveAriaUnavailableReason),
        // 0 when presented. ObjectiveEscort: one own vehicle group's ordinary Move to the current objective.
        public byte ObjectiveType;
        public int ObjectiveFrameReason;
        public AriaObjectiveScoreObservation ObjectiveScore;
        public AriaObjectiveTouchObservation ObjectiveEscort;
        // W37c: escort-capable own groups for the current escort goal (AriaObjectiveEscortPolicy.Eligible) and how many
        // of them have a displayed accepted Move to it.
        public byte EscortCapable, EscortAccepted;
        public bool Active, Finished, DrawerOpen, SelectionVisible, AttackMode, MapOpen, MapContactInView, SelectionMode, FocusThreatDrag, FocusGroupDrag, FocusAdvanceDrag, PlacementOpen, AdvancePreferred, ThreatNearForce, AssaultAtBase;
        public bool ExpandedSession, PlayerDesignatedAlive, EnemyDesignatedAlive, CanAffordRifle, CanAffordRocketeer, CanAffordTank;
        public bool CanAffordAntiAir, PadReady, AirQueueOffered, PadPresent, ReadinessEligible, CanUpgradeReadiness;
        public bool CanAffordLogisticsTruck, LogisticsTruckCommitted, RifleRecruitPending, AirProfile;
        public bool CanAffordTanker;
        public int TankersCommitted;
        public bool CombinedArmsField, RocketeerRecruitPending;
        public bool PresentedHomeThreat;
        // Public fog view: a visible hostile vehicle slipping behind the advancing army
        // (InterceptKnown), and its on-screen attack tap when reachable (InterceptThreat, -20002).
        public bool InterceptKnown;
        public AriaTouchTarget InterceptThreat;
        // Public attack-move ground beside the labelled home objective (shares -20006 with threat ground).
        public AriaTouchTarget HomeGround;
        // Evidence: why HomeGround was not offered (1 crowded, 2 off screen, 4 covered; 8 not evaluated).
        public int HomeGroundRefusal;
        // Ranged raid on home (AriaHomeDefensePolicy.DefenseTarget): the nearest presented enemy minimap marker
        // within AriaPublicHomeThreatPolicy.RangedRadius of home that is not already a presented home threat.
        // HomeAttackerSight: 0 none, 1 fog-visible unit, 2 last-seen ghost marker. HomeAttacker is that visible
        // unit's on-screen attack tap (-20002); HomeAttackerGround the clear ground beside it (-20006).
        // HomeGroundFar: clear home ground on the side away from the column (-20006).
        public AriaTouchTarget HomeAttacker, HomeAttackerGround, HomeGroundFar;
        public int HomeAttackerSight; public float HomeAttackerDistance;
        // HUD base-under-attack warning, read from ISkirmishBaseAlertPresentation exactly as presented.
        // Reason 1 perimeter entry, 2 base damage. BaseAlertDefenderHighlight: slot + 1 of the squad card the
        // warning highlights on this page (0 none or another page); BaseAlertDefenderPage: its paging control.
        public bool BaseAlertPresented, BaseAlertAttackerShown, BaseAlertDefenderKnown;
        public byte BaseAlertReason; public int BaseAlertSequence, BaseAlertDefenderHighlight;
        public AriaTouchTarget BaseAlertDefenderPage;
        public int OwnStructureAttackUnits;
        // Breacher coverage: affordability, pending/fielded, and the Build -> catalog card target.
        public bool CanAffordBreacher, BreacherRecruitPending, BreacherFielded;
        public AriaTouchTarget RecruitBreacher;
        // Coverage recruits (bits: 1 marksman, 2 apc_fast, 4 radar, 8 apc_heavy, 16 drone, 32 transport_heli,
        // 64 gunner, 128 tank, 256 siege, 512 AA, 1024 attack heli) and their Build -> catalog card targets
        // (the AA card is RecruitAntiAir; RecruitAttackHeli is the Helipad's Unit_Veh_Helicopter_Attack card).
        public int CoverageAffordMask, CoveragePendingMask, CoverageFieldedMask;
        public AriaTouchTarget RecruitMarksman, RecruitApcFast, RecruitRadar, RecruitApcHeavy, RecruitDrone, RecruitTransportHeli,
            RecruitGunner, RecruitTank, RecruitSiege, RecruitAttackHeli;
        // Research and producer buildings (see SkirmishAriaPublicView); readiness from the HUD readiness model.
        public int ResearchNeedMask, BuildingNeedMask, BuildingPresentMask, BuildingAffordMask, ReadinessStage, ResearchCost;
        // FC/BT/CE: coverage and producer-building affordability above the objective reserve (see SkirmishAriaPublicView).
        public int CoverageObjectiveAffordMask, BuildingObjectiveAffordMask;
        public bool ResearchInProgress;
        // The satellite dish build card (the helipad card is AirPad).
        public AriaTouchTarget BuildDish;
        // Support orders: clear public ground behind the presented column toward home (-20016, radar and
        // transport heli Move) and ahead of it toward the enemy objective (-20017, drone Scan).
        public AriaTouchTarget RearGround, FrontGround;
        // Air-capable base assault: clear ground 30 units from home toward the column (-20018), the Move target for the
        // AA (and the transport heli when the column's rear ground is not on screen).
        public AriaTouchTarget SupportHomeGround;
        public int SupportCommandResultFrame;
        public bool SupportCommandAccepted;
        // The open full map's "center on HQ" control (an ordinary popup button).
        public AriaTouchTarget MapCenterHome;
        // Ordinary Move and Scan command buttons and their armed modes.
        public AriaTouchTarget Move, Scan;
        public bool MoveMode, ScanMode;
        public bool CanBuildAirPad;
        public bool AirRecruitPending, SelectedAircraft;
        public int OwnAttackAirLive, OwnAttackAirActive, OwnAttackAirLanded, OwnAirFuel;
        public int OwnDefenseTowers;
        public bool AntiAirCommitted;
        public Vector2 FocusThreatDragEnd, FocusGroupDragEnd, FocusAdvanceDragEnd;
        public int SelectedSlot, SelectedCount, AvailableSquads, Infantry, Frame, VisibleHostileCombat, OwnMaterials, ExpandedRetries, VisibleHostileAir;
        public float Time, PlayerHealth, EnemyHealth, ForceHealth;
        public AriaAssaultMapObservation AssaultMap;
        public bool AssaultDistanceVisible;
        public float NearestFriendlyObjectiveDistance;
        public AriaTouchTarget Squad0, Squad1, Squad2, Squad3, Squad4;
        public AriaTouchTarget PreviousSquadPage, NextSquadPage, SelectedSquadToToggle;
        public AriaTouchTarget Site0, Site1, Site2, Site3, Site4, Site5;
        public AriaTouchTarget PadSite0, PadSite1, PadSite2, PadSite3;
        public AriaTouchTarget PadSite(int index) => index switch
        { 0 => PadSite0, 1 => PadSite1, 2 => PadSite2, _ => PadSite3 };
        public AriaTouchTarget Site(int index) => index switch { 0 => Site0, 1 => Site1, 2 => Site2, 3 => Site3, 4 => Site4, _ => Site5 };
        public AriaTouchTarget AdvanceGround, ThreatGround, FocusAdvance, DefenseBuild, PlacementConfirm, PlacementCancel, FocusPlayer, Select, GroupStart, GroupEnd, FocusEnemy, Attack, Hold, EnemyBase, Recruit, CloseDrawer, Threat, FocusThreat, FocusGroup, CloseMap;
        public AriaTouchTarget RecruitAntiAir, AirPad, UpgradeReadiness, RecruitAir, OpenAircraftActions, ReturnAircraft;
        public AriaTouchTarget RecruitLogisticsTruck;
        public AriaTouchTarget RecruitTanker;
        public AriaTouchTarget RecruitRocketeer;
        public int ExpandedAssaultMask;
        public int ExpandedSelectedMask;
        public int ExpandedStructureMask;
        public int ExpandedAttackOrderMask;
public SkirmishPublicArmyOrderGoal CurrentAssaultGoal, AdvanceOrderGoal, VisibleObjectiveOrderGoal;
        public bool ExpandedBaseAssault;
        public uint ExpandedGroup0, ExpandedGroup1, ExpandedGroup2, ExpandedGroup3, ExpandedGroup4;
        public UiExpandedPrecisionAttackResult PrecisionAttack;
        public uint ExpandedGroup(int slot) => slot switch {
            0=>ExpandedGroup0,1=>ExpandedGroup1,2=>ExpandedGroup2,3=>ExpandedGroup3,4=>ExpandedGroup4,_=>0 };
        public int ExpandedAirMask;
        // Support unit kind per roster slot on this page (2 bits per slot: 1 radar, 2 drone, 3 transport heli).
        public int ExpandedSupportSlots;
        // Roster cards on this page whose presented role fights armour (rocketeer, tank, siege).
        public int ExpandedAntiArmorMask;
        public bool ExpandedCanPreviousPage, ExpandedCanNextPage;
        public int ExpandedPageIndex;
        public int ExpandedPageCount, ExpandedTotalGroups, ExpandedSelectedGroups, ExpandedSelectedOffPage;
        public uint ExpandedRosterRevision;
        public AriaTouchTarget Squad(int index) => index switch
        { 0 => Squad0, 1 => Squad1, 2 => Squad2, 3 => Squad3, 4 => Squad4, _ => default };
    }
}
