using Game.Configs;
using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public static class UnitTransportAttackPayloadUtility
    {
        public const string NoAttackPayloadFeedbackKey = "tactical.transport.attack_payload_missing";
        private const int MaxNestedTransportDepth = 8;

        public static string ResolveNoAttackPayloadFeedback()
        {
            return GameText.Get(
                NoAttackPayloadFeedbackKey,
                "Transport has no weapons or attack-capable passengers.");
        }

        public static bool IsPlayerControlledMovableTransport(EntityManager em, Entity entity)
        {
            return entity != Entity.Null &&
                   em.Exists(entity) &&
                   em.HasComponent<Faction>(entity) &&
                   FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id) &&
                   em.HasComponent<UnitMove>(entity) &&
                   (em.HasBuffer<UnitTransportPassengerElement>(entity) ||
                    em.HasComponent<UnitTransportCapacity>(entity) ||
                    em.HasComponent<UnitTransportCargoCapacity>(entity));
        }

        public static bool HasAttackCapablePayload(EntityManager em, Entity transport)
        {
            return HasAttackCapablePayload(em, transport, 0);
        }

        public static bool IsAttackDeployPassenger(EntityManager em, Entity passenger)
        {
            return IsAttackCapableUnit(em, passenger) ||
                   HasAttackCapablePayload(em, passenger);
        }

        public static bool IsAttackCapableUnit(EntityManager em, Entity entity)
        {
            if (entity == Entity.Null ||
                !em.Exists(entity) ||
                !em.HasComponent<Faction>(entity) ||
                !FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id) ||
                !em.HasComponent<UnitMove>(entity) ||
                !em.HasComponent<UnitCombat>(entity) ||
                !em.HasComponent<UnitAttack>(entity) ||
                em.GetComponentData<UnitCombat>(entity).CanAttack == 0)
            {
                return false;
            }

            return !em.HasComponent<UnitHealth>(entity) ||
                   em.GetComponentData<UnitHealth>(entity).Current > 0;
        }

        private static bool HasAttackCapablePayload(EntityManager em, Entity transport, int depth)
        {
            if (depth >= MaxNestedTransportDepth ||
                transport == Entity.Null ||
                !em.Exists(transport) ||
                !em.HasBuffer<UnitTransportPassengerElement>(transport))
            {
                return false;
            }

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            for (int i = 0; i < passengers.Length; i++)
            {
                Entity passenger = passengers[i].Passenger;
                if (IsAttackCapableUnit(em, passenger) ||
                    HasAttackCapablePayload(em, passenger, depth + 1))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
