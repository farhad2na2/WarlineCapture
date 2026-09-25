using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
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
            using var query = em.CreateEntityQuery(ComponentType.ReadWrite<SkirmishExpandedSessionComponent>());
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                    TryInitialize(em, entities[i]);
            }
            finally
            {
                entities.Dispose();
            }
        }

        private static void TryInitialize(EntityManager em, Entity entity)
        {
            SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
            if (session.IsLegacy != 0)
                return;
            if (session.InitializationComplete != 0)
                return;
            if (session.Phase == SkirmishSessionPhase.Failed || session.Phase == SkirmishSessionPhase.Finished ||
                session.Phase == SkirmishSessionPhase.Cleaning)
                return;
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(entity) ||
                em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup == null)
            {
                Fail(ref session, SkirmishReasonCode.MissingResolvedSetup);
                em.SetComponentData(entity, session);
                return;
            }

            SkirmishWorldSetup.SuppressUnselectedStartupConfigs(em);
            ProjectRoles(em, entity, em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup);
            session.InitializationComplete = 1;
            session.Phase = SkirmishSessionPhase.Spawning;
            em.SetComponentData(entity, session);
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
            SkirmishResearchService.EnsureSession(em, session, setup);
            if (!em.HasBuffer<SkirmishReadinessRequest>(session)) em.AddBuffer<SkirmishReadinessRequest>(session);
        }

        private static void Fail(ref SkirmishExpandedSessionComponent session, SkirmishReasonCode code)
        {
            session.FailureCode = code;
            session.Phase = SkirmishSessionPhase.Failed;
        }
    }
}
