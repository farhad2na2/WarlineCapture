using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MatchHudMinimapMarkerSystem : ISystem
{
    private const int MaxMarkers = 256;

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
        public NativeList<MatchHudMinimapMarkerElement> Markers;

        private void Execute(in UnitHealth health, in LocalTransform transform, in Faction faction)
        {
            if (health.Current <= 0 || Markers.Length >= MaxMarkers)
                return;

            Markers.Add(new MatchHudMinimapMarkerElement
            {
                Position = transform.Position,
                FactionId = faction.Id
            });
        }
    }
}
