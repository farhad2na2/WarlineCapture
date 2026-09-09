using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private bool TryAdvanceExtraction(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime)
        {
            if (!SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int index)) return false;
            ref var definition = ref catalog.Blob.Value.Missions[index];
            if (definition.Extraction.Enabled == 0) return false;
            if (runtime.Outcome != MissionOutcomeKind.None) return true;
            var em = system.EntityManager;
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            bool ready = false, openingComplete = false;
            if (em.HasComponent<CampaignMissionOpeningPresentationComponent>(root))
            {
                var opening = em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root);
                openingComplete = opening.SessionToken.Equals(runtime.SessionToken) && opening.Stage >= 6;
            }
            if (em.HasComponent<CampaignMissionExtractionState>(root))
            {
                var extraction = em.GetComponentData<CampaignMissionExtractionState>(root);
                if (!extraction.SessionToken.Equals(runtime.SessionToken) || extraction.AttemptOrdinal != runtime.AttemptOrdinal ||
                    extraction.SourceVersion != runtime.SourceVersion) return true;
                if (facts.CommandSquadSpawned != 0)
                    ProjectExtractionRoster(em, root, ref extraction, ref definition.Extraction, in runtime, ref facts, SystemAPI.Time.DeltaTime);
                ready = extraction.Ready != 0;
                if (ready && openingComplete && runtime.Phase == MissionPhaseKind.Engage)
                {
                    facts.ElapsedMilliseconds = SaturatingAddMilliseconds(facts.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
                    if (facts.ElapsedMilliseconds >= definition.Extraction.DeadlineMilliseconds) facts.ExtractionTimedOut = 1;
                }
                em.SetComponentData(root, extraction);
            }
            em.SetComponentData(root, facts);
            if (CampaignMissionExtractionRuleUtility.TryAdvance(in runtime, in facts, ready, openingComplete,
                    definition.Extraction.RequiredPassengers, out var next)) em.SetComponentData(root, next);
            return true;
        }

        internal static void ProjectExtractionRoster(EntityManager em, Entity root, ref CampaignMissionExtractionState state,
            ref CampaignMissionExtractionDefinitionBlob definition, in CampaignMissionRuntimeComponent runtime,
            ref CampaignMissionAttemptFactsComponent facts, float deltaTime)
        {
            var members = em.GetBuffer<CampaignMissionExtractionMember>(root);
            int initialized = 0, passengers = 0, lost = 0, aboard = 0, inAircraft = 0, rode = 0, escortAlive = 0, escortLost = 0, hostileDead = 0;
            bool carrierDead = false, aircraftDead = false, contested = false;
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                for (int prior = 0; prior < i; prior++)
                    if (members[prior].Entity == member.Entity) facts.HostileRosterIntegrityFault = 1;
                bool exists = em.Exists(member.Entity) && em.HasComponent<UnitHealth>(member.Entity);
                if (!exists && member.Dead == 0) { facts.HostileRosterIntegrityFault = 1; continue; }
                var health = exists ? em.GetComponentData<UnitHealth>(member.Entity) : default;
                if (health.Max > 0) member.HealthInitialized = 1;
                initialized += member.HealthInitialized;
                if (member.HealthInitialized != 0 && health.Current <= 0) member.Dead = 1;
                Entity transport = exists && em.HasComponent<UnitTransportPassenger>(member.Entity)
                    ? em.GetComponentData<UnitTransportPassenger>(member.Entity).Transport : Entity.Null;
                if (member.Kind == 1)
                {
                    passengers++; lost += member.Dead;
                    if (transport == state.Carrier && transport != Entity.Null && member.Dead == 0) member.RodeCarrier = 1;
                    rode += member.RodeCarrier;
                    if (member.Dead == 0 && (transport == state.Carrier || transport == state.Aircraft) && transport != Entity.Null) aboard++;
                    if (member.Dead == 0 && transport == state.Aircraft && transport != Entity.Null) inAircraft++;
                }
                else if (member.Kind == 0) { escortLost += member.Dead; escortAlive += member.Dead == 0 ? 1 : 0; }
                else if (member.Kind == 2) carrierDead = member.Dead != 0;
                else if (member.Kind == 3) aircraftDead = member.Dead != 0;
                else
                {
                    hostileDead += member.Dead;
                    if (member.Dead == 0 && em.HasComponent<LocalTransform>(member.Entity) &&
                        !em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity) &&
                        math.distancesq(em.GetComponentData<LocalTransform>(member.Entity).Position.xz, state.LandingCenter.xz) <= definition.LandingRadius * definition.LandingRadius)
                        contested = true;
                }
                members[i] = member;
            }
            if (initialized == members.Length && members.Length > 0) state.Ready = 1;
            if (passengers != definition.RequiredPassengers) facts.HostileRosterIntegrityFault = 1;
            facts.ExtractionPassengerTotal = passengers; facts.CivilianTotalCount = passengers; facts.CivilianLossCount = lost;
            facts.ExtractionPassengersAboard = aboard; facts.ExtractionCarrierLegCount = rode;
            facts.CommandSquadAlive = escortAlive > 0 ? (byte)1 : (byte)0; facts.SquadLossCount = escortLost;
            facts.HostileDefeatedCount = hostileDead;
            facts.ExtractionAircraftLost = aircraftDead ? (byte)1 : (byte)0;
            if (inAircraft == definition.RequiredPassengers && rode == definition.RequiredPassengers && lost == 0)
                state.CarrierTransferComplete = 1;
            if (carrierDead && state.CarrierTransferComplete == 0) facts.ExtractionCarrierLost = 1;
            facts.ExtractionContested = contested ? (byte)1 : (byte)0;
            if (runtime.Phase != MissionPhaseKind.Engage || state.Ready == 0 || aircraftDead || !em.HasComponent<LocalTransform>(state.Aircraft)) return;
            var position = em.GetComponentData<LocalTransform>(state.Aircraft).Position;
            bool atLanding = math.distancesq(position.xz, state.LandingCenter.xz) <= definition.LandingRadius * definition.LandingRadius;
            if (state.DepartureCleared == 0)
            {
                bool canHold = atLanding && !contested && inAircraft == definition.RequiredPassengers && rode == definition.RequiredPassengers && lost == 0;
                state.SecureHoldMilliseconds = canHold ? SaturatingAddMilliseconds(state.SecureHoldMilliseconds, deltaTime) : 0;
                if (state.SecureHoldMilliseconds >= definition.SecureHoldMilliseconds) state.DepartureCleared = 1;
            }
            facts.ExtractionSecureMilliseconds = state.SecureHoldMilliseconds;
            bool airborne = em.HasComponent<UnitAirComponent>(state.Aircraft) && em.GetComponentData<UnitAirComponent>(state.Aircraft).Airborne != 0;
            bool atDeparture = math.distancesq(position.xz, state.DepartureCenter.xz) <= definition.DepartureRadius * definition.DepartureRadius;
            if (state.DepartureCleared != 0 && atDeparture && airborne && inAircraft == definition.RequiredPassengers && lost == 0)
            { facts.ExtractionPassengersDelivered = inAircraft; facts.ExtractionDeparted = 1; }
        }
    }
}
