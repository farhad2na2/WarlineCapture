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
    }
}
