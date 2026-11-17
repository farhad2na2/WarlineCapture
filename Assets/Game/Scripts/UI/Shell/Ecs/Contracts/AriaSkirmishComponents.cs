using Game.UI.Contracts;
using Unity.Entities;
namespace Game.UI.Shell.Contracts.Ecs
{
    public struct AriaSkirmishObservationOwner : IComponentData { public Entity Boundary; public int TransitionSequence; }
    public struct AriaSkirmishObservationComponent : IComponentData { public AriaSkirmishObservation Value; }
    public struct AriaSkirmishPlanComponent : IComponentData
    {
        public AriaObjectiveTouchPlan ObjectiveTouch;
        public AriaObjectiveSupportPlan ObjectiveSupport;
        public AriaPublicScanPlan PublicScan;
        public AriaPublicBoardingPlan PublicBoarding;
        public AriaOffensiveAirPlan OffensiveAir;
        public AriaSkirmishIntent Intent;
        public int DefenseSite, DefenseSiteAction, DefenseSitePending, DefensePositioned, DefenseStage, DefenseActions, DefensesPlaced, Slot, ActionsAtTarget, Cycle, RecruitBurstRemaining, Infantry, MapFocusActions, GroupStage, GroupAction, RecruitAction, MapNavigationStage, OpeningSquads;
        public float DefenseDeadline, DefenseReadyAt, RecruitUnavailableAt, NextRecruitAt, ObserveUntil, LastProgressAt, EnemyHealth, PlayerHealth, ForceHealth, RegroupAt, GroupReadyAt, OpeningUntil, MapNavigationReadyAt, MapOpenedAt;
        public byte AdvanceNavigation, TargetPending, RecruitDrawerSeen, AssaultStarted, ExpandedRetries;
        public byte PagedToAssault, AssaultSelecting, AssaultIssued, StructureOrdered;
        public Game.Skirmish.Contracts.SkirmishPublicArmyOrderGoal ObservedAssaultGoal;
        // Fixed capacity: excess public groups fail closed; no page-bit identity collisions.
        public Unity.Collections.FixedList512Bytes<uint> PrecisionAcceptedGroups, PrecisionExhaustedGroups;
        public byte PrecisionStage, PrecisionAttempts;
        public byte AssaultFocusStage;
        public int AssaultFocusActions, AssaultFocusTarget, AssaultFocusFrame;
        public float AssaultFocusDeadline;
        public uint PrecisionGroup, PrecisionRoster, PrecisionBaseline;
        public int PrecisionSlot, PrecisionPage, PrecisionIssueFrame, PrecisionActions;
        public float PrecisionDeadline, PrecisionReadyAt;
        public int SelectionResetPage;
        public int AssaultPageSeen;
        public int PagesOrdered;
        public uint SweepRosterRevision;
        public int EconomicMilestones, MaterialsHighWater;
        public int ObservedPageIndex;
        public int HomeDefenseStage, HomeDefenseAction, HomeDefensePage;
        public float HomeDefenseObserveUntil;
        public int HomeDefenseReason, HomeDefenseSlot, HomeDefenseScope, HomeDefenseRounds;
        // AriaHomeDefensePolicy recovery bits (re-centre once, re-tap once, last press was focus, disarmed once).
        public int HomeDefenseRecovery, HomeDefenseRecoveryAction;
        public float HomeHealthSeen, HomeHealthPeak, HomeDamageAt;
        public byte AssaultDistanceObserved;
        public float BestAssaultDistance;
        public int AssaultObjectiveHudIdentity;
        public UnityEngine.Vector3 AssaultObjectiveWorld;
        public byte AssaultInspectStage;
        // Close presses completed while the full map stayed open, and the one "center on HQ" press
        // used to vary the control before closing again (lane 8 S026: Close pressed 3x, Blocked 115).
        public byte AssaultInspectCloses, AssaultInspectCentered;
        public int AssaultInspectCloseActions;
        public int AssaultInspectActions, AssaultInspectOpenTarget, AssaultInspectMapIdentity, AssaultInspectOpenedFrame;
        public uint AssaultInspectRevision;
        public float AssaultInspectDeadline, AssaultInspectReadyAt;
        public int AssaultInspectObjectiveHudIdentity;
        public UnityEngine.Vector3 AssaultInspectObjectiveWorld;
        // Separate from the legacy two-tower opening. A legal preview alone never
        // proves that the requested helipad site was selected.
        public int PadStage, PadSiteIndex, PadAction, PadTargetId, PadFeedbackFrame;
        public float PadDeadline, PadReadyAt;
        public int AirCycleStage, AirCycleAction, AirCycleCount;
        public float AirSortieObservedAt, AirCycleDeadline, AirServiceReadyAt;
        // AriaBreacherPolicy: one breacher squad after the first assault order.
        public byte BreacherStage;
        public int BreacherActions;
        public float BreacherDeadline;
        // AriaCoverageRecruitPolicy: marksman, apc_fast, radar, apc_heavy, drone, transport heli (bits 1..32), one at a time.
        public byte CoverageDone, CoverageCurrent;
        public float CoverageDeadline;
        // AriaSupportOrderPolicy: one ordinary order per fielded support kind (radar, drone, transport heli).
        public byte SupportStage, SupportKind, SupportTried, SupportDone;
        public int SupportSlot, SupportActions;
        public float SupportDeadline, SupportReadyAt;
        // AriaGroundColumnPolicy stall guard (pages given up) and AriaFindBasePolicy (bounded base search).
        public int ColumnStallKey, ColumnSkips, ColumnSkipKeyA, ColumnSkipKeyB;
        public float ColumnStallSince;
        public byte FindBaseActive, FindBaseTaps, FindBaseRun;
        public int FindBaseActions, FindBaseLastTarget, FindBaseGiveUps;
        // AriaCoverageRecruitPolicy progress (taps completed, purchase start) and the latched first assault.
        public int CoverageActions;
        public float CoverageStarted;
        public byte CoverageAssaultSeen;
        // AriaInterceptPolicy: finish a visible vehicle slipping past the column.
        public byte InterceptStage, InterceptCount;
        public int InterceptSlot, InterceptActions;
        public float InterceptDeadline, InterceptLastSeenAt, InterceptReadyAt;
    }
}
