using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MatchHudMinimapMarkerSystem : ISystem
{
    private const int MaxMarkers = 1024;
    private const byte CollectPlayerMarkers = 1;
    private const byte CollectEnemyMarkers = 2;

    private Entity _markerBoundaryEntity;

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        Entity markerBoundaryEntity = GetOrCreateMarkerBoundary(ref state, em);
        DynamicBuffer<MatchHudMinimapMarkerElement> markers =
            em.GetBuffer<MatchHudMinimapMarkerElement>(markerBoundaryEntity);
        markers.Clear();
        if (markers.Capacity < MaxMarkers)
            markers.Capacity = MaxMarkers;

        var markerScratch = new NativeList<MatchHudMinimapMarkerElement>(MaxMarkers, Allocator.TempJob);
        new CollectMarkersJob
        {
            MaxMarkers = MaxMarkers,
            CollectMode = CollectPlayerMarkers,
            Markers = markerScratch
        }.Run();
        new CollectMarkersJob
        {
            MaxMarkers = MaxMarkers,
            CollectMode = CollectEnemyMarkers,
            Markers = markerScratch
        }.Run();

        for (int i = 0; i < markerScratch.Length; i++)
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
    private partial struct CollectMarkersJob : IJobEntity
    {
        public int MaxMarkers;
        public byte CollectMode;
        public NativeList<MatchHudMinimapMarkerElement> Markers;

        private void Execute(in UnitHealth health, in LocalTransform transform, in Faction faction)
        {
            if (health.Current <= 0 || Markers.Length >= MaxMarkers || !ShouldCollectFaction(faction.Id))
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
}
