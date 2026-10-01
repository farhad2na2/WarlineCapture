using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes GroundedMissionId = CampaignMissionSequence.GroundedSignal;
        private static readonly FixedString64Bytes GroundedRelayRole = "role.hostile.relay";
        private static readonly FixedString64Bytes GroundedTerminalRole = "role.protected.terminal";
        private static readonly FixedString64Bytes GroundedApronAnchor = "anchor.ch04.m04.apron";
        private static readonly FixedString64Bytes GroundedHardwareAnchor = "anchor.ch04.m04.hardware";

        private bool TryAdvanceGroundedSignal(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime)
        {
            if (!runtime.MissionId.Equals(GroundedMissionId)) return false;
            var em = system.EntityManager;
            if (runtime.Outcome != MissionOutcomeKind.None || !em.HasComponent<CampaignMissionExtractionState>(root) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int index)) return true;
            ref var extractionDefinition = ref catalog.Blob.Value.Missions[index].Extraction;
            var extraction = em.GetComponentData<CampaignMissionExtractionState>(root);
            if (!extraction.SessionToken.Equals(runtime.SessionToken) || extraction.AttemptOrdinal != runtime.AttemptOrdinal ||
                extraction.SourceVersion != runtime.SourceVersion) return true;
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            if (facts.CommandSquadSpawned == 0) return true;
            if (!em.HasComponent<CampaignMissionGroundedSignalState>(root) ||
                em.GetComponentData<CampaignMissionGroundedSignalState>(root).Initialized == 0)
            {
                if (!SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) || !metadata.Blob.IsCreated) return true;
                InitializeGroundedSignal(em, root, in runtime, in extraction, ref metadata.Blob.Value);
            }
            var mission = em.GetComponentData<CampaignMissionGroundedSignalState>(root);
            if (!CampaignMissionGroundedSignalRuleUtility.Matches(in mission, in runtime)) return true;
            bool opening = em.HasComponent<CampaignMissionOpeningPresentationComponent>(root) &&
                em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).SessionToken.Equals(runtime.SessionToken) &&
                em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage >= 6;
            // Project the original roster through the canonical extraction health and transport tracker.
            // Its checkpoint hold is kept at zero until hardware custody is established.
            ProjectExtractionRoster(em, root, ref extraction, ref extractionDefinition, in runtime, ref facts,
                CampaignMissionGroundedSignalRuleUtility.CanExtract(in mission) ? SystemAPI.Time.DeltaTime : 0f);
            if (!CampaignMissionGroundedSignalRuleUtility.CanExtract(in mission))
            {
                extraction.SecureHoldMilliseconds = 0;
                extraction.DepartureCleared = 0;
                extraction.CarrierTransferComplete = 0;
                facts.ExtractionSecureMilliseconds = 0;
                facts.ExtractionDeparted = 0;
                facts.ExtractionPassengersDelivered = 0;
            }
            int unloaded = 0, atHardware = 0, inCarrier = 0;
            var specialists = em.GetBuffer<CampaignMissionGroundedSignalSpecialist>(root);
            for (int i = 0; i < specialists.Length; i++)
            {
                var member = specialists[i];
                if (!em.Exists(member.Entity) || !em.HasComponent<UnitHealth>(member.Entity))
                { mission.Failure = GroundedSignalFailure.SpecialistLost; continue; }
                var health = em.GetComponentData<UnitHealth>(member.Entity);
                if (health.Max > 0 && health.Current <= 0) mission.Failure = GroundedSignalFailure.SpecialistLost;
                Entity transport = em.HasComponent<UnitTransportPassenger>(member.Entity)
                    ? em.GetComponentData<UnitTransportPassenger>(member.Entity).Transport : Entity.Null;
                if (transport == extraction.Aircraft) member.SawInsertionPlane = 1;
                if (transport == extraction.Carrier && health.Current > 0) inCarrier++;
                bool grounded = transport == Entity.Null && !em.HasComponent<Disabled>(member.Entity) && health.Current > 0 && em.HasComponent<LocalTransform>(member.Entity);
                if (grounded)
                {
                    var position = em.GetComponentData<LocalTransform>(member.Entity).Position;
                    if (member.SawInsertionPlane != 0 && math.distancesq(position.xz, mission.ApronCenter.xz) <= mission.ApronRadius * mission.ApronRadius)
                        member.UnloadedAtApron = 1;
                    if (!em.HasComponent<UnitPathRequest>(member.Entity) && !em.HasComponent<UnitPathFollow>(member.Entity) &&
                        math.distancesq(position.xz, mission.HardwareCenter.xz) <= mission.HardwareRadius * mission.HardwareRadius) atHardware++;
                }
                unloaded += member.UnloadedAtApron;
                specialists[i] = member;
            }
            mission.SpecialistsAboardCarrier = (byte)inCarrier;
            if (mission.Inserted == 0 && em.Exists(extraction.Aircraft) && em.HasComponent<UnitHealth>(extraction.Aircraft))
            {
                var aircraftHealth = em.GetComponentData<UnitHealth>(extraction.Aircraft);
                if (aircraftHealth.Max > 0 && aircraftHealth.Current <= 0) mission.Failure = GroundedSignalFailure.AircraftLost;
            }
            if (mission.Inserted == 0 && !em.Exists(extraction.Aircraft)) mission.Failure = GroundedSignalFailure.AircraftLost;
            if (specialists.Length != 2) mission.Failure = GroundedSignalFailure.Integrity;
            if (unloaded == 2) mission.Inserted = 1;
            ObserveGroundedSiteHealth(em, mission.Relay, ref mission.RelayHealthInitialized, out bool relayLost);
            bool terminalLost = false;
            var protectedMembers = em.GetBuffer<CampaignMissionGroundedSignalProtectedMember>(root);
            for (int i = 0; i < protectedMembers.Length; i++)
            {
                var protectedMember = protectedMembers[i];
                ObserveGroundedSiteHealth(em, protectedMember.Entity, ref protectedMember.HealthInitialized, out bool lost);
                terminalLost |= lost;
                mission.TerminalHealthInitialized |= protectedMember.HealthInitialized;
                protectedMembers[i] = protectedMember;
            }
            if (mission.RelayHealthInitialized != 0 && relayLost) mission.RelayDisabled = 1;
            if (mission.TerminalHealthInitialized != 0 && terminalLost) mission.Failure = GroundedSignalFailure.TerminalLost;
            if (facts.CivilianLossCount > 0) mission.Failure = GroundedSignalFailure.SpecialistLost;
            if (facts.ExtractionCarrierLost != 0) mission.Failure = GroundedSignalFailure.CarrierLost;
            bool active = extraction.Ready != 0 && opening && runtime.Phase == MissionPhaseKind.Engage;
            if (active)
            {
                mission.ElapsedMilliseconds = SaturatingAddMilliseconds(mission.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
                if (mission.ElapsedMilliseconds >= extractionDefinition.DeadlineMilliseconds) mission.Failure = GroundedSignalFailure.Deadline;
                if (mission.HardwareRecovered == 0)
                {
                    bool contested = false;
                    var roster = em.GetBuffer<CampaignMissionExtractionMember>(root);
                    for (int i = 0; i < roster.Length; i++)
                        if (roster[i].Kind == 4 && roster[i].Dead == 0 && em.Exists(roster[i].Entity) && em.HasComponent<LocalTransform>(roster[i].Entity) &&
                            math.distancesq(em.GetComponentData<LocalTransform>(roster[i].Entity).Position.xz, mission.HardwareCenter.xz) <= mission.HardwareRadius * mission.HardwareRadius)
                            contested = true;
                    mission.RecoveryHoldMilliseconds = CampaignMissionGroundedSignalRuleUtility.AdvanceRecovery(mission.RecoveryHoldMilliseconds,
                        (int)math.round(SystemAPI.Time.DeltaTime * 1000f), mission.Inserted != 0 && mission.RelayDisabled != 0 && atHardware == 2 && !contested && mission.Failure == GroundedSignalFailure.None);
                    if (mission.RecoveryHoldMilliseconds >= CampaignMissionGroundedSignalRuleUtility.RecoveryMilliseconds) mission.HardwareRecovered = 1;
                }
            }
            facts.ElapsedMilliseconds = mission.ElapsedMilliseconds;
            facts.GroundedSignalInserted = mission.Inserted;
            facts.GroundedSignalRelayDisabled = mission.RelayDisabled;
            facts.GroundedSignalTerminalLost = mission.Failure == GroundedSignalFailure.TerminalLost ? (byte)1 : (byte)0;
            em.SetComponentData(root, mission); em.SetComponentData(root, extraction); em.SetComponentData(root, facts);
            if (CampaignMissionGroundedSignalRuleUtility.TryAdvance(in runtime, in facts, in mission, extraction.Ready != 0, opening, out var next))
                em.SetComponentData(root, next);
            return true;
        }

        private static void ObserveGroundedSiteHealth(EntityManager em, Entity entity, ref byte initialized, out bool lost)
        {
            lost = initialized != 0 && !em.Exists(entity);
            if (!em.Exists(entity) || !em.HasComponent<UnitHealth>(entity)) return;
            var health = em.GetComponentData<UnitHealth>(entity);
            if (health.Max > 0) { initialized = 1; lost = health.Current <= 0; }
        }

        private static void InitializeGroundedSignal(EntityManager em, Entity root, in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionExtractionState extraction, ref OperationMapBlob map)
        {
            var mission = new CampaignMissionGroundedSignalState { SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal,
                SourceVersion = runtime.SourceVersion, Initialized = 1 };
            bool hasApron = CampaignMissionSpawnSystem.TryFindAnchor(ref map, GroundedApronAnchor, out var apron);
            bool hasHardware = CampaignMissionSpawnSystem.TryFindAnchor(ref map, GroundedHardwareAnchor, out var hardware);
            if (!hasApron || !hasHardware) mission.Failure = GroundedSignalFailure.Integrity;
            mission.ApronCenter = apron.Position; mission.ApronRadius = math.max(1f, apron.Radius);
            mission.HardwareCenter = hardware.Position; mission.HardwareRadius = math.max(1f, hardware.Radius);
            if (!em.HasBuffer<CampaignMissionGroundedSignalSpecialist>(root)) em.AddBuffer<CampaignMissionGroundedSignalSpecialist>(root);
            if (!em.HasBuffer<CampaignMissionGroundedSignalProtectedMember>(root)) em.AddBuffer<CampaignMissionGroundedSignalProtectedMember>(root);
            var specialists = em.GetBuffer<CampaignMissionGroundedSignalSpecialist>(root); specialists.Clear();
            var protectedMembers = em.GetBuffer<CampaignMissionGroundedSignalProtectedMember>(root); protectedMembers.Clear();
            var roster = em.GetBuffer<CampaignMissionExtractionMember>(root);
            for (int i = 0; i < roster.Length; i++)
            {
                Entity entity = roster[i].Entity;
                if (roster[i].Kind == 1) specialists.Add(new CampaignMissionGroundedSignalSpecialist { Entity = entity });
                if (!em.Exists(entity) || !em.HasComponent<CampaignMissionUnitRoleComponent>(entity)) continue;
                var role = em.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
                if (role.MissionRoleId.Equals(GroundedRelayRole)) mission.Relay = entity;
                if (role.MissionRoleId.Equals(GroundedTerminalRole))
                {
                    mission.ProtectedTerminal = entity;
                    protectedMembers.Add(new CampaignMissionGroundedSignalProtectedMember { Entity = entity });
                    if (em.HasComponent<UnitMovementBehavior>(entity))
                    {
                        var behavior = em.GetComponentData<UnitMovementBehavior>(entity);
                        behavior.AllowIdleWander = 0;
                        em.SetComponentData(entity, behavior);
                    }
                }
            }
            if (specialists.Length != 2 || mission.Relay == Entity.Null || mission.ProtectedTerminal == Entity.Null ||
                !em.Exists(extraction.Aircraft) || !em.HasBuffer<UnitTransportPassengerElement>(extraction.Aircraft)) mission.Failure = GroundedSignalFailure.Integrity;
            if (mission.Failure == GroundedSignalFailure.None)
            {
                var passengers = em.GetBuffer<UnitTransportPassengerElement>(extraction.Aircraft);
                var passengerState = new UnitTransportPassengerStateSystem();
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                for (int i = 0; i < specialists.Length; i++)
                {
                    var specialist = specialists[i];
                    passengerState.BoardPassenger(em, ref ecb, passengers, specialist.Entity, extraction.Aircraft);
                    specialist.SawInsertionPlane = 1; specialists[i] = specialist;
                }
                ecb.Playback(em); ecb.Dispose();
            }
            QualifyGroundedRelay(em, mission.Relay);
            if (em.HasComponent<CampaignMissionGroundedSignalState>(root)) em.SetComponentData(root, mission);
            else em.AddComponentData(root, mission);
        }

        private static void QualifyGroundedRelay(EntityManager em, Entity relay)
        {
            if (!em.Exists(relay)) return;
            // The military relay uses the radar vehicle's presentation and health, but is installed equipment.
            // Apply this only to this mission's spawned instance; the shared radar prefab and armed guards are unchanged.
            if (em.HasComponent<UnitCombat>(relay))
            {
                var combat = em.GetComponentData<UnitCombat>(relay);
                combat.CanAttack = 0;
                combat.AutoEngage = 0;
                em.SetComponentData(relay, combat);
            }
            if (em.HasComponent<UnitMovementBehavior>(relay))
            {
                var movement = em.GetComponentData<UnitMovementBehavior>(relay);
                movement.AllowIdleWander = 0;
                em.SetComponentData(relay, movement);
            }
            if (!em.HasComponent<CampaignMissionStationaryUnitTag>(relay)) em.AddComponent<CampaignMissionStationaryUnitTag>(relay);
            if (em.HasComponent<EngageTarget>(relay)) em.RemoveComponent<EngageTarget>(relay);
            if (em.HasComponent<RecentAttacker>(relay)) em.RemoveComponent<RecentAttacker>(relay);
            if (em.HasComponent<UnitIdleWanderComponent>(relay)) em.RemoveComponent<UnitIdleWanderComponent>(relay);
            if (em.HasComponent<UnitPathRequest>(relay)) em.RemoveComponent<UnitPathRequest>(relay);
            if (em.HasComponent<UnitPathFollow>(relay)) em.RemoveComponent<UnitPathFollow>(relay);
        }
    }
}
