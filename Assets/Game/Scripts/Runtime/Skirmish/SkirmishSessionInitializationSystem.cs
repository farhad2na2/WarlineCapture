using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SkirmishSessionInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            foreach ((RefRW<SkirmishExpandedSessionComponent> sessionRef, Entity entity) in
                     SystemAPI.Query<RefRW<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                ref SkirmishExpandedSessionComponent session = ref sessionRef.ValueRW;
                if (session.IsLegacy != 0)
                    continue;
                if (session.InitializationComplete != 0)
                    continue;
                if (session.Phase == SkirmishSessionPhase.Failed || session.Phase == SkirmishSessionPhase.Finished)
                    continue;
                if (!em.HasComponent<SkirmishResolvedSetupRecord>(entity) ||
                    em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup == null)
                {
                    Fail(ref session, SkirmishReasonCode.MissingResolvedSetup);
                    em.SetComponentData(entity, session);
                    continue;
                }

                SkirmishWorldSetup.SuppressUnselectedStartupConfigs(em);
                ProjectRoles(em, entity, em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup);
                session.InitializationComplete = 1;
                session.Phase = SkirmishSessionPhase.Spawning;
                em.SetComponentData(entity, session);
            }
        }

        private static void ProjectRoles(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            if (!em.HasComponent<SkirmishObjectiveStateComponent>(session))
            {
                em.AddComponentData(session, new SkirmishObjectiveStateComponent
                {
                    Kind = setup.ObjectiveKind,
                    State = SkirmishObjectiveStateKind.Pending,
                    Outcome = SkirmishOutcomeKind.None,
                    Reason = SkirmishEndReasonKind.None
                });
            }

            if (!em.HasComponent<SkirmishCapacityComponent>(session))
            {
                em.AddComponentData(session, new SkirmishCapacityComponent
                {
                    FactionId = 1,
                    InfantryCap = setup.InfantryCapEach,
                    GroundCap = setup.GroundCapEach,
                    AirCap = setup.TacticalAirCapEach,
                    SupplyCap = setup.SupplyCapEach
                });
            }

            if (!em.HasComponent<SkirmishEconomyStockComponent>(session))
            {
                em.AddComponentData(session, new SkirmishEconomyStockComponent
                {
                    FactionId = 1,
                    Materials = setup.MaterialsEach,
                    Oil = setup.OilEach,
                    Fuel = setup.UsableFuelEach,
                    MaterialsCapacity = setup.MaterialsCapacityEach,
                    OilCapacity = setup.OilCapacityEach,
                    FuelCapacity = setup.FuelCapacityEach
                });
            }

            if (!em.HasBuffer<SkirmishProductionReservation>(session))
                em.AddBuffer<SkirmishProductionReservation>(session);

            SkirmishArmyGroupSystem.EnsureSession(em, session, setup);
        }

        private static void Fail(ref SkirmishExpandedSessionComponent session, SkirmishReasonCode code)
        {
            session.FailureCode = code;
            session.Phase = SkirmishSessionPhase.Failed;
        }
    }
}
