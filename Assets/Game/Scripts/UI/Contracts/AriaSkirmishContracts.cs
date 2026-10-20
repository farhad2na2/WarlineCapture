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
        public AriaObjectiveTouchObservation PublicObjective;
        public AriaObjectiveSupportObservation ObjectiveSupport;
        public AriaAircraftServiceObservation AircraftService;
        public AriaPublicScanObservation PublicScan;
        public AriaPublicBoardingObservation PublicBoarding;
        public AriaOffensiveAirObservation OffensiveAir;
        public AriaAirRecruitObservation AirRecruit;
        public bool Active, Finished, DrawerOpen, SelectionVisible, AttackMode, MapOpen, MapContactInView, SelectionMode, FocusThreatDrag, FocusGroupDrag, FocusAdvanceDrag, PlacementOpen, AdvancePreferred, ThreatNearForce, AssaultAtBase;
        public bool ExpandedSession, PlayerDesignatedAlive, EnemyDesignatedAlive, CanAffordRifle, CanAffordRocketeer, CanAffordTank;
        public bool CanAffordAntiAir, PadReady, AirQueueOffered, PadPresent, ReadinessEligible, CanUpgradeReadiness;
        public bool CanAffordLogisticsTruck, LogisticsTruckCommitted, RifleRecruitPending, AirProfile;
        public bool CanAffordTanker;
        public int TankersCommitted;
        public bool CombinedArmsField, RocketeerRecruitPending;
        public bool PresentedHomeThreat;
        public int OwnStructureAttackUnits;
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
        public bool ExpandedBaseAssault;
        public uint ExpandedGroup0, ExpandedGroup1, ExpandedGroup2, ExpandedGroup3, ExpandedGroup4;
        public UiExpandedPrecisionAttackResult PrecisionAttack;
        public uint ExpandedGroup(int slot) => slot switch {
            0=>ExpandedGroup0,1=>ExpandedGroup1,2=>ExpandedGroup2,3=>ExpandedGroup3,4=>ExpandedGroup4,_=>0 };
        public int ExpandedAirMask;
        public bool ExpandedCanPreviousPage, ExpandedCanNextPage;
        public int ExpandedPageIndex;
        public int ExpandedPageCount, ExpandedTotalGroups, ExpandedSelectedGroups, ExpandedSelectedOffPage;
        public uint ExpandedRosterRevision;
        public AriaTouchTarget Squad(int index) => index switch
        { 0 => Squad0, 1 => Squad1, 2 => Squad2, 3 => Squad3, 4 => Squad4, _ => default };
    }
}
