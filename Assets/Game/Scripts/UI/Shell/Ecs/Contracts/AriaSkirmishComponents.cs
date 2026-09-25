using Game.UI.Contracts;
using Unity.Entities;
namespace Game.UI.Shell.Contracts.Ecs
{
    public struct AriaSkirmishObservationComponent : IComponentData { public AriaSkirmishObservation Value; }
    public struct AriaSkirmishPlanComponent : IComponentData
    {
        public AriaSkirmishIntent Intent;
        public int DefenseSite, DefenseSiteAction, DefenseSitePending, DefensePositioned, DefenseStage, DefenseActions, DefensesPlaced, Slot, ActionsAtTarget, Cycle, RecruitBurstRemaining, Infantry, MapFocusActions, GroupStage, GroupAction, RecruitAction, MapNavigationStage, OpeningSquads;
        public float DefenseDeadline, DefenseReadyAt, RecruitUnavailableAt, NextRecruitAt, ObserveUntil, LastProgressAt, EnemyHealth, PlayerHealth, ForceHealth, RegroupAt, GroupReadyAt, OpeningUntil, MapNavigationReadyAt, MapOpenedAt;
        public byte AdvanceNavigation, TargetPending, RecruitDrawerSeen, AssaultStarted, ExpandedRetries;
        public byte PagedToAssault, AssaultSelecting, AssaultIssued, StructureOrdered;
        public int AssaultPageSeen;
        public int PagesOrdered;
        public uint SweepRosterRevision;
        public int EconomicMilestones, MaterialsHighWater;
        public int ObservedPageIndex;
        // Separate from the legacy two-tower opening. A legal preview alone never
        // proves that the requested helipad site was selected.
        public int PadStage, PadSiteIndex, PadAction, PadTargetId, PadFeedbackFrame;
        public float PadDeadline, PadReadyAt;
        public int AirCycleStage, AirCycleAction, AirCycleCount;
        public float AirSortieObservedAt, AirCycleDeadline, AirServiceReadyAt;
    }
}
