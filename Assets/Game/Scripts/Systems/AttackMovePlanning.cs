using System.Collections.Generic;
using Game.Components;
using Game.Runtime.Pathfinding;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    // Shared read-only preparation. Planning never issues or accepts a command.
    internal static class AttackMovePlanning
    {
        internal static EntityQuery CreateSelectedFighterQuery(EntityManager em) => em.CreateEntityQuery(
            typeof(SelectedUnitTag), typeof(UnitMove), typeof(UnitGrid), typeof(UnitAttack),
            typeof(UnitCombat), typeof(UnitHealth), typeof(LocalTransform), typeof(Faction));
        internal static void CollectFighters(EntityManager em, NativeArray<Entity> entities, byte faction, NativeList<Entity> fighters)
        {
            foreach (var unit in entities)
                if (em.Exists(unit) && em.HasComponent<Faction>(unit) &&
                    em.GetComponentData<Faction>(unit).Id == faction &&
                    em.HasComponent<UnitMove>(unit) && em.HasComponent<UnitGrid>(unit) &&
                    em.HasComponent<UnitAttack>(unit) && em.HasComponent<UnitCombat>(unit) &&
                    em.HasComponent<UnitHealth>(unit) && em.HasComponent<LocalTransform>(unit) &&
                    !em.HasComponent<StaticGridBlocker>(unit) && !em.HasComponent<RuntimeBuildingCombatTag>(unit) &&
                    em.GetComponentData<UnitHealth>(unit).Current > 0 && em.GetComponentData<UnitCombat>(unit).CanAttack != 0 &&
                    em.GetComponentData<UnitAttack>(unit).Damage > 0 && !em.HasComponent<UnitTransportPassenger>(unit)) fighters.Add(unit);
        }
        // Mirrors the original sequential filtered-fighter index and reservation order.
        // Fails closed for an original fallback cell that is not actually reservable.
        internal static bool TryPlanAll(EntityManager em, in GridConfig grid, in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked, in NativeArray<byte> friendlyPass, in NativeBitArray occupied,
            NativeArray<Entity> fighters, int2 desired, MapSurfacePathfindingSnapshot.Context surface, int2[] goals)
        {
            if (fighters.Length == 0 || goals == null || goals.Length != fighters.Length) return false;
            var move = new UnitMoveOrderSystem();
            var current = move.BuildSelectedCurrentFootprintCells(em, grid, fighters);
            var reserved = new HashSet<int>();
            for (int i = 0; i < fighters.Length; i++)
                if (!move.TryPlanManualMoveGoal(em, grid, walkable, blocked, friendlyPass, occupied,
                    reserved, current, fighters[i], desired, i, surface, out goals[i])) return false;
            return true;
        }
    }
}
