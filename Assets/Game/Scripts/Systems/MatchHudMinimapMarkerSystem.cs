using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct MatchHudMinimapMarkerSystem : ISystem
    {
        private const int MaxMarkers = 1024;
        private const byte CollectPlayerMarkers = 1;
        private const byte CollectEnemyMarkers = 2;
        private const double MarkerRefreshIntervalSeconds = 0.2d;

        private Entity _markerBoundaryEntity;
        private double _nextMarkerRefreshTime;

        // RequireForUpdate intentionally omitted: this producer creates the marker boundary and clears stale markers when no sources remain.
        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            Entity markerBoundaryEntity = GetOrCreateMarkerBoundary(ref state, em);
            double now = SystemAPI.Time.ElapsedTime;
            if (now < _nextMarkerRefreshTime)
                return;

            _nextMarkerRefreshTime = now + MarkerRefreshIntervalSeconds;
            var markerScratch = new NativeList<MatchHudMinimapMarkerElement>(MaxMarkers, Allocator.TempJob);
            state.Dependency = new CollectMarkersJob
            {
                MaxMarkers = MaxMarkers,
                CollectMode = CollectPlayerMarkers,
                Markers = markerScratch
            }.Schedule(state.Dependency);
            state.Dependency = new CollectMarkersJob
            {
                MaxMarkers = MaxMarkers,
                CollectMode = CollectEnemyMarkers,
                Markers = markerScratch
            }.Schedule(state.Dependency);
            state.Dependency = new CollectScanIntelMarkersJob
            {
                MaxMarkers = MaxMarkers,
                Markers = markerScratch
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
        private partial struct CollectMarkersJob : IJobEntity
        {
            public int MaxMarkers;
            public byte CollectMode;
            public NativeList<MatchHudMinimapMarkerElement> Markers;

            private void Execute(in UnitHealth health, in LocalTransform transform, in Faction faction)
            {
                if (Markers.Length >= MaxMarkers ||
                    health.Current <= 0 ||
                    !ShouldCollectFaction(faction.Id))
                    return;

                Markers.Add(new MatchHudMinimapMarkerElement
                {
                    Position = transform.Position,
                    FactionId = faction.Id
                });
            }

            private bool ShouldCollectFaction(byte factionId)
            {
                return CollectMode switch
                {
                    CollectPlayerMarkers => factionId == FactionIdentity.PlayerFactionId,
                    CollectEnemyMarkers => factionId != FactionIdentity.NeutralFactionId &&
                                           factionId != FactionIdentity.PlayerFactionId,
                    _ => false
                };
            }
        }

        [BurstCompile]
        [WithAll(typeof(ScanIntelRevealedTag))]
        [WithNone(typeof(UnitHealth))]
        private partial struct CollectScanIntelMarkersJob : IJobEntity
        {
            public int MaxMarkers;
            public NativeList<MatchHudMinimapMarkerElement> Markers;

            private void Execute(in ScanIntelLastSeen lastSeen)
            {
                if (Markers.Length >= MaxMarkers ||
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
