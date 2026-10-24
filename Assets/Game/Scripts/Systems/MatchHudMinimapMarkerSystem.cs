using Unity.Burst;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct MatchHudMinimapMarkerSystem : ISystem
    {
        private const int MaxMarkers = Game.Simulation.AriaPublicPlayerMarkerPolicy.MaxMarkers;
        private const byte CollectPlayerMarkers = 1;
        private const byte CollectEnemyMarkers = 2;
        private const double MarkerRefreshIntervalSeconds = Game.Simulation.AriaPublicPlayerMarkerPolicy.RefreshIntervalSeconds;

        private Entity _markerBoundaryEntity;
        private double _nextMarkerRefreshTime;

        // RequireForUpdate intentionally omitted: this producer creates the marker boundary and clears stale markers when no sources remain.
        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            Entity markerBoundaryEntity = GetOrCreateMarkerBoundary(ref state, em);
            double now = SystemAPI.Time.ElapsedTime;
            if (now < _nextMarkerRefreshTime)
            {
                state.Dependency = new PruneDeadMarkersJob
                {
                    Health = SystemAPI.GetComponentLookup<UnitHealth>(true),
                    FogOwned = SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true),
                    FogSight = SystemAPI.GetComponentLookup<SkirmishContactSightComponent>(true),
                    FogKnowledge = SystemAPI.GetBufferLookup<SkirmishContactKnowledgeElement>(true),
                    Now = (float)now,
                    Markers = em.GetBuffer<MatchHudMinimapMarkerElement>(markerBoundaryEntity)
                }.Schedule(state.Dependency);
                return;
            }

            _nextMarkerRefreshTime = now + MarkerRefreshIntervalSeconds;
            var markerScratch = new NativeList<MatchHudMinimapMarkerElement>(MaxMarkers, Allocator.TempJob);
            var represented = new NativeParallelHashSet<ContactKey>(MaxMarkers, Allocator.TempJob);
            ComponentLookup<CampaignMissionCombatSuppressedTag> combatSuppressionLookup =
                SystemAPI.GetComponentLookup<CampaignMissionCombatSuppressedTag>(true);
            bool defenseIntel = SystemAPI.TryGetSingleton(out ThreatWarningLedgerState ledger) &&
                                SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent mission) &&
                                ThreatWarningResolveSystem.Matches(in ledger, in mission);
            bool missionRoster=defenseIntel || SystemAPI.TryGetSingleton(out CampaignMissionExtractionState extraction) && extraction.Initialized!=0 &&
                SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent extractionMission) && extraction.SessionToken.Equals(extractionMission.SessionToken) && extraction.AttemptOrdinal==extractionMission.AttemptOrdinal;
            var produced=new NativeParallelHashSet<Entity>(MaxMarkers,Allocator.TempJob);
            if(defenseIntel)
                foreach(var units in SystemAPI.Query<DynamicBuffer<BuildingProducedUnitReadModel>>())
                    for(int i=0;i<units.Length && produced.Count()<MaxMarkers;i++)
                        if(units[i].HasOwnerFaction!=0 && units[i].OwnerFactionId==FactionIdentity.PlayerFactionId) produced.Add(units[i].Unit);
            state.Dependency = new CollectMarkersJob
            {
                MaxMarkers = MaxMarkers,
                CollectMode = CollectPlayerMarkers,
                DefenseRoster = missionRoster,
                Represented = represented,
                ProducedUnits = produced,
                RuntimeBuildingLookup = SystemAPI.GetComponentLookup<RuntimeBuildingCombatInfo>(true),
                MapBuildingLookup = SystemAPI.GetComponentLookup<OperationMapBuildingComponent>(true),
                MissionRoleLookup = SystemAPI.GetComponentLookup<CampaignMissionUnitRoleComponent>(true),
                LastSeenLookup = SystemAPI.GetComponentLookup<ScanIntelLastSeen>(true),
                CombatSuppressionLookup = combatSuppressionLookup,
                FogOwned = SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true),
                FogDisabled = SystemAPI.GetComponentLookup<Disabled>(true),
                FogPassenger = SystemAPI.GetComponentLookup<UnitTransportPassenger>(true),
                FogPassengerOwner = SystemAPI.GetComponentLookup<SupportPassengerOwnerComponent>(true),
                FogSight = SystemAPI.GetComponentLookup<SkirmishContactSightComponent>(true),
                FogKnowledge = SystemAPI.GetBufferLookup<SkirmishContactKnowledgeElement>(true),
                Now = (float)now,
                Markers = markerScratch
            }.Schedule(state.Dependency);
            state.Dependency = new CollectMarkersJob
            {
                MaxMarkers = MaxMarkers,
                CollectMode = CollectEnemyMarkers,
                DefenseRoster = missionRoster,
                Represented = represented,
                ProducedUnits = produced,
                RuntimeBuildingLookup = SystemAPI.GetComponentLookup<RuntimeBuildingCombatInfo>(true),
                MapBuildingLookup = SystemAPI.GetComponentLookup<OperationMapBuildingComponent>(true),
                MissionRoleLookup = SystemAPI.GetComponentLookup<CampaignMissionUnitRoleComponent>(true),
                RequireObservedIntel = defenseIntel,
                LastSeenLookup = SystemAPI.GetComponentLookup<ScanIntelLastSeen>(true),
                CombatSuppressionLookup = combatSuppressionLookup,
                FogOwned = SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true),
                FogDisabled = SystemAPI.GetComponentLookup<Disabled>(true),
                FogPassenger = SystemAPI.GetComponentLookup<UnitTransportPassenger>(true),
                FogPassengerOwner = SystemAPI.GetComponentLookup<SupportPassengerOwnerComponent>(true),
                FogSight = SystemAPI.GetComponentLookup<SkirmishContactSightComponent>(true),
                FogKnowledge = SystemAPI.GetBufferLookup<SkirmishContactKnowledgeElement>(true),
                Now = (float)now,
                Markers = markerScratch
            }.Schedule(state.Dependency);
            state.Dependency = new CollectScanIntelMarkersJob
            {
                MaxMarkers = MaxMarkers,
                FogOwned = SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true),
                Markers = markerScratch
            }.Schedule(state.Dependency);
            state.Dependency = new CollectFrozenLedgerMarkersJob
            {
                MaxMarkers = MaxMarkers, Now = (float)now,
                Represented = represented, Markers = markerScratch
            }.Schedule(state.Dependency);
            DynamicBuffer<MatchHudMinimapMarkerElement> markers =
                em.GetBuffer<MatchHudMinimapMarkerElement>(markerBoundaryEntity);
            if (markers.Capacity < MaxMarkers)
                markers.Capacity = MaxMarkers;
            state.Dependency = new PublishMarkersJob
            {
                MaxMarkers = MaxMarkers,
                Source = markerScratch,
                Destination = markers
            }.Schedule(state.Dependency);
            state.Dependency = markerScratch.Dispose(state.Dependency);
            state.Dependency = produced.Dispose(state.Dependency);
            state.Dependency = represented.Dispose(state.Dependency);
        }

        [BurstCompile]
        private struct PruneDeadMarkersJob : IJob
        {
            [ReadOnly] public ComponentLookup<UnitHealth> Health;
            [ReadOnly] public ComponentLookup<SkirmishAttemptOwnedComponent> FogOwned;
            [ReadOnly] public ComponentLookup<SkirmishContactSightComponent> FogSight;
            [ReadOnly] public BufferLookup<SkirmishContactKnowledgeElement> FogKnowledge;
            public float Now;
            public DynamicBuffer<MatchHudMinimapMarkerElement> Markers;
            public void Execute()
            {
                for (int i = Markers.Length - 1; i >= 0; i--)
                {
                    Entity source = Markers[i].SourceEntity;
                    if (source != Entity.Null && FogOwned.HasComponent(source) &&
                        !SkirmishFogConsumerUtility.IsVisible(FogOwned, FogSight, FogKnowledge, source, 1))
                    {
                        if (SkirmishFogConsumerUtility.TryKnowledge(FogKnowledge, source, 1, out var record) &&
                            record.Sight == SkirmishContactSight.LastSeen && record.LastSeenHealth > 0)
                        {
                            var marker = Markers[i]; marker.SourceEntity = Entity.Null;
                            marker.Position = record.LastSeenPosition; marker.IsLastSeen = 1;
                            marker.AgeSeconds = math.max(0, Now - record.LastObservedTimeSeconds);
                            Markers[i] = marker;
                        }
                        else Markers.RemoveAt(i);
                        continue;
                    }
                    if (source != Entity.Null && (!Health.HasComponent(source) || Health[source].Current <= 0))
                        Markers.RemoveAt(i);
                }
            }
        }

        [BurstCompile]
        private struct PublishMarkersJob : IJob
        {
            public int MaxMarkers;
            [ReadOnly] public NativeList<MatchHudMinimapMarkerElement> Source;
            public DynamicBuffer<MatchHudMinimapMarkerElement> Destination;

            public void Execute()
            {
                Destination.Clear();
                int copyCount = math.min(Source.Length, MaxMarkers);
                for (int i = 0; i < copyCount; i++)
                    Destination.Add(Source[i]);
            }
        }

        private Entity GetOrCreateMarkerBoundary(ref SystemState state, EntityManager em)
        {
            if (_markerBoundaryEntity != Entity.Null &&
                em.Exists(_markerBoundaryEntity) &&
                em.HasComponent<MatchHudMinimapMarkerStateComponent>(_markerBoundaryEntity) &&
                em.HasBuffer<MatchHudMinimapMarkerElement>(_markerBoundaryEntity))
            {
                return _markerBoundaryEntity;
            }

            _markerBoundaryEntity = em.CreateEntity(typeof(MatchHudMinimapMarkerStateComponent));
            em.AddBuffer<MatchHudMinimapMarkerElement>(_markerBoundaryEntity);
            em.SetName(_markerBoundaryEntity, "MatchHudMinimapMarkers");
            return _markerBoundaryEntity;
        }

        [BurstCompile]
        [WithOptions(EntityQueryOptions.IncludeDisabledEntities)]
        private partial struct CollectMarkersJob : IJobEntity
        {
            public int MaxMarkers;
            public byte CollectMode;
            public bool RequireObservedIntel;
            public bool DefenseRoster;
            public NativeParallelHashSet<ContactKey> Represented;
            [ReadOnly] public NativeParallelHashSet<Entity> ProducedUnits;
            [ReadOnly] public ComponentLookup<RuntimeBuildingCombatInfo> RuntimeBuildingLookup;
            [ReadOnly] public ComponentLookup<OperationMapBuildingComponent> MapBuildingLookup;
            [ReadOnly] public ComponentLookup<CampaignMissionUnitRoleComponent> MissionRoleLookup;
            [ReadOnly] public ComponentLookup<ScanIntelLastSeen> LastSeenLookup;
            [ReadOnly] public ComponentLookup<CampaignMissionCombatSuppressedTag> CombatSuppressionLookup;
            [ReadOnly] public ComponentLookup<SkirmishAttemptOwnedComponent> FogOwned;
            [ReadOnly] public ComponentLookup<Disabled> FogDisabled;
            [ReadOnly] public ComponentLookup<UnitTransportPassenger> FogPassenger;
            [ReadOnly] public ComponentLookup<SupportPassengerOwnerComponent> FogPassengerOwner;
            [ReadOnly] public ComponentLookup<SkirmishContactSightComponent> FogSight;
            [ReadOnly] public BufferLookup<SkirmishContactKnowledgeElement> FogKnowledge;
            public float Now;
            public NativeList<MatchHudMinimapMarkerElement> Markers;

            private void Execute(
                Entity entity,
                in UnitHealth health,
                in LocalTransform transform,
                in Faction faction)
            {
                if (CollectMode == CollectPlayerMarkers && !DefenseRoster && !RequireObservedIntel)
                {
                    bool passengerOwner = FogPassengerOwner.HasComponent(entity);
                    if (!Game.Simulation.AriaPublicPlayerMarkerPolicy.Include(Markers.Length, MaxMarkers, faction.Id,
                        FogOwned.HasComponent(entity), FogDisabled.HasComponent(entity), FogPassenger.HasComponent(entity),
                        passengerOwner, passengerOwner && FogPassengerOwner[entity].Landed != 0, health.Current)) return;
                    if (FogOwned.HasComponent(entity))
                        Represented.Add(new ContactKey { Session = FogOwned[entity].SessionId, Contact = FogOwned[entity].StableObjectId });
                    Markers.Add(new MatchHudMinimapMarkerElement { SourceEntity = entity, Position = transform.Position, FactionId = faction.Id });
                    return;
                }
                if (Markers.Length >= MaxMarkers ||
                    (CollectMode == CollectEnemyMarkers && CombatSuppressionLookup.HasComponent(entity)) ||
                    !ShouldCollectFaction(faction.Id))
                    return;
                if (!FogOwned.HasComponent(entity) && FogDisabled.HasComponent(entity)) return;
                if (FogOwned.HasComponent(entity) && faction.Id == FactionIdentity.PlayerFactionId &&
                    (FogPassenger.HasComponent(entity) || FogPassengerOwner.HasComponent(entity) &&
                     FogPassengerOwner[entity].Landed == 0)) return;
                if (FogOwned.HasComponent(entity) && faction.Id != FactionIdentity.PlayerFactionId &&
                    !SkirmishFogConsumerUtility.IsVisible(FogOwned, FogSight, FogKnowledge, entity, 1))
                {
                    if (SkirmishFogConsumerUtility.TryKnowledge(FogKnowledge, entity, 1, out var record) &&
                        record.Sight == SkirmishContactSight.LastSeen && record.LastSeenHealth > 0)
                    {
                        Represented.Add(new ContactKey { Session = FogOwned[entity].SessionId,
                            Contact = FogOwned[entity].StableObjectId });
                        Markers.Add(new MatchHudMinimapMarkerElement
                        {
                            SourceEntity = Entity.Null, Position = record.LastSeenPosition,
                            FactionId = faction.Id, IsLastSeen = 1,
                            AgeSeconds = math.max(0, Now - record.LastObservedTimeSeconds)
                        });
                    }
                    return;
                }
                if (health.Current <= 0 || FogDisabled.HasComponent(entity)) return;
                if (RequireObservedIntel && !LastSeenLookup.HasComponent(entity)) return;
                if (faction.Id==FactionIdentity.NeutralFactionId && !MissionRoleLookup.HasComponent(entity)) return;
                if (DefenseRoster && MapBuildingLookup.HasComponent(entity) && !MissionRoleLookup.HasComponent(entity)) return;
                if (DefenseRoster && CollectMode==CollectPlayerMarkers && !MissionRoleLookup.HasComponent(entity) &&
                    !RuntimeBuildingLookup.HasComponent(entity) && !ProducedUnits.Contains(entity)) return;

                if (FogOwned.HasComponent(entity))
                    Represented.Add(new ContactKey { Session = FogOwned[entity].SessionId,
                        Contact = FogOwned[entity].StableObjectId });
                Markers.Add(new MatchHudMinimapMarkerElement
                {
                    SourceEntity = entity,
                    Position = RequireObservedIntel ? LastSeenLookup[entity].Position : transform.Position,
                    FactionId = faction.Id
                });
            }

            private bool ShouldCollectFaction(byte factionId)
            {
                return CollectMode switch
                {
                    CollectPlayerMarkers => factionId == FactionIdentity.PlayerFactionId || DefenseRoster && factionId == FactionIdentity.NeutralFactionId,
                    CollectEnemyMarkers => factionId != FactionIdentity.NeutralFactionId &&
                                           factionId != FactionIdentity.PlayerFactionId,
                    _ => false
                };
            }
        }

        private struct ContactKey : IEquatable<ContactKey>
        {
            public FixedString64Bytes Session;
            public FixedString64Bytes Contact;
            public bool Equals(ContactKey other) => Session.Equals(other.Session) && Contact.Equals(other.Contact);
            public override int GetHashCode() => (Session.GetHashCode() * 397) ^ Contact.GetHashCode();
        }

        [BurstCompile]
        private partial struct CollectFrozenLedgerMarkersJob : IJobEntity
        {
            public int MaxMarkers;
            public float Now;
            public NativeParallelHashSet<ContactKey> Represented;
            public NativeList<MatchHudMinimapMarkerElement> Markers;
            private void Execute(in SkirmishExpandedSessionComponent session, in SkirmishFogStateComponent fog,
                in DynamicBuffer<SkirmishContactLedgerElement> ledger, in SkirmishContactLedgerStateComponent ledgerState)
            {
                if (session.IsLegacy != 0 || !session.SessionId.Equals(ledgerState.SessionId)) return;
                for (int i = 0; i < ledger.Length && Markers.Length < MaxMarkers; i++)
                {
                    var contact = ledger[i];
                    if (contact.ObserverFactionId != 1 || contact.ContactFactionId == 1 ||
                        contact.Sight != SkirmishContactSight.LastSeen || contact.LastSeenHealth <= 0) continue;
                    float age = math.max(0, Now - contact.LastObservedTimeSeconds);
                    if (age >= fog.LastSeenExpireSeconds || !Represented.Add(new ContactKey {
                        Session = session.SessionId, Contact = contact.ContactStableObjectId })) continue;
                    Markers.Add(new MatchHudMinimapMarkerElement {
                        SourceEntity = Entity.Null, Position = contact.LastSeenPosition,
                        FactionId = contact.ContactFactionId, IsLastSeen = 1, AgeSeconds = age });
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(ScanIntelRevealedTag))]
        [WithNone(typeof(UnitHealth))]
        private partial struct CollectScanIntelMarkersJob : IJobEntity
        {
            public int MaxMarkers;
            public NativeList<MatchHudMinimapMarkerElement> Markers;

            [ReadOnly] public ComponentLookup<SkirmishAttemptOwnedComponent> FogOwned;
            private void Execute(Entity entity, in ScanIntelLastSeen lastSeen)
            {
                if (FogOwned.HasComponent(entity) || Markers.Length >= MaxMarkers ||
                    !FactionIdentity.IsHostileToPlayer(lastSeen.FactionId))
                    return;

                Markers.Add(new MatchHudMinimapMarkerElement
                {
                    Position = lastSeen.Position,
                    FactionId = lastSeen.FactionId
                });
            }
        }
    }
}
