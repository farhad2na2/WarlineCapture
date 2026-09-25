using Game.Components;
using Game.Tactical.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitEngagementSystem))]
    public partial struct AttackMoveSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { state.RequireForUpdate<AttackMoveOrder>(); }
        public void OnUpdate(ref SystemState state) => ResumeTravel(state.EntityManager, SystemAPI.Time.ElapsedTime);

        public static TacticalCommandResult IssueSelected(EntityManager em, int2 cell, float3 position, int frame)
        {
            using var selected = em.CreateEntityQuery(typeof(SelectedUnitTag), typeof(UnitMove), typeof(UnitGrid),
                typeof(UnitAttack), typeof(UnitCombat), typeof(UnitHealth), typeof(LocalTransform), typeof(Faction));
            using var entities = selected.ToEntityArray(Allocator.Temp);
            return IssueUnits(em, entities, FactionIdentity.PlayerFactionId, cell, position, frame);
        }

        public static TacticalCommandResult IssueUnits(EntityManager em, NativeArray<Entity> entities,
            byte actingFaction, int2 cell, float3 position, int frame)
        {
            using var fighters = new NativeList<Entity>(Allocator.Temp);
            foreach (var unit in entities)
                if (em.Exists(unit) && em.HasComponent<Faction>(unit) &&
                    em.GetComponentData<Faction>(unit).Id == actingFaction &&
                    em.HasComponent<UnitMove>(unit) && em.HasComponent<UnitGrid>(unit) &&
                    em.HasComponent<UnitAttack>(unit) && em.HasComponent<UnitCombat>(unit) &&
                    em.HasComponent<UnitHealth>(unit) && em.HasComponent<LocalTransform>(unit) &&
                    !em.HasComponent<StaticGridBlocker>(unit) && !em.HasComponent<RuntimeBuildingCombatTag>(unit) &&
                    em.GetComponentData<UnitHealth>(unit).Current > 0 && em.GetComponentData<UnitCombat>(unit).CanAttack != 0 &&
                    em.GetComponentData<UnitAttack>(unit).Damage > 0 && !em.HasComponent<UnitTransportPassenger>(unit)) fighters.Add(unit);
            if (fighters.Length == 0) return TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection);
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            byte faction = em.HasComponent<Faction>(fighters[0]) ? em.GetComponentData<Faction>(fighters[0]).Id : (byte)0;
            using var accepted = new NativeList<Entity>(Allocator.Temp);
            var result = SelectedMoveOrderCommandSystem.TryIssueMoveOrderToCell(em, fighters.AsArray(), grids, surfaces,
                default, cell, position, frame, faction, false, accepted);
            if (!result.CommandResult.Accepted) return result.CommandResult;
            AriaCommandEvidence.Accepted("AttackMove", actingFaction);
            foreach (var unit in accepted)
            {
                if (!em.HasComponent<UnitTarget>(unit)) continue;
                var destination = em.GetComponentData<UnitTarget>(unit).Cell;
                var order = new AttackMoveOrder { Destination = destination,
                    Position = GridUtils.CellToWorldCenter(grids.GetSingleton<GridConfig>(), destination), RetryAt = 0 };
                if (em.HasComponent<AttackMoveOrder>(unit)) em.SetComponentData(unit, order); else em.AddComponentData(unit, order);
                // Keep player-order pathfinding priority; combat checks AttackMoveOrder
                // to distinguish this interruptible advance from a plain Move.
            }
            return TacticalCommandResult.Success();
        }

        internal static void ResumeTravel(EntityManager em, double time)
        {
            using var query = em.CreateEntityQuery(typeof(AttackMoveOrder), typeof(LocalTransform), typeof(UnitHealth));
            using var units = query.ToEntityArray(Allocator.Temp);
            foreach (var unit in units)
            {
                var order = em.GetComponentData<AttackMoveOrder>(unit);
                if (em.GetComponentData<UnitHealth>(unit).Current <= 0 || em.HasComponent<HoldPositionOrderTag>(unit))
                { em.RemoveComponent<AttackMoveOrder>(unit); continue; }
                if (em.HasComponent<EngageTarget>(unit) || em.HasComponent<BaseBreachOrder>(unit)) continue;
                if (math.distancesq(em.GetComponentData<LocalTransform>(unit).Position.xz, order.Position.xz) <= 4)
                { em.RemoveComponent<AttackMoveOrder>(unit); continue; }
                if (em.HasComponent<UnitPathFollow>(unit) || em.HasComponent<UnitPathRequest>(unit) ||
                    em.HasComponent<UnitPathRetryCooldown>(unit) ||
                    (em.HasComponent<UnitAirMovement>(unit) && em.HasComponent<UnitTarget>(unit)) || time < order.RetryAt) continue;
                order.RetryAt = time + 1;
                em.SetComponentData(unit, order);
                if (!UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, unit, order.Destination)) continue;
                // Keep player-order pathfinding priority; combat checks AttackMoveOrder
                // to distinguish this interruptible advance from a plain Move.
                // The normal Move command cancels old orders; this is resuming the same destination.
                if (em.HasComponent<AttackMoveOrder>(unit)) em.SetComponentData(unit, order); else em.AddComponentData(unit, order);
            }
        }
    }
}
