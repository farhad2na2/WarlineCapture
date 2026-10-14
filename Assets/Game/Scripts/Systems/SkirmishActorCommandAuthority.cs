using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    internal static class SkirmishActorCommandAuthority
    {
        public static bool IsActiveOwnedActor(EntityManager em, Entity session, byte faction, Entity actor, bool allowLoaded = false)
        {
            if ((faction != 1 && faction != 2) || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishObjectiveClockComponent>(session)) return false;
            var match = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (match.IsLegacy != 0 || match.Phase != SkirmishSessionPhase.Playing || match.SessionId.IsEmpty ||
                em.GetComponentData<SkirmishObjectiveClockComponent>(session).Paused != 0) return false;
            using var gameplay = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            if (!gameplay.IsEmptyIgnoreFilter && (gameplay.CalculateEntityCount() != 1 ||
                gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0)) return false;
            return IsLivingOwnedActor(em, actor, faction, allowLoaded) &&
                em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).SessionId.Equals(match.SessionId);
        }

        public static bool IsLivingOwnedActor(EntityManager em, Entity actor, byte faction, bool allowLoaded = false)
        {
            if (!em.Exists(actor) || !em.HasComponent<Faction>(actor) ||
                em.GetComponentData<Faction>(actor).Id != faction ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(actor) || !em.HasComponent<UnitHealth>(actor) ||
                em.GetComponentData<UnitHealth>(actor).Current <= 0 || em.HasComponent<UnitDeathAnimationComponent>(actor) ||
                em.HasComponent<UnitSpawnTransitTag>(actor) ||
                (!allowLoaded && (em.HasComponent<Disabled>(actor) || em.HasComponent<UnitTransportPassenger>(actor)))) return false;
            var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            return own.FactionId == faction && own.IsStructure == 0 && own.CapacityReleased == 0 &&
                !own.SessionId.IsEmpty && !own.StableObjectId.IsEmpty;
        }

        public static bool IsSameOwnedPair(EntityManager em, Entity transport, Entity passenger)
        {
            if (!em.Exists(transport) || !em.HasComponent<Faction>(transport)) return false;
            byte faction = em.GetComponentData<Faction>(transport).Id;
            return (faction == 1 || faction == 2) && IsLivingOwnedActor(em, transport, faction) &&
                IsLivingOwnedActor(em, passenger, faction) &&
                em.GetComponentData<SkirmishAttemptOwnedComponent>(transport).SessionId.Equals(
                    em.GetComponentData<SkirmishAttemptOwnedComponent>(passenger).SessionId);
        }
    }
}
