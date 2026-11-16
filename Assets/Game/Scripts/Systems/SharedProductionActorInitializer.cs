using Game.Components;using Unity.Entities;using Unity.Mathematics;namespace Game.Runtime { internal static class SharedProductionActorInitializer {
        public static void Initialize(
            EntityManager em,
            Entity instance,
            float3 pos,
            int2 cell,
            RuntimeBuildingEntity building,
            bool isAirUnit,
            ref uint randomState)
        {
            if (em.HasComponent<UnitGridInitialized>(instance))
                em.RemoveComponent<UnitGridInitialized>(instance);
            if (em.HasComponent<UnitPrevWorldPos>(instance))
                em.SetComponentData(instance, new UnitPrevWorldPos { Value = pos });
            if (em.HasComponent<UnitAirComponent>(instance))
            {
                em.SetComponentData(instance, new UnitAirComponent
                {
                    HomePosition = pos,
                    HomeCell = cell,
                    HomeInitialized = 1,
                    ReturningHome = 0,
                    Airborne = 0
                });
            }
            if (em.HasComponent<UnitMoveVisualComponent>(instance))
                em.SetComponentData(instance, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
            if (em.HasComponent<Faction>(instance))
                em.SetComponentData(instance, new Faction { Id = ResolveProducedUnitFaction(building) });
            if (em.HasComponent<UnitRespawnPrefab>(instance))
                em.SetComponentData(instance, new UnitRespawnPrefab { Prefab = Entity.Null });
            if (em.HasComponent<UnitAttackCooldownComponent>(instance))
                em.SetComponentData(instance, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
            if (em.HasComponent<UnitIdleWanderComponent>(instance))
            {
                randomState = math.max(1u, randomState + 1u);
                em.SetComponentData(instance, new UnitIdleWanderComponent
                {
                    RandomState = randomState,
                    RetrySeconds = 0f,
                    CurrentIdleDelaySeconds = 0f
                });
            }
            if (em.HasComponent<UnitMovementBehavior>(instance) && em.GetComponentData<UnitMovementBehavior>(instance).AllowIdleWander == 0)
            {
                if (em.HasComponent<AutoWanderMoveTag>(instance))
                    em.RemoveComponent<AutoWanderMoveTag>(instance);
            }
            if (em.HasComponent<UnitPathFollow>(instance))
                em.RemoveComponent<UnitPathFollow>(instance);
            if (em.HasComponent<UnitPathRange>(instance))
                em.RemoveComponent<UnitPathRange>(instance);
            if (em.HasComponent<EngageTarget>(instance))
                em.RemoveComponent<EngageTarget>(instance);
            if (em.HasComponent<UnitPathRequest>(instance))
                em.RemoveComponent<UnitPathRequest>(instance);
            if (em.HasComponent<UnitTarget>(instance))
                em.RemoveComponent<UnitTarget>(instance);
            if (em.HasComponent<AutoWanderMoveTag>(instance))
                em.RemoveComponent<AutoWanderMoveTag>(instance);
            if (em.HasComponent<SelectedUnitTag>(instance))
                em.RemoveComponent<SelectedUnitTag>(instance);
        }

        internal static byte ResolveProducedUnitFaction(RuntimeBuildingEntity building)
        {
            if (building == null || !building.HasOwnerFaction || building.OwnerFactionId == FactionIdentity.NeutralFactionId)
                return FactionIdentity.PlayerFactionId;

            return building.OwnerFactionId;
        }

}}
