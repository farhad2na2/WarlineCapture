using Unity.Entities;

public sealed class SelectionRuntimeQuerySystem
{
    private World _queryWorld;
    private EntityQuery _selectedMoveQuery;
    private EntityQuery _gridConfigQuery;
    private EntityQuery _selectedTagQuery;

    public EntityQuery SelectedMoveQuery => _selectedMoveQuery;
    public EntityQuery GridConfigQuery => _gridConfigQuery;
    public EntityQuery SelectedTagQuery => _selectedTagQuery;

    public void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _selectedMoveQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
        _gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        _selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
    }
}
