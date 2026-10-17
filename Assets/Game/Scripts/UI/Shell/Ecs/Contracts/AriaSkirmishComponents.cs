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
        public byte AssaultDistanceObserved;
        public float BestAssaultDistance;
        public int AssaultObjectiveHudIdentity;
        public UnityEngine.Vector3 AssaultObjectiveWorld;
        public byte AssaultInspectStage;
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
    }
}
