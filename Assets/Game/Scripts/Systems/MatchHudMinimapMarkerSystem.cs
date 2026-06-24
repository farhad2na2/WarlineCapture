using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MatchHudMinimapMarkerSystem : ISystem
{
    private const int MaxMarkers = 1024;
    private const byte CollectPlayerMarkers = 1;
    private const byte CollectEnemyMarkers = 2;

    private Entity _markerBoundaryEntity;

    // RequireForUpdate intentionally omitted: this producer creates the marker boundary and clears stale markers when no sources remain.
    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        Entity markerBoundaryEntity = GetOrCreateMarkerBoundary(ref state, em);
        DynamicBuffer<MatchHudMinimapMarkerElement> markers =
            em.GetBuffer<MatchHudMinimapMarkerElement>(markerBoundaryEntity);
        markers.Clear();
        if (markers.Capacity < MaxMarkers)
            markers.Capacity = MaxMarkers;

        var markerScratch = new NativeList<MatchHudMinimapMarkerElement>(MaxMarkers * 2, Allocator.TempJob);
        var markerWriter = markerScratch.AsParallelWriter();
        state.Dependency = new CollectMarkersJob
        {
            MaxMarkers = MaxMarkers,
            CollectMode = CollectPlayerMarkers,
            Markers = markerWriter
        }.ScheduleParallel(state.Dependency);
        state.Dependency = new CollectMarkersJob
        {
            MaxMarkers = MaxMarkers,
            CollectMode = CollectEnemyMarkers,
            Markers = markerWriter
        }.ScheduleParallel(state.Dependency);
        state.Dependency = new CollectScanIntelMarkersJob
        {
            MaxMarkers = MaxMarkers,
            Markers = markerWriter
        }.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        int copyCount = math.min(markerScratch.Length, MaxMarkers);
        for (int i = 0; i < copyCount; i++)
            markers.Add(markerScratch[i]);

        markerScratch.Dispose();
    }

    private Entity GetOrCreateMarkerBoundary(ref SystemState state, EntityManager em)
    {
        if (_markerBoundaryEntity != Entity.Null &&
            em.Exists(_markerBoundaryEntity) &&
            em.HasComponent<MatchHudMinimapMarkerBoundary>(_markerBoundaryEntity) &&
            em.HasBuffer<MatchHudMinimapMarkerElement>(_markerBoundaryEntity))
        {
            return _markerBoundaryEntity;
        }

        _markerBoundaryEntity = em.CreateEntity(typeof(MatchHudMinimapMarkerBoundary));
        em.AddBuffer<MatchHudMinimapMarkerElement>(_markerBoundaryEntity);
        em.SetName(_markerBoundaryEntity, "MatchHudMinimapMarkers");
        return _markerBoundaryEntity;
    }

    [BurstCompile]
    [WithChangeFilter(typeof(LocalTransform))]
    private partial struct CollectMarkersJob : IJobEntity
    {
        public int MaxMarkers;
        public byte CollectMode;
        public NativeList<MatchHudMinimapMarkerElement>.ParallelWriter Markers;

        private void Execute(in UnitHealth health, in LocalTransform transform, in Faction faction)
        {
            if (health.Current <= 0 || !ShouldCollectFaction(faction.Id))
                return;

            Markers.AddNoResize(new MatchHudMinimapMarkerElement
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
        public NativeList<MatchHudMinimapMarkerElement>.ParallelWriter Markers;

        private void Execute(in ScanIntelLastSeen lastSeen)
        {
            if (!FactionIdentity.IsHostileToPlayer(lastSeen.FactionId))
                return;

            Markers.AddNoResize(new MatchHudMinimapMarkerElement
            {
                Position = lastSeen.Position,
                FactionId = lastSeen.FactionId
            });
        }
    }
}
