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
        public int HomeDefenseReason, HomeDefenseSlot, HomeDefenseScope, HomeDefenseRounds, HomeDefenseMask;
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
        public int CoverageDone;
        public byte CoverageCurrent;
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
        // AriaFindBaseRecovery after a given-up search (retry time, bounded Attack disarm).
        public float FindBaseRetryAt, FindBaseDisarmAt; public int FindBaseDisarmActions; public byte FindBaseDisarms;
        // AriaCoverageRecruitPolicy progress (taps completed, purchase start) and the latched first assault.
        public int CoverageActions;
        public float CoverageStarted;
        public byte CoverageAssaultSeen, CoverageBatch;
        // Air-capable base assault: the air opening's light attack heli has been queued once (pending or live),
        // so coverage may now spend a free air slot (AriaCoverageRecruitPolicy.AirOpeningAfford).
        public byte CoverageAirOpened;
        // Air-capable base assault (lane 26): kinds whose purchase was interrupted once, the interrupted breacher
        // purchase, and the support order's one Home focus and disarm presses.
        public int CoverageInterrupted;
        public byte BreacherRetried, SupportFocused, SupportPresses, SupportPageDirection, SupportTrace;
        // AriaResearchPolicy (readiness research, one per stage) and AriaBuildingPolicy (producer buildings once).
        public byte ResearchCurrent, ResearchTried, ResearchDone;
        public int ResearchActions;
        public float ResearchDeadline;
        public byte BuildCurrent, BuildTried, BuildDone;
        public float BuildDeadline;
        // AriaInterceptPolicy: finish a visible vehicle slipping past the column.
        public byte InterceptStage, InterceptCount;
        public int InterceptSlot, InterceptActions;
        public float InterceptDeadline, InterceptLastSeenAt, InterceptReadyAt;
        // FC/BT/CE objective skill (AriaSkirmishPlanSystem.ObjectiveSkill, Game.Simulation.AriaObjectiveSkillPolicy):
        // missing-frame grace, own-popup bound, bounded objective/escort order retries, public progress revision.
        // ObjectiveOwner: 0 free, 2 an escort order is in flight. ObjectiveFrameAction: last AriaObjectiveFrameAction.
        public AriaObjectiveTouchPlan EscortTouch;
        public float ObjectiveMissingSince, ObjectivePopupSince, ObjectiveRetryAt, EscortRetryAt, ObjectiveProgressAt;
        public byte ObjectiveMissing, ObjectivePopupOpen, ObjectiveProgressKnown, ObjectiveOwner, ObjectiveFrameAction;
        public int ObjectiveFailures, ObjectiveAccepted, EscortFailures, EscortAccepted;
        public uint ObjectiveProgressRevision;
        // Presented simulation seconds minus observation time, so a helipad preview started on the objective
        // clock keeps its deadline while the Build drawer hides the goal HUD.
        public float ObjectiveClockOffset;
        // Objective gate 3: one coverage turn after each accepted objective order (ObjectiveCoverageTurn) and the
        // one-time rocketeer purchase (AriaObjectiveSkillPolicy.OneTimeBuy: 0 none, 1 buying, 2 done).
        public byte ObjectiveCoverageTurn, ObjectiveRocketeerStage;
        // W32 gate 3: one escort turn after each accepted objective order, before support/coverage take the frame.
        public byte ObjectiveEscortTurn;
        public float ObjectiveRocketeerDeadline;
        // W37c CE cadence: escorts first, then the truck order. TruckWaitSince: when the current truck order first became
        // ready (0 = none waiting). TruckReleased: this order was released (TruckReleaseReason 1 all escorts accepted,
        // 2 the 45 s bound, 3 no escort offer left); TruckReleases counts releases for the evidence trace.
        public float TruckWaitSince, TruckReleaseWaited;
        // W37f: TruckReleaseReasons 4 (W37d/e truck-first) and 5 (W37f first proof: truck Hold first) are no longer used. TruckReleaseEscortsOld is the
        // W37c count (accepted order already at the goal) beside the fresh-Move count, for the evidence trace.
        public byte TruckReleased, TruckReleaseReason, TruckReleaseEscorts, TruckReleaseCapable, TruckReleaseEscortsOld;
        public int TruckReleases;
        // W37e: CE finished-objective latches released because the observer's choice moved away (evidence count).
        public int ObjectiveLatchReleases;
    }
}
