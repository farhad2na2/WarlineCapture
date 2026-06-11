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

        foreach (var (health, transform, faction) in
                 SystemAPI.Query<RefRO<UnitHealth>, RefRO<LocalTransform>, RefRO<Faction>>())
        {
            if (health.ValueRO.Current <= 0)
                continue;

            markers.Add(new MatchHudMinimapMarkerElement
            {
                Position = transform.ValueRO.Position,
                FactionId = faction.ValueRO.Id
            });

            if (markers.Length >= MaxMarkers)
                break;
        }
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
}
