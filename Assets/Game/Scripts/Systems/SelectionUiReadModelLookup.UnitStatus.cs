using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public sealed partial class SelectionUiReadModelLookup
    {
        public enum FocusedUnitUiStatus
        {
            Idle = 0,
            Moving = 1,
            Engaged = 2,
            ReturningToBase = 3,
            MissileLaunched = 4,
            AirspaceClear = 5,
            TrackingAirTarget = 6,
            InterceptingMissile = 7,
            AirDefenseReloading = 8,
            Holding = 9
        }
        public FocusedUnitUiStatus GetFocusedUnitUiStatus(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.Exists(entity))
                return FocusedUnitUiStatus.Idle;

            if (entityManager.HasComponent<UnitAirComponent>(entity) && entityManager.GetComponentData<UnitAirComponent>(entity).ReturningHome != 0)
                return FocusedUnitUiStatus.ReturningToBase;

            if (entityManager.HasComponent<GroundMissileInFlightComponent>(entity) ||
                HasCommandedGroundMissileTarget(entityManager, entity))
            {
                return FocusedUnitUiStatus.MissileLaunched;
            }

            if (HasAutoGroundMissileTarget(entityManager, entity))
                return FocusedUnitUiStatus.Idle;

            if (TryGetAirMissileLauncherStatus(entityManager, entity, out FocusedUnitUiStatus airDefenseStatus))
                return airDefenseStatus;

            if (entityManager.HasComponent<HoldPositionOrderTag>(entity))
                return FocusedUnitUiStatus.Holding;

            if (entityManager.HasComponent<EngageTarget>(entity))
                return FocusedUnitUiStatus.Engaged;

            if (entityManager.HasComponent<UnitTarget>(entity) ||
                entityManager.HasComponent<UnitPathRequest>(entity) ||
                entityManager.HasComponent<UnitPathFollow>(entity))
            {
                return FocusedUnitUiStatus.Moving;
            }

            return FocusedUnitUiStatus.Idle;
        }

    }
}
