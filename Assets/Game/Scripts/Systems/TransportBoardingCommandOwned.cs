using Game.Components;
using Game.Tactical.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct TransportBoardingCommandSystem
    {
        public static bool TryIssueOwnedDisembark(EntityManager em, Entity session, byte actingFaction,
            Entity transport, UnitTransportCapacitySystem capacitySystem, UnitMoveOrderSystem moveSystem,
            EntityQuery gridPathingQuery, out TacticalCommandReasonCode reason)
        {
            reason = TacticalCommandReasonCode.InvalidTransport;
            if (!SkirmishActorCommandAuthority.IsActiveOwnedActor(em, session, actingFaction, transport) ||
                !em.HasBuffer<UnitTransportPassengerElement>(transport)) return false;
            var passengers = em.GetBuffer<UnitTransportPassengerElement>(transport, true);
            for (int i = 0; i < passengers.Length; i++)
            {
                Entity passenger = passengers[i].Passenger;
                if (!SkirmishActorCommandAuthority.IsActiveOwnedActor(em, session, actingFaction, passenger, true) ||
                    !em.HasComponent<UnitTransportPassenger>(passenger) ||
                    em.GetComponentData<UnitTransportPassenger>(passenger).Transport != transport)
                { reason = TacticalCommandReasonCode.InvalidPassenger; return false; }
            }
            // Use the same ordinary Unload branch as the player command. A requested
            // drop cell would turn a landed cargo-plane unload into an airdrop.
            DisembarkResult result = TryDisembarkTransport(em, transport, capacitySystem, moveSystem,
                gridPathingQuery, default, hasRequestedDropCell: 0);
            if (result.Accepted) CancelDeployAttackForManualDisembark(em, transport);
            reason = result.ReasonCode;
            return result.Accepted;
        }
    }
}
