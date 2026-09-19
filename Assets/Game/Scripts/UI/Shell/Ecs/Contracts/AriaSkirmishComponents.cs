using Game.UI.Contracts;
using Unity.Entities;
namespace Game.UI.Shell.Contracts.Ecs
{
    public struct AriaSkirmishObservationComponent : IComponentData { public AriaSkirmishObservation Value; }
    public struct AriaSkirmishPlanComponent : IComponentData
    {
        public AriaSkirmishIntent Intent;
        public int Slot, ActionsAtTarget, Cycle, Infantry, MapFocusActions;
        public float RecruitUnavailableAt, NextRecruitAt, ObserveUntil, LastProgressAt, EnemyHealth, PlayerHealth, ForceHealth;
        public byte TargetPending;
    }
}
